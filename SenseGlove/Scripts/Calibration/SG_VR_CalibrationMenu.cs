using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> VR Wrapper around a SG_CalibrationSequence. </summary>
	public class SG_VR_CalibrationMenu : MonoBehaviour 
	{

		public SG_CalibrationSequence sequence;

		public SG.SG_ConfirmZone calibrationZone;

		/// <summary> Optional zone to cancel calibration at any time. </summary>
		public SG.SG_ConfirmZone cancelZone;

		/// <summary> Optional calibration instructions </summary>
		public TextMesh instructions3D;


		public static float instructionStayingTime = 2.5f;

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
                    if (sequence != null && sequence.linkedGlove != null && sequence.linkedGlove.connectsTo != HandSide.Any)
                    {
                        txt += (sequence.linkedGlove.IsRight ? "\r\nRight" : "\r\nLeft");
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


		private void CalibrationZone_Activated(object source, SG_HandDetector.GloveDetectionArgs args)
		{
			NextCalibrationStep();
			UpdateInstructions();
		}

		private void CalibrationZone_Reset(object source, SG_HandDetector.GloveDetectionArgs args)
		{
			UpdateInstructions();
		}

		private void CancelZone_Activated(object source, SG_HandDetector.GloveDetectionArgs args)
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
				calibrationZone.Activated += CalibrationZone_Activated;
                calibrationZone.Reset += CalibrationZone_Reset;
			}
			if (cancelZone != null) { cancelZone.Activated += CancelZone_Activated; }
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
				calibrationZone.Activated -= CalibrationZone_Activated;
				calibrationZone.Reset -= CalibrationZone_Reset;
			}
			if (cancelZone != null) { cancelZone.Activated -= CancelZone_Activated; }
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