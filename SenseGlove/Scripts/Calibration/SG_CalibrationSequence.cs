using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

	

	/// <summary> Unity wrapper for a SenseGlove calibration Sequence. Only mainly useful for SenseGlove Haptic Gloves. </summary>
	public class SG_CalibrationSequence : SG_HandComponent
	{

		//------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Optional 3D instructions in the scene </summary>
		[Header("Calibration Components")]
		public TextMesh instructions3D;
		/// <summary> Optional 2D instructions using Unity UI. </summary>
		public UnityEngine.UI.Text instructionsUI;

		/// <summary> If true, we show the instructions to confirm with a thumbs up. Otherwise, we'll just have it occur naturally. </summary>
		public bool showConfirmInstructions = true;

		/// <summary> Fires when calbration is aborted. </summary>
		public SG.Util.SGEvent CalibrationStarted = new Util.SGEvent();
		/// <summary> Fires when calibration finishes </summary>
		public SG.Util.SGEvent CalibrationFinished = new Util.SGEvent();

		private SG_HapticGlove hapticGlove;

		private SGCore.HG_CalibrationState lastState = SGCore.HG_CalibrationState.Unknown;

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


        //------------------------------------------------------------------------------------------------------------------
        // Functions

		public void ResetCalibration(bool overrideActive)
        {
			if (this.hapticGlove != null)
            {
				SGCore.HandLayer.ResetCalibration(this.hapticGlove.TracksRightHand());
				//this should call a calibrationChaged event, if the glove is connected
				this.CalibrationStarted.Invoke();
            }
        }

		public void EndCalibration()
		{
			if (this.hapticGlove != null)
			{
				SGCore.HandLayer.EndCalibration(this.hapticGlove.TracksRightHand());
				//this should call a calibrationChaged event, if the glove is connected
				this.CalibrationFinished.Invoke();
			}
		}

		protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
			if (hapticGlove != null)
			{
				//unlink events
				this.hapticGlove.CalibrationStateChanged.RemoveListener(UpdateCalibrationAssets);
				this.hapticGlove.DeviceDisconnected.RemoveListener(UpdateCalibrationAssets);
				this.hapticGlove.DeviceConnected.RemoveListener(UpdateCalibrationAssets);
			}
			base.LinkToHand_Internal(newHand, firstLink);
			if (this.TrackedHand != null && this.TrackedHand.deviceSelector != null)
			{
				this.hapticGlove = TrackedHand.deviceSelector.GetDevice<SG_HapticGlove>();
				if (hapticGlove != null)
				{
					this.hapticGlove.CalibrationStateChanged.AddListener(UpdateCalibrationAssets);
					this.hapticGlove.DeviceDisconnected.AddListener(UpdateCalibrationAssets);
					this.hapticGlove.DeviceConnected.AddListener(UpdateCalibrationAssets);
				}
			}
			UpdateCalibrationAssets();
		}

		public void UpdateCalibrationAssets()
        {
			if (this.hapticGlove != null)
			{
				bool rightHand = this.hapticGlove.TracksRightHand();
				SGCore.HG_CalibrationState nextState = this.hapticGlove.LastCalibrationState;
				if (nextState != SGCore.HG_CalibrationState.CalibrationLocked
					&& (this.showConfirmInstructions || nextState != SGCore.HG_CalibrationState.AllSensorsMoved))
                {
					InstructionText = SGCore.HandLayer.GetCalibrationInstructions(rightHand);
				}
				else
                {
					InstructionText = "";
				}
				if (lastState != nextState)
				{
					lastState = nextState;
					if (nextState == SGCore.HG_CalibrationState.CalibrationLocked)
					{
						this.CalibrationFinished.Invoke();
					}
				}
			}
        }


        //------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void Start()
        {
			UpdateCalibrationAssets();
        }
    }
}