using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SG
{


    /// <summary> Class to access device tracking and input using UnityXR's generic system. Useabe with multiple headsets from Unity 2019+. </summary>
    public class SG_XR_Devices
    {

        /// <summary> Used to check whether or not we swapped last time, which becomes relevant if we're using Vives. </summary>
        private const string switchedKey = "sgSwap";

        // Swapping of hands (Mostly vive trackers only)

        public static bool HandsSwitched
        {
            get
            {
                return PlayerPrefs.GetInt(switchedKey, 0) == 1;
            }
            set
            {
                bool original = HandsSwitched;
                PlayerPrefs.SetInt(switchedKey, value ? 1 : 0);
                if (original != value)
                {
                    OnHandsSwapped();
                }
            }
        }

        public static void SwitchHands()
        {
            HandsSwitched = !HandsSwitched;
        }


        public static event System.EventHandler HandsAreSwapped;

        private static void OnHandsSwapped()
        {
            if (HandsAreSwapped != null)
            {
                HandsAreSwapped.Invoke(null, new System.EventArgs());
            }
        }



#if UNITY_2019_4_OR_NEWER
        /// <summary> The Tracking type that we're using to determine (controller) locations. Required to correct all these standards back to their native tracking method. </summary>
        public enum TrackingType
        {
            /// <summary> Not (yet) sure which Tracking method is used... </summary>
            Unknown,
            /// <summary> We're using the device's native plugin to interface with it. Use default offsets </summary>
            Native,
            /// <summary> We're using UnityXR, which does not (yet) play nice with SteamVR Trackers. </summary>
            UnityXR,
            /// <summary> We're using OpenXR, which does whatever it wants. </summary>
            OpenXR
        }

        /// <summary> Class to contain an Input Device, along with any accessors / functions we might require </summary>
        /// <remarks> I can also null this class. </remarks>
        public class SG_XR_LinkedDevice
        {
            /// <summary> The UnityEngine XR Device. Since this is a struct, it cannot be null </summary>
            public UnityEngine.XR.InputDevice XRDevice
            {
                get; set;
            }

            /// <summary> Whether or not this device is (still) linked. </summary>
            public bool DeviceLinked
            {
                get; set;
            }

            /// <summary> Default constructor for an unlinked device </summary>
            public SG_XR_LinkedDevice()
            {
                DeviceLinked = false;
            }

            /// <summary> Constructor to make a new linked device form a UnityXRDevice </summary>
            /// <param name="device"></param>
            public SG_XR_LinkedDevice(UnityEngine.XR.InputDevice device)
            {
                this.XRDevice = device;
                DeviceLinked = true;
            }


            /// <summary> Function to directly pass the Device to another function call. </summary>
            /// <param name="device"></param>
            /// <returns></returns>
            public virtual bool GetDevice(out UnityEngine.XR.InputDevice device)
            {
                device = this.XRDevice;
                return DeviceLinked;
            }

            public virtual bool TryGetLocation(out Vector3 position, out Quaternion rotation)
            {
                if (this.DeviceLinked)
                {
                    XRDevice.TryGetFeatureValue(CommonUsages.devicePosition, out position);
                    XRDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
                    return true;
                }
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

        }

        /// <summary> A reference to a hand tracking reference, along with the controller reference. </summary>
        public class SG_XR_HandReference : SG_XR_LinkedDevice
        {
            public SGCore.PosTrackingHardware Hardware
            {
                get; set;
            }

            public SG_XR_HandReference()
            {
                DeviceLinked = false;
                Hardware = SGCore.PosTrackingHardware.Custom;
            }

            public SG_XR_HandReference(InputDevice device)
            {
                this.XRDevice = device;
                DeviceLinked = true;
                this.Hardware = SGCore.PosTrackingHardware.Custom;
            }

            public SG_XR_HandReference(InputDevice device, SGCore.PosTrackingHardware hardwareType)
            {
                this.XRDevice = device;
                DeviceLinked = true;
                this.Hardware = hardwareType;
            }


        }


        // XR Resources

        private static SG_XR_LinkedDevice headTracking = null;
        /// <summary> In which way UnityXR is accessing the tracking references for the left and the right hand. </summary>
        private static TrackingType trackingMethod = TrackingType.Unknown;
        private static SG_XR_HandReference leftHandTracking = null, rightHandTracking = null;


        // Timing / Scanning Resources

        private static float scanInterval = 1.0f; //scan every Xs while not linked, no matter how many times people ask for it.
        private static bool init = true;
        private static System.DateTime lastPing;


        //----------------------------------------------------------------------------------------------------------------------------
        // UnityXR Utility Functions


        /// <summary> Returns the Unity XR list of all connected devices. </summary>
        /// <returns></returns>
        public static List<UnityEngine.XR.InputDevice> GetDevices()
        {
            List<UnityEngine.XR.InputDevice> inputDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevices(inputDevices);
            return inputDevices;
        }

        public static string ReportDevices()
        {
            return ReportDevices(GetDevices());
        }

        /// <summary> Reports the names and characteristics of each XR Device </summary>
        /// <returns></returns>
        public static string ReportDevices(List<UnityEngine.XR.InputDevice> inputDevices)
        {
            string db = "Found " + inputDevices.Count + " device(s):";
            for (int i = 0; i < inputDevices.Count; i++)
            {
                db += "\n" + Report(inputDevices[i]);
            }
            return db;
        }

        /// <summary> Reports all relevant details of a single device. </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static string Report(UnityEngine.XR.InputDevice device, string delim = ", ")
        {
            return device.name + delim + device.serialNumber + delim + device.subsystem + delim + device.characteristics;
        }

        /// <summary> Returns true if a collection of input characteristics contains another (set of) characteristics).  </summary>
        /// <remarks> For example, if allChars = (Left, Controller), and you look for (Left), this will return true.  </remarks>
        /// <param name="allChars"></param>
        /// <param name="toFind"></param>
        /// <returns></returns>
        public static bool HasCharacteristic(UnityEngine.XR.InputDeviceCharacteristics allChars, UnityEngine.XR.InputDeviceCharacteristics toFind)
        {
            return (allChars & toFind) == toFind; //Íf the LHS == toFind, that means that all bytes that are 1 in toFind are also 1 in allChars.
        }


        public static bool TryGetDevice(UnityEngine.XR.InputDeviceCharacteristics deviceChars, out UnityEngine.XR.InputDevice device)
        {
            List<UnityEngine.XR.InputDevice> devices = GetDevices();
            return TryGetDevice(devices, deviceChars, out device);
        }

        /// <summary> Returns true if a UnityXR InputDevice exists in devices with a specific set of device characteristics </summary>
        /// <param name="devices"></param>
        /// <param name="deviceChars"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool TryGetDevice(List<UnityEngine.XR.InputDevice> devices, UnityEngine.XR.InputDeviceCharacteristics deviceChars, out UnityEngine.XR.InputDevice device)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if ((devices[i].characteristics & deviceChars) == deviceChars) //bitwise operator. if all (relevant) bits are equal, returns true
                {
                    device = devices[i];
                    return true;
                }
            }
            device = new UnityEngine.XR.InputDevice();
            return false;
        }

        /// <summary> Retireve all InputDevices that could be Vive Trackers. </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        private static List<InputDevice> GetViveTrackers(List<InputDevice> devices)
        {
            List<InputDevice> trackers = new List<InputDevice>();
            for (int i = 0; i < devices.Count; i++)
            {
                string dN = devices[i].name.ToLower();
                string mnF = devices[i].manufacturer.ToLower();
                bool viveProduct = dN.Contains("vive");
                bool namedTracker = dN.Contains("tracker");
                bool oldTracker = mnF.Equals("htc") && dN.Contains("lhr-");

                //Link to any Vive Produc that calls itself a tracker, or a non-vive device naming itself "Tracker". Or the old vive trackers, which are just LHR-SerialNumber
                if ((viveProduct && namedTracker) || (!viveProduct && namedTracker) || oldTracker)
                {
                    trackers.Add(devices[i]);
                }
            }
            return trackers;
        }

        /// <summary> Splits a list of InputDevices into three lists of those assigned a Left, Right, or None. </summary>
        /// <param name="devices"></param>
        /// <param name="lefts"></param>
        /// <param name="rights"></param>
        /// <returns></returns>
        public static void SplitByHanded(List<InputDevice> devices, out List<InputDevice> lefts, out List<InputDevice> rights, out List<InputDevice> others)
        {
            lefts = new List<InputDevice>();
            rights = new List<InputDevice>();
            others = new List<InputDevice>();
            for (int i = 0; i < devices.Count; i++)
            {
                InputDeviceCharacteristics allChars = devices[i].characteristics;
                if (HasCharacteristic(allChars, InputDeviceCharacteristics.Left))
                {
                    lefts.Add(devices[i]);
                }
                else if (HasCharacteristic(allChars, InputDeviceCharacteristics.Right))
                {
                    rights.Add(devices[i]);
                }
                else
                {
                    others.Add(devices[i]);
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------
        // Linking Functions

        /// <summary> Returns true if  </summary>
        /// <param name="xrDevice"></param>
        /// <returns></returns>
        public static bool IsLinked(SG_XR_LinkedDevice xrDevice)
        {
            return xrDevice != null && xrDevice.DeviceLinked;
        }

        /// <summary> Returns true if one or more devices are still missing. </summary>
        /// <returns></returns>
        public static bool MissingDevices()
        {
            return headTracking == null || rightHandTracking == null || leftHandTracking == null;
        }

        private static void TryLinkHands(List<InputDevice> devices)
        {
            string hmdName = headTracking != null ? headTracking.XRDevice.name.ToLower() : "";

            //It's a headset meant to be used with Vive Trackers.
            if (hmdName.Length > 0 && (hmdName.Contains("vive") | hmdName.Contains("valve")))
            {
                List<InputDevice> trackers = GetViveTrackers(devices);
                if (trackers.Count > 0) //there's at least one tracker found
                {
                    // It is possible to map a Vive Tracker to the left or right hand via Binding UI.
                    // In that case, the tracker COULD have a clear handed-ness associated with it (Unless you assigned it to the knee or something).
                    UnityEngine.XR.InputDevice leftTracker = new InputDevice(), rightTracker = new InputDevice();
                    bool leftPresent = false, rightPresent = false;
                    List<InputDevice> lefts, rights, others;
                    SplitByHanded(trackers, out lefts, out rights, out others);

                    if (lefts.Count == 0 && rights.Count == 0) //Neither tracker has been assigned a proper index through SteamVR. So we default to right, left.
                    {
                        rightTracker = trackers[0]; rightPresent = true;
                        if (trackers.Count > 1) { leftTracker = trackers[1]; leftPresent = true; }
                    }
                    else
                    {   //at least one of our two trackers is known. That means if one of them is not, you can find it in the 'others' section (provided it's not empty).
                        if (rights.Count > 0) { rightTracker = rights[0]; rightPresent = true; }
                        else if (others.Count > 0) { rightTracker = others[0]; rightPresent = true; }

                        if (lefts.Count > 0) { leftTracker = lefts[0]; leftPresent = true; }
                        else if (others.Count > 0) { leftTracker = others[0]; leftPresent = true; }
                    }
                    //Now we can evaluate the Trackers
                    if (leftPresent)
                    {
                        //if (leftHandTracking == null) { Debug.Log("Linked SG_XR_Devices Left Hand to " + Report(leftTracker)); }
                        leftHandTracking = new SG_XR_HandReference(leftTracker, SGCore.PosTrackingHardware.ViveTracker);
                    }
                    else if (leftHandTracking != null)
                    {
                        leftHandTracking.DeviceLinked = false;
                    }
                    if (rightPresent)
                    {
                        //if (rightHandTracking == null) { Debug.Log("Linked SG_XR_Devices Right Hand to " + Report(rightTracker)); }
                        rightHandTracking = new SG_XR_HandReference(rightTracker, SGCore.PosTrackingHardware.ViveTracker);
                    }
                    else if (rightHandTracking != null) //could not find a right hand tracking device. So stoppit
                    {
                        rightHandTracking.DeviceLinked = false;
                    }
                }
            }
            else if (hmdName.Contains("wvr hmd")) //Vive Focus with wrist trackers (TrackedDevices). Requires Vive Input Utility?
            {
                InputDevice leftDevice;
                if (TryGetDevice(devices, InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left, out leftDevice))
                {
                    leftHandTracking = new SG_XR_HandReference(leftDevice, IdentifyTrackingHardware(hmdName, leftDevice.name, leftDevice.manufacturer, trackingMethod));
                    //Debug.Log("Linked SG_XR_Devices Left Hand to " + Report(leftDevice));
                }
                InputDevice rightDevice;
                if (TryGetDevice(devices, InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right, out rightDevice))
                {
                    rightHandTracking = new SG_XR_HandReference(rightDevice, IdentifyTrackingHardware(hmdName, rightDevice.name, rightDevice.manufacturer, trackingMethod));
                    //Debug.Log("Linked SG_XR_Devices Right Hand to " + Report(rightDevice));
                }
            }
            else // If we get here, it's not a Vive. So just proceed as through it were controllers.
            {
                if (leftHandTracking == null)
                {
                    InputDevice leftDevice;
                    if (TryGetDevice(devices, InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, out leftDevice))
                    {
                        leftHandTracking = new SG_XR_HandReference(leftDevice, IdentifyTrackingHardware(hmdName, leftDevice.name, leftDevice.manufacturer, trackingMethod));
                        //Debug.Log("Linked SG_XR_Devices Left Hand to " + Report(leftDevice));
                    }
                }
                if (rightHandTracking == null)
                {
                    InputDevice rightDevice;
                    if (TryGetDevice(devices, InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, out rightDevice))
                    {
                        rightHandTracking = new SG_XR_HandReference(rightDevice, IdentifyTrackingHardware(hmdName, rightDevice.name, rightDevice.manufacturer, trackingMethod));
                        //Debug.Log("Linked SG_XR_Devices Right Hand to " + Report(rightDevice));
                    }
                }
            }
        }

        /// <summary> Checks the current InputDevices to see if they are (still) valid. </summary>
        private static void CheckDevices()
        {
            List<InputDevice> devices = GetDevices();

            // Debug.Log(ReportDevices(devices));

            if (headTracking == null || !headTracking.DeviceLinked)
            {
                InputDevice xrHMD;
                if (TryGetDevice(devices, InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, out xrHMD))
                {
                    headTracking = new SG_XR_LinkedDevice(xrHMD);
                    headTracking.DeviceLinked = true;

                    string hmdName = xrHMD.name.ToLower();
                    if (hmdName.Contains("openxr"))
                    {
                        trackingMethod = TrackingType.OpenXR;
                    }
                    else
                    {
                        trackingMethod = TrackingType.Native;
                    }
                    //Debug.Log("Linked SG_XR_Devices Head Tracking to " + Report(xrHMD) + ", Concluded we're using " + trackingMethod.ToString() + " tracking");
#if UNITY_EDITOR
                    if (trackingMethod == TrackingType.OpenXR)
                    {
                        Debug.LogWarning("It looks like you're using OpenXR to manage your devices. Unfortunately, that plugin makes it difficult for " +
                            "SenseGlove to check which device you're using. To prevent this from happening, override your Trackign Hardware in any SG_HapticGlove(s) you're using.");
                    }
#endif
                }
            }
            TryLinkHands(devices);
        }

        /// <summary> This function checks if we need to update the InputDevices </summary>
        public static void CheckUpdate()
        {
            if (init)
            {
                init = false;
                lastPing = System.DateTime.UtcNow;
                CheckDevices();
            }
            else if (MissingDevices())
            {
                System.DateTime currTime = System.DateTime.UtcNow;
                if (currTime.Subtract(lastPing).TotalSeconds >= scanInterval)
                {
                    CheckDevices();
                    lastPing = currTime;
                }
            }
        }

        /// <summary> Returns the XR device associated with the HMD. </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool GetHMDDevice(out InputDevice hmdDevice)
        {
            CheckUpdate();
            if (headTracking != null)
            {
                return headTracking.GetDevice(out hmdDevice);
            }
            hmdDevice = new InputDevice();
            return false;
        }

        public static bool GetHandDevice(bool rightHand, out SG_XR_HandReference device)
        {
            CheckUpdate();

            //Swapping is only relevant if Vive trackers are involved. Otherwise it does not (yet) make sense(glove).
            if (HandsSwitched &&
                ((rightHandTracking != null && rightHandTracking.Hardware == SGCore.PosTrackingHardware.ViveTracker)
                || leftHandTracking != null && leftHandTracking.Hardware == SGCore.PosTrackingHardware.ViveTracker))
            {
                rightHand = !rightHand;
            }

            SG_XR_HandReference hand = rightHand ? rightHandTracking : leftHandTracking;
            if (hand != null)
            {
                device = hand;
                return true;
            }
            device = null;
            return false;
        }

        /// <summary> Returns the XR Device associated with the cheosen hand </summary>
        /// <param name="rigthHand"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool GetHandDevice(bool rightHand, out InputDevice device)
        {
            SG_XR_HandReference handRef;
            if (GetHandDevice(rightHand, out handRef)) //this one takes care of swapping etc
            {
                return handRef.GetDevice(out device);
            }
            device = new InputDevice();
            return false;
        }


        //----------------------------------------------------------------------------------------------------------------------------
        // SG Plugin Functionality

        public static SGCore.PosTrackingHardware IdentifyTrackingHardware(string hmdName, string deviceName, string manufacturerName, TrackingType trackingAPI)
        {
            if (deviceName.Length > 0)
            {
                deviceName = deviceName.ToLower();
                hmdName = hmdName.Length > 0 ? hmdName.ToLower() : "";
                manufacturerName = manufacturerName.Length > 0 ? manufacturerName.ToLower() : "";
                // Sorted by HMD for my own sanity

                // Vive Pro / Valve Index - Uses trackers
                if (deviceName.Contains("vive") && (deviceName.Contains("tracker") || (manufacturerName.Equals("htc") && deviceName.Contains("lhr-"))))
                {
                    return SGCore.PosTrackingHardware.ViveTracker;
                }

                // Oculus Quest 2 Controllers
                if (hmdName.Contains("oculus") || hmdName.Contains("meta"))
                {
                    if (hmdName.Contains("quest"))
                    {
                        return hmdName.Contains("pro") ? SGCore.PosTrackingHardware.QuestProController : SGCore.PosTrackingHardware.Quest2Controller; //xrDevice = "meta quest pro"
                    }
                    else if (hmdName.Contains("rift"))
                    {
                        return SGCore.PosTrackingHardware.OculusTouch;
                    }
                    else if (deviceName.Contains("oculus")) //Unable to identify by the hmdName, so check the device itself
                    {
                        return SGCore.PosTrackingHardware.Quest2Controller;
                    }
                }
                else if (deviceName.Contains("oculus"))
                {   //If OpenXR enabled, we cannot determine headset name. In that case, I'm assuming you're using a Quest 2.
                    return SGCore.PosTrackingHardware.Quest2Controller;
                }

                // Pico Neo - Can't seem to get it to work with OpenXR
                if (hmdName.Contains("pico") || deviceName.Contains("pico")) //there is currently no way to distinguish between Pico Neo 3 and Pico Neo 2. Both are a "PicoXR HMD"
                {
                    //We can't get their current SDK to work with the Neo 2. Besides, that HMD is rarely used anymore. Assuming Neo 3 from now on
                    return SGCore.PosTrackingHardware.PicoNeo3;
                }

                if (hmdName.Contains("wvr") && deviceName.Contains("tracker")) //Wave VR - Vive Focus
                {
                    return SGCore.PosTrackingHardware.ViveFocus3WristTracker; //this is a Vive Pro Wrist Tracker.
                }
            }
            return SGCore.PosTrackingHardware.Custom; //an unsupported thing
        }

        /// <summary> Retireve the location of our Tracking Reference. </summary>
        public static bool GetTrackingReferenceLocation(bool rightHand, out Vector3 position, out Quaternion rotation)
        {
            SG_XR_HandReference handRef;
            if (GetHandDevice(rightHand, out handRef)) //GetHandDevice calls CheckDevicesI()
            {
                return handRef.TryGetLocation(out position, out rotation);
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        /// <summary> Retireve the location of our Tracking Reference - and also tells you what it's identified as. </summary>
        public static bool GetTrackingReferenceLocation(bool rightHand, out Vector3 position, out Quaternion rotation, out SGCore.PosTrackingHardware trackingHardware)
        {
            SG_XR_HandReference handRef;
            if (GetHandDevice(rightHand, out handRef)) //GetHanDevice calls CheckDevices etc
            {
                trackingHardware = handRef.Hardware;
                return handRef.TryGetLocation(out position, out rotation);
            }
            trackingHardware = SGCore.PosTrackingHardware.Custom;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        /// <summary> Check which tracking hardware is used when using SenseGloves </summary>
        /// <returns></returns>
        public static bool GetTrackingHardware(bool rightHand, out SGCore.PosTrackingHardware trackingHardware)
        {
            SG_XR_HandReference handRef;
            if (GetHandDevice(rightHand, out handRef)) //this fuction taked into account swapping etc 
            {
                trackingHardware = handRef.Hardware;
                return true;
            }
            trackingHardware = SGCore.PosTrackingHardware.Custom;
            return false;
        }

#endif

        /// <summary> Returns true if there is a valid headset connected that is on the user' head. </summary>
        /// <returns></returns>
        public static bool HeadsetOnHead()
        {
            CheckUpdate();
            if (headTracking != null)
            {
                bool userPresent;
                headTracking.XRDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);
                return userPresent;
            }
            return true; //Default to true - this is probably a 2D or simulated XR Headset...
        }

        public static bool TryCheckForController(out bool isController)
        {
            if (leftHandTracking.DeviceLinked)
            {
                if ((leftHandTracking.XRDevice.characteristics & InputDeviceCharacteristics.Controller) == InputDeviceCharacteristics.Controller)
                {
                    isController = true;
                    return true;
                }
            }
            else if (rightHandTracking.DeviceLinked)
            {
                if ((rightHandTracking.XRDevice.characteristics & InputDeviceCharacteristics.Controller) == InputDeviceCharacteristics.Controller)
                {
                    isController = true;
                    return true;
                }
            }
            isController = false;
            return false;
        }

    }
}
