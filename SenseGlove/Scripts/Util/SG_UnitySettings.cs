using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> How Communication is handled on Desktop Devices. </summary>
    public enum CommunicationSetup
    {
        /// <summary> Default Option. An extenal program called SenseCom will run your SenseGlove connections when used in your scene(s). If the program is not running, an attempt is made to start it. </summary>
        SenseComPreferred,

        // Not Yet Implemented...
        ///// <summary> Default for Android. This application runs SenseGlove communications inside its own runtime.  If SenseCom is already running, ít will keep control over the runtime instead. </summary>
        //StandaloneModePreferred,

        /// <summary> Will neither Start SenseCom nor activate internal communications. Useful for Server-side builds OR manual activation(s). </summary>
        Disabled
    }



    /// <summary> What to use as a tracking source for any kind of hand tracking </summary>
    public enum GlobalWristTracking
    {
        /// <summary> Use the UnityXR Device API (only useful for devices that are recognized by UnityXR) </summary>
        UnityXR,

        /// <summary> Positional data comes from a Gameobject that is specifically mentioned. summary>
        UseGameObject,
    }


    /// <summary> Which Tracking offsets to default to when running. </summary>
    public enum TrackingHardware
    {
        /// <summary> TrackingHardware not known (yet). Will apply no offsets. </summary>
        Unknown,

        /// <summary> The user has developed a custom solution, so we will use offsets set from the SG_UnitySettings instead. </summary>
        Custom,

        /// <summary> Default option. Our scripts will attempt to find out what tracking hardware is used, based on the name of the device(s) attached. 
        /// Mainly releveant for Desktop builds. It is always better to specify which HMD you're building for. </summary>
        AutoDetect,

        /// <summary> Oculus Quest 2 - Standalone HMD running Android. Requires Oculus XR Plugin </summary>
        Quest2Controller,
        /// <summary> Meta Quest Pro - Standalone HMD running Android. Requires Oculus XR Plugin </summary>
        QuestProController,
        /// <summary> Meta Quest 3 - Standalone HMD running Android. Requires Oculus XR Plugin </summary>
        Quest3Controller,
        /// <summary> Vive Focues 3 - Standalone HMD running Android. Requires Vive Wave SDK </summary>
        ViveWristTracker,
        /// <summary> Any of the HTC family headsets, or Valve Index. Requires OpenVRLoader plugin </summary>
        ViveTracker,

        /// <summary> Pico Neo Motion Tracker. Small white cylinder inisde a strap. </summary>
        PicoMotionTracker,

        /// <summary> Pico Neo 3 Controllers - Standalone HMD that requires PicoXR XR Plugin </summary>
        PicoNeo3Controller,

        /// <summary> Pico Neo 3 Controllers - Standalone HMD that requires PicoXR XR Plugin </summary>
        PicoNeo2Controller,
    }





    //[CreateAssetMenu(menuName = "SenseGlove/SenseGloveSettings")]

    /// <summary> Contains Global SenseGlove Settings for this project. Accessed via SG_Core.Settings </summary>
    public class SG_UnitySettings : ScriptableObject
    {

        // Communications


        // DESKTOP ONLY(?)

        /// <summary> Controls when / if the communications for SenseGlove are initialized.  </summary>
        [Tooltip("The way SenseGlove communications will be initialized / disposed on this system. On Android, Standalone will always be used.")] 
        public CommunicationSetup SGCommunications = CommunicationSetup.SenseComPreferred;

        /// <summary> Android only: If true, we will force a permissions dialog from within the App. This is required for Quest devices, but runs into trouble with Vive Devices... </summary>
        [Tooltip("Android only: If true, we will force a permissions dialog from within the App. This is required for Quest devices, but runs into trouble for Vive Devices. We will try to aumatically adjust then when you switch Wrist Tracking Offsets.")] 
        public bool ForceBluetoothPermissionsDialog = false;


        // ANDROID ONLY

        // N\\A


        // Tracking

        /// <summary> Wrist Tracking Methods </summary>
        [Tooltip("The way in which we acquire your tracking device location (Controllers, Trackers, etc) in this project.")] 
        public GlobalWristTracking WristTrackingMethod = GlobalWristTracking.UnityXR;

        /// <summary> Which tracking hardware is used for the SenseGlove device(s). We're assuming the same hardware is used for both devices. </summary>
        [Tooltip("The offsets from tracking device to Nova Glove used inside this project.")]
        public TrackingHardware GlobalWristTrackingOffsets = TrackingHardware.AutoDetect;


#if UNITY_EDITOR
        private TrackingHardware lastWristHW = TrackingHardware.AutoDetect;
#endif

        // XR Inputs?



        /// <summary> Returns the name of the Tracking Hardware </summary>
        public string TrackingHardwareName
        {
            get
            {
                if (GlobalWristTrackingOffsets == TrackingHardware.AutoDetect)
                {
                    TrackingHardware autoDetected = SG.SG_XR_Devices.GetDeterminedTrackingHardware();
                    string name = autoDetected != TrackingHardware.Unknown ? autoDetected.ToString() : "Checking...";
                    return GlobalWristTrackingOffsets.ToString() + ": " + name;
                }
                return GlobalWristTrackingOffsets.ToString();
            }
        }

        // IF Custom


        /// <summary> When selecting a custom solution, this is where you set the offset values for Position </summary>
        [Header("Right Hand Custom Offsets")]
        public Vector3 customPosOffsetRight = Vector3.zero;

        /// <summary> When selecting a custom solution, this is where you set the offset values for local euler angles. </summary>
        public Vector3 customRotOffsetRight = Vector3.zero;


        /// <summary> When selecting a custom solution, this is where you set the offset values for Position </summary>
        [Header("Left Hand Custom Offsets")]
        public Vector3 customPosOffsetLeft = Vector3.zero;

        /// <summary> When selecting a custom solution, this is where you set the offset values for local euler angles. </summary>
        public Vector3 customRotOffsetLeft = Vector3.zero;

        [HideInInspector] public bool leftQuat = true, rightQuat = true;
        [HideInInspector] public Quaternion customLeftQuat = Quaternion.identity, customRightQuat = Quaternion.identity;


        public void RecalculateOffsets()
        {
            customLeftQuat = Quaternion.Euler(customRotOffsetLeft);
            customRightQuat = Quaternion.Euler(customRotOffsetRight);
        }


        /// <summary> Retireve the Left Custom Rotation Offset as a quaternion </summary>
        public Quaternion CustomLeftRotationOffset
        {
            get
            {
                if (leftQuat)
                {
                    leftQuat = false;
                    customLeftQuat = Quaternion.Euler(customRotOffsetLeft);
                }
                return customLeftQuat;
            }
        }

        /// <summary> Retireve the Right Custom Rotation Offset as a quaternion </summary>
        public Quaternion CustomRightRotationOffset
        {
            get
            {
                if (rightQuat)
                {
                    rightQuat = false;
                    customRightQuat = Quaternion.Euler(customRotOffsetRight);
                }
                return customRightQuat;
            }
        }


        /// <summary> Retrieve the Custom offsets for the chosen hand - easy acces. </summary>
        /// <param name="rightHand"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffset"></param>
        public void GetCustomOffsets(bool rightHand, out Vector3 positionOffset, out Quaternion rotationOffset)
        {
            positionOffset = rightHand ? customPosOffsetRight : customPosOffsetLeft;
            rotationOffset = rightHand ? CustomRightRotationOffset : CustomLeftRotationOffset;
        }


        // Device Linking

//#if UNITY_EDITOR
//        private void OnValidate()
//        {
//            TrackingHardware currentHW = GlobalWristTrackingOffsets;
//            if (this.lastWristHW != currentHW) //only when you change tracking hardware and it's not a special one...
//            {
//                if (currentHW != TrackingHardware.Unknown && currentHW != TrackingHardware.AutoDetect && currentHW != TrackingHardware.Custom)
//                {
//                    //Update the BT permissions
//                    ForceBluetoothPermissionsDialog = currentHW == TrackingHardware.Quest2Controller
//                        || currentHW == TrackingHardware.Quest3Controller
//                        || currentHW == TrackingHardware.QuestProController;
//                }
//            }
//            this.lastWristHW = this.GlobalWristTrackingOffsets;
//        }
//#endif

    }
}
