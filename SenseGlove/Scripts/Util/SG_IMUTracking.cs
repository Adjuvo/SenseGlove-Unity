using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> Uses an SG_HapticGlove's IMU output to rotate a transform relative to another one. </summary>
	public class SG_IMUTracking : MonoBehaviour
	{
		/// <summary> The SG_HapticGlove from which we can collect the IMU rotation. </summary>
		public SG_HapticGlove imuSource;

		/// <summary> The rotation is calibrated to - and moves relative to - this object. If left unassgined, you won't be able to calibrate. </summary>
		public Transform relativeTo;

		/// <summary> Calibrate automatically the first time we connect to a glove. </summary>
		protected bool firstCalibr = true;

		/// <summary> Used for calibration. </summary>
		protected Quaternion qCalibr = Quaternion.identity;

		public KeyCode calibrateIMUKey = KeyCode.None;

		/// <summary> Calibrate the IMU to the relativeTO Transform </summary>
		public void CalibrateIMU()
        {
			this.firstCalibr = false;
			Quaternion currIMU;
			if (this.relativeTo != null && this.imuSource != null && this.imuSource.GetIMURotation(out currIMU))
            {
				qCalibr = this.relativeTo.rotation * Quaternion.Inverse(currIMU);
            }
		}


		/// <summary> Updates the IMu rotation. </summary>
		public void UpdateRotation()
		{
			Quaternion currIMU;
			if (this.imuSource != null && this.imuSource.GetIMURotation(out currIMU))
            {
				if (this.firstCalibr)
                {
					this.CalibrateIMU();
                }
				if (this.relativeTo != null)
                {
					this.transform.rotation = this.qCalibr * currIMU;
				}
				else
                {
					this.transform.rotation = currIMU;
                }
			}
		}


		// Update is called once per frame
		void Update()
		{
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
			if (Input.GetKeyDown(this.calibrateIMUKey))
            {
				this.CalibrateIMU();
            }
#endif
			UpdateRotation();
		}
	}
}