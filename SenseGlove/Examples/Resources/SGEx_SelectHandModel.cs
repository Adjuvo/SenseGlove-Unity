using UnityEngine;

namespace SG.Examples
{
    public class SGEx_SelectHandModel : MonoBehaviour
    {
        public SG_HapticGlove leftGlove, rightGlove;

        private GameObject leftHandModel, rightHandModel;

        public KeyCode swapHandsKey = KeyCode.Return;

        private bool leftReady = false, rightReady = false;

        // Use this for initialization
        void Start()
        {
            if (leftGlove != null)
            {
                leftGlove.gameObject.SetActive(true);
                leftHandModel = leftGlove.transform.GetChild(0).gameObject;
            }
            if (rightGlove != null)
            {
                rightGlove.gameObject.SetActive(true);
                rightHandModel = rightGlove.transform.GetChild(0).gameObject;
            }
            SetModels(false, false);
        }

        // Update is called once per frame
        void Update()
        {
            if (leftGlove != null)
            {
                if (leftGlove.IsConnected && !leftReady)
                {
                    this.leftReady = true;
                    if (!rightReady)
                    {
                        SetModels(true, false);
                    }
                }
            }
            if (rightGlove != null)
            {
                if (rightGlove.IsConnected && !rightReady)
                {
                    this.rightReady = true;
                    if (!leftReady)
                    {
                        SetModels(false, true);
                    }
                }
            }

            if (Input.GetKeyDown(swapHandsKey) && (leftReady || rightReady))
            {
                SetModels(!this.leftHandModel.activeInHierarchy, !this.rightHandModel.activeInHierarchy);
            }
        }

        public void SetModels(bool left, bool right)
        {
            if (leftHandModel != null)
            {
                leftHandModel.SetActive(left);
            }
            if (rightHandModel != null)
            {
                rightHandModel.SetActive(right);
            }
        }

    }

}