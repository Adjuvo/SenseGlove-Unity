using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> VR Wrapper around a SG_CalibrationSequence to acces calibration during play. </summary>
	public class SG_XR_CalibrationMenu : MonoBehaviour 
	{
		/// <summary> the sequence this menu responds to. </summary>
		public SG_CalibrationSequence sequence;

		/// <summary> Zone tart activates calibration </summary>
		public SG.SG_ConfirmZone calibrationZone;

		/// <summary> Optional zone to cancel calibration at any time. </summary>
		public SG.SG_ConfirmZone cancelZone;

		/// <summary> Optional calibration instructions </summary>
		public TextMesh instructions3D;

		/// <summary> Once calibration completes, how long will the instructiontext remain? </summary>
		public static float instructionStayingTime = 2.5f;

		/// <summary> timer to clear instructions. </summary>
		private float timer_clearInstr;


		/// <summary> Access instruction text </summary>
		public string InstructionText
        {
			get { return instructions3D != null ? instructions3D.text : ""; }
			set { if (instructions3D != null) { instructions3D.text = value; } }
        }

		public bool CancelZoneActive
        {
			get { return this.cancelZone != null && this.cancelZone.zoneEnabled; }
			set { if (this.cancelZone != null) { this.cancelZone.SetZone(value); } }
        }


		public void NextCalibrationStep()
        {
			if (sequence != null)
            {
				if (sequence.CalibrationActive)
                {
					sequence.NextCalibrationStep();
                }
				else
                {
					sequence.StartCalibration();
					CancelZoneActive = true;
				}
            }
        }
		


		public void UpdateInstructions()
        {
			//gets instructions for the sphere and for 3D instructions.
			if (this.sequence != null && this.sequence.internalSequence != null)
			{
				string msg = sequence.internalSequence.GetCurrentInstuction();
				InstructionText = msg;
            }
			if (this.calibrationZone != null)
			{
				if (calibrationZone.HandInZone)
				{
					calibrationZone.InstructionText = "Remove Hand";
				}
				else if (!sequence.CalibrationActive)
				{
                    string txt = "Calibrate";
                    if (sequence != null && sequence.linkedGlove != null && sequence.linkedGlove.connectsTo != HandSide.AnyHand)
                    {
                        txt += (sequence.linkedGlove.TracksRightHand() ? "\r\nRight" : "\r\nLeft");
                    }
                    calibrationZone.InstructionText = txt;
				}
				else
				{
					calibrationZone.InstructionText = "Confirm";
				}
			}
        }

		public void CancelCalibration()
        {
			this.sequence.CancelCalibration();
			CancelZoneActive = false;
			UpdateInstructions();
			InstructionText = "Calibration was Cancelled.";
			timer_clearInstr = 0; //timeout instructions
		}


		private void CalibrationZone_Activated(SG_TrackedHand args)
		{
			NextCalibrationStep();
			UpdateInstructions();
		}

		private void CalibrationZone_Reset(SG_TrackedHand args)
		{
			UpdateInstructions();
		}

		private void CancelZone_Activated(SG_TrackedHand args)
		{
			CancelCalibration();
		}

		private void OnCalibrationFinished()
        {
			UpdateInstructions();
			CancelZoneActive = false;
			timer_clearInstr = 0;
		}

		private void OnCalibrationAborted()
        {
			CancelZoneActive = false;
			UpdateInstructions();
			InstructionText = this.sequence.CancellationMessage;
			timer_clearInstr = 0;
		}


		void OnEnable()
		{
			if (calibrationZone != null) 
			{ 
				calibrationZone.OnConfirm.AddListener(CalibrationZone_Activated);
                calibrationZone.OnReset.AddListener(CalibrationZone_Reset);
			}
			if (cancelZone != null) { cancelZone.OnConfirm.AddListener(CancelZone_Activated); }
			if (sequence != null) 
			{ 
				this.sequence.CalibrationFinished.AddListener(OnCalibrationFinished); 
				this.sequence.CalibrationAbort.AddListener(OnCalibrationAborted); 
			}
        }

        void OnDisable()
        {
			if (calibrationZone != null)
			{
				calibrationZone.OnConfirm.RemoveListener(CalibrationZone_Activated);
				calibrationZone.OnReset.RemoveListener(CalibrationZone_Reset);
			}
			if (cancelZone != null) { cancelZone.OnConfirm.RemoveListener(CancelZone_Activated); }
			if (sequence != null) 
			{ 
				this.sequence.CalibrationFinished.RemoveListener(OnCalibrationFinished);
				this.sequence.CalibrationFinished.AddListener(OnCalibrationAborted);
			}
		}

		// Use this for initialization
		void Start ()
		{
			InstructionText = "";
			timer_clearInstr = instructionStayingTime;
			if (calibrationZone != null)
            {
				calibrationZone.instructionsStayVisible = true;
			}
			if (cancelZone != null)
            {
				this.cancelZone.instructionsStayVisible = false;
				this.cancelZone.InstructionText = "Cancel";
				this.cancelZone.SetZone(false);
            }
			UpdateInstructions();
		}
	
		// Update is called once per frame
		void Update ()
		{
			if (timer_clearInstr < instructionStayingTime)
            {
				timer_clearInstr += Time.deltaTime;
				if (timer_clearInstr >= instructionStayingTime)
                {
					InstructionText = ""; //clear instructions after we're done.
				}
            }
		}
	}
}