using SGCore.Kinematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SG.Calibration
{

    /// <summary> Used to animate a set of hands showing the different calibation steps. </summary>
    public class SG_CalibrationVisuals : MonoBehaviour
    {
        /// <summary> Progress Bar Image </summary>
        [SerializeField] private Image progressBar;

        /// <summary> Indictaes how to make a proper thumbs up </summary>
        [SerializeField] private GameObject thumbsUpNotification;

        /// <summary> Contains visuals for a 3D hand model </summary>
        [SerializeField] private GameObject handModelVisual;


        /// <summary> Animation Controller for said hand </summary>
        [Header("Hand Animation Parameters (Optional)")]
        [SerializeField] private Animator animationController;

        [SerializeField] private string animationKey = "CalibrationStep";
        
        [SerializeField] private int thumbsUpValue = 0;
        [SerializeField] private int thumbBelowRingValue = 1;
        [SerializeField] private int thumbAbdValue = 2;
        [SerializeField] private int handsTogetherValue = 3;

        /// <summary> Enables / Disables the Progress bar visual </summary>
        public bool ProgressBarEnabled
        {
            get { return progressBar != null ? progressBar.gameObject.activeSelf : false; }
            set { if (progressBar != null) { progressBar.gameObject.SetActive(value && this.HandModelEnabled); } }
        }

        /// <summary> Set the Progress Bar to a percentage between 0 .. 1. </summary>
        /// <param name="value01"></param>
        public void SetProgressBarValue(float value01)
        {
            if (progressBar != null)
                progressBar.fillAmount = Mathf.Clamp01(value01);
        }


        /// <summary> Enables / Disables popup about thumbs up </summary>
        public bool ThumbsUpEnabled
        {
            get { return thumbsUpNotification != null ? thumbsUpNotification.activeSelf : false; }
            set { if (thumbsUpNotification != null) { thumbsUpNotification.SetActive(value && this.HandModelEnabled); } }
        }



        public bool HandModelEnabled
        {
            get { return handModelVisual != null ? handModelVisual.activeSelf : false; }
            set { if (handModelVisual != null) { handModelVisual.SetActive(value); } }
        }

        private void PlayAnimation(int value)
        {
            if (animationController == null)
                return;
            animationController.SetInteger(animationKey, value);
        }

        public void PlayVisualsFor(FingerCalibrationOrder fingerState)
        {
            ThumbsUpEnabled = false; //false by default since most of them don't care.
            switch (fingerState)
            {
                case FingerCalibrationOrder.Thumbsup:
                    ThumbsUpEnabled = true;
                    PlayAnimation(thumbsUpValue);
                    break;
                case FingerCalibrationOrder.ThumbBelowRingfinger:
                    PlayAnimation(thumbBelowRingValue);
                    break;
                case FingerCalibrationOrder.ThumbAbduction:
                    PlayAnimation(thumbAbdValue);
                    break;
                case FingerCalibrationOrder.HandsTogether:
                    PlayAnimation(handsTogetherValue);
                    break;
                default: //Hide everything
                    break;
            }
        }

    }
}