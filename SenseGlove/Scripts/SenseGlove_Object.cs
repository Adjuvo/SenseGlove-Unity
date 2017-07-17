/*
 * 
 * 
 * 
 * 
 */

using UnityEngine;

using SenseGloveCs;
using System;

/// <summary>
/// A SenseGlove object with Unity Wrappers and other fun stuff! Used by applications that depend on the SenseGlove.
/// This Script is responsible for ensuring a stable connection between Unity and the SenseGlove using the SenseGloveCs DLL.
/// </summary>
public class SenseGlove_Object : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Publicly visible attributes

    /// <summary>Determines if this SenseGlove_Object will attempt to connect on startup, or wait until the RetryConnection method is called.</summary>
    [Header("Communication Settings")]
    [Tooltip("Determines if this SenseGlove_Object will attempt to connect on startup, or wait until the RetryConnection method is called.")]
    public bool connectOnStartUp = true;
    
    /// <summary> The logic used to connect to this SenseGlove. </summary>
    [Tooltip("The logic used to connect to this SenseGlove.")]
    public ConnectionMethod connectionMethod = ConnectionMethod.FindNextGlove;

    /// <summary> The address of this glove (as found in the Device Manager) to which this SenseGlove is connected. </summary>
    [Tooltip("The address of this glove (as found in the Device Manager) to which this SenseGlove is connected.")]
    public string address = "COM3";

    /// <summary> Indicates if this glove is a left or right hand. </summary>
    [Tooltip("Indicates if this glove is a left or right hand. ")]
    public bool rightHand = true;


    /// <summary> Determines up to which level the Kinematics of the glove are Updated. </summary>
    [Header("Hand Model Settings")]
    [Tooltip("Determines up to which level the Kinematic Model is updated. Setting it to a lower level increases performance.")]
    public Kinematics updateTo = Kinematics.HandPositions;

    /// <summary> Determine wether or not the wrist is updated by the DLL. </summary>
    [Tooltip("Determines whether or not the wrist should be updated.")]
    public bool updateWrist = true;

    /// <summary> Represents the foreArm. USed for calibration & relative movement. </summary>
    [Tooltip("(Optional) The GameObject representing a foreArm. The wrist can be calibrated to align with its X-axis.")]
    public GameObject foreArm;

    /// <summary> Check for gestures within the DLL </summary>
    [Tooltip("NOT YET IMPLEMENTED - Gesture recognition")]
    private bool checkGestures = false;

    //--------------------------------------------------------------------------------------------------------------------------
    // Private attributes

    /// <summary> The SenseGlove object through which we retrieve data and send commands. </summary>
    private SenseGlove glove;
    /// <summary> A 'Briefcase' representing all of the Data that can be obtained from this SenseGlove. </summary>
    private GloveData gloveData;

    private bool calibratedWrist = false;

    /// <summary>  Time that has elapsed since the Setup was called. Can be used to delay the Update Finction by setting it back to 0. </summary>
    private float elapsedTime = 0;

    /// <summary> The time it takes for one out of two steps of the Setup to complete. </summary>
    private static float setupTime = 0.5f;

    /// <summary> Only true during the frame where the setup finishes. </summary>
    private bool gloveReady = false;

    /// <summary> Indicates if Calibration has already started. </summary>
    private bool calibrating = false;
    /// <summary> Indicates which of the calibration steps is currently being performed. </summary>
    private int calSteps = 0;

    /// <summary> Determines if a connection should be made. </summary>
    private bool standBy = false;

    /// <summary> Ensures we send a debug message only once. </summary>
    private bool canReport = true;

    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void GloveReadyEventHandler(object source, System.EventArgs args);
    public event GloveReadyEventHandler OnGloveLoaded;

    protected void GloveLoaded()
    {
        if (OnGloveLoaded != null)
        {
            OnGloveLoaded(this, null);
        }
    }


    public delegate void CalibrationFinishedEventHandler(object source, System.EventArgs args);
    public event CalibrationFinishedEventHandler OnCalibrationFinished;

    protected void CalibrationFinished()
    {
        if (OnCalibrationFinished != null)
        {
            OnCalibrationFinished(this, null);
        }
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Unity / MonoDevelop

    // Use this for initialization
    void Start()
    {
        SenseGloveCs.DeviceScanner.pingTime = 200;
        SenseGloveCs.DeviceScanner.scanDelay = 500;
        if (this.connectOnStartUp)
        {
            RetryConnection();
        }
        else
        {
            this.Disconnect();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!standBy)
        {
            if (this.elapsedTime < SenseGlove_Object.setupTime) { this.elapsedTime += Time.deltaTime; }
            else if (this.glove == null) //No connection yet...
            {
                if (this.connectionMethod == ConnectionMethod.HardCoded)
                {
                    //no glove was ever assigned...
                    this.RetryConnection(); //keep trying!
                }
                else
                {
                    SenseGlove myGlove = ExtractSenseGlove(SenseGloveCs.DeviceScanner.GetDevices());
                    if (myGlove != null) { this.glove = myGlove; } //The glove matches our parameters!
                    else
                    {
                        if (this.canReport)
                        {
                            string message = this.gameObject.name + " looking for SenseGlove...";
                            if (this.connectionMethod == ConnectionMethod.FindNextLeftHand)
                            {
                                message = this.gameObject.name + " looking for left-handed SenseGlove...";
                            }
                            else if (this.connectionMethod == ConnectionMethod.FindNextRightHand)
                            {
                                message = this.gameObject.name + " looking for right-handed SenseGlove...";
                            }
                            SenseGlove_Debugger.Log(message);
                            this.canReport = false;
                        }
                        this.elapsedTime = 0;
                    }
                }
            }
            else if (this.connectionMethod == ConnectionMethod.HardCoded && !this.glove.IsConnected())
            {   //lost connection :(
                this.canReport = true;
                this.RetryConnection(); //keep trying!
            }
            else if (!gloveReady)
            {
                if (this.glove.GetData(false).dataLoaded)
                {
                    bool runSetup = this.gloveData == null; //used to raise event only once!
                    this.gloveData = this.glove.GetData(false); //get the latest data without calculating anything.
                    this.SetupWrist();
                    this.calibratedWrist = false;
                    this.gloveReady = true;
                    if (runSetup)
                    {
                        this.GloveLoaded();  //raise the event!
                    }
                }
            }
            else //glove != null && gloveReady!
            {
                //Update to the latest GloveData.
                Quaternion lowerArm = this.foreArm != null ? this.foreArm.transform.rotation : Quaternion.identity;
                this.gloveData = this.glove.Update(this.updateTo, this.updateWrist, SenseGlove_Util.ToQuaternion(lowerArm), this.checkGestures);

                //Calibrate once more after reconnecting to the glove.
                if (!calibratedWrist)
                {
                    this.CalibrateWrist();
                    this.calibratedWrist = true;
                }

                //Update the public values automatically.
                if (connectionMethod != ConnectionMethod.HardCoded)
                {
                    this.address = glove.communicator.Address();
                }
                this.rightHand = glove.IsRight();
                
            }
        }


    }


    // OnApplicationQuit is called when the game shuts down.
    void OnApplicationQuit()
    {
        this.CancelCalibration();
        if (glove != null)
        {
            glove.Disconnect();
            SenseGlove_Debugger.Log("Disconnected the SenseGlove on " + glove.communicator.Address());
        }
        SenseGloveCs.DeviceScanner.CleanUp();
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Communication methods.

    /// <summary>
    /// Extract a SenseGlove matching the parameters of this SenseGlove_Object from a list retieved by the SenseGloveCs.DeviceScanner.
    /// </summary>
    /// <param name="devices"></param>
    /// <returns></returns>
    private SenseGlove ExtractSenseGlove(SenseGloveCs.IODevice[] devices)
    {
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i] is SenseGlove)
            {
                SenseGlove tempGlove = ((SenseGlove)devices[i]);
                GloveData tempData = tempGlove.GetData(false);
                //SenseGlove_Debugger.Log("Dataloaded = " + tempData.dataLoaded + ". IsUsed = " + SenseGlove_Manager.IsUsed(tempData.deviceID) + ". isRight = " + tempData.isRight);
                if (tempData.dataLoaded && !SenseGlove_Manager.IsUsed(tempData.deviceID))
                {   //the SenseGlove is done loading data AND is not already in memory

                    if ( (this.connectionMethod == ConnectionMethod.FindNextGlove) 
                        || (this.connectionMethod == ConnectionMethod.FindNextLeftHand && !tempData.isRight)
                        || (this.connectionMethod == ConnectionMethod.FindNextRightHand && tempData.isRight) )
                    {
                        return tempGlove;
                    }
                }
            }
        }
        return null;
    }


    /// <summary>
    /// Disconnect the glove and stop updating until the RetryConnection is called.
    /// This allows a developer to change the communication variables before calling the RetryConnection method.
    /// </summary>
    public void Disconnect()
    {
        this.gloveReady = false;
        if (this.gloveData != null)
        {
            SenseGlove_Manager.SetUsed(this.gloveData.deviceID, false);
        }
        this.glove = null; //The DeviceScanner will still keep them, specifically their communicator, in memory.
        this.standBy = true;
    }

    /// <summary> 
    /// Disconnect and retry the connecting to the SenseGlove, 
    /// such as when a different glove is connected or when the (manual) connection is lost. 
    /// </summary>
    public void RetryConnection()
    {
        this.Disconnect();
        if (this.connectionMethod != ConnectionMethod.HardCoded)
        {
            if (!SenseGloveCs.DeviceScanner.IsScanning()) { SenseGloveCs.DeviceScanner.StartScanning(true); }
        }
        else //we're dealing with a custom connection!
        {
            if (canReport)
            {
                SenseGlove_Debugger.Log("Attempting to connect to " + this.address);
            }
            Communicator PCB = null;
            if (this.address.Contains("COM")) //Serial connections
            {
                if (this.address.Length > 4 && this.address.Length < 6) { this.address = "\\\\.\\" + this.address; }
                PCB = new SerialCommunicator(this.address);
            }
            if (PCB != null)
            {
                PCB.Connect();
                if (PCB.IsConnected())
                {
                    this.glove = new SenseGlove(PCB);
                }
                else if (canReport)
                {
                    SenseGlove_Debugger.Log("ERROR: Could not connect to " + this.address);
                    canReport = false;
                }
            }
            else if (canReport)
            {
                Debug.Log("ERROR: " + this.address + " is not a valid address.");
                canReport = false;
            }
        }


        this.elapsedTime = 0;
        this.standBy = false;
    }

    /// <summary> Check if this glove's setup is completed. A.k.a. The glove is loaded. </summary>
    /// <returns></returns>
    public bool GloveReady()
    {
        return this.gloveReady;
    }

    /// <summary>
    /// Check if this glove's setup is completed. A.k.a. The glove is loaded.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Replaced with the GloveReady function.")]
    public bool SetupFinished()
    {
        return this.gloveReady;
    }

    /// <summary> Manually assign IMU Correction for old firmware versions. </summary>
    public void SetupWrist()
    {
        if (this.glove != null && this.glove.gloveData.dataLoaded)
        {
            string ID = this.gloveData.deviceID;
            string[] gloveVersion = this.gloveData.firmwareVersion.Split('.');
            if (gloveVersion[0][0] == 'v') { gloveVersion[0] = gloveVersion[0].Substring(1); } //if there is a v in front of it, remove this.
            int mainVersion = int.Parse(gloveVersion[0]);
            int subVersion = int.Parse(gloveVersion[1]);
            if (mainVersion <= 2 && subVersion <= 19)
            {
                if (ID.Contains("120101"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(Mathf.PI, 0, 0)); //correction for glove 1
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120203"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI / 2.0f)); //correction?
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120307") || ID.Contains("120204") || ID.Contains("120310") || ID.Contains("120309") || ID.Contains("120311") || ID.Contains("120312"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI)); //correction for glove 7 & 4?  
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
            }
        }
    }


    /// <summary> Call the CalibrationFinished event when the calculations within the DLL are finished. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Glove_OnFingerCalibrationFinished(object source, System.EventArgs args)
    {
        this.gloveData = glove.Update(this.updateTo); //retrieve the latest gloveData, which contains the new lengths.
        CalibrationFinished();
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Wrapper methods

    /// <summary>
    /// Retrieve the GloveData from this SenseGlove, with which one can access all kinds of information.
    /// </summary>
    /// <returns></returns>
    public GloveData GetGloveData()
    {
        return this.gloveData;
    }

    /// <summary>
    /// Retrieve the SenseGlove Object, which can be used for calibration or connection purposes.
    /// </summary>
    /// <returns></returns>
    public SenseGlove GetSenseGlove()
    {
        return this.glove;
    }

    /// <summary>
    /// Initializa Calibration of the chosen fingers and the chosen complexity
    /// </summary>
    /// <param name="whichFingers"></param>
    /// <param name="simpleCalibration"></param>
    /// <returns></returns>
    public bool Calibrate(bool[] whichFingers, bool simpleCalibration = true)
    {
        if (glove != null && glove.IsConnected())
        {
            glove.Calibrate(whichFingers, simpleCalibration);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Proceed to the next calibration step of the fingers.
    /// </summary>
    /// <returns></returns>
    private bool NextCalibrationStep()
    {
        if (glove != null && glove.IsConnected())
        {
            glove.NextCalibrationStep();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reset the Calibration of the glove if, for instance, something went wrong.
    /// </summary>
    public void CancelCalibration()
    {
        if (glove != null)
        {
            glove.ResetCalibration();
            SenseGlove_Debugger.Log("Canceled Calibration");
        }
    }


    /// <summary>
    /// Perform the next calibration step of the fingers. If we are not already calibrating, start the calibration!
    /// </summary>
    /// <param name="calibrationKey">The key used to call this method, used for the debug messages.</param>
    public void NextCalibrationStep(KeyCode calibrationKey)
    {
        if (glove != null && gloveReady)
        {

            if (!calibrating)
            {
                this.CancelCalibration();
                this.Calibrate(new bool[] { false, true, true, true, true }, true);
                calSteps = 0;
                // SenseGlove_Debugger.Log("Calibrate is called : " + calSteps);
            }
            else if (calSteps <= 3)
            {
                this.NextCalibrationStep();
                // SenseGlove_Debugger.Log("NextStep is called : " + calSteps);
            }

            if (calSteps == 0)
            {
                calibrating = true;
                SenseGlove_Debugger.Log("Started Calibration. Please stretch your fingers in front of you and press " + calibrationKey.ToString());
                calSteps++;
            }
            else if (calSteps == 1)
            {
                SenseGlove_Debugger.Log("Step 1 completed. Please bend you MCP joint to 45* and press " + calibrationKey.ToString());
                calSteps++;
            }
            else if (calSteps == 2)
            {
                SenseGlove_Debugger.Log("Step 2 completed. Please bend you MCP joint to 90* and press " + calibrationKey.ToString());
                calSteps++;
            }
            else
            {
                SenseGlove_Debugger.Log("Step 3 completed. Calibration has finished. Resizing model.");
                calibrating = false;
                calSteps = 0;
            }
        }

    }

    /// <summary>
    /// Set the finger lengths used but this sense glove as a 5x3 array, 
    /// which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order.
    /// </summary>
    /// <param name="newFingerLengths"></param>
    public void SetFingerLengths(float[][] newFingerLengths)
    {
        if (this.glove != null)
        {
            this.glove.SetHandLengths(newFingerLengths);
        }
    }

    /// <summary>
    /// Retrive the finger lengths used by this SenseGlove. 
    /// Returns a 5x3 array which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order.
    /// Returns an empty array if unsuccesfull.
    /// </summary>
    /// <returns></returns>
    public float[][] GetFingerLengths()
    {
        if (this.glove != null)
        {
            return this.gloveData.handModel.GetFingerLengths();   
        }
        return new float[][] { };
    }
   
    /// <summary>
    /// Get the positions of the starting finger joints, the CMC or MCP joints.
    /// </summary>
    /// <returns></returns>
    public float[][] GetStartJointPositions()
    {
        if (this.glove != null)
        {
            return this.gloveData.handModel.GetJointPositions();
        }
        return new float[][] { };
    }

    /// <summary>
    /// Set the positions of the starting finger joints, the CMC or MCP joints.
    /// </summary>
    /// <returns></returns>
    public void SetStartJointPositions(float[][] positions)
    {
        if (this.glove != null)
        {
            this.glove.SetJointPositions(positions);
        }
    }


    /// <summary>
    /// Calibrate the Wrist, based on the orientatio of the foreArm.
    /// </summary>
    /// <returns></returns>
    public bool CalibrateWrist()
    {
        if (glove != null && glove.IsConnected() && foreArm != null)
        {
            glove.CalibrateWrist(null, SenseGlove_Util.ToQuaternion(this.foreArm.transform.rotation));
            SenseGlove_Debugger.Log("Calibrated Wrist");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set the current lowerArm quaternion as the 'zero'
    /// </summary>
    /// <param name="lowerArm"></param>
    /// <returns></returns>
    public bool CalibrateWrist(Quaternion lowerArm)
    {
        if (glove != null && glove.IsConnected())
        {
            glove.CalibrateWrist(null, SenseGlove_Util.ToQuaternion(lowerArm));
            SenseGlove_Debugger.Log("Calibrated Wrist");
            return true;
        }
        return false;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Haptic Feedback



    /// <summary>
    /// Verify if this SenseGlove has a particular functionality (buzz motors, haptic feedback, ect)
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public bool HasFunction(GloveFunctions function)
    {
        if (this.glove != null)
        {
            return glove.HasFunction(function);
        }
        return false;
    }



}

public enum ConnectionMethod
{
    /// <summary> Connect to the first unconnected SenseGlove on the system. </summary>
    FindNextGlove = 0,
    /// <summary> Connect to the first unconnected Right Handed SenseGlove on the system. </summary>
    FindNextRightHand,
    /// <summary> Connect to the first unconnected Left Handed SenseGlove on the system. </summary>
    FindNextLeftHand,
    /// <summary> Connect to a COM port that may or may not be a SenseGlove. </summary>
    HardCoded
}
