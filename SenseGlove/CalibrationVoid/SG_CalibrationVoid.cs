using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
 * Main logic for the Calibration Void, which resets calibration when required, then wait for up to two hands to change their CalibrationState into "Calibration Locked"
 * Afterwards, it automatically takes you to a next scene if one is specified.
 */

namespace SG
{


    public class SG_CalibrationVoid : MonoBehaviour
    {
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Void Stage Enum

        public enum VoidStage
        {
            /// <summary> Windows only - check whether the SenseCom process is running, and wait until it does...  </summary>
            StartupSenseCom,

            /// <summary> Wait until you've got the first glove. </summary>
            WaitingForFirstGlove,

            /// <summary> 1-2 gloves are calibration </summary>
            GlovesCalibrating,

            /// <summary>  1-2 gloves are done calibrating. Go to the next phase. </summary>
            Done,

            /// <summary> When something goes wrong </summary>
            NoGloves,
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Individual Calibration

        /// <summary> Keeps track of calibration state(s) for one glove. </summary>
        public class IndividualCalibration
        {
            public SG_TrackedHand TrackedHand { get; set; }

            public SG_HapticGlove Glove { get; set; }

            public SG_CalibrationSequence CalibrationHelper { get; set; }

            public SG_HandAnimator ExampleHand { get; set; }

            public bool HasReset { get; private set; } //if this glove has been reset during the Calibration Void yet. Should it reconnect, it will reset itself.

            protected float openClose_freq = 1.0f;

            public IndividualCalibration(SG_TrackedHand hand, SG_HandAnimator example, float openCloseTime)
            {
                TrackedHand = hand;
                CalibrationHelper = hand != null ? hand.calibration : null;
                Glove = hand != null && hand.deviceSelector != null ? hand.deviceSelector.GetDevice<SG_HapticGlove>() : null;

                openClose_freq = 1 / openCloseTime;

                ExampleHand = example;
                if (example != null)
                {
                    example.enabled = false; //it's not allowed to update itself :L

                    Transform exHand = example.transform.parent != null ? example.transform.parent : example.transform;
                    Transform exParent = hand.handModel != null ? hand.handModel.wristTransform : hand.GetPoser(SG_TrackedHand.TrackingLevel.RenderPose).GetTransform(HandJoint.Wrist);
                    Vector3 currScale = exHand.localScale;
                    exHand.localScale = new Vector3(currScale.x * 0.99f, currScale.y * 0.99f, currScale.z * 0.99f); //set the hand to 99% if its starting scale
                    exHand.parent = exParent;
                    exHand.localRotation = Quaternion.identity;
                    exHand.localPosition = Vector3.zero;
                }
                this.SetHandExample(false);
                if (Glove != null)
                {
                    Glove.DeviceConnected.AddListener(OnGloveConnected);
                    Glove.DeviceDisconnected.AddListener(OnGloveDisconnected);
                    Glove.CalibrationStateChanged.AddListener(OnCalibrationStateChanged);
                    Debug.Log("Subscribing...");
                }
                else
                {
                    Debug.LogError("NULL GLOVE!");
                }
            }

            ~IndividualCalibration()
            {
                UnsubscribeEvents();
            }

            private void OnCalibrationStateChanged()
            {
                if (Glove == null && Glove.InternalGlove != null)
                {
                    this.SetHandExample(false);
                }
                else
                {
                    SGCore.HG_CalibrationState state = Glove.InternalGlove.GetCalibrationState();
                    this.SetHandExample(state == SGCore.HG_CalibrationState.MoveFingers || state == SGCore.HG_CalibrationState.AllSensorsMoved);
                    if (state == SGCore.HG_CalibrationState.AllSensorsMoved)
                    {
                        ToThumbsUp();
                    }
                }
            }

            public void UnsubscribeEvents()
            {
                if (Glove != null)
                {
                    //Debug.Log("Unsubscribing...");
                    Glove.DeviceConnected.RemoveListener(OnGloveConnected);
                    Glove.DeviceDisconnected.RemoveListener(OnGloveDisconnected);
                    Glove.CalibrationStateChanged.RemoveListener(OnCalibrationStateChanged);
                }
            }

            private void ToThumbsUp()
            {
                //Set example to a nice enough thumbs up.
                SGCore.Kinematics.BasicHandModel handModel = this.TrackedHand.GetHandModel();
                float[] flexions = new float[5] { 0.0f, 1.0f, 1.0f, 1.0f, 1.0f };
                float abd = 0.3f;
                SGCore.Kinematics.Vect3D[][] handAngles = SGCore.Kinematics.Anatomy.HandAngles_FromNormalized(handModel.IsRight, flexions, abd, 0.0f);
                SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel);
                SG_HandPose confirmPose = new SG_HandPose(iPose);
                this.ExampleHand.UpdateHand(confirmPose, true);
            }

            private void OnGloveConnected()
            {
                OnCalibrationStateChanged();
            }

            private void OnGloveDisconnected()
            {
                SetHandExample(false);
            }

            public void UpdateExample()
            {
                if (this.ExampleHand == null && Glove != null && Glove.InternalGlove != null && Glove.InternalGlove.GetCalibrationState() != SGCore.HG_CalibrationState.MoveFingers)
                    return;
                //only needed when it's not in 

                float animationEval = 0.5f + SG.Util.SG_Util.GetSine(openClose_freq, 0.5f, Time.timeSinceLevelLoad/* + (openCloseTime * 0.75f)*/);
                float[] allFlex = new float[5] { animationEval, animationEval, animationEval, animationEval, animationEval }; //all finger flexion is equal
                float abd = 0.6f;

                SGCore.Kinematics.BasicHandModel handModel = this.TrackedHand.GetHandModel();
                SGCore.Kinematics.Vect3D[][] handAngles = SGCore.Kinematics.Anatomy.HandAngles_FromNormalized(handModel.IsRight, allFlex, abd, 0.0f);
                SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel);

                SG_HandPose newPose = new SG_HandPose(iPose);
                this.ExampleHand.UpdateHand(newPose, true);

            }

            public bool IsConnected()
            {
                return Glove.IsConnected();
            }

            public bool RequiresCalibration()
            {
                return Glove != null && Glove.DeviceType != SGCore.DeviceType.SENSEGLOVE && Glove.DeviceType != SGCore.DeviceType.BETADEVICE;
            }

            /// <summary> Call this whenever you need to reset calibration: When entering the  </summary>
            public void ResetCalibration(bool forceReset)
            {
                if (HasReset && !forceReset)
                    return;

                if (this.RequiresCalibration() && this.Glove != null && this.Glove.InternalGlove != null)
                {
                    Debug.Log("Reset Calibration of the " + (this.Glove.TracksRightHand() ? "right hand" : "left hand"));
                    this.Glove.InternalGlove.ResetCalibration();
                    HasReset = true; //let the device know 
                }
            }

            public SGCore.HG_CalibrationState GetCalibrationState()
            {
                if (Glove == null)
                    return SGCore.HG_CalibrationState.CalibrationLocked; //done

                if (Glove.IsConnected())
                {
                    if (RequiresCalibration())
                    {
                        return Glove.InternalGlove.GetCalibrationState();
                    }
                    return SGCore.HG_CalibrationState.CalibrationLocked;
                }
                return SGCore.HG_CalibrationState.Unknown;
            }

            public void SetHandExample(bool enabled)
            {
                if (ExampleHand != null && ExampleHand.handModelInfo != null)
                {
                    bool nowActive = ExampleHand.handModelInfo.gameObject.activeSelf;
                    if (nowActive != enabled)
                    {
                        ExampleHand.handModelInfo.gameObject.SetActive(enabled);
                    }
                }
            }



        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Void Properties

        [Header("Calibration Components")]
        [SerializeField] private SG_User user;

        /// <summary> The main Instructions to show to the user. </summary>
		[SerializeField] private TextMesh mainInstructions;

        /// <summary> Example Handmodels that will be used to show the user how to move their hand. </summary>
		[SerializeField] private SG_HandAnimator exampleHandLeft, exampleHandRight;

        /// <summary> (Optional) Sphere to reset the calibration with. </summary>
        [SerializeField] private SG_ConfirmZone resetSphere;

        [SerializeField] private VoidStage currentStage = VoidStage.WaitingForFirstGlove;

        /// <summary> While the main reason to this scene is to calibrate gloves, if we don't find any I'll transition you after this amount.  </summary>
        [Header("Control Parameters Components")]
        [SerializeField] private float noGloveTimeout = 30f;
        /// <summary> The amount of time were we'll show a 'welcome' message, before one is requested to move their fingers. </summary>
        [SerializeField] private float introTime = 1.5f;

        /// <summary> How Long it takes for the example hand to open and close again </summary>
        [SerializeField] private float exampleOpenCloseTime = 2.0f;



        /// <summary> Once completed, the scene will automatically change after this much time has finished. </summary>
		[Header("Completion Settings")]
        [SerializeField] private float changeSceneAfter = 1.5f;

        /// <summary> If the index > -1, we will change to this Scene after Calibration completes, unless a specific name is provided. </summary>
        [SerializeField] private int goToSceneIndex = 1;
        /// <summary> If it's not empty, we will change to this scene after calibration compltes. </summary>
        [SerializeField] private string goToSceneName = "";

        /// <summary> Fires when any state here changes... </summary>
        public UnityEvent CalibrationStateChanged = new UnityEvent();
        /// <summary> This Event fires when the Calibration is completed, but before the next scene is loaded. </summary>
        public UnityEvent CalibrationCompleted = new UnityEvent();

        /// <summary> Calibration algorithm for each glove. </summary>
        protected IndividualCalibration leftHandCal = null, rightHandCal = null; //start at NULL so I know they are not (yet) assigned.

        /// <summary> When true, this boolean 'unlocks' the while loop that blocks the next scene from opening until calibration completes </summary>
        protected bool openNextScene = false;
        /// <summary> The actual Scene Index to load. </summary>
        protected int loadSceneIndex = -1;

        protected float timer_currStage = 0.0f;

        /// <summary> Whether or not we've evaluated that SenseCom is running... </summary>
        protected bool sensecomRunning = false;

        protected bool showIntroduction = true;
        protected SGCore.HG_CalibrationState lastOverallState = SGCore.HG_CalibrationState.Unknown;


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Access the main instructions text </summary>
		public string MainInstr
        {
            get { return instructions; }
            set
            {
                instructions = value;
                if (mainInstructions != null)
                {
                    mainInstructions.text = value;
                }
            }
        }

        private string instructions = "";

        /// <summary> Returns true if this script -can- transition into your next scene. If false, no next scene transition happens. We'll just wait for the user to confirm. </summary>
        public bool HasSceneTransition
        {
            get { return this.loadSceneIndex > -1; }
        }

        /// <summary> Retrieve the current calibrationvoid stage </summary>
        public VoidStage CurrentStage { get { return this.currentStage; } }

        /// <summary> Will set the next Scene by it's build Index, and starts a routine to load it in the background if it is valid. </summary>
        /// <remarks> The intended </remarks>
        public void SetNextSceneByIndex(int buildIndex)
        {
            this.goToSceneIndex = buildIndex;
        }

        /// <summary> Will set the next Scene by it's name, and starts a routine to load it in the background if it is valid. </summary>
        public void SetNextSceneByName(string name)
        {
            this.goToSceneName = name;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utility Functions

        public static SGCore.HG_CalibrationState GetEarliestState(SGCore.HG_CalibrationState d1State, bool d1Connected, SGCore.HG_CalibrationState d2State, bool d2Connected)
        {
            if (d1Connected || d2Connected)
            {
                SGCore.HG_CalibrationState s1 = d1Connected ? d1State : SGCore.HG_CalibrationState.CalibrationLocked;
                SGCore.HG_CalibrationState s2 = d2Connected ? d2State : SGCore.HG_CalibrationState.CalibrationLocked;
                return (SGCore.HG_CalibrationState)(Mathf.Min((int)s1, (int)s2));
            }
            return SGCore.HG_CalibrationState.Unknown;
        }

        public void DestroyExamples()
        {
            if (exampleHandLeft != null) { GameObject.Destroy(exampleHandLeft.transform.parent.gameObject); }
            if (exampleHandRight != null) { GameObject.Destroy(exampleHandRight.transform.parent.gameObject); }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Calibration Void Functions

        public void CollectComponents()
        {
            if (user == null)
            {
                user = GameObject.FindObjectOfType<SG_User>();
            }



            if (user != null)
            {
                leftHandCal = new IndividualCalibration(user.leftHand, exampleHandLeft, exampleOpenCloseTime);
                leftHandCal.SetHandExample(false);
                rightHandCal = new IndividualCalibration(user.rightHand, exampleHandRight, exampleOpenCloseTime);
                rightHandCal.SetHandExample(false);
            }
        }




        public void GoToStage(VoidStage nextStage, bool forceLoad = false)
        {
            if (nextStage == this.currentStage && !forceLoad)
                return;

            Debug.Log("Calibration Void Moving to " + nextStage.ToString());

            timer_currStage = 0.0f; //reset the timer.
            this.currentStage = nextStage;

            string instr = "";
            switch (nextStage)
            {

                case VoidStage.GlovesCalibrating:

                    if (leftHandCal != null)
                    {
                        leftHandCal.ResetCalibration(true);
                    }
                    if (rightHandCal != null)
                    {
                        rightHandCal.ResetCalibration(true);
                    }

                    lastOverallState = SGCore.HG_CalibrationState.Unknown;

                    if (this.resetSphere != null)
                    {
                        this.resetSphere.SetZone(true);
                    }

                    //updates instructions by stage...
                    instr = "Follow the instructions below your hand..."; //TODO: get this from the HandLayer?
                    break;

                case VoidStage.Done:
                    instr = "Calibration Complete!";
                    if (this.resetSphere != null)
                    {
                        this.resetSphere.SetZone(false);
                    }
                    if (this.HasSceneTransition)
                    {
                        instr += "\nTaking you to the next scene...";
                        CalibrationCompleted.Invoke();
                        StartCoroutine(UnlockSceneAfter(this.changeSceneAfter));
                    }

                    break;

                case VoidStage.NoGloves:
                    instr = "Could not find any SenseGlove Devices.";
                    if (this.HasSceneTransition)
                    {
                        instr += "\nTaking you to the next scene...";
                    }
                    if (this.resetSphere != null)
                    {
                        this.resetSphere.SetZone(false);
                    }
                    break;

                case VoidStage.WaitingForFirstGlove:
                    instr = "Awaiting connection to a glove...";
                    if (this.resetSphere != null)
                    {
                        this.resetSphere.SetZone(false);
                    }
                    break;

                case VoidStage.StartupSenseCom:
                    instr = "Starting up SenseCom...";
                    if (this.resetSphere != null)
                    {
                        this.resetSphere.SetZone(false);
                    }
                    break;

            }
            MainInstr = instr;
            CalibrationStateChanged.Invoke();
        }


        private void UpdateCurrentState(float dT)
        {
            try
            {
                timer_currStage += dT; //the amount of time we've spent in this stage.
                switch (currentStage)
                {
                    case VoidStage.GlovesCalibrating:

                        SGCore.HG_CalibrationState leftState = leftHandCal.GetCalibrationState();
                        bool leftConnected = leftHandCal.IsConnected();
                        SGCore.HG_CalibrationState rightState = rightHandCal.GetCalibrationState();
                        bool rightConnected = rightHandCal.IsConnected();
                        SGCore.HG_CalibrationState overallState = GetEarliestState(leftState, leftConnected, rightState, rightConnected);

                        if (overallState == SGCore.HG_CalibrationState.CalibrationLocked)
                        {
                            GoToStage(VoidStage.Done);
                        }
                        else
                        {
                            //Check Intro.
                            if (showIntroduction)
                            {
                                // welcome etc. stays on for X amount of time OR when the relevant fingers are 
                                if (timer_currStage >= introTime
                                    || overallState >= SGCore.HG_CalibrationState.AllSensorsMoved)
                                {
                                    showIntroduction = false;
                                }
                            }
                            //Update overall Instructions
                            if (showIntroduction)
                            {
                                MainInstr = "Welcome! it's time to calibrate your fingers";
                            }
                            else
                            {

                                if (overallState != lastOverallState)
                                {
                                    if (overallState == SGCore.HG_CalibrationState.AllSensorsMoved)
                                    {
                                        MainInstr = "Give us a Thumbs up to confirm your calibration";
                                    }
                                    else if (overallState == SGCore.HG_CalibrationState.MoveFingers)
                                    {
                                        MainInstr = "Open and close your hand until all your fingers are moving...";
                                    }
                                    else
                                    {
                                        MainInstr = "Something went wrong...";
                                    }
                                }
                                lastOverallState = overallState;
                                if (overallState == SGCore.HG_CalibrationState.MoveFingers)
                                {
                                    leftHandCal.UpdateExample();
                                    rightHandCal.UpdateExample();
                                }
                            }

                        }
                        break;

                    case VoidStage.WaitingForFirstGlove:

                        if (timer_currStage >= this.noGloveTimeout)
                        {
                            GoToStage(VoidStage.NoGloves);
                        }

                        //check for any connection
                        if (leftHandCal.IsConnected() || rightHandCal.IsConnected())
                        {
                            GoToStage(VoidStage.GlovesCalibrating);
                        }
                        break;
                    case VoidStage.StartupSenseCom:

                        if (!sensecomRunning) //it wasn't running and we cannae get it to start
                        {
                            if (timer_currStage >= noGloveTimeout)
                            {
                                this.UnlockNextScene(); //_should_ automagically throw us into the next scene if needed.
                            }
                        }
                        if (SGCore.SenseCom.IsRunning())
                        {
                            this.GoToStage(VoidStage.WaitingForFirstGlove);
                        }
                        break;
                    case VoidStage.NoGloves:
                        if (timer_currStage >= introTime)
                        {
                            this.UnlockNextScene(); //_should_ automagically throw us into the next scene if needed.
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("CALVOID: " + ex.Message);
                Debug.LogError(ex.StackTrace);
                Debug.LogError(ex.Source);
                Debug.LogError(ex.TargetSite);
                Debug.LogError(ex.InnerException);
            }
        }




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Scene Transitions

        private void VerifyNextScene()
        {
            // Step 1: Evaluate if we have a Scene to transition to, and warn users when their settings are invalid.
            this.openNextScene = false;
            int calVoidIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            loadSceneIndex = UnityEngine.SceneManagement.SceneManager.GetSceneByName(this.goToSceneName).buildIndex;
            int sceneTotal = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            if (loadSceneIndex < 0 && goToSceneName.Length > 0)
            {
                Debug.LogError("Could not find \"" + goToSceneName + "\" using GetSceneByName(). Make sure it is included in your Build, and that it is spelled correctly. Or clear the goToSceneName parameter.", this);
            }
            if (loadSceneIndex == calVoidIndex) //it's valid Scene Name, that we wanted! (If empty, it returns -1 as well). CalVoidScene can't be -1?
            {
                Debug.LogError("goToSceneName \"" + goToSceneName + "\" leads to this Calibration Void. You will be stuck there forever. Are you sure you meant to do that?", this);
            }
            if (loadSceneIndex < 0) //we still don't have a proper index from the name.
            {
                loadSceneIndex = goToSceneIndex;
                if (loadSceneIndex == calVoidIndex)
                {
                    Debug.LogError("goToSceneIndex of " + loadSceneIndex.ToString() + " leads to this Calibration Void. You will be stuck there forever. Are you sure you meant to do that?", this);
                }
                else if (loadSceneIndex >= sceneTotal)
                {
                    Debug.LogError("goToSceneIndex of " + loadSceneIndex.ToString() + "is not a valid Build Index, as it's >= " + sceneTotal.ToString(), this);
                }
            }
            //Debug.Log("GoToScene = " + goToSceneName + ", BuildIndex = " + UnityEngine.SceneManagement.SceneManager.GetSceneByName(this.goToSceneName).buildIndex + ", CalVoid = " + calVoidIndex, this);
            if (loadSceneIndex < 0 || loadSceneIndex >= sceneTotal) //we still don't have a valid Scene index. (The second redundant check is there for sanity's sake)
            {
                Debug.Log("No Valid goToSceneName or nextSceneIndex found. The Calibration Void will not automatically continue to the next...", this);
                loadSceneIndex = -1; //set it to < 0 regardless.
            }
        }



        private IEnumerator TryLoadNextScene()
        {
            yield return null; //wait one frame so other scripts can set the next Scene's Build Index / Name during their Start() / Awake().
            VerifyNextScene(); //evaluates and modifies loadSceneIndex
            if (this.loadSceneIndex > -1)
            {
                Debug.Log("Loading next scene (" + this.loadSceneIndex + ") in the background...");
                AsyncOperation asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(this.loadSceneIndex);
                if (asyncOperation != null)
                {
                    //Don't let the Scene activate until you allow it to
                    asyncOperation.allowSceneActivation = false;
                    //When the load is still in progress, output the Text and progress bar
                    while (!asyncOperation.isDone)
                    {
                        // Check if the load has finished
                        if (asyncOperation.progress >= 0.90f)
                        {
                            //Wait to you press the space key to activate the Scene
                            if (openNextScene)
                            {
                                //Activates the Scene
                                asyncOperation.allowSceneActivation = true;
                            }
                        }
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogError("Something went wrong creating an AsyncOperation!");
                }
            }
        }

        private IEnumerator UnlockSceneAfter(float time)
        {
            yield return new WaitForSeconds(time);
            UnlockNextScene();
        }

        /// <summary> If the Calibration Void has a next scene, load it. </summary>
        public void UnlockNextScene()
        {
            //if (this.HasSceneTransition)
            //{
            //    Debug.Log("Opening the next Scene...");
            //}
            this.openNextScene = true;
        }

        public void ResetVoid()
        {
            SG.Util.SG_SceneControl.ResetScene();
        }

        public void EndCalibration(bool rightHand)
        {
            SGCore.HandLayer.EndCalibration(rightHand);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Inputs

        public void RetryCalibration(bool rightHand)
        {
            if (this.currentStage == VoidStage.GlovesCalibrating)
            {
                IndividualCalibration cal = rightHand ? rightHandCal : leftHandCal;
                if (cal != null)
                {
                    cal.ResetCalibration(true);
                }
            }
        }

        /// <summary> Event that fires when a user puts their hand inside the ResetSphere </summary>
		/// <param name="hand"></param>
		private void ResetSphereActivated(SG_TrackedHand hand)
        {
            if (this.resetSphere != null && this.resetSphere.isActiveAndEnabled)
            {
                RetryCalibration(hand.TracksRightHand());
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void OnEnable()
        {
            if (this.resetSphere != null)
            {
                this.resetSphere.OnConfirm.AddListener(ResetSphereActivated);
            }
        }

        private void OnDisable()
        {
            if (this.resetSphere != null)
            {
                this.resetSphere.OnConfirm.RemoveListener(ResetSphereActivated);
            }
            if (leftHandCal != null)
            {
                leftHandCal.UnsubscribeEvents();
                leftHandCal = null;
            }
            if (rightHandCal != null)
            {
                rightHandCal.UnsubscribeEvents();
                rightHandCal = null;
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            CollectComponents();
            if (this.resetSphere != null)
            {
                this.resetSphere.InstructionText = "Reset\nCalibration";
            }
            StartCoroutine(TryLoadNextScene());
#if UNITY_ANDROID && !UNITY_EDITOR
            GoToStage(VoidStage.WaitingForFirstGlove, true);
#else
            GoToStage(VoidStage.StartupSenseCom, true);
#endif
        }

        // Update is called once per frame
        void Update()
        {
            UpdateCurrentState(Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (this.changeSceneAfter < 0.0f)
            {
                this.changeSceneAfter = 0.01f;
            }
        }
#endif

    }
}