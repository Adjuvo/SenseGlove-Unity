using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Allows us to move a controller around in 3D space. </summary>
    public class SG_XR_ControllerTracking : MonoBehaviour
    {
        /// <summary> Determines if this controller grabs a left- or right hand. </summary>
        public bool rightHand = true;

        /// <summary> Optional component to move relative to a CameraRig. </summary>
        public Transform origin;

#if UNITY_2019_4_OR_NEWER

        /// <summary> Retrieve the Unity XR Device linked to this script's tracking. You can use this to access button inputs etc. </summary>
        /// <param name="xrDevice"></param>
        /// <returns></returns>
        public bool GetInputDevice(out UnityEngine.XR.InputDevice xrDevice)
        {
            return SG_XR_Devices.GetHandDevice(this.rightHand, out xrDevice);
        }


        // Update is called once per frame
        protected virtual void Update()
        {
            Vector3 pos;
            Quaternion rot;
            if (SG_XR_Devices.GetTrackingReferenceLocation(this.rightHand, out pos, out rot))
            {
                if (origin == null)
                {
                    this.transform.rotation = rot;
                    this.transform.position = pos;
                }
                else //move relative to the origin
                {
                    this.transform.rotation = origin.rotation * rot;
                    this.transform.position = origin.position + (origin.rotation * pos);
                }
            }
        }
#endif

    }
}