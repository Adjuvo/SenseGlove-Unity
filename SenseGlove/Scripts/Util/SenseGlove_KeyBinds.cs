using UnityEngine;
using SenseGloveCs.Kinematics;

/// <summary> A utility class you can attach to a SenseGlove_Object to call its functions using the keyboard. </summary>
[RequireComponent(typeof(SenseGlove_Object))] //must be connected to a GameObject containing a SenseGlove_Object
public class SenseGlove_KeyBinds : MonoBehaviour
{
    //---------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    /// <summary> The SenseGlove_Object that is controlled by this script. </summary>
    private SenseGlove_Object senseGlove;

    /// <summary> The SenseGlove_HandModel connected to this same Object </summary>
    private SenseGlove_HandModel model;

    /// <summary> (Optional) grabscript that is attached to the senseGlove. </summary>
    private SenseGlove_GrabScript grabScript;

    /// <summary> The key used to align the wrist with the SenseGlove's foreArm. </summary>
    [Header("Keybinds")]
    [Tooltip("The key used to align the wrist with the SenseGlove's foreArm.")]
    public KeyCode calibrateWristKey = KeyCode.P;

    /// <summary> The key to start a new Semi-Automatic finger Calibration. </summary>
    [Tooltip("The key to enter the next step of the finger calibration. If calibration is set to manual, use this same key to store the next datapoint.")]
    public KeyCode calibrateHandKey = KeyCode.LeftShift;
    
    /// <summary> The keys to cancel and reset the calibration of the fingers. </summary>
    [Tooltip("The key to cancel and reset the calibration of the fingers.")]
    public KeyCode cancelCalibrationKey = KeyCode.Escape;
    
    /// <summary> The Key to reset the finger lengths back to their original sizes. </summary>
    [Tooltip("The Key to reset the finger lengths back to their original sizes.")]
    public KeyCode resetModelKey = KeyCode.I;

    /// <summary> The key which forces the grabScript to drop any object it is currently holding. </summary>
    [Tooltip("The key which forces the grabScript to drop any object it is currently holding.")]
    public KeyCode releaseObjectKey = KeyCode.E;

    /// <summary> The time in seconds before the SenseGlove_Grabscript can pick up another object again. </summary>
    [Header("Settings")]
    [Tooltip("The time in seconds before the SenseGlove_Grabscript can pick up another object again. ")]
    public float releaseTimeOut = 1;

    /// <summary> Which variable to calibrate when the CalibrateFingersKey is pressed. </summary>
    [Tooltip("Which variable to calibrate when the CalibrateFingersKey is pressed.")]
    public CalibrateVariable variableToCalibrate = CalibrateVariable.FingerVariables;

    /// <summary> How to collect the snapshots required to calibrate the chosen variable. </summary>
    [Tooltip("How to collect the snapshots required to calibrate the chosen variable.")]
    public CollectionMethod collectionMethod = CollectionMethod.SemiAutomatic;

    //Protected
    
    /// <summary> Whether or not the Calibration has been initiated from this script. Used to prevent canceling external calibration. </summary>
    protected bool initiated = false;

    #endregion Properties

    //---------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    void Start ()
    {
        this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        if (this.senseGlove != null)
        {
            this.senseGlove.CalibrationFinished += SenseGlove_OnCalibrationFinished;
        }
        this.grabScript = this.gameObject.GetComponent<SenseGlove_GrabScript>();
        this.model = this.senseGlove.GetComponent<SenseGlove_HandModel>();
    }

    /// <summary> Resets the initation from this script when my calibration completes. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_OnCalibrationFinished(object source, GloveCalibrationArgs args)
    {
        this.initiated = false;
    }

    // Update is called once per frame
    void Update ()
    {
        if (this.senseGlove != null)
        {
            if (Input.GetKeyDown(this.calibrateWristKey))
            {
                //this.senseGlove.CalibrateWrist();
                this.model.CalibrateWrist();
            }

            ///////

            if (Input.GetKeyDown(this.calibrateHandKey))
            {
                if (this.collectionMethod == CollectionMethod.Manual)
                {
                    if (!this.senseGlove.IsCalibrating)
                    {
                        this.initiated = true;
                        this.senseGlove.StartCalibration(this.variableToCalibrate, this.collectionMethod);
                    }
                    else if (this.initiated) //its already running, and these keybinds initiated it.
                    {
                        this.senseGlove.NextCalibrationStep();
                    }
                    else //its running, and its not a calibration from us.
                    {
                        SenseGlove_Debugger.Log("Could not start calibration because another one is running.");
                    }
                }
                else if (!this.senseGlove.IsCalibrating)
                {
                    //Check if the Sense Glove is Calibrating yet.
                    this.initiated = true;
                    this.senseGlove.StartCalibration(this.variableToCalibrate, this.collectionMethod);
                }
                else
                {
                    SenseGlove_Debugger.Log("Could not start calibration because another one is running.");
                }
            }

            ////////

            if (Input.GetKeyDown(this.cancelCalibrationKey))
            {
                this.senseGlove.CancelCalibration();
                this.initiated = false;
            }

            /////////

            if (Input.GetKeyDown(this.resetModelKey))
            {
                SenseGlove_Debugger.Log("Reset Hand Model");
                this.senseGlove.ResetKinematics();
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

    #endregion Monobehaviour

}
