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


		void CalibrationZoneActivated(SG_TrackedHand byHand)
		{
			if (byHand != null && byHand == this.sequence.TrackedHand)
			{
				this.sequence.ResetCalibration(true);
			}
		}

		void OnEnable()
		{
			if (calibrationZone != null)
			{
				calibrationZone.OnConfirm.AddListener(CalibrationZoneActivated);
			}
		}

		void OnDisable()
		{
			if (calibrationZone != null)
			{
				calibrationZone.OnConfirm.RemoveListener(CalibrationZoneActivated);
			}
		}

		// Use this for initialization
		void Start()
		{
			if (calibrationZone != null)
			{
				calibrationZone.instructionsStayVisible = false;
				calibrationZone.InstructionText = this.sequence.IsRight ? "Calibrate\nRight" : "Calibrate\nLeft";
			}

		}
	}
}