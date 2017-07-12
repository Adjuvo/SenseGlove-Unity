using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SenseGlove_Object))] //must be connected to a GameObject containing a SenseGlove_Object
public class SenseGlove_KeyBinds : MonoBehaviour
{
    [Header("Objects")]

    private SenseGlove_Object senseGlove;
    private SenseGlove_PhysGrab grabScript;

    [Header("Keybinds")]

    public KeyCode calibrateWristKey = KeyCode.P;

    public KeyCode calibrateFingersKey = KeyCode.RightShift;
    public KeyCode cancelCalibrationKey = KeyCode.Escape;

    public KeyCode releaseObjectKey = KeyCode.E;

    [Header("Settings")]
    public float releaseTimeOut = 1;

    // Use this for initialization
    void Start ()
    {
        this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        this.grabScript = this.gameObject.GetComponent<SenseGlove_PhysGrab>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (this.senseGlove != null)
        {
            if (Input.GetKeyDown(this.calibrateWristKey))
            {
                this.senseGlove.CalibrateWrist();
            }

            ///////

            if (Input.GetKeyDown(this.calibrateFingersKey))
            {
                this.senseGlove.NextCalibrationStep(this.calibrateFingersKey);
            }

            ////////

            if (Input.GetKeyDown(this.cancelCalibrationKey))
            {
                this.senseGlove.CancelCalibration();
            }
        }	

        if (grabScript != null)
        {
            if (Input.GetKeyDown(this.releaseObjectKey))
            {
                this.grabScript.ManualRelease(this.releaseTimeOut);
            }
        }
        
	}

    void OnApplicationQuit()
    {
        if (this.senseGlove != null)
        {
            this.senseGlove.CancelCalibration();
        }
    }

}
