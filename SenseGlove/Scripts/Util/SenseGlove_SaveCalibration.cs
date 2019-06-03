using UnityEngine;

/// <summary> Utility Class to store and reload profiles in between sessions. </summary>
[RequireComponent(typeof(SenseGlove_Object))]
public class SenseGlove_SaveCalibration : MonoBehaviour
{
    /// <summary> KeyCode to reset this hand model back to defaults. </summary>
    public KeyCode resetCalibrationKey = KeyCode.R;

    /// <summary> Key to access the profile within PlayerPrefs. </summary>
    private string profileKey = "";

    /// <summary> Sense Glove connected to this GameObject, used to access all kinds of interactables. </summary>
    private SenseGlove_Object senseGlove;

    /// <summary> Semaphpre flas to prevent calling redundant CalibrationFinished calls. </summary>
    private bool calGuard = false;

    /// <summary> Flag to apply the calibration during the next Update() </summary>
    private bool firstCal = false;

    /// <summary> The last calibration data retrieved from my SenseGlove_Object. Can be retrieved here to store on disk. </summary>
    public string LastCalibrationData { get; private set; }




    /// <summary> Load the last calibration data for the left / right hand and apply it to the Sense Glove object. </summary>
    public void LoadCalibration()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady)
        {
            this.profileKey = (this.senseGlove.IsRight ? "SG_Right" : "SG_Left");
            this.LastCalibrationData = PlayerPrefs.GetString(this.profileKey, "");
            
            this.calGuard = true; //we don't need to save this calibration data
                this.senseGlove.LoadCalibrationData(this.LastCalibrationData);
            this.calGuard = false;
        }
    }

    /// <summary> Retrieve the last calibration data from the SenseGlove_Object. </summary>
    private void GetLastCalibration()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady) 
        {
            this.LastCalibrationData = this.senseGlove.SerializeCalibration(); 
            //SenseGlove_Debugger.Log("Updated Calibration Profile: " + this.LastCalibrationData);
        }
    }


    /// <summary> Fires when the Sense Glove is connected. Ready calibration for the next Update() </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_GloveLoaded(object source, System.EventArgs args)
    {
        this.firstCal = true; //ready to apply profile during the next Update().
    }

    /// <summary> Fires when the sense glove's calibration finishes. Store the new variables. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_CalibrationFinished(object source, GloveCalibrationArgs args)
    {
        if (!calGuard) //LoadCalibration also fires a CalibrationFinished event.
            this.GetLastCalibration();
    }



    // Use this for initialization
    void Start()
    {
        this.LastCalibrationData = "";
        this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        if (this.senseGlove != null) //Redundant check, should never happen.
        {
            this.senseGlove.GloveLoaded += SenseGlove_GloveLoaded;
            this.senseGlove.CalibrationFinished += SenseGlove_CalibrationFinished;
        }
    }

    // Runs every frame
    private void Update()
    {
        if (Input.GetKeyDown(this.resetCalibrationKey))
            this.senseGlove.ResetKinematics();
    }

    // Runs every frame, after Update()
    private void LateUpdate()
    {
        if (this.firstCal) //called in the next Update function so that re-sizeable hand models can be set up first. 
        {
            this.LoadCalibration();
            this.firstCal = false;
        }
    }

    //On closing, store the last calibration data.
    private void OnApplicationQuit()
    {
        if (this.LastCalibrationData.Length > 0 && this.profileKey.Length > 0) //if we have loaded anything.
        {
            PlayerPrefs.SetString(this.profileKey, this.LastCalibrationData);
            //SenseGlove_Debugger.Log("Stored profile " + this.LastCalibrationData);
        }
    }
}
