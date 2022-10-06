using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

	

	/// <summary> Unity wrapper for a SenseGlove calibration Sequence. </summary>
	public class SG_CalibrationSequence : MonoBehaviour
	{
		/// <summary> When to start a calibration sequence. </summary>
        public enum StartCondition
        {
			/// <summary> Calibration sequence will ony start when we call A StartCalibration or NextCalibrationStep function. </summary>
			Manual,
			/// <summary> Only start calibration when the HapticGlove determines it must be done (you're not in the same range as last time). </summary>
			WhenNeeded,
			/// <summary> This calibration will always run when the glove (re)Connects </summary>
			OnConnected
		}



		public enum CalibrationType
		{
			Quick,
			//GuidedSteps
		}

		//------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The glove from which to collect calibration data </summary>
		public SG.SG_HapticGlove linkedGlove;

		/// <summary> Used to control finger animation </summary>
		public SG.SG_TrackedHand linkedHand;

		/// <summary> Calibration Sequence inside the .dll, can be any kind of combination of gestures or movements. </summary>
		public SGCore.Calibration.HG_CalibrationSequence internalSequence = null;
		
		/// <summary> Last calibration stage, used to check for changes. </summary>
		protected int lastStage = -1;

		/// <summary> Which calibration algorithm to use for this sequence. </summary>
		public CalibrationType calibrationType = CalibrationType.Quick;

		/// <summary> When to start this calibration sequence </summary>
		public StartCondition startCondition = StartCondition.Manual;

		/// <summary> Whether or not debug information is passed to the user. </summary>
		public bool debugEnabled = false;

		/// <summary> Optional Element do debug Calibration Data onto </summary>
		public TextMesh debugText;

		/// <summary> Hotkey to start the calibration and confirm the current step(s_ </summary>
		public KeyCode nextStepKey = KeyCode.None;
		/// <summary> Hotkey to cancel calibration if it's running </summary>
		public KeyCode cancelKey = KeyCode.None;
		/// <summary> Hotkey to reset the calibration for the linkedGlove's left / right indication. </summary>
		public KeyCode resetCalibrationKey = KeyCode.None;

		protected bool eventsLinked = false;

		/// <summary> If true, we're currently running through a calibration sequence. </summary>
		public bool CalibrationActive
		{
			get; protected set;
		}

		/// <summary> Time after which the instructions dissapear. </summary>
		public static float resetInstrTime = 2.5f;
		/// <summary> Timer variable for the instructions </summary>
		protected float timer_resetInstr = 0;
		/// <summary> Instructions will revert to this message when not in use. </summary>
		public string baseInstrMessage = "";
		/// <summary> Optional 3D instructions in the scene </summary>
		public TextMesh instructions3D;
		/// <summary> Optional 2D instructions using Unity UI. </summary>
		public UnityEngine.UI.Text instructionsUI;
		
		/// <summary> Fires when calibration finishes </summary>
		public SG.Util.SGEvent CalibrationFinished;
		/// <summary> Fires when calbration is aborted. </summary>
		public SG.Util.SGEvent CalibrationAbort;

		/// <summary> The message from the calibration sequence if it was aborted for any reason. </summary>
		public string CancellationMessage
        {
			get; private set;
        }


		//------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Accesor for instruction messages </summary>
		public string InstructionText
		{
			get
			{
				if (instructions3D != null) { return instructions3D.text; }
				else if (instructionsUI != null) { return instructionsUI.text; }
				return "";
			}
			set
			{
				if (instructions3D != null) { instructions3D.text = value; }
				if (instructionsUI != null) { instructionsUI.text = value; }
			}
		}

		/// <summary> Accesor for Debug messages </summary>
		public string DebugText
		{
			get
			{
				return debugText != null ? debugText.text : "";
			}
			set
			{
				if (debugText != null) { debugText.text = value; }
			}
		}


		public bool CanAnimate
        {
			get
			{
				if (internalSequence != null && internalSequence is SGCore.Calibration.HG_QuickCalibration)
                {
					return ((SGCore.Calibration.HG_QuickCalibration)internalSequence).CanAnimate;
                }
				return false;
			}
        }

		//------------------------------------------------------------------------------------------------------------------
		// Functions

		public void LinkHand(SG_TrackedHand newHand)
        {
			UnlinkEvents();
			this.linkedHand = newHand;
			// Debug.Log(this.name + "(" + (this.linkedHand != null ? (this.linkedHand.TracksRightHand() ? "R" : "L") : "BEFORE LINK") + "): Setup.");
			if (this.linkedGlove == null)
			{
				SG_HapticGlove myGlove = this.linkedHand != null && this.linkedHand.deviceSelector != null ? this.linkedHand.deviceSelector.GetDevice<SG_HapticGlove>() : null;
				this.linkedGlove = myGlove;
			}
			//Also link my instructions to the hand's wrist if we don;t have one yet
			if (this.instructions3D == null && newHand.statusIndicator != null)
            {
				this.instructions3D = newHand.statusIndicator.wristText;
            }
			this.LinkEvents();
		}



		/// <summary> Starts the calibration sequence if it hasn't already.  </summary>
		/// <param name="cancelActive">If true, this will cancel any active calibration and start a new one</param>
		public void StartCalibration(bool cancelActive = false)
		{
			if (cancelActive)
            {
				CancelCalibration();
            }
			if (!CalibrationActive)
			{
				SetupSequence(); //ensure we're alsways checking for values on time
				if (internalSequence != null && linkedGlove != null && !linkedGlove.CalibrationLocked && linkedGlove.StartCalibration(this))
				{
                    //linkedGlove's calibration is now locked ot this stage.
					internalSequence.Reset();
					lastStage = internalSequence.CurrentStageInt;
					CalibrationActive = true;
					//Debug.Log("Started Calibration Sequence!");
					InstructionText = this.internalSequence.GetCurrentInstuction(this.nextStepKey != KeyCode.None ? this.nextStepKey.ToString() : "");
					CancellationMessage = "";
				}
			}
		}


		/// <summary> Finish the calibration sequence and calibrate the hand. </summary>
		protected void FinishCalibration()
		{
			CalibrationActive = false;
			Debug.Log("Calibration has finished!");
			InstructionText = "Calibration Finished!";
			timer_resetInstr = 0;
			SGCore.Calibration.SensorRange range;
			//SGCore.HandProfile newProfile;
			if (internalSequence.CompileRange(out range))
			{
                SGCore.HandProfile newProfile;
                SGCore.Calibration.HG_CalibrationSequence.CompileProfile(range, linkedGlove.DeviceType, linkedGlove.TracksRightHand(), out newProfile); //turn it into a newprofile
                SG_HandProfiles.SetProfile(newProfile);

                if (SG_HandProfiles.SaveLastRange(range, this.linkedGlove.InternalGlove))
                {
                    //Debug.Log("Saved Range: " + range.ToString(true));
                }
            }
			else
			{
				//Debug.Log("We could not compile a range. Something went wrong....");
			}
            //HandAnimationEnabled = true;
            linkedGlove.CompleteCalibration(this);
			DebugText = "";
			CalibrationFinished.Invoke();
		}


		/// <summary> Cancel the calibration sequence in case something went wrong. </summary>
		/// <param name="disconnect">Optional parameter to indicate if it was cancelled due to a disconnect.</param>
		public void CancelCalibration(bool disconnect = false) //stop calibration.
		{
			if (CalibrationActive)
			{
				Debug.Log("Cancelling Active Calibration!");
				InstructionText = "Calibration Cancelled.";
				timer_resetInstr = 0;
				internalSequence.Reset();
                linkedGlove.CompleteCalibration(this);
				//HandAnimationEnabled = true;
			}
			DebugText = "";
			CalibrationActive = false;
		}


		/// <summary> Activates the next calibration step. If no calibaraion is active, start calibration instead. </summary>
		public void NextCalibrationStep()
		{
			if (this.CalibrationActive)
			{
				internalSequence.ConfirmCurrentStep();
				Debug.Log("Confirmed Calibration Step");
			}
			else
            {
				this.StartCalibration();
            }
		}

		/// <summary> Updates the calibration sequence; adds points, and checks for completion. </summary>
		protected void UpdateCalibration()
		{
			if (linkedGlove != null && !linkedGlove.IsConnected())
			{
				Debug.Log("Lost Calibration because of a reset!");
				this.CancelCalibration();
				InstructionText = "Lost connection! Calibration cancelled";
				CalibrationAbort.Invoke();
			}
			else
			{
				//active stuff.
				//Debug.Log("Calibration is active!");
				this.internalSequence.Update(Time.deltaTime);
				//Debug.Log("Gathered " + internalSequence.DataPointCount + " data points over " + internalSequence.elapsedTime + "s");

#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
				if (Input.GetKeyDown(this.nextStepKey))
				{
					NextCalibrationStep();
				}
#endif

				if (internalSequence.Completed)
				{
					//Debug.Log("This sequence has ended " + (internalSequence.AutoCompleted ? "automatically." : "manually."));
					FinishCalibration();
				}
				else if (CalibrationActive)
				{
					if (this.lastStage != internalSequence.CurrentStageInt) //the stage updated...
					{
						this.InstructionText = internalSequence.GetCurrentInstuction();
					}
					this.lastStage = internalSequence.CurrentStageInt;
					if (this.debugText != null)
					{
						this.DebugText = this.internalSequence.GetDebugInfo();
					}

					//Thumbs Up Goes here(?)




				}

			}
		}




		/// <summary> Retrieve the internal "preview" pose, as internal SG notation. Does not include wrist position/rotation! </summary>
		/// <param name="calibrationPose"></param>
		/// <returns></returns>
		public bool GetCalibrationPose(SGCore.Kinematics.BasicHandModel handDimensions, out SG_HandPose calibrationPose)
        {
			SGCore.HandPose iPose;
			if (GetCalibrationPose(handDimensions, out iPose))
            {
				calibrationPose = new SG.SG_HandPose(iPose);
				return true;
            }
			calibrationPose = null;
			return false;
		}

		/// <summary> Retrieve the internal "preview" pose, as internal SG notation </summary>
		/// <param name="handDimensions"></param>
		/// <param name="iCalibrationPose"></param>
		/// <returns></returns>
		public bool GetCalibrationPose(SGCore.Kinematics.BasicHandModel handDimensions, out SGCore.HandPose iCalibrationPose)
        {
			if (this.internalSequence != null)
			{
				return this.internalSequence.GetHandPose(handDimensions, out iCalibrationPose);
			}
			iCalibrationPose = null;
			return false;
		}

		/// <summary> Reset the profile for the hand our LinkedGlove is connected to. </summary>
		public void ResetHandCalibration()
		{
			SG.SG_HandProfiles.RestoreDefaults(linkedGlove.TracksRightHand());
			timer_resetInstr = 0;
			InstructionText = "Reset " + (linkedGlove.TracksRightHand() ? "right" : "left") + " hand calibration.";
		}


		/// <summary> Check if you need to start calibration, when calibrationStage changes. </summary>
		public void CheckForStart_ChangedState()
        {
			if (this.startCondition == StartCondition.WhenNeeded && this.linkedGlove != null && !linkedGlove.CalibrationLocked && this.linkedGlove.GetCalibrationStage() == SGCore.Calibration.CalibrationStage.CalibrationNeeded)
			{
				Debug.Log("Automatically starting calibration because " + linkedGlove.name + " needs it, and " + this.name + " is set to start when needed.");
				this.StartCalibration();
			}
        }

		/// <summary> Check if you need to start Calibration when the glove connects. </summary>
		public void CheckForStart_Connected()
        {			if (this.startCondition == StartCondition.OnConnected && this.linkedGlove != null && !linkedGlove.CalibrationLocked)
            {
				Debug.Log("Automatically starting calibration because " + linkedGlove.name + " just connected, and " + this.name + " is set to start OnConnected.");
				this.StartCalibration();
			}
        }

		/// <summary> Create a new internal Calibration sequence based on the parameters in the Inspector, it it hasn't been created already </summary>
		public void SetupSequence()
        {
			if (linkedGlove != null && internalSequence == null && linkedGlove.IsConnected())
			{
				SGCore.HapticGlove lastGlove = this.linkedGlove.InternalGlove;
				if (calibrationType == CalibrationType.Quick)
				{
					this.internalSequence = new SGCore.Calibration.HG_QuickCalibration(lastGlove);
				}
				//else
				//{
				//	this.internalSequence = new SGCore.Calibration.Nova_GuidedCalibrationSequence(lastGlove);
				//}
				//Debug.Log("Linked a glove to this Calibration Sequence. Ready to start when you are! [" + nextStepKey.ToString() + "]");
			}
		}


		protected void LinkEvents()
        {
			if (!eventsLinked)
			{
				eventsLinked = true;
				//Debug.Log(this.name + "(" + (this.linkedHand != null ? (this.linkedHand.TracksRightHand() ? "R" : "L") : "BEFORE LINK") + "): Events Linked.");
				if (this.linkedGlove != null) { this.linkedGlove.CalibrationStateChanged.AddListener(CheckForStart_ChangedState); }
				if (this.linkedGlove != null) { this.linkedGlove.DeviceConnected.AddListener(CheckForStart_Connected); }
			}
		}

		protected void UnlinkEvents()
        {
			if (eventsLinked)
            {
				eventsLinked = false;
				//Debug.Log(this.name + "(" + (this.linkedHand != null ? (this.linkedHand.TracksRightHand() ? "R" : "L") : "BEFORE LINK") + "): Events UnLinked.");
				if (this.linkedGlove != null) { this.linkedGlove.CalibrationStateChanged.RemoveListener(CheckForStart_ChangedState); }
				if (this.linkedGlove != null) { this.linkedGlove.DeviceConnected.RemoveListener(CheckForStart_Connected); }
			}
        }

		//------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		void OnEnable()
        {
			LinkEvents();
        }

		void OnDisable()
        {
			UnlinkEvents();
		}

		// Use this for initialization
		void Start()
		{
			//if (linkedGlove == null) { linkedGlove = linkedHand.gloveHardware; }
			this.InstructionText = baseInstrMessage;
			timer_resetInstr = resetInstrTime;
			this.CancellationMessage = "";
			DebugText = "";
		}


		// Update is called once per frame
		void Update()
		{
			if (!CalibrationActive)
			{
				//DebugText = "Calibration Inactive...";
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
				if (Input.GetKeyDown(nextStepKey)) { StartCalibration(); }
				else if (Input.GetKeyDown(resetCalibrationKey)) { ResetHandCalibration(); }
#endif
				if (timer_resetInstr < resetInstrTime)
				{
					timer_resetInstr += Time.deltaTime;
					if (timer_resetInstr >= resetInstrTime)
					{
						InstructionText = baseInstrMessage;
					}
				}
			}
			else
			{
				UpdateCalibration();
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
				if (Input.GetKeyDown(cancelKey))
				{
					CancelCalibration();
				}
#endif
			}
		}
	}
}