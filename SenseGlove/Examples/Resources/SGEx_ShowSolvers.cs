using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Examples
{

	public class SGEx_ShowSolvers : MonoBehaviour
	{
		public SG_HapticGlove glove;

		public SG_TrackedHand[] leftHands = new SG_TrackedHand[0];
		public SG_TrackedHand[] rightHands = new SG_TrackedHand[0];

		private bool init = false;

		public void SetObjects(SG_TrackedHand[] objects, bool active)
        {
			for (int i=0; i<objects.Length; i++)
            {
				if (objects[i] != null) { objects[i].gameObject.SetActive(active); }
            }
        }




		public void CalibrateLefts()
        {
			Calibrate(leftHands);
		}

		public void CalibrateRights()
        {
			Calibrate(rightHands);
        }

		private void Calibrate(SG_TrackedHand[] hands)
        {
			for (int i = 0; i < hands.Length; i++)
			{
				if (hands[i] != null && hands[i].handAnimation != null) { hands[i].handAnimation.CalibrateWrist(); }
			}
		}


		private void Setup()
        {
			SetObjects(leftHands, false);
			SetObjects(rightHands, false);

			for (int i = 0; i < rightHands.Length; i++)
			{
				//rightHands[i].gloveHardware.ResetCalibration();
				if (rightHands[i] != null && rightHands[i].handAnimation != null) { rightHands[i].handAnimation.updateWrist = true; }
			}
			for (int i = 0; i < leftHands.Length; i++)
			{
				//leftHands[i].gloveHardware.ResetCalibration();
				if (leftHands[i] != null && leftHands[i].handAnimation != null) { leftHands[i].handAnimation.updateWrist = true; }
			}
		}

		void Start()
        {
			Setup();
        }

		// Update is called once per frame
		void Update()
		{
			if (glove && glove.IsConnected)
            {
				if (!init) 
				{
					init = true;
					SetObjects(leftHands, !glove.IsRight);
					SetObjects(rightHands, glove.IsRight);

					//Debug.Log("HandProfile: " + leftHands[0].handAnimation.HandModel.HandKinematics.ToString(false));
				}
            }
		}
	}
}