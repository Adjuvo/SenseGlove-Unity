using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A utility class you can attach to a SenseGlove_Object to call its functions using the keyboard. </summary>
[RequireComponent(typeof(SenseGlove_Object))] //must be connected to a GameObject containing a SenseGlove_Object
public class SenseGlove_KeyBinds : MonoBehaviour
{
    /// <summary> The SenseGlove_Object that is controlled by this script. </summary>
    [Header("Objects")]
    private SenseGlove_Object senseGlove;
    /// <summary> (Optional) grabscript that is attacked to the senseGlove. </summary>
    private SenseGlove_PhysGrab grabScript;

    /// <summary> The key used to align the wrist with the SenseGlove's foreArm. </summary>
    [Header("Keybinds")]
    [Tooltip("The key used to align the wrist with the SenseGlove's foreArm.")]
    public KeyCode calibrateWristKey = KeyCode.P;

    /// <summary> Using the index finger as a 'feeler', calibrate the thumb CMC joint and finger length </summary>
    [Tooltip("The key used to calibrate the thumb, using the index link as a 'feeler'.")]
    public KeyCode calibrateThumb = KeyCode.T;

    /// <summary> The key to start a new Semi-Automatic finger Calibration. </summary>
    [Tooltip("The key to enter the next step of the finger calibration.")]
    public KeyCode calibrateFingersKey = KeyCode.LeftShift;
    
    /// <summary> The keys to cancel and reset the calibration of the fingers. </summary>
    [Tooltip("The key to cancel and reset the calibration of the fingers.")]
    public KeyCode cancelCalibrationKey = KeyCode.Escape;

    /// <summary> The key to enter the next step of the finger calibration. </summary>
    [Tooltip("The key to enter the next step of the finger calibration.")]
    public KeyCode manualCalibrationKey = KeyCode.RightShift;

    /// <summary> The key which forces the grabScript to drop any object it is currently holding. </summary>
    [Tooltip("The key which forces the grabScript to drop any object it is currently holding.")]
    public KeyCode releaseObjectKey = KeyCode.E;

    /// <summary> The time in seconds before the SenseGlove_Grabscript can pick up another object again. </summary>
    [Header("Settings")]
    [Tooltip("The time in seconds before the SenseGlove_Grabscript can pick up another object again. ")]
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
                this.senseGlove.StartCalibration();
            }

            ///////

            if (Input.GetKeyDown(this.manualCalibrationKey))
            {
                this.senseGlove.NextCalibrationStep(this.calibrateFingersKey);
            }

            ////////

            if (Input.GetKeyDown(this.cancelCalibrationKey))
            {
                this.senseGlove.CancelCalibration();
            }

            /////////

            if (Input.GetKeyDown(this.calibrateThumb))
            {
                this.senseGlove.CalibrateThumb();
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
