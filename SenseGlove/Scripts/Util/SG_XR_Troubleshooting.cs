using SG.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A script meant to run through your project settings to confirm if your (XR and non XR) SenseGlove settings are properly set up.
 * 
 * author: max@senseglove.com
 */

namespace SG
{

    public class SG_XR_Troubleshooting : MonoBehaviour
    {
        public TextMesh debugTextElement;

        protected SG_TrackedHand[] trackedHands = new SG_TrackedHand[0];

        public string DebugText
        {
            get { return debugTextElement != null ? debugTextElement.text : "N\\A"; }
            set { if (debugTextElement != null) { debugTextElement.text = value; } }
        }


        protected List<string> Logs = new List<string>();
        protected List<string> WarningsDetected = new List<string>();
        protected List<string> ErrorsDetected = new List<string>();

        //TODO: Output to console.

        //TODO: Stuff that doesn't change during RunTime
        //TODO: Stuff that does change during runtime (e.g. Device Connections, TrackedObject locations).
        protected bool testing = false;

        public const float TrackerMinMoveDistance = 0.15f; //we must move at least 15cm from our starting location to register that the controller / tracking reference(s) move.
        protected ObjectTracker leftWristRef = null, rightWristRef = null;

        //--------------------------------------------------------------------------------------------------------------------------------------
        // Utility Functions

        /// <summary> Compiles all errors, warnings and normal logs to output them in that order. Tod: Make a log somewhere? </summary>
        public void UpdateInstructions()
        {
            string txt = "";
            if (ErrorsDetected.Count == 0 && WarningsDetected.Count == 0) //no warnings or errors, yay!
            {
                txt = "";
            }
            else if (ErrorsDetected.Count == 0) //Errors. BORKERO!
            {
                txt = "";
            }
            else //WARNINGS: These may dissapear...
            {
                txt = "";
            }

            if (ErrorsDetected.Count > 0)
            {
                txt = "Errors Detected:\n" + Compile(ErrorsDetected);
            }
            
            if (WarningsDetected.Count > 0)
            {
                if (txt.Length > 0)
                    txt += "\n\nWarnings Detected:\n" + Compile(WarningsDetected);
                else
                    txt = "Warnings Detected:\n" + Compile(WarningsDetected);
            }
            if (Logs.Count > 0)
            {
                if (txt.Length > 0)
                    txt += "\n\nLogs:\n" + Compile(Logs);
                else
                    txt = "Logs:\n" + Compile(Logs);
            }
            DebugText = txt;
        }


        /// <summary> Utility Function: Converts a list of strings into something to throw onto a Debug.Log. </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string Compile(List<string> list)
        {
            if (list.Count == 0)
            {
                return "";
            }
            string txt = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                txt += "\n" + list[i];
            }
            return txt;
        }


        /// <summary> Uiltiy function to get the two Glove Instances. </summary>
        /// <param name="leftGlove"></param>
        /// <param name="rightGlove"></param>
        public static void GetGloveInstances(out SGCore.HapticGlove leftGlove, out SGCore.HapticGlove rightGlove)
        {
            rightGlove = null;
            if (SGCore.HandLayer.GetGloveInstance(true, out SGCore.HapticGlove gloveR))
                rightGlove = gloveR;

            leftGlove = null;
            if (SGCore.HandLayer.GetGloveInstance(false, out SGCore.HapticGlove gloveL))
                leftGlove = gloveL;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------
        // Tests and Evaluations


        /// <summary> Are we building / running on a platform that SenseGlove can support? </summary>
        private void EvaluatePlatformSettings()
        {
#if UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
            Logs.Add("SenseGlove Connections are supported on your current platform!");
#else
            ErrorsDetected.Add("PLATFORM ERROR: SenseGlove Hardware is NOT supported on your current platform! This is due to the limits of hardware-communications. For more info, check: https://senseglove.gitlab.io/SenseGloveDocs/compatibility");
#endif
        }


        /// <summary> Have the SenseGlove Connections been set up correctly? </summary>
        private void EvaluateSGConnectionSetup()
        {
            CommunicationSetup comms = SG_Core.Settings.SGCommunications;
            //Reminder: We can check the current build target. Using UNITY_ANDROID for example!

            if (comms == CommunicationSetup.Disabled)
            {
                //ErrorsDetected.Add("TODO: Communication is set to Manual. Someone will need to have Initialzed it somewhere. Check if they did. If not, warn them.");
                if (SGCore.SGConnect.ScanningActive())
                {
                    Logs.Add("Auto-Connection is Disabled, but a process, like SenseCom, is running. You should be fine to connect to gloves in this session.");
                }
                else
                {
                    WarningsDetected.Add("CONNECTION WARNING: SGCommunications are Disabled. You will need to manually Initialize these via SG_Core.Initialize(). You won't be able to connect to SenseGlove Devices without it!");
                }
            }
            else if (comms == CommunicationSetup.SenseComPreferred)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (SGCore.SenseCom.IsRunning())
                {
                    Logs.Add("SenseGlove Connections are initialized. Please wait for the Gloves to be connected...");
                }
                else
                {
                    ErrorsDetected.Add("COMMUNICATIONS ERROR: Could not initialize SenseGlove Connections for Android! We won't be able to connect to SenseGlove Devices this way!");
                }
#else
                //ErrorsDetected.Add("TODO: Check if SenseCom is running. If not, check if it is installed. If not, warn the user.");
                if (SGCore.SenseCom.IsRunning())
                {
                    Logs.Add("SenseCom is preferred and connections are initialized.");
                }
                else if (SGCore.SenseCom.SenseCom_Installed())
                {
                    if (SGCore.SenseCom.GetExePath(out string scPath))
                    {
                        //only report this error if timeSInceInit has reached a few seconds...
                        if (Time.timeSinceLevelLoad > 5.0f)
                        {
                            ErrorsDetected.Add("COMMUNICATIONS ERROR: SenseCom is installed, but no commonication process is running. Please launch the executable manually, and check for any error messages. " +
                                "It should be located at " + scPath);
                        }
                        else
                        {
                            WarningsDetected.Add("COMMUNICATIONS WARNING: SenseCom process has not yet started. Waiting to see if this changes.");
                        }
                    }
                    else
                    {
                        ErrorsDetected.Add("COMMUNICATIONS ERROR: SenseCom is installed, but its process could not be started. Please launch the executable manually, and check for any error messages.");
                    }     
                }
                else
                {
                    ErrorsDetected.Add("COMMUNICATIONS ERROR: SGCommunications is set to SenseCom Preferred, but this software is not installed. You can get is via https://github.com/Adjuvo/SenseCom.");
                }
#endif
            }
//            else if (comms == CommunicationSetup.StandaloneModePreferred)
//            {
//                //ErrorsDetected.Add("TODO: This should work most if not all of the time. It's the default on Android. But warn them about possible freezes.");
//                if (SGCore.SGConnect.ScanningActive())
//                {
//#if UNITY_EDITOR
//                    Logs.Add("COMMUNICATIONS WARNING: SGCommunications is set to Standalone Mode. It may take a little while before a device connects. For development, we therefore recommend using SenseCom instead.");
//#else
//                    Logs.Add("COMMUNICATIONS WARNING: SGCommunications is set to Standalone Mode. It may take a little while before a device connects.");
//#endif
//                }
//                else
//                {
//                    WarningsDetected.Add("COMMUNICATIONS WARNING: SGCommunications is set to Standalone Mode. Communications will run inside the application, except if SenseCom is already running.");
//                }
//            }
        }

        /// <summary> Do we have all the (Android) permissions that we need (e.g. Bluetooth / Nearby Devices) </summary>
        private void EvaluatePermissions()
        {
#if UNITY_ANDROID //we're on Android or Building for Android in the near future.
    #if UNITY_EDITOR
            WarningsDetected.Add("When running SenseGlove Plugin on Android, please ensure that you enable Bluetooth Permissions in order to connect to SenseGlove Devices.");
    #else
            SG_Core.BluetoothPermissionCode permissionCode = SG_Core.CheckBluetoothPermissions();
            switch (permissionCode)
            {
                case SG_Core.BluetoothPermissionCode.NoPermission:
                    ErrorsDetected.Add("BT PERMISSION ERROR: This Application does not have permissions for Bluetooth / Nearby Devices. It's therefore impossible to connect to SenseGlove Devices.");
                    break;
                case SG_Core.BluetoothPermissionCode.PermissionGranted:
                    Logs.Add("Bluetooth permissions have been granted to this application.");
                    break;
                default:
                    WarningsDetected.Add("BT PERMISSION WARNING: Could not determine if this .apk has BLUETOOTH_CONNECT Permission enabled...");
                    break;
            }
    #endif
#endif
        }





        /// <summary> Are there two Haptic Gloves connected? If not; report! </summary>
        private void EvaluateActiveSGConnections()
        {
            string whereCheck = "";
            SGCore.HapticGlove leftGlove, rightGlove;
            GetGloveInstances(out leftGlove, out rightGlove);


#if UNITY_ANDROID && !UNITY_EDITOR

            // Paired Devices (Android only at the moment).
            if (SG_IAndroid.Andr_GetPairedDevices(out string pairedSerialized))
            {
                string[] addresses = pairedSerialized.Split('\n');

                switch (addresses.Length)
                {
                    case 0:
                        ErrorsDetected.Add("ERROR: There are no SenseGlove Devices Paired to your device. Exit the application and pair them!");
                        break;
                    case 1:
                        string device = addresses[0];
                        WarningsDetected.Add("WARNING: There's only one device paired to your system (" + device + "). Make sure that is intentional!");
                        break;
                    case 2:
                        string gloveOne = addresses[0];
                        string gloveTwo = addresses[1];
                        Logs.Add("Two gloves are paired (" + gloveOne + ", " + gloveTwo + "). Make sure these are the gloves you are wearing.");
                        break;
                    default:
                        WarningsDetected.Add("WARNING: There are " + addresses.Length + " SenseGlove devices paired, which can cause connection delays.\nMake sure you only have the devices paired that you are using.");
                        break;
                }
            }
            else
            {
                ErrorsDetected.Add("PAIRING ERROR: Could not get any Paired Devices");
            }

            
            
            // Connection States
            bool showConnectionStates = leftGlove == null || rightGlove == null;
            if (showConnectionStates)
            {
                SGCore.Util.ConnectionStatus[] states = SGCore.DeviceList.GetConnectionStates();
                string connStates = "Connection States (" + states.Length + "):";
                if (states.Length > 0)
                {
                    int idleConnections = 0;
                    int remainingConnections = 0;
                    for (int i = 0; i < states.Length; i++)
                    {
                        //This is not YET been detected as a SenseGlove Device
                        bool isSGDevice = states[i].LastTestState == (int)SGCore.Util.SC_TestState.TS_NEW_SGDEVICE || states[i].LastTestState == (int)SGCore.Util.SC_TestState.TS_EXISTING_SGDEVICE;
                        if (!isSGDevice)
                        {
                            remainingConnections++;
                            if (states[i].LastTestState == (int)SGCore.Util.SC_TestState.TS_IDLING)
                            {
                                idleConnections++;
                            }
                        }
                        if (!states[i].IsConnected || !isSGDevice) //always report when this is not a SenseGlove Device.
                        {
                            string tstState = states[i].LastTestState.ToString();
                            //string connCode = states[i].LastConnectionCode.ToString(); //not relevant on android; it's either 1 or -1, apparently.
                            string exitCode = states[i].LastExitCode.ToString();
                            connStates += "\n" + i.ToString() + ": Test State: " + tstState.ToString();
                            if (states[i].LastExitCode != (int)SGCore.Util.SC_ExitCode.E_UNKNOW)
                            {
                                connStates += ", Exit Code: " + states[i].LastExitCode.ToString();
                            }
                        }
                    }
                    //When we're here, at lease one of our devices has no yet been detected. (0 or 1 detected). Ten it's a warning.
                    //if ALL remaining connections are idle, it's an error
                    if (idleConnections >= remainingConnections)
                    {
                        ErrorsDetected.Add("All connections have been checked, but there are still missing glove(s)!"
                            + connStates);
                    }
                    else
                    {

                        WarningsDetected.Add("Still attempting to connect... " + connStates);
                    }
                }
                else
                {
                    ErrorsDetected.Add("CONNECTION ERROR: No active connection states.");
                }
            }

          //  ErrorsDetected.Add("TODO: On Android, Show the list of 'Paired' devices. If this is not exactly two, give them a notification.");
#else
            switch (SG_Core.Settings.SGCommunications)
            {
                case CommunicationSetup.SenseComPreferred:
                    whereCheck = "Please Check SenseCom for the latest connection status.";
                    break;
                //case CommunicationSetup.StandaloneModePreferred:
                //    whereCheck = "It may take a while for devices to connect when using Standalone Mode...";
                //    break;
                default:
                    whereCheck = "";
                    break;
            }
#endif

            if (rightGlove == null && leftGlove == null) //retrieves the amount of (connected) Haptic Gloves.
            {
                WarningsDetected.Add("CONNECTION WARNING: No Haptic Gloves have been detected. " + whereCheck);
            }
            else if (leftGlove == null)
            {
                WarningsDetected.Add("CONNECTION WARNING: Only the right glove has been detected! " + whereCheck);
            }
            else if (rightGlove == null)
            {
                WarningsDetected.Add("CONNECTION WARNING: Only the left glove has been detected! " + whereCheck);
            }
            else
            {
                Logs.Add("Left- and Right hand connections detected: (" + leftGlove.GetDeviceID() + " & " + rightGlove.GetDeviceID() + ")");
            }

        }

        /// <summary> Has wrist tracking been set up correctly? </summary>
        private void EvaluateSGWristTracking()
        {
            //Hey maybe you're missing one glove.
            // if this is a Vive Focus 3 ... what gives?

            // Part 1: Check Scene Setup
            CheckSceneTrackingLinks();

            //Check OPENXR
            if (SG_XR_Devices.GetTrackingPluginType() == SG_XR_Devices.TrackingPluginType.OpenXR)
            {
                ErrorsDetected.Add("It seems you are using OpenXR as your Tracking Provider. While you are free to do so, this plugin does not play well with Auto Detection of Tracking Hardware, and may change the tracking reference of your controller!");
            }

            // Part 2: if UnityXR; check valid Devices when using Unity XR, check if transforms are even moving if using GameObjects.
            CheckTrackingObjects();

            CheckOffsetCompatibility();

            CheckTrackedHands();
        }

        /// <summary> Check the validity of the SG_trackedHands. </summary>
        protected void CheckTrackedHands()
        {
            for (int i=0; i< trackedHands.Length; i++)
            {
                if (trackedHands[i].overrideWristLocation)
                {
                    WarningsDetected.Add(trackedHands[i].name + " has overrideWristLocation set to true. It will not move unless its GameObject does. Make sure this is intentional.");
                }
            }
        }

        /// <summary> Did they link the Scene(s) correctly based on their chosen XR settings?> </summary>
        protected void CheckSceneTrackingLinks()
        {
            GlobalWristTracking trackingMethod = SG_Core.Settings.WristTrackingMethod;
            SG_XR_SceneTrackingLinks sceneLinks = SG_XR_SceneTrackingLinks.CurrentSceneLinks;

            if (sceneLinks == null)
            {
                if (trackingMethod == GlobalWristTracking.UnityXR)
                {
                    ErrorsDetected.Add("SCENE ERROR: There is no SG_XR_SceneTrackingLinks component included in this scene! This component tells us where to locate the XR Rig. "
                        + " Without this component, your hands will only move around the Scene's origin!");
                }
                else if (trackingMethod == GlobalWristTracking.UseGameObject)
                {
                    ErrorsDetected.Add("SCENE ERROR: There is no SG_XR_SceneTrackingLinks component included in this scene! This component tells us where to locate the left- and right hand's tracking reference. "
                        + " Without this component, we won't know which GameObjects in the Scene are to be used for wrist tracking!");
                }
            }
            else if (trackingMethod == GlobalWristTracking.UnityXR && sceneLinks.xrRig == null)
            {
                ErrorsDetected.Add("SCENE ERROR: You have not assigned an xrRig component to the SG_XR_SceneTrackingLinks included in this scene! "
                        + " Without this field assigned, your hands will only move around the Scene's origin!");
            }
            else if (trackingMethod == GlobalWristTracking.UseGameObject && (sceneLinks.leftHandTrackingDevice == null || sceneLinks.rightHandTrackingDevice == null))
            {
                ErrorsDetected.Add("SCENE ERROR: You have not assigned a leftHandTrackingDevice and/or rightHandTrackingDevice component to the SG_XR_SceneTrackingLinks included in this scene! "
                        + " Without these fields assigned, we won't know which GameObjects in the Scene are to be used for wrist tracking!");
            }
            else
            {
                Logs.Add("This scene's tracking objects are linked correctly via SG_XR_SceneTrackingLinks.");
            }
        }



        protected void CheckTrackingObjects()
        {
            GlobalWristTracking trackingMethod = SG_Core.Settings.WristTrackingMethod;
            SG_XR_SceneTrackingLinks sceneLinks = SG_XR_SceneTrackingLinks.CurrentSceneLinks;

            Vector3 leftTrackedPos = Vector3.zero, rightTrackedPos = Vector3.zero;
            bool leftReferenceFound, rightReferenceFound;
            string leftErrMsg, rightErrMsg;

            if (trackingMethod == GlobalWristTracking.UseGameObject)
            {
                //Here, I should report if the object moves like, at all! We'll have already reported if the link isn't made...?
                leftReferenceFound = sceneLinks.leftHandTrackingDevice != null;
                leftTrackedPos = sceneLinks.leftHandTrackingDevice != null ? sceneLinks.leftHandTrackingDevice.position : Vector3.zero;

                rightReferenceFound = sceneLinks.rightHandTrackingDevice != null;
                rightTrackedPos = sceneLinks.rightHandTrackingDevice != null ? sceneLinks.rightHandTrackingDevice.position : Vector3.zero;

                leftErrMsg  = "WRIST GAMEOBJECT ERROR: The GameObject linked to the Left Hand does not seem to be moving. It is your responsibility to ensure that it does!";
                rightErrMsg = "WRIST GAMEOBJECT ERROR: The GameObject linked to the Right Hand does not seem to be moving.  It is your responsibility to ensure that it does!";
            }
            else if (trackingMethod == GlobalWristTracking.UnityXR)
            {
                //While here, I don't want to report if I don't have a Refernce at all.

                //if true, there is such a device available inside the 
                rightReferenceFound = SG_XR_Devices.GetTrackingDeviceLocation_InPlayArea(true, out rightTrackedPos, out Quaternion qR);
                leftReferenceFound = SG_XR_Devices.GetTrackingDeviceLocation_InPlayArea(true, out leftTrackedPos, out Quaternion qL);
                if (rightReferenceFound && leftReferenceFound)
                {
                    Logs.Add("Both hands can be tracked via UnityEngine.XR.InputDevice.");
                }
                else if (rightReferenceFound)
                {
                    ErrorsDetected.Add("UNITY.XR ERROR: Could not find a UnityEngine.XR.InputDevice to map to the left hand! Only the right hand can move...\n" + SG_XR_Devices.ListXRDevices());
                }
                else if (leftReferenceFound)
                {
                    ErrorsDetected.Add("UNITY.XR ERROR: Could not find a UnityEngine.XR.InputDevice to map to the right hand! Only the left hand can move...\n" + SG_XR_Devices.ListXRDevices());
                }
                else //not a single tracking reference found... Could be you don't have the correct plugin?
                {
                    ErrorsDetected.Add("UNITY.XR ERROR: Could not find a UnityEngine.XR.InputDevice to map to the right and left hand! Neither hand will move. Ensure you have your XR Plugin set up properly\n" + SG_XR_Devices.ListXRDevices());
                }

                leftErrMsg = "UNITY.XR ERROR: The tracking device for the Left Hand does not seem to be moving. It may be turned off or outside of your tracking area...";
                rightErrMsg = "UNITY.XR ERROR: The tracking device for the Right Hand does not seem to be moving. It may be turned off or outside of your tracking area...";
            }
            else
            {
                ErrorsDetected.Add("No Tracking Object validation written for " + trackingMethod.ToString() + "!");
                leftReferenceFound = false; rightReferenceFound = false;
                leftErrMsg = "";
                rightErrMsg = "";
            }


            leftWristRef.Update(leftTrackedPos);
            if (leftReferenceFound && !leftWristRef.ActiveAndMoving)
                WarningsDetected.Add(leftErrMsg);
            else if (!leftReferenceFound)
                leftWristRef.Reset();

            rightWristRef.Update(rightTrackedPos);
            if (rightReferenceFound && !rightWristRef.ActiveAndMoving)
                WarningsDetected.Add(rightErrMsg);
            else if (!rightReferenceFound)
                rightWristRef.Reset();
        }


        /// <summary> Check Whether or not the current device(s) are compatible with the chosen offsets, Hardware Wise </summary>
        /// TODO; Visualize Tracker Mounts and controllers. "Your device should look like this"
        protected void CheckOffsetCompatibility()
        {
            //Part 3: Check Wrist Offsets. + If your glove is Compatible?
            TrackingHardware userSelectedHardWare = SG_Core.Settings.GlobalWristTrackingOffsets;
            if (userSelectedHardWare == TrackingHardware.Unknown)
            {
                Logs.Add("You have chosen to use Unknown a.k.a. No Tracking Offsets. Instead, we will use the GameObject(s) assigned by SG_XR_SceneTrackingLinks to directly determine the wrist location.");
            }
            else if (userSelectedHardWare == TrackingHardware.Custom)
            {
                Logs.Add("You have chosen to use Custom Wrist Tracking offsets. In this case, it is up to you, as a developer, to make sure these offsets are correct!");
            }
            else
            {
                // We're not using Custom Stuff. So warn if they've selected a specific one, and report if they've chosen AutoDetect

                TrackingHardware determinedHW = userSelectedHardWare; //store this for later
                if (userSelectedHardWare != TrackingHardware.AutoDetect) //using anything other than Auto Detect
                {
                    Logs.Add("You have chosen to use " + userSelectedHardWare.ToString() + " as you TrackingHardware in the SenseGlove Settings. Please ensure this is correct when building your demo." +
                        " You can even set these automatically in any Build Scripts.");
                }
                else
                {
                    WarningsDetected.Add("AUTODETECT WARNING: You have chosen to use our built-in detection of Tracking Devices. Please note that this method is not always guaranteed to work, " +
                        "especially as new devices are added and device names are altered between Unity and OS Versions.");

                    determinedHW = SG_XR_Devices.GetDeterminedTrackingHardware();
                    if (determinedHW == TrackingHardware.Unknown)
                    {
                        WarningsDetected.Add("AUTODETECT WARNING: Based on the devices currently connected to your system, we can NOT (yet) determine which tracking hardware to use. It would be better if you specified which one to use:\n"
                            + SG_XR_Devices.ListXRDevices());
                    }
                    else
                    {
                        WarningsDetected.Add("AUTODETECT WARNING: Based on the devices currently connected to your system, we have determined that your Tracking is based on: " + determinedHW + ". If that does not match your expectations," +
                            " please change your GlobalWristTrackingOffsets in the SenseGlove Settings to your specific tracking device:\n" + SG_XR_Devices.ListXRDevices());
                    }
                }

                // Once we have deterined the Tracking Hardware, check if the current gloves are compatible.
                if (userSelectedHardWare == TrackingHardware.AutoDetect && determinedHW == TrackingHardware.Unknown)
                {
                    WarningsDetected.Add("COMPATIBILITY WARNING: Waiting for the Auto-Detection to kick in before we can check if the glove is compatible!");
                }
                else
                {
                    //TODO: Check if the current device(s) are compatible with Nova Glove(s).
                    SGCore.PosTrackingHardware iHW = SG_Conversions.ToInternalTracking(determinedHW);

                    SGCore.HapticGlove leftGlove, rightGlove, checkedFor = null;
                    GetGloveInstances(out leftGlove, out rightGlove);

                    if (rightGlove == null && leftGlove == null) //no glove(s) detected
                    {
                        WarningsDetected.Add("Can't check for compatibility until a glove connects...");
                    }
                    else
                    {
                        //We're assuming for now that our user(s) have a pair of gloves of the same hardware type.
                        checkedFor = leftGlove != null ? leftGlove : rightGlove; //if both are != null, i don't care which one I grab. If left == null, I just assign right since I check if checkedFor != null later.
                        if (checkedFor != null)
                        {
                            if (checkedFor.IsCompatibleWith(iHW))
                            {
                                Logs.Add(SG_Conversions.GetFriendlyDeviceName(checkedFor.GetDeviceType()) + " has built-in offsets for " + determinedHW.ToString() + " as tracking hardware.");
                            }
                            else
                            {
                                ErrorsDetected.Add(SG_Conversions.GetFriendlyDeviceName(checkedFor.GetDeviceType()) + " has NO built-in offsets for " + determinedHW.ToString() + "! Check if this device is compatible. You may need to update your software.");
                            }
                        }
                    }
                }
            }
        }


        /// <summary> Runs through your Scene to validate the settings there. </summary>
        public void EvaluateProjectAndScene()
        {
            Logs.Clear();
            ErrorsDetected.Clear();
            WarningsDetected.Clear();

            EvaluatePlatformSettings();
            EvaluateSGConnectionSetup();
            EvaluatePermissions();

            EvaluateActiveSGConnections();
            EvaluateSGWristTracking();

            UpdateInstructions();
        }


        //--------------------------------------------------------------------------------------------------------------------------------------
        // Timing and order


        public IEnumerator StartEvaluation(float afterSeconds)
        {
            DebugText = "Evaluating...";
            testing = true;
            leftWristRef = new ObjectTracker(TrackerMinMoveDistance);
            rightWristRef = new ObjectTracker(TrackerMinMoveDistance);
            do
            {
                yield return new WaitForSeconds(afterSeconds);
                EvaluateProjectAndScene();
            }
            while (this.gameObject.activeInHierarchy && testing);
        }


        //--------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Start is called before the first frame update
        void Start()
        {
            trackedHands = GameObject.FindObjectsOfType<SG_TrackedHand>();
            SG_Core.Setup(); //spawns a copy so we run through initialization
            StartCoroutine(StartEvaluation(1.0f));
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDisable()
        {
            testing = false;
        }
    }
}


//--------------------------------------------------------------------------------------------------------------------------------------
// Utility Classes

public class ObjectTracker
{

    public float MinimumMoveDistance { get; set; }

    protected uint samples = 0;
    protected Vector3 lastLockLocation = Vector3.zero;

    public bool ActiveAndMoving { get; protected set; }


    public ObjectTracker(float moveDist)
    {
        samples = 0;
        MinimumMoveDistance = moveDist;
    }

    public void Reset()
    {
        samples = 0;
    }

    public void Update(Vector3 objectPosition)
    {
        Vector3 currentPos = objectPosition;
        if (samples == 0)
        {
            lastLockLocation = currentPos;
        }
        else
        {
            float diff = (currentPos - lastLockLocation).magnitude;
            if (diff > MinimumMoveDistance)
            {
                ActiveAndMoving = true;
                lastLockLocation = currentPos;
            }
        }
        samples++;
    }

}