
using UnityEngine; //used for Debug.Log()
using SGCore.Diagnostics; // Used to access Debugger.


namespace SG
{
    /// <summary> 
    /// Utility Script that allows access to the internal debugger of the SenseGloveCs Library, and controls debug messages from the SenseGlove SDK specifically.
    /// </summary>
    public class SG_Debugger : MonoBehaviour
    {
        //----------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> The level of debug messages that one will recieve from the DLL. </summary>
        [Tooltip("The level of debug messages that one will recieve from the DLL.")]
        public DebugLevel DLL_debugLevel = SGCore.Diagnostics.Debugger.defaultDebugLvl;


        /// <summary> Enables or disables debug messages from the Unity SDK scripts. </summary>
        [Tooltip("Enables or disables debug messages from the Unity SDK scripts.")]
        public bool unityEnabled = true;

        /// <summary> Copies the unityEnabled boolean so it works in a static method. </summary>
        /// <remarks> 
        /// Becomes troublesome if you're using multiple SG_Debugger scripts.
        /// Still, I would like to be able to control my debug messages via the inspector.
        /// </remarks>
        private static bool unityEnabled_S = true;


        //----------------------------------------------------------------------------------------------------
        // Class Methods

        /// <summary> Fires when our debugger reports that a new message has been recieved. </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void Instance_DebugMessageRecieved(object source, DebugArgs args)
        {
            Debug.Log(args.message);
        }


        // Static (Unity Related)

        /// <summary>  Write a message to the SG_Debugger. </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            if (SG_Debugger.unityEnabled_S)
            {
                Debug.Log(message);
            }
        }

        /// <summary> Write a message to the SG_Debugger to appear as a warning. </summary>
        /// <param name="message"></param>
        public static void LogWarning(string message)
        {
            if (SG_Debugger.unityEnabled_S)
            {
                Debug.LogWarning(message);
            }
        }

        /// <summary> Write a message to the SG_Debugger to appear as an error. </summary>
        /// <param name="message"></param>
        public static void LogError(string message)
        {
            if (SG_Debugger.unityEnabled_S)
            {
                Debug.LogError(message);
            }
        }

        //----------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Ensure the Debugger is active before the Start() functions are called.
        void Awake()
        {
            SGCore.Diagnostics.Debugger.DebugLevel = this.DLL_debugLevel;
            SGCore.Diagnostics.Debugger.Instance.DebugMessageRecieved += Instance_DebugMessageRecieved;
        }

        //runs after all Update() methods have been called.
        void LateUpdate()
        {
            //update the static variable.
            if (SG_Debugger.unityEnabled_S != this.unityEnabled)
            {
                SG_Debugger.unityEnabled_S = this.unityEnabled;
            }

            //Update the internal debug level
            if (SGCore.Diagnostics.Debugger.DebugLevel != this.DLL_debugLevel)
            {
                SGCore.Diagnostics.Debugger.DebugLevel = this.DLL_debugLevel;
            }
        }

        private void OnDestroy()
        {
            SGCore.Diagnostics.Debugger.Instance.DebugMessageRecieved -= Instance_DebugMessageRecieved; //unsubscribe
        }

        // unsubscribe on quit.
        private void OnApplicationQuit()
        {
            SGCore.Diagnostics.Debugger.Instance.DebugMessageRecieved -= Instance_DebugMessageRecieved; //unsubscribe again
        }

    }
}