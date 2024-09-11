/************************************************************************************
Filename    :   SG_Core.cs
Content     :   Ensures the connection between your Unity App and SenseGlove devices. Holds important references.
Author      :   Max Lammers

Changes to this file may be lost when updating the SenseGlove Plugin
************************************************************************************/

using UnityEngine;
using SG.Util;
using System.Collections.Generic;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR && UNITY_2020_2_OR_NEWER
	using UnityEngine.Android;
#endif

namespace SG
{
	public enum SenseGloveDevice
	{
		Unknown,
		Nova2,
		Nova1,
		DK1Exoskeleton
	}

    /// <summary> A Core part of the SenseGlove API that exist in the scene. Manages the chosen communication method, and allows access to Settings. </summary>
    public sealed class SG_Core : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Singleton Pattern

        /// <summary> Singleton pattern for the SG_Core Class. You're not supposed to referencing this class anywhere. Use GetInstance() instead. </summary>
        private static SG_Core _instance = null;

        /// <summary> Returns an instance of this class </summary>
        /// <returns></returns>
        public static SG_Core GetInstance()
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SG_Core>();
                if (Application.isPlaying) //only create a new instance of the SG_Core if the application is playing.
                {
                    if (_instance == null)
                    {
                        GameObject sgObj = new GameObject("[SenseGlove_Core]");
                        _instance = sgObj.AddComponent<SG_Core>();
                    }
                    DontDestroyOnLoad(_instance);
                }
                if (_instance == null) //Somehow, we're still NULL?
                {
                    throw new System.NotSupportedException("You're not supposed to be calling this function from the editor!");
                }
            }
            return _instance;
        }


        /// <summary> Container for SenseGlove Settings that _should_ be accessible throughout the API </summary>
        private static SG_UnitySettings _settings = null;

        /// <summary>  </summary>
        /// <returns></returns>
        private static SG_UnitySettings GetSettings()
        {
            if (_settings == null)
            {
                //Let's try to collect if from the Resources folder.
                SG_UnitySettings localSett = Resources.Load<SG_UnitySettings>("SenseGloveSettings");
                if (localSett == null) //it does (not yet) exist.
                {
                    throw new System.MissingFieldException("Could not load the SenseGloveSettings file from Resources (which comes default with the Plugin). Please ensure that the SenseGloveSettings.asset is still inside a Resources folder, or re-import the package.");
                }
                _settings = localSett;
            }
            return _settings;
        }


		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Public Functions


		/// <summary> Ensures an instance of SG_Core is running in the background </summary>
		public static void Setup()
		{
			GetInstance(); //this will create one if it doesn't exist yet..
		}


		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Public Functions


		/// <summary> Access SenseGlove-related Settings </summary>
		public static SG_UnitySettings Settings
		{
			get { return SG_Core.GetSettings(); }
		}





        //----------------------------------------------------------------------------------------------------
        // Member Variables

        //	/// <summary> Putting this variable here for now. Need a way to change that in .ini or Build settings. </summary>
        //	private static bool standaloneMode = false;

        /// <summary> If true, Initialization of communication resources was succesful, so we must dispose of it OnApplicationQuit. </summary>
        private static bool initialized = false;

        /// <summary> a queue of debug messages from SG_Conections, used to show the last few messages from this back-end. </summary>
        private static List<string> msgQueue = new List<string>();
        /// <summary> The maximum number of messages in the queue. </summary>
        private static int maxQueue = 5;
        /// <summary> Optional 3D Text element on which to project the last few messages. </summary>
        private static TextMesh debugElement = null;
        /// <summary> Optional 2D UI element on which to project the last few messages. </summary>
        private static Text debugUIElement = null;

        /// <summary> Whether or not to load profiles on focus </summary>
        private static bool loadProfilesOnFocus = true;

        /// <summary> If set to true, we're also upding ConnectionStates. This is mainly relevant to Android, so it's false by default. </summary>
        public static bool andr_checkConnectionStates = false;

        // Instance Members

        /// <summary> When added into the scene manually, you can assign a 3D text to host any debug messages. </summary>
        public TextMesh debugText;
        /// <summary> When added into the scene manually, you can assign a 2D UI text to host any debug messages. </summary>
        public Text debugUIText;


#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary> The time in between hardware updates to check for new devices. Set slightly faster than the Update Rate of the Android Library. </summary>
        private static readonly float hardwareUpdateTime = 0.5f; //check for new devices every x amount of seconds

        /// <summary> Internal timer to keep track of the next HardwareTick. </summary>
        private float hwTime = 0;

        /// <summary> The number of available SGDevices in the last tick. </summary>
        private static int lastDevices = 0;

        /// <summary> If true, we should unlink the class during OnApplicationQuit </summary>
        private static bool classLinked = false;

        /// <summary> If 1, we initialized the library, and should therefore dispose during OnApplicationQuit. </summary>
        private static int libraryInit = -1;



        //----------------------------------------------------------------------------------------------------
        // Data Updates

        /// <summary> Updates deviceStrings only. Since they;re longer and usually don't get updated that often (ever second) </summary>
        private static void Andr_HardwareUpdate()
        {
        	if (SG.Util.SG_IAndroid.Andr_ActiveDevices(out lastDevices))
        	{
        		for (int i = 0; i < lastDevices; i++)
        		{
        			string devString;
        			if (SG.Util.SG_IAndroid.Andr_GetDeviceString(i, out devString))
        			{
        				SGCore.Util.SGConnect_Android.An_PostDeviceString(i, devString);
        			}
        		}
        	}
        }

        /// <summary> Updates Haptics and Sensor Data </summary>
        private static void Andr_FrameUpdate()
        {
        	for (int i = 0; i < lastDevices; i++)
        	{
        		// No longer required as I pass this on whenever I want.
        		//string hapticsToSend;
        		//if (SGCore.Util.SGConnect_Android.An_GetHaptics(i, out hapticsToSend))
        		//{
        		//	SG.Util.SG_IAndroid.Andr_WriteHaptics(i, hapticsToSend);
        		//}
        		string sData;
        		if (SG.Util.SG_IAndroid.Andr_GetSensorData(i, out sData))
        		{
        			SGCore.Util.SGConnect_Android.An_PostSensorData(i, sData);
        		}
        	}
        	if (andr_checkConnectionStates)
            {
        		string serializedStates;
        		if (SG.Util.SG_IAndroid.Andr_GetConnectionStates(out serializedStates))
                {
        			SGCore.Util.SGConnect_Android.An_PostDeviceStates(serializedStates);
        		}
            }
        }
#endif

        /// <summary> If true, an instance of SG_Connections exists within the scene. </summary>
        public static bool ExistsInScene
        {
            get { return _instance != null; }
        }


        //----------------------------------------------------------------------------------------------------
        // Initalization / Disposing of Android Resources.


#if UNITY_ANDROID && !UNITY_EDITOR && UNITY_2020_2_OR_NEWER
        private static void DisplayNoPermissionsError()
        {
            //Spawn an error message that says permission is required for hand tracking functionality
            GameObject errorObject = new GameObject("BluetoothDeniedError");
            TextMesh text = errorObject.AddComponent<TextMesh>();
            //for now hardcoded
            errorObject.transform.SetParent(Camera.main.transform);
            errorObject.transform.localPosition = new Vector3(0, 0, 0.3f);
            errorObject.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);
            text.text = "Permission for nearby devices required for glove functionality";
            text.color = Color.red;
            text.anchor = TextAnchor.MiddleCenter;
        }

        private static void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
        {
            Debug.LogWarning($"{permissionName} PermissionDeniedAndDontAskAgain");
            DisplayNoPermissionsError();
        }

        private static void PermissionCallbacks_PermissionDenied(string permissionName)
        {
        	Debug.LogWarning($"{permissionName} PermissionCallbacks_PermissionDenied");
        	DisplayNoPermissionsError();

        }

        private static void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Debug.LogWarning($"{permissionName} PermissionCallbacks_PermissionGranted");
            Andr_TryInitialize();
        }


#endif

        /// <summary> Initialized the Android Library is we haven't already </summary>
        /// <remarks> This function could be called every time we switch scenes, and thatshouldn't break things. </remarks>
        private static void Andr_TryInitialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_2020_2_OR_NEWER
            if (SG_Core.Settings.ForceBluetoothPermissionsDialog)
            {
        	    //If permission isnt granted, it will ask for permission and return out of this function, if the function is allowed to continue then the application will crash. If permission is granted then this function gets called again
        	    //Otherwise, this function won't be called again and something will be done to show the user that the correct permissions is needed to continue
        	    if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
        	    {
        		    PermissionCallbacks callbacks = new PermissionCallbacks();
        		    callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
        		    callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
        		    callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
        		    Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT", callbacks);
        		    return;
        	    }
            }
#endif
            if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
            {
                andr_checkConnectionStates = true;
        	    if (!classLinked)
        	    {
        		    classLinked = SG_IAndroid.SetupLink();
        		    if (classLinked)
        		    {
        			    Log("Android Bridge Succesful!");
        		    }
        		    else
        		    {
        			    Log("Could not link Android resources. SGConnect.aar might be missing or broken.");
        		    }
        	    }

                SGCore.Util.SGConnect_Android.AndroidHapticEvent += SGConnect_Android_AndroidHapticEvent; //subscribe to the event such that we can send haptics as soon as required.

        	    if (classLinked && libraryInit != 1) //we've not yet initialized the Android library!
        	    {
        		    libraryInit = SG_IAndroid.Andr_Init();
        		    Log("Initialized Android Back-End with code " + libraryInit);
        		    if (libraryInit == 1)
        		    {
        			    SGCore.Util.SGConnect_Android.An_Init(); //let our C# back-end know we succeeded.
        		    }
        	    }

            }
            else
            {
        	    Log("C# Library does not support Android back-end! It supports " + SGCore.Library.GetBackEndType().ToString());
            }
#endif
        }

        /// <summary> Fires when our C# back-end receives a haptic command to send to the device. Pass it on to the Android Side, please. </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void SGConnect_Android_AndroidHapticEvent(object source, SGCore.Util.SGConnect_Android.AndroidHapticEventArgs args)
        {
            Log("Sending Haptics to device" + args.DeviceIndex.ToString() + "_" + args.ChannelIndex.ToString() + ": " + args.HapticCommand);
            SG_IAndroid.Andr_WriteHaptics(args.DeviceIndex, args.ChannelIndex, args.HapticCommand);
        }

        /// <summary> Disposes of the Android Library if we haven't already. </summary>
        private static void Andr_TryDispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        			if (libraryInit == 1) //we did initialize the library!
        			{
        				int disposeCode = SG.Util.SG_IAndroid.Andr_Dispose(); //TDOD: Something with the code
        				SGCore.Util.SGConnect_Android.An_Dispose(); //let our back-end know we've finished.
        				Log("Disposed of Android Back-End with code " + disposeCode);
        			}
        			if (classLinked)
        			{
        				bool unlinked = SG.Util.SG_IAndroid.DisposeLink(); //explicitly remove the link
        				classLinked = false;
        			}
        			SGCore.Util.SGConnect_Android.AndroidHapticEvent -= SGConnect_Android_AndroidHapticEvent; //Unsubscribe from the event.

#endif
        }


        /// <summary> Utility method to log to Unity, but also to write the last few messages to a 3D text in the scene. </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            Debug.Log("[SenseGlove] " + message);

            if (msgQueue.Count + 1 > maxQueue) { msgQueue.RemoveAt(0); }
            msgQueue.Add(message);

            if (debugElement != null || debugUIElement != null)
            {
                string msg = "";
                for (int i = 0; i < msgQueue.Count; i++)
                {
                    msg += msgQueue[i];
                    if (i < msgQueue.Count - 1) { msg += "\r\n"; }
                }
                if (debugElement != null) { debugElement.text = msg; }
                if (debugUIElement != null) { debugUIElement.text = msg; }
            }
        }

        public enum BluetoothPermissionCode  // 0 = no, 1 = yes, 2 = unable to determine. 3 = not required.
        {
            Unknown,
            NoPermission,
            PermissionGranted,
            NoPermissionNeeded,
        }

        /// <summary> Checks whether or not Bluetooth Permissions have been Granted on this (Android) Application. </summary>
        /// <returns></returns>
        public static BluetoothPermissionCode CheckBluetoothPermissions()
        {
			// 0 = no, 1 = yes, 2 = unable to determine. 3 = not required.
#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_2020_2_OR_NEWER
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT") ? BluetoothPermissionCode.PermissionGranted : BluetoothPermissionCode.NoPermission;
#else
            return BluetoothPermissionCode.Unknown;
#endif
#else
			return BluetoothPermissionCode.NoPermissionNeeded;
#endif
        }


        /// <summary> Create a new instance of SG_Connection in the Scene, if we don't have on e yet. </summary>
        //public static void SetupConnections()
        //{
        //    //Log("Ensuring we have some form of connections available to us!");
        //    if (instance == null)
        //    {
        //        GameObject connectionObj = new GameObject("[SG Connections]");
        //        //Log("Created a new instance!");
        //        instance = connectionObj.AddComponent<SG_Core>();
        //    }
        //}

        /// <summary> Initialize the SenseGlove back-end, be it SenseCom or Android Class wrappers </summary>
        private static void Initialize()
        {
            if (!initialized)
            {
                initialized = true;

                if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
                {
                    Log("Initalizing AndroidStrings");
                    Andr_TryInitialize();
                }
                else if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.Sockets)
                {
                    Log("Initalizing Socket Communication");
                    SGCore.DeviceList.Initialize();
                }
                else
                {
                    Log("Initalizing SenseCom");
                    if (!SGCore.SenseCom.IsRunning()) //TODO: Standalone Mode?
                    {
                        if (SGCore.SenseCom.StartupSenseCom())
                        {
                            Log("Started up SenseCom.");
                        }
                        else
                        {
                            Log("SenseCom is not currently running. You won't be able to communicate with your glove(s) until it is running.");
                        }
                    }
                }
            }
            else
            {
                //Log("Already initialized a scene before this one.");
            }
        }

        /// <summary> Dispose of any Android resources. </summary>
        private static void Dispose()
        {
            if (initialized)
            {
                initialized = false;
                if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
                {
                    Andr_TryDispose();
                    Log("Disposed Communications (Standalone modes only)");
                }
                else if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.Sockets)
                {
                    Log("Disposing Socket Communication");
                    SGCore.DeviceList.Dispose();
                }
            }
            else
            {
                //Log("We never initialized, so we don't have to dispose either");
            }
        }


        //----------------------------------------------------------------------------------------------------
        // Monobehaviour

        void Awake()
        {
            if (_instance == null) //when created and in the scene, be sure to add one of these.
            {
                //Log("Linked SenseGlove Connections to " + this.name);
                _instance = this;
                debugElement = this.debugText;
                debugUIElement = this.debugUIText;
                if (SG_Core.Settings.SGCommunications != CommunicationSetup.Disabled)
                {
                    Initialize(); //initialized me!
                }
            }
        }


        void OnDestroy() //called after OnApplicationQuit for some insane reason.
        {
            if (_instance == this)
            {
                //Log("Unlinked SenseGlove Connections from " + this.name);
                debugElement = null;
                debugUIElement = null;
                _instance = null; //clear my refernce to the instance, so another can take its place.
            }
        }


        void OnApplicationQuit() //is called before OnDestory
        {
            if (_instance == this)
            {
                Dispose();
            }
        }

        //Fires when tabbing in / out of this application
        void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            if (hasFocus && loadProfilesOnFocus)
            {
                //Debug.Log("Reloaded SenseGlove Profiles from disk...");
                SGCore.HandLayer.LoadCalibrationFromDisk(); //reload profiles. Done here because this script is always there when using SenseGlove scripts.
            }
#endif
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        	if (classLinked)
        	{
        		hwTime += Time.deltaTime;
        		if (hwTime >= hardwareUpdateTime)
        		{
        			hwTime = 0;
        			Andr_HardwareUpdate();
        		}
        		Andr_FrameUpdate();
        	}
#endif
        }

    }

}