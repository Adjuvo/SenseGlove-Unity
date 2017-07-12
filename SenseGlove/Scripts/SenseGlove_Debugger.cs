using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> A Utility Script that allows access to the internal debugger of the SenseGloveCs Library. </summary>
public class SenseGlove_Debugger : MonoBehaviour {

    /// <summary> Enables or disables the SenseGlove Debugger. </summary>
    [Tooltip("Enables or disable the SenseGlove Debugger")]
    public bool isEnabled = true;

	// Ensure the Debugger is active before the Start() functions are called.
	void Awake()
    {
        Debugger.storeMessages = true;
	}
	
	// Update is called once per frame
	void Update ()
    {
        string[] lastMessages = Debugger.GetMessages(); //Always retrieve the latest messages, which will clear the buffer in the DLL.
        if (isEnabled)
        {
            foreach (string msg in lastMessages)
            {
                Debug.Log(msg);
            }
        }
	}
}
