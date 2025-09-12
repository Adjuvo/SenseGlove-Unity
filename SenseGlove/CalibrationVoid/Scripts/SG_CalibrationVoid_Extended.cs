using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
 * Version 2.0 of the initial SenseGlove Calibration Void. Runs the user through a series of steps with instructions, including the calibration of wrist tracking.
 * Makes the resulting calibration much better compared to the basic "open and close your hands" - but takes more time.
 * 
 * @author
 * amber@senseglove.com
 * max@senseglove.com
 */


namespace SG.Calibration
{
    //----------------------------------------------------------------------------------------------------------------------------
    // CalibrationSteps

    public enum CalibrationSteps
    {
        /// <summary> Undefined. Only occurs during errors. </summary>
        Unknown,
        /// <summary> Initialization completed, awaiting the push of a button to get you started </summary>
        AwaitingStart,
        /// <summary> Calibraiton has begun and we're now running through 1-2 hands' calibration. </summary>
        CalibratingFingers,
        /// <summary> Calibration is completed. Will return back to AwaitingStart if you allow it. </summary>
        Completed
    }


    /// <summary> Controls the order of finger tracking. </summary>
    /// <remarks> Should keep calibration in memory for each state </remarks>
    public enum FingerCalibrationOrder
    {
        /// <summary> Used to calibrate finger flexion (1) and thumb extension (0). Stay Stable for X amount of time... </summary>
        Thumbsup,
        /// <summary> Used to calibrate thumb flexion (1). Must be far enough away from thumb extension. Must be stable X amount of time. </summary>
        ThumbBelowRingfinger,
        /// <summary> Calibrates thumb abduction (1). Move move X amount of times instead of being stable!  </summary>
        ThumbAbduction,
        /// <summary> Calibrates finger extension (0) and thumb adduction (0) </summary>
        HandsTogether,

        /// <summary> Utility component do assign arrays by index or to check for the amount of steps completed. </summary>
        All
    }


    /// <summary> Controls UI and instructions around calibration logic for SenseGlove Haptic Gloves. </summary>
    public class SG_CalibrationVoid_Extended : MonoBehaviour
    {

        //----------------------------------------------------------------------------------------------------------------------------
        // Components

        /// <summary> If true, calibration is immedeately started when this component's Start() function is called. 
        /// If not, it waits for a confirmation from somewhere inside the Scene. </summary>
        [Header("Calibration Parameters")]
        [SerializeField] private bool calibrateOnStartup = false;

        /// <summary> If true, the Calibration UI and its components are hidden until StartCalibration() is called. Useful when you're dropping this inside a scene </summary>
        [SerializeField] private bool uiHiddenUntilStart = false;

        /// <summary> IF true, the user's hands are invisible during calibration. Useful for Passthrough, or to avoid confusion. </summary>
        [SerializeField] private bool hideHandsDuringCalibration = false;


        /// <summary> If true, when calibration is insufficient, you'll have to redo it for a maximum of X times.  </summary>
        [SerializeField] private bool repeatStepsOnFailedCalibration = true;


        /// <summary> If true, we add an extra step for you to calibrate your wrist offsets during the "HandsTogether" stage of finger calibration
        /// This is not possible with one hand. </summary>
        [SerializeField] private bool calibrateWristOffsets = false;

        /// <summary> The wearer's wrist and/or tracking objects must be this close together to calibrate the wrist tracking. To avoid grotesque miscalculations. </summary>
        [SerializeField] private float minimumWristCalibrationDistance = 0.30f;

        /// <summary> If true, we return to "Awaiting Start" and hide the UI again if that was indended. </summary>
        [SerializeField] private bool resetSelfAfterCompletion = true;

        /// <summary> When calibration completes, there's a small delay to show instructions, transition to a next scene, etc </summary>
        [SerializeField] private float calibrationEndDelay = 2.0f;

        /// <summary> Time max. time we spend per step </summary>
        [SerializeField] private float timePerStep = 8.0f;

        /// <summary> The amount of time it takes to consider a hand position 'stable' </summary>
        [SerializeField] private float secondsHoldStable = 0.8f;

        /// <summary> A short cooldown after confirming, before going to the next stap. </summary>
        [SerializeField] private float secondsBetweenSteps = 0.5f;




        /// <summary> SenseGlove User;  to access the left- and right hand model(s). </summary>
        [Header("Calibration Components")]
        [SerializeField] private SG_User user;

        /// <summary> The main Instructions to show to the user. </summary>
        [SerializeField] private TextMesh mainInstructionsElement;
        /// <summary> Shows the timer for the current step. </summary>
        [SerializeField] private Text timerTextElement;

        /// <summary> Instruction elements for the left- and right hand. </summary>
        [SerializeField] private SG_CalibrationVisuals leftInstructions, rightInstructions;

        /// <summary> Optional component: A sound to play to confirm the current calibration step </summary>
        [SerializeField] private AudioSource confirmStepSound;

        /// <summary> Haptic cue to confirm the current calibration step </summary>
        [SerializeField] private SG_CustomWaveform confirmStepVibration;

        /// <summary> A series of UI Elements that can be hidden or enabled. </summary>
        [SerializeField] private GameObject[] uiElements = new GameObject[0];


        /// <summary> Fires when the Calibration is started </summary>
        [Header("Calibration Events")]
        public UnityEngine.Events.UnityEvent CalibrationStarted;

        /// <summary> Fires when the calibrationvoid is completed. </summary>
        public UnityEngine.Events.UnityEvent CalibrationCompleted;

        //RESET?

        // Private components

        /// <summary> Controls the global calibration steps. </summary>
        private CalibrationSteps calibrationStep = CalibrationSteps.Unknown;

        /// <summary> Contains component and states for left- and right hand calibration(s). </summary>
        private SG_GloveCalibrationData leftHandCalibration, rightHandCalibration;

        /// <summary> The current Finger Calibration state. </summary>
        private FingerCalibrationOrder fingerState = FingerCalibrationOrder.All;

        private int maxCalibrationAttempts = 2;
        private float gloveStatusCheckTime = 1.0f;

        private Coroutine fingerCalibrationRoutine = null;

        private Coroutine matchGloveVisuals = null;
        private bool updateGloveStatesEnabled = true; //to temproarity disable matching glove states for 'hands together' step with one hand.

        //----------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> General Instruction Test for user(s) </summary>
        public string MainInstructions
        {
            get { return mainInstructionsElement != null ? mainInstructionsElement.text : ""; }
            set
            {
                if (mainInstructionsElement != null)
                    mainInstructionsElement.text = value;
            }
        }

        public bool CalibrationRunning
        {
            get { return calibrationStep == CalibrationSteps.CalibratingFingers; }
        }

        public bool StateTimerVisible
        {
            get { return timerTextElement != null ? timerTextElement.gameObject.activeSelf : false; }
            set { if (timerTextElement != null) { timerTextElement.gameObject.SetActive(value); } }
        }



        //----------------------------------------------------------------------------------------------------------------------------
        // Init / Setup

        /// <summary> (Attempt to) collect necessary components from within the current scene, outside of the prefab. 
        /// Expecially important when instantiating this prefab. </summary>
        private void CollectComponents()
        {
            if (user == null)
                user = GameObject.FindObjectOfType<SG_User>();
        }


        /// <summary> Assuming all relevant components have been detected (or not), get everything ready to run a full calibration. But do not start it yet! </summary>
        private void InitializeCalibration()
        {
            SG_Core.Setup(); //Initializes the SenseGlove Hand Tracking Component(s) that enable SenseCom

            //TODO: Create calibration example elements and link these as well!

            leftHandCalibration = new SG_GloveCalibrationData(false);
            rightHandCalibration = new SG_GloveCalibrationData(true);

            SetUiElementsEnabled(true); //give them the time to call their Start() functions
                                        //   SetInstructionModelsEnabled(false);

            GoToState(CalibrationSteps.AwaitingStart);
        }


        /// <summary> (Re)Creates the appropriate calibration data containers based on the gloves that are connected. </summary>
        private SG_GloveCalibrationData CreateCalibrationAsset(bool rightHand)
        {
            if (SGCore.HapticGlove.GetGlove(rightHand, out SGCore.HapticGlove glove))
            {
                if (glove is SGCore.Nova.Nova2Glove)
                    return new SG_Nova2CalibrationData((SGCore.Nova.Nova2Glove)glove);
                else if (glove is SGCore.Nova.NovaGlove)
                    return new SG_Nova1CalibrationData((SGCore.Nova.NovaGlove)glove);
            }
            return new SG_GloveCalibrationData(rightHand);
        }


        /// <summary> Show / hide the UI Elements. </summary>
        /// <param name="enable"></param>
        public void SetUiElementsEnabled(bool enable)
        {
            foreach (GameObject obj in uiElements)
                obj.SetActive(enable);
        }

        public void SetHandModelsVisible(bool visible)
        {
            if (user == null)
                return;
            if (user.leftHand != null)
                user.leftHand.HandModelEnabled = visible;
            if (user.rightHand != null)
                user.rightHand.HandModelEnabled = visible;
        }

        public void UpdateRemainingTime(float timeInSeconds)
        {
            if (timerTextElement == null)
                return;

            timeInSeconds = Mathf.Max(0, timeInSeconds); //ensure it's more than 0?
            //we floor to the nearest integer
            int wholeSeconds = Mathf.CeilToInt(timeInSeconds);
            timerTextElement.text = wholeSeconds.ToString();
        }

        public void PlayComfirmationFeedback()
        {
            if (confirmStepVibration != null && user != null)
            {
                if (user.leftHand != null)
                    user.leftHand.SendCustomWaveform(confirmStepVibration, confirmStepVibration.intendedMotor);
                if (user.rightHand != null)
                    user.rightHand.SendCustomWaveform(confirmStepVibration, confirmStepVibration.intendedMotor);
            }
            if (confirmStepSound != null)
                confirmStepSound.Play();
        }

        //----------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Start the calibration sequence, assuming one isn't already running. </summary>
        /// <param name="forceReset">If true, the calibration will be completely reset if one is already running. </param>
        public void StartCalibration(bool forceReset)
        {
            if (CalibrationRunning)
            {
                if (!forceReset)
                {
                    Debug.LogWarning("Calibration is already running!", this);
                    return;
                }
            }
            Debug.Log("Staring calibration void");
            CalibrationStarted?.Invoke();
            GoToState(CalibrationSteps.CalibratingFingers);
        }

        //TODO: Cancel Calibration


        private void TryCalibrateWrists()
        {
            if (calibrateWristOffsets && user != null
                && user.leftHand != null && user.rightHand != null) //we're relying on the SG_User's TrackedHands for calibration. For now.
            {
                if (!user.leftHand.IsConnected() || !user.rightHand.IsConnected())
                {
                    Debug.Log("Only one glove connected, so wrist calibration is skipped!");
                    return;
                }

                Vector3 rightPosOutput, leftPosOutput;
                Quaternion rightRotOutput, leftRotOutput;

                Transform rightWrist = user.rightHand.GetTransform(SG_TrackedHand.TrackingLevel.RealHandPose, HandJoint.Wrist);
                Transform leftWrist = user.leftHand.GetTransform(SG_TrackedHand.TrackingLevel.RealHandPose, HandJoint.Wrist);

                if ((rightWrist.position - leftWrist.position).magnitude > minimumWristCalibrationDistance)
                {
                    Debug.LogError("Wrists are too far away. Skipped calibration!");
                    return;
                }
                CalibrateWristOffset(rightWrist, leftWrist, out rightPosOutput, out leftPosOutput, out rightRotOutput, out leftRotOutput);
                user.leftHand.CalculateWristCorrection(leftPosOutput, leftRotOutput);
                user.rightHand.CalculateWristCorrection(rightPosOutput, rightRotOutput);
                Debug.Log("Locked in Wrist Calibration!");
            }
        }

        /// <summary> Check every so often </summary>
        /// <returns></returns>
        private IEnumerator MatchGloveVisuals()
        {
            SetInstructionModelsEnabled(false);
            do
            {
                yield return new WaitForSeconds(gloveStatusCheckTime);
                if (updateGloveStatesEnabled)
                    UpdateExampleHandStates();
            }
            while (this.enabled);
        }


        //----------------------------------------------------------------------------------------------------------------------------
        // Wrist Calibration

        public static Vector3 rotOffsetModelHandsTogether = new Vector3(0, 0, -11f); // additional tilt to get the hands aligned for hands together in the hand model.
        public static Vector3 rotOffsetLeftToRightHandCoordinates = new Vector3(180f, 0, 0);
        public static Vector3 posOffsetModelHandsTogether = new Vector3(0, 0.031f, 0);

        /// <summary> </summary>
        /// <param name="rightHand">The right hand transform of SG_HapticGlove gameobject</param>
        /// <param name="leftHand"> The left hand transform of SG_HapticGlove gameobject</param>
        /// <param name="rightPosOutput">New aligned worldposition of the right hand taking into account the offsets of the handmodels</param>
        /// <param name="leftPosOutput"> New aligned worldposition of the left  hand taking into account the offsets of the handmodels</param>
        /// <param name="rightRotOutput">New aligned worldrotation of the right hand taking into account the offsets of the handmodels</param>
        /// <param name="leftRotOutput"> New aligned worldrotation of the left  hand taking into account the offsets of the handmodels</param>
        public static void CalibrateWristOffset(Transform rightHand, Transform leftHand, out Vector3 rightPosOutput, out Vector3 leftPosOutput, out Quaternion rightRotOutput, out Quaternion leftRotOutput)
        {
            AlignObjectsWithOffset(rightHand, leftHand, out rightPosOutput, out leftPosOutput, out rightRotOutput, out leftRotOutput);
        }

        // from two objects that are misaligned, align them with both rotation and position so for example two hands palms are together, allowing for extra offsets to tune this match.
        // all is in worldspace
        public static void AlignObjectsWithOffset(Transform obj1Input, Transform obj2Input, out Vector3 obj1PosOutput, out Vector3 obj2PosOutput, out Quaternion obj1RotOutput, out Quaternion obj2RotOutput)
        {
            Quaternion originalRot1 = obj1Input.localRotation;
            Quaternion originalRot2 = obj2Input.localRotation;

            Quaternion handModelOffset = Quaternion.Euler(rotOffsetLeftToRightHandCoordinates + rotOffsetModelHandsTogether); //transforms include 180 degrees for flip left/right hand axis orientation

            obj1RotOutput = Quaternion.Slerp(originalRot1, originalRot2 * handModelOffset, 0.5f);
            obj2RotOutput = Quaternion.Slerp(originalRot1 * handModelOffset, originalRot2, 0.5f);

            Vector3 positionBetween = Vector3.Lerp(obj1Input.position, obj2Input.position, 0.5f);
            obj1PosOutput = positionBetween + obj1RotOutput * posOffsetModelHandsTogether;
            obj2PosOutput = positionBetween + obj2RotOutput * posOffsetModelHandsTogether;

        }



        //----------------------------------------------------------------------------------------------------------------------------
        // Moving between states

        private void GoToState(CalibrationSteps newState)
        {
            //TODO: Decide to show or hide the content
            //TODO: Disable animated hands
            calibrationStep = newState;
            StateTimerVisible = false; //hide this by default.
            SetProgressBarsEnabled(false);

            if (fingerCalibrationRoutine != null)
            {
                StopCoroutine(fingerCalibrationRoutine);
                fingerCalibrationRoutine = null;
            }

            switch (calibrationStep)
            {
                case CalibrationSteps.AwaitingStart:

                    MainInstructions = "Press Button to Start Hand calibration";
                    if (uiHiddenUntilStart)
                        SetUiElementsEnabled(false);

                    //TODO: Start a coroutine to update the hands based on TrackedHand state?
                    UpdateExampleHandStates();
                    updateGloveStatesEnabled = true;
                    break;

                case CalibrationSteps.CalibratingFingers:

                    MainInstructions = "Follow the hand instructions";
                    SetUiElementsEnabled(true);
                    updateGloveStatesEnabled = true;

                    if (hideHandsDuringCalibration)
                        SetHandModelsVisible(false);

                    PlayComfirmationFeedback();

                    //Create the appropriate calibration steps (and, if necessary; skip calibration entirely).
                    leftHandCalibration = CreateCalibrationAsset(false);
                    leftHandCalibration.InitializeCalibration();

                    rightHandCalibration = CreateCalibrationAsset(true);
                    rightHandCalibration.InitializeCalibration();


                    if (rightHandCalibration.CalibrationRequired() || leftHandCalibration.CalibrationRequired())
                    {
                        fingerCalibrationRoutine = StartCoroutine(RunFingerCalibration());
                    }
                    else
                    {
                        Debug.LogError("Neither of these gloves require calibration. Skipping to the end!  //TODO: Include wrist calibration if desired!");
                        GoToState(CalibrationSteps.Completed);
                    }
                    break;

                case CalibrationSteps.Completed:
                    MainInstructions = "Calibration Complete!";


                    updateGloveStatesEnabled = false;
                    SetInstructionModelsEnabled(false); //turn these off.
                    leftHandCalibration.LockInCalibration();
                    rightHandCalibration.LockInCalibration();
                    SetHandModelsVisible(true); //unhide the hand models
                    StartCoroutine(EndResetCalibrationVoid(calibrationEndDelay));
                    break;

                default:
                    MainInstructions = "Unhandled State. Something might have happened during setup...";
                    break;
            }



        }


        private IEnumerator RunFingerCalibration()
        {
            fingerState = (FingerCalibrationOrder)0; //casting to the first one.
            int calibrationAttempts = 0;


            yield return null;


            do // this while loop means we keep going until we have all the states done.
            {
                //These reset for every state
                float stateTimeLimit = timePerStep; //time in seconds to complete the time. TODO: Make this longer for the first state?
                float stateTimer = 0.0f; //how long we've been in this state for
                //the first one is always a bit longer (but never shorter than secondsHoldStable or longer than 1.5 seconds) 
                float stableTimeLimit = secondsHoldStable;
                float stableTimer = 0.0f; //how long we've been stabel for
                bool stateStable = false;

                UpdateRemainingTime(stateTimeLimit);
                StateTimerVisible = true;
                //Update hand visuals!
                SetInstructionModelsEnabled(true);
                PlayInstructionsFor(fingerState);

                //Debug.Log("Entering " + fingerState + " for " + stateTimeLimit + "s or when we're stable for " + stableTimeLimit);
                if (fingerState == FingerCalibrationOrder.HandsTogether)
                {
                    //TODO: Enable both Hand models despite one of them not being on!
                    SetInstructionModelsEnabled(true);
                    updateGloveStatesEnabled = false; //disable them being updated based on glove state.
                    do
                    {
                        yield return null;
                        float dT = Time.deltaTime;
                        stateTimer += dT;
                        UpdateRemainingTime(stateTimeLimit - stateTimer);

                        //    Debug.Log("Assess Hands together");
                        leftHandCalibration.UpdateFingerData(dT, fingerState);
                        rightHandCalibration.UpdateFingerData(dT, fingerState);

                        //TODO: Assess Wrist Tracking stability.
                        bool fingersStable = leftHandCalibration.IsStateStable(fingerState, true) && rightHandCalibration.IsStateStable(fingerState, true);

                        //TODO: Is this a VR Application?
                        bool wristStable = true; //to debug

                        //TODO: Only check for wrist stability (and calibration) if we're running a VR Application!?
                        if (wristStable && fingersStable)
                        {
                            stableTimer += dT;
                            SetProgressBarsEnabled(true);
                            SetProgressBarValue(stableTimer / stableTimeLimit);
                        }
                        else
                        {
                            SetProgressBarsEnabled(false);
                            SetProgressBarValue(0.0f);
                            stableTimer = 0;
                        }
                    }
                    while (stateTimer < stateTimeLimit && stableTimer < stableTimeLimit && this.enabled);

                    //Exit hands together state
                    //TODO: Don't do this with only one glove and/or in Desktop mode (headset at 0,0,0)
                    updateGloveStatesEnabled = true; //and turn it back to normal.
                    UpdateExampleHandStates();
                    TryCalibrateWrists();
                }
                else if (fingerState == FingerCalibrationOrder.ThumbAbduction) //Special case where one does not need to be stable, but needs to move X amount of times!
                {
                    int currMotions = 0;
                    float motionSteps = 1.0f / (float)SG_GloveCalibrationData.RequiredAbdMotions;
                    do
                    {
                        yield return null;
                        float dT = Time.deltaTime;
                        stateTimer += dT;
                        UpdateRemainingTime(stateTimeLimit - stateTimer);

                        leftHandCalibration.UpdateFingerData(dT, fingerState);
                        rightHandCalibration.UpdateFingerData(dT, fingerState);

                        currMotions = Mathf.Min(leftHandCalibration.GetAbductionMotions(), rightHandCalibration.GetAbductionMotions());
                        SetProgressBarsEnabled(true);
                        SetProgressBarValue(motionSteps * currMotions);
                    }
                    while (stateTimer < stateTimeLimit && currMotions < SG_GloveCalibrationData.RequiredAbdMotions && this.enabled);
                    //exit thumb abduction
                }
                else
                {
                    do
                    {
                        yield return null;
                        float dT = Time.deltaTime;
                        stateTimer += dT;
                        UpdateRemainingTime(stateTimeLimit - stateTimer);

                        //TODO: Make things move as opposed to stable-only...?
                        leftHandCalibration.UpdateFingerData(dT, fingerState);
                        rightHandCalibration.UpdateFingerData(dT, fingerState);
                        stateStable = leftHandCalibration.IsStateStable(fingerState, true) && rightHandCalibration.IsStateStable(fingerState, true);
                        if (stateStable)
                        {
                            stableTimer += dT;
                            SetProgressBarsEnabled(true);
                            SetProgressBarValue(stableTimer / stableTimeLimit);
                        }
                        else
                        {
                            stableTimer = 0;
                            SetProgressBarsEnabled(false);
                            SetProgressBarValue(0.0f);
                        }
                    }
                    while (stateTimer < stateTimeLimit && stableTimer < stableTimeLimit && this.enabled);
                    //Exit finger tracking states.
                }
                //Debug.Log("Completed " + fingerState + "! Waiting " + secondsBetweenSteps + "s for the next one...");
                StateTimerVisible = false;

                leftHandCalibration.StoreFingerData(fingerState);
                rightHandCalibration.StoreFingerData(fingerState);
                SetProgressBarsEnabled(false);

                fingerState++; //go to the next fingerState
                PlayComfirmationFeedback();
                yield return new WaitForSeconds(secondsBetweenSteps);

                //If this placed us to the "ALL" step - please assess the validity of our calibration. If it's not sufficient, then go again.
                if (fingerState == FingerCalibrationOrder.All)
                {
                    calibrationAttempts++;
                    if (calibrationAttempts < maxCalibrationAttempts && !(leftHandCalibration.CalibrationValid() && rightHandCalibration.CalibrationValid()))
                    {
                        if (repeatStepsOnFailedCalibration) //but only if we want you to!
                        {
                            leftHandCalibration.ResetForNextAttempt();
                            rightHandCalibration.ResetForNextAttempt();
                            fingerState = (FingerCalibrationOrder)0; //reset back to 0 which will let me go here.
                        }
                    }
                }
            }
            while (fingerState != FingerCalibrationOrder.All && this.enabled);

            Debug.Log("Exited all of the calibration steps " + fingerState.ToString());

            GoToState(CalibrationSteps.Completed);
        }

        private IEnumerator EndResetCalibrationVoid(float time)
        {
            if (time > 0.01f)
                yield return new WaitForSeconds(time);

            CalibrationCompleted?.Invoke(); // call the event!
            Debug.Log("CalibrationComplete has been invoked!");

            if (resetSelfAfterCompletion)
                GoToState(CalibrationSteps.AwaitingStart);
        }


        //----------------------------------------------------------------------------------------------------------------------------
        // UI / Feedback

        /// <summary> Enable / Disable the progress bar(s) </summary>
        /// <param name="enabled"></param>
        public void SetProgressBarsEnabled(bool enabled)
        {
            if (leftInstructions != null)
                leftInstructions.ProgressBarEnabled = enabled;
            if (rightInstructions != null)
                rightInstructions.ProgressBarEnabled = enabled;
        }

        /// <summary> Set the progress bar value to a set number. </summary>
        /// <param name="value01"></param>
        public void SetProgressBarValue(float value01)
        {
            if (leftInstructions != null)
                leftInstructions.SetProgressBarValue(value01);
            if (rightInstructions != null)
                rightInstructions.SetProgressBarValue(value01);
        }

        public void SetInstructionModelsEnabled(bool enabled)
        {
            if (leftInstructions != null)
                leftInstructions.HandModelEnabled = enabled && user != null && user.leftHand != null && user.leftHand.IsConnected();
            if (rightInstructions != null)
                rightInstructions.HandModelEnabled = enabled && user != null && user.rightHand != null && user.rightHand.IsConnected();
        }

        public void PlayInstructionsFor(FingerCalibrationOrder fingerState)
        {
            if (leftInstructions != null)
                leftInstructions.PlayVisualsFor(fingerState);
            if (rightInstructions != null)
                rightInstructions.PlayVisualsFor(fingerState);
        }


        private void UpdateExampleHandStates()
        {
            if (user == null)
                return;

            if (leftInstructions != null && user.leftHand != null)
                leftInstructions.HandModelEnabled = user.leftHand.IsConnected();
            if (rightInstructions != null && user.rightHand != null)
                rightInstructions.HandModelEnabled = user.rightHand.IsConnected();
        }


        //----------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void Start()
        {
            CollectComponents(); //ensures we have a user if one is present
            InitializeCalibration();
            if (calibrateOnStartup)
                StartCalibration(true);
        }

        private void Update() //temp. Remove this.
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //Pretend I've pushed the button
                StartCalibration(false);
            }
        }

        private void OnEnable()
        {
            matchGloveVisuals = StartCoroutine(MatchGloveVisuals());
        }

        private void OnDisable()
        {
            //Redundant, but I'm doing it anyway because I'm paranoid.
            if (matchGloveVisuals != null)
            {
                StopCoroutine(matchGloveVisuals);
                matchGloveVisuals = null;
            }
        }

#if UNITY_EDITOR
        void Reset()
        {
            CollectComponents();
        }
#endif

    }
}