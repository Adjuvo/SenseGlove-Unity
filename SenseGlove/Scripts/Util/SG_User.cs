using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Utility Class to manage up to two SG_TrackedHands, and to swap their hands around. </summary>
    public class SG_User : MonoBehaviour
    {
        public SG_TrackedHand leftHand;
        public SG_TrackedHand rightHand;

        public KeyCode swapHandsKey = KeyCode.None;

        /// <summary> Set up the collision of the hands </summary>
        public void SetupHands()
        {
            if (leftHand != null && leftHand.hardware != null) { leftHand.hardware.connectionMethod = SG_SenseGloveHardware.ConnectionMethod.NextLeftHand; }
            if (leftHand != null && leftHand.hardware != null) { rightHand.hardware.connectionMethod = SG_SenseGloveHardware.ConnectionMethod.NextRightHand; }

            if (leftHand != null && rightHand != null)
            {
                if (leftHand.rigidBodyLayer != null && rightHand.rigidBodyLayer != null)
                {
                    leftHand.rigidBodyLayer.SetIgnoreCollision(rightHand.rigidBodyLayer, true);
                }
            }
        }

        public void SwapHandTracking()
        {
            if (leftHand != null && rightHand != null)
            {
                leftHand.SwapTracking(this.rightHand);
            }
        }

        // Use this for initialization
        void Start()
        {
            SetupHands();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(swapHandsKey))
            {
                SwapHandTracking();
            }
        }
    }
}