using UnityEngine;

namespace SG.Util
{

    /// <summary> SenseGlove Android-to-C# Magic. This is just a wrapper to be able to call the proper functions. If you're not a SG developer, and don't want to break the system, leave this class alone.</summary>
    public sealed class SG_IAndroid
    {
        // -------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Magic Unity-Only C# Class that can call functions of a java object </summary>
        private AndroidJavaClass senseComClass;

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary> Location of SGConnect within our Unity Package. </summary>
        private static readonly string clientClassLocation = "com.senseglove.sgconnect.SGConnect";
#endif

        /// Function names declared here in case we ever change them.
        private static readonly string fn_Init = "Init";
        private static readonly string fn_Dispose = "Dispose";
        private static readonly string fn_ScanActive = "ScanningActive";
        private static readonly string fn_ActiveDev = "ActiveDevices";
        private static readonly string fn_DeviceStr = "GetDeviceString";
        private static readonly string fn_SensorStr = "GetSensorString";
        private static readonly string fn_SendHaptics = "WriteHaptics";
        private static readonly string fn_LibVer = "GetLibraryVersion";
        private static readonly string fn_paired = "GetPaired_Serialized";
        private static readonly string fn_ConnStates = "GetConnectionStates";
        private static readonly string fn_retryConnections = "ReleaseIdleConnections";

        /// <summary> Singleton instance - There should only be one of this class. </summary>
        private static SG_IAndroid instance = new SG_IAndroid();


        // -------------------------------------------------------------------------------------------------------------------------
        // Debugging

        /// <summary> If assigned, the AndroidInterface will output any errors / states to this Text UI element </summary>
        private static UnityEngine.UI.Text debugTxt;

        /// <summary> Link a Text Element to this class, allowing it to output debug information </summary>
        /// <param name="textElement"></param>
        public static void LinkDebugger(UnityEngine.UI.Text textElement)
        {
            debugTxt = textElement;
            debugTxt.text = "Linked Android Wrapper to this element";
        }


        /// <summary> Internal: Log to either the debugText or Console. </summary>
        /// <param name="message"></param>
        private static void Log(string message)
        {
            if (debugTxt != null)
            {
                debugTxt.text = message;
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log(message);
            }
#endif
        }

        // -------------------------------------------------------------------------------------------------------------------------
        // Construction

        /// <summary> Default constructor is private for instance </summary>
        private SG_IAndroid() { }

        /// <summary> Destructor to ensure our Instance is cleaned up when the process terminated. </summary>
        ~SG_IAndroid()
        {
            DisposeLink();
        }


        // -------------------------------------------------------------------------------------------------------------------------
        // Unity <> Android Magic


        /// <summary> Generic function to call any function with various inputs / return values. </summary>
        /// <param name="funcName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static bool CallFunctionGet<T>(string funcName, out T res, object[] parameters = null)
        {
            //#if !UNITY_EDITOR && UNITY_ANDROID
            if (SG_IAndroid.instance.senseComClass != null)
            {
                if (parameters == null)
                {
                    parameters = new object[] { };
                }
                try
                {
                    Log("Attempting to call " + funcName);
                    res = instance.senseComClass.CallStatic<T>(funcName, parameters);
                    Log("Done calling " + funcName);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SG_IAndroid.Log(ex.Message + "\r\n\r\n" + ex.StackTrace);
                }
            }
            //#endif
            res = default(T);
            return false;
        }

        private static bool CallFunctionVoid(string funcName, object[] parameters = null)
        {
            if (SG_IAndroid.instance.senseComClass != null)
            {
                if (parameters == null)
                {
                    parameters = new object[] { };
                }
                try
                {
                    //Log("Attempting to call " + funcName);
                    instance.senseComClass.CallStatic(funcName, parameters);
                    //Log("Done calling " + funcName);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SG_IAndroid.Log(ex.Message + "\r\n\r\n" + ex.StackTrace);
                }
            }
            return false;
        }


        // -------------------------------------------------------------------------------------------------------------------------
        // Setup Unity <> Android

        /// <summary> Returns true if we can call Android functions from this script. </summary>
        public static bool AndroidLinked
        {
            get { return instance.senseComClass != null; }
        }

        /// <summary> Links the Android Library to our Unity Process. Only works on Android builds, otherwise it throws an error. Once called, you should call Dispose when your process terminates. </summary>
        /// <returns></returns>
        public static bool SetupLink()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (instance.senseComClass == null)
            {
                try
                {
                    instance.senseComClass = new AndroidJavaClass(clientClassLocation);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SG_IAndroid.Log(ex.Message + "\r\n\r\n" + ex.StackTrace);
                }
            }
#endif
            return false;
        }


        /// <summary> Unlinks this Unity Process from the Android Library and clear up any resources. </summary>
        /// <returns></returns>
        public static bool DisposeLink()
        {
            if (instance.senseComClass != null)
            {
                //TODO: Also dispose ?
                instance.senseComClass.Dispose();
                instance.senseComClass = null;
                return true;
            }
            return false;
        }




        // -------------------------------------------------------------------------------------------------------------------------
        // Exposed Android Functions


        /// <summary> Initializes the SGConnect Android library </summary>
        /// <returns> The Init Code as returned by the Android Library. If > 0, it succeeded! </returns>
        public static int Andr_Init()
        {
            int code = 666;
            CallFunctionGet<int>(fn_Init, out code);
            return code;
        }


        /// <summary> Disposes of the SGConnect Android library. </summary>
        /// <returns> The Dispose Code as returned by the Android Library. If > 0, it succeeded! </returns>
        public static int Andr_Dispose()
        {
            int code = 666;
            CallFunctionGet<int>(fn_Dispose, out code);
            return code;
        }


        /// <summary> Retruns true if the library is linked and activated. </summary>
        /// <returns></returns>
        public static bool Andr_ScanningActive()
        {
            bool scanActive;
            CallFunctionGet<bool>(fn_ScanActive, out scanActive);
            return scanActive;
        }



        /// <summary> Returns the amount of Devices detected by the Android Library </summary>
        /// <param name="activeDevices"></param>
        /// <returns></returns>
        public static bool Andr_ActiveDevices(out int activeDevices)
        {
            bool successfulCall = CallFunctionGet<int>(fn_ActiveDev, out activeDevices);
            return successfulCall;
        }


        /// <summary> Returns a string showing the Android library version </summary>
        /// <param name="librString"></param>
        /// <returns>"SGConnect Android Library vX.Y.Z"</returns>
        public static bool Andr_GetLibraryVersion(out string librString)
        {
            bool successfulCall = CallFunctionGet<string>(fn_LibVer, out librString);
            return successfulCall;
        }

        /// <summary> Returns the unprocessed Device string of the device at deviceIndex within the SGConnect Android Library. </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="sensorData"></param>
        /// <returns></returns>
        public static bool Andr_GetDeviceString(int deviceIndex, out string sensorData)
        {
            bool successfulCall = CallFunctionGet<string>(fn_DeviceStr, out sensorData, new object[] { deviceIndex });
            return successfulCall;
        }

        /// <summary> Returns the unporcessed Sensor string of the device at deviceIndex within the SGConnect Android Library. </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="sensorData"></param>
        /// <returns></returns>
        public static bool Andr_GetSensorData(int deviceIndex, out string sensorData)
        {
            bool successfulCall = CallFunctionGet<string>(fn_SensorStr, out sensorData, new object[] { deviceIndex });
            return successfulCall;
        }

        /// <summary> Write a Haptic Command to the device at deviceIndex within the SGConnect Android Library. </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static bool Andr_WriteHaptics(int deviceIndex, int channel, string cmd)
        {
            int sendRes;
            bool successfulCall = CallFunctionGet<int>(fn_SendHaptics, out sendRes, new object[] { deviceIndex, channel, cmd });
            return successfulCall;
        }


        /// <summary> Retruns a string containin the names of all paired SenseGlove devices on this Android Device, delimited by the '\n' character. </summary>
        /// <param name="devicesSerialized"></param>
        /// <returns></returns>
        public static bool Andr_GetPairedDevices(out string devicesSerialized)
        {
            bool successfulCall = CallFunctionGet<string>(fn_paired, out devicesSerialized);
            return successfulCall;
        }

        /// <summary> Retrieve a string containing serialzied Connection States. </summary>
        /// <param name="statesSerialized"></param>
        /// <returns></returns>
        public static bool Andr_GetConnectionStates(out string statesSerialized)
        {
            bool successfulCall = CallFunctionGet<string>(fn_ConnStates, out statesSerialized);
            return successfulCall;
        }

        public static bool Andr_RetryConnections()
        {
            bool succesfullCall = CallFunctionVoid(fn_retryConnections);
            return succesfullCall;
        }



    }

}