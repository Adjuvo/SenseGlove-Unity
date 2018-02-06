using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> 
/// Utility Script that allows access to the internal debugger of the SenseGloveCs Library,
/// and controls debug messages from the SenseGlove SDK specifically.
/// </summary>
public class SenseGlove_Debugger : MonoBehaviour
{
    /// <summary> The level of debug messages that one will recieve from the DLL. </summary>
    [Tooltip("The level of debug messages that one will recieve from the DLL. <")]
    public DebugLevel debugLevel = SenseGloveCs.Debugger.defaultDebugLvl;

    /// <summary> Enables or disables debug messages from the SenseGloveCs.dll. </summary>
    [Tooltip("Enables or disables debug messages from the SenseGloveCs.dll.")]
    public bool dllEnabled = true;

    /// <summary> Enables or disables debug messages from the Unity SDK scripts. </summary>
    [Tooltip("Enables or disables debug messages from the Unity SDK scripts.")]
    public bool unityEnabled = true;

    /// <summary> Copies the unityEnabled boolean so it works in a static method. </summary>
    /// <remarks> 
    /// Becomes troublesome if you're using multiple SenseGlove_Debugger scripts.
    /// Still, I would like to be able to control my debug messages via the inspector.
    /// </remarks>
    private static bool unityEnabled_S = true;

	// Ensure the Debugger is active before the Start() functions are called.
	void Awake()
    {
        Debugger.storeMessages = true;
        SenseGlove_Debugger.unityEnabled_S = this.unityEnabled;
        SenseGloveCs.Debugger.debugLevel = this.debugLevel;
	}
	
    void LateUpdate()
    {
        string[] lastMessages = Debugger.GetMessages(); //Always retrieve the latest messages, which will clear the buffer in the DLL.
        if (dllEnabled)
        {
            foreach (string msg in lastMessages)
            {
                Debug.Log(msg);
            }
        }

        //update the static variable.
        if (SenseGlove_Debugger.unityEnabled_S != this.unityEnabled)
        {
            SenseGlove_Debugger.unityEnabled_S = this.unityEnabled;
        }

        //Update the internal debug level
        if (SenseGloveCs.Debugger.debugLevel != this.debugLevel)
        {
            SenseGloveCs.Debugger.debugLevel = this.debugLevel;
        }

    }

    /// <summary>  Write a message to the SenseGlove_Debugger. </summary>
    /// <param name="message"></param>
    public static void Log(string message)
    {
        if (SenseGlove_Debugger.unityEnabled_S)
        {
            Debug.Log(message);
        }
    }

}
