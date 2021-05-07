using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> THE Unity wrapper for a calibrationSequence. </summary>
	public class SG_CalibrationSequence : MonoBehaviour
	{
		/// <summary> The glove from which to collect calibration data </summary>
		public SG.SG_HapticGlove linkedGlove;
		/// <summary> Used to enable / disable animation? </summary>
		public SG.SG_TrackedHand linkedHand;


		public SGCore.Calibration.HapticGlove_CalibrationSequence internalSequence = null;
		protected int lastStage = -1;


		public SGCore.Calibration.CalibrationType calibrationType = SGCore.Calibration.CalibrationType.Quick;

		public bool autoStartWhenNeeded = false;


		public KeyCode nextStepKey = KeyCode.None;
		public KeyCode cancelKey = KeyCode.None;
		public KeyCode resetCalibrationKey = KeyCode.None;


		

		public bool CalibrationActive
		{
			get; protected set;
		}

		public static float resetInstrTime = 2.5f;
		protected float timer_resetInstr = 0;


		public string baseInstrMessage = "";
		public TextMesh instructions3D;
		public UnityEngine.UI.Text instructionsUI;

		public SG.Util.SGEvent CalibrationFinished;
		public SG.Util.SGEvent CalibrationAbort;


		public string CancellationMessage
        {
			get; private set;
        }


        //public bool HandAnimationEnabled
        //{
        //    get
        //    {
        //        return linkedHand != null && linkedHand.handAnimation != null ? linkedHand.handAnimation.enabled : false;
        //    }
        //    set
        //    {
        //        if (linkedHand != null && linkedHand.handAnimation != null) { linkedHand.handAnimation.enabled = value; }
        //    }
        //}


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


		public void StartCalibration()
		{
			SetupSequence(); //ensure we're alsways checking for values on time,
			if (!CalibrationActive && internalSequence != null)
			{
				if (linkedGlove != null && !linkedGlove.CalibrationLocked && linkedGlove.LockCalibration(this))
				{
                    //linkedGlove's calibration is now locked ot this stage.
					internalSequence.Reset();
					lastStage = internalSequence.CurrentStageInt;
					CalibrationActive = true;
					Debug.Log("Started Calibration Sequence!");
					InstructionText = this.internalSequence.GetCurrentInstuction(this.nextStepKey != KeyCode.None ? this.nextStepKey.ToString() : "");
                    //start doing stuff
                    //HandAnimationEnabled = false;
					CancellationMessage = "";
				}
			}
		}

		protected void FinishCalibration()
		{
			CalibrationActive = false;
			Debug.Log("Calibration has finished!");
			InstructionText = "Calibration Finished!";
			timer_resetInstr = 0;
			SGCore.Calibration.SensorRange range;
			//SGCore.HandProfile newProfile;
			if (internalSequence.CompileRange(out range))
			//&& SGCore.Calibration.Nova_CalibrationSequence.CompileProfile(range, linkedGlove.DeviceType, linkedGlove.IsRight, out newProfile))
			{
				//Debug.Log("We were able to compile a calibration range: " + range.ToString());
				//SG.SG_HandProfiles.SaveProfile(newProfile);
				this.linkedGlove.CalibrateHand(range);
			}
			else
			{
				//Debug.Log("We could not compile a range. Something went wrong....");
			}
            //HandAnimationEnabled = true;
            linkedGlove.UnlockCalibraion(this);
			CalibrationFinished.Invoke();
		}


		public void CancelCalibration(bool disconnect = false) //stop calibration.
		{
			if (CalibrationActive)
			{
				Debug.Log("Cancelling Active Calibration!");
				InstructionText = "Calibration Cancelled.";
				timer_resetInstr = 0;
				internalSequence.Reset();
                linkedGlove.UnlockCalibraion(this);
                //HandAnimationEnabled = true;
            }
			CalibrationActive = false;
		}

		/// <summary> Activates the next calibration step. If no calibaraion is active, start calibration instead. </summary>
		public void NextCalibrationStep()
		{
			if (this.CalibrationActive)
			{
				internalSequence.ConfirmCurrentStep();
			}
			else
            {
				this.StartCalibration();
            }
		}

		public void UpdateCalibration()
		{
			if (linkedGlove != null && !linkedGlove.IsConnected)
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


				if (Input.GetKeyDown(this.nextStepKey))
				{
					NextCalibrationStep();
				}

				if (internalSequence.Completed)
				{
					Debug.Log("This sequence has ended " + (internalSequence.AutoCompleted ? "automatically." : "manually."));
					FinishCalibration();
				}
				else if (CalibrationActive)
				{
					if (this.lastStage != internalSequence.CurrentStageInt)
					{
						this.InstructionText = internalSequence.GetCurrentInstuction();
					}
					this.lastStage = internalSequence.CurrentStageInt;

					if (linkedHand != null && linkedHand.handAnimation != null)
					{
						SGCore.HandPose pose;
						if (this.internalSequence.GetHandPose(out pose))
						{
							linkedHand.handAnimation.UpdateHand(new SG.SG_HandPose(pose));
						}
					}
				}
			}
		}

		public void ResetHandCalibration()
		{
			SG.SG_HandProfiles.RestoreDefaults(linkedGlove.IsRight);
			timer_resetInstr = 0;
			InstructionText = "Reset " + (linkedGlove.IsRight ? "right" : "left") + " hand calibration.";
		}

		public void CheckForStart()
        {
			if (this.linkedGlove != null && this.linkedGlove.CalibrationStage == SGCore.Calibration.CalibrationStage.CalibrationNeeded 
				&& this.autoStartWhenNeeded && calibrationType == SGCore.Calibration.CalibrationType.Quick)
            {
				Debug.Log("Automatically starting calibration because " + linkedGlove.name + " needs it, and " + this.name + " has autoStartWhenNeeded set to True.");
				this.StartCalibration();
            }
        }


		public void SetupSequence()
        {
			if (linkedGlove != null && internalSequence == null && linkedGlove.IsConnected)
			{
				SGCore.HapticGlove lastGlove = this.linkedGlove.InternalGlove;
				if (calibrationType == SGCore.Calibration.CalibrationType.Quick)
				{
					this.internalSequence = new SGCore.Calibration.HapticGlove_QuickCalibration(lastGlove);
				}
				else
				{
					this.internalSequence = new SGCore.Calibration.Nova_GuidedCalibrationSequence(lastGlove);
				}
				//Debug.Log("Linked a glove to this Calibration Sequence. Ready to start when you are! [" + nextStepKey.ToString() + "]");
			}
		}


		void OnEnable()
        {
			if (this.linkedGlove != null) { this.linkedGlove.CalibrationStateChanged.AddListener(CheckForStart); }
        }

		void OnDisable()
        {
			if (this.linkedGlove != null) { this.linkedGlove.CalibrationStateChanged.RemoveListener(CheckForStart); }
		}

		// Use this for initialization
		void Start()
		{
			if (linkedGlove == null) { linkedGlove = linkedHand.gloveHardware; }
			this.InstructionText = baseInstrMessage;
			timer_resetInstr = resetInstrTime;
			this.CancellationMessage = "";
			//CheckForStart();
		}


		// Update is called once per frame
		void Update()
		{
			if (!CalibrationActive)
			{
				if (Input.GetKeyDown(nextStepKey)) { StartCalibration(); }
				else if (Input.GetKeyDown(resetCalibrationKey)) { ResetHandCalibration(); }
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
				if (Input.GetKeyDown(cancelKey))
				{
					CancelCalibration();
				}
			}
		}
	}
}