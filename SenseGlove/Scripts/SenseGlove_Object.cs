
using UnityEngine;

using SenseGloveCs;
using SenseGloveCs.Calibration;
using System;

/// <summary> 
/// Responsible for ensuring a stable connection between Unity and the SenseGlove using the SenseGloveCs DLL.
/// Contains wrapper functions for motor controls and access to up-to-date SenseGlove_Data
/// </summary>
public class SenseGlove_Object : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Public properties - May be assigned via the inspector

    /// <summary>Determines if this SenseGlove_Object will connect on startup, or wait until the RetryConnection method is called.</summary>
    [Header("Communication Settings")]

    [Tooltip("Determines if this SenseGlove_Object will connect on startup, or wait until the RetryConnection method is called.")]
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
    public UpdateLevel updateTo = UpdateLevel.HandPositions;

    /// <summary> Determine whether or not the wrist is updated by the DLL. </summary>
    [Tooltip("Determines whether or not the wrist should be updated.")]
    public bool updateWrist = true;
    
    /// <summary> Represents the foreArm. USed for calibration & relative movement. </summary>
    [Tooltip("(Optional) The GameObject representing a foreArm. The wrist can be calibrated to align with its X-axis.")]
    public GameObject foreArm;

    /// <summary> The method that is used to solve the kinematics of the Hand, standard set to Inverse Kinematics. </summary>
    [Tooltip("The method that is used to solve the kinematics of the hand, standard set to Inverse Kinematics.")]
    public SolveType solver = SolveType.Kinematic3D;

    /// <summary> Determine whether the fingers are contrained within natural limits. </summary>
    [Tooltip("Determines whether the fingers are contrained within natural limits.")]
    public bool limitFingers = true;

    /// <summary> Determine wheter or not the relative wrist orientation is constained to natural limits. </summary>
    [Tooltip("Determine wheter or not the relative wrist orientation is constained to natural limits. ")]
    public bool limitWrist = true;

    /// <summary> Check for gestures within the DLL </summary>
    [Tooltip("NOT YET IMPLEMENTED - Gesture recognition")]
    private bool checkGestures = false;


    /// <summary> Which algorithm is used to calculate the new finger lengths and/or joint positions. </summary>
    /// <remarks> will be made public once we have more algorithms. </remarks>
    [Tooltip("Which algorithm is used to calculate the new finger lengths and/or joint positions.")]
    private Algorithm calibrationAlgorithm = Algorithm.PointsOnACircle2D;

    /// <summary> The way that new datapoints are gathered for the calibration algorithm. </summary>
    [Header("Calibration Settings")]
    [Tooltip("The way that new datapoints are gathered for the calibration algorithm.")]
    public CalibrationType calibrationMethod = CalibrationType.SemiAutomatic;

    /// <summary> Whether or not the (complex) calucation is performed in a separate worker thread. </summary>
    [Tooltip("Whether or not the (complex) calucation is performed in a separate worker thread.")]
    public bool async = true;



    //--------------------------------------------------------------------------------------------------------------------------
    // Private attributes

    /// <summary> The SenseGlove object through which we retrieve data and send commands. </summary>
    private SenseGlove glove;
    /// <summary> A 'Briefcase' representing all of the Data that can be obtained from this SenseGlove. </summary>
    private GloveData gloveData;

    /// <summary> GloveData converted to Unity variables. </summary>
    private SenseGlove_Data convertedGloveData;

    /// <summary> The last fired calibration arguments. </summary>
    private CalibrationArgs calibrationArguments;

    /// <summary> Used ot fire the calibrationFinsihed event during the next lateUpdate. </summary>
    private bool fireCalibration = false;

    /// <summary> Determines whether or not the wrist has been calibrated on startup. </summary>
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

    /// <summary> Original (hard-coded) lengths of the Sense Glove </summary>
    private float[][] originalLengths;

    /// <summary> Original (hard-coded) joint positions of the Sense Glove </summary>
    private float[][] originalJoints;

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    #region Events

    /// <summary> Event delegate for the GloveReady event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void GloveReadyEventHandler(object source, System.EventArgs args);

    /// <summary> Occurs when the SenseGlove_Object has connected to the Sense Glove, and all glove-related data has been retrieved from the device. </summary>
    public event GloveReadyEventHandler OnGloveLoaded;

    /// <summary> Used to call the OnGloveLoaded event. </summary>
    protected void GloveLoaded()
    {
        if (OnGloveLoaded != null)
        {
            OnGloveLoaded(this, null);
        }
    }

    /// <summary> Event delegate function for the CalibrateionFinished event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void CalibrationFinishedEventHandler(object source, CalibrationArgs args);

    /// <summary> Occurs when the finger calibration is finished. Passes the new finger length and joint positions as arguments. </summary>
    public event CalibrationFinishedEventHandler OnCalibrationFinished;

    /// <summary> Used to call the OnCalibrationFinished event. </summary>
    /// <param name="calibrationArgs"></param>
    protected void CalibrationFinished(CalibrationArgs calibrationArgs)
    {
        if (OnCalibrationFinished != null)
        {
            OnCalibrationFinished(this, calibrationArgs);
        }
    }

    #endregion Events

    //------------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    void Start()
    {
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
                    if (myGlove != null) //The glove matches our parameters!
                    {
                        this.glove = myGlove;
                        SenseGlove_Manager.SetUsed(this.glove.GetData(false).deviceID, true);
                        this.glove.OnFingerCalibrationFinished += Glove_OnFingerCalibrationFinished;
                    } 
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

                    float[][] oldFingerLengths = null;
                    float[][] oldStartPositions = null;
                    if (this.gloveData != null)
                    {
                        oldFingerLengths = this.GetFingerLengths();
                        oldStartPositions = SenseGlove_Util.ToPosition(this.GetStartJointPositions());
                    }
                    
                    this.gloveData = this.glove.GetData(false); //get the latest data without calculating anything.

                    if (oldFingerLengths != null) { this.SetFingerLengths(oldFingerLengths); } //re-apply old fingerlengths, if possible.
                    if (oldStartPositions != null) { this.SetStartJointPositions(oldStartPositions); } //re=apply joint positions, if possible.

                    this.convertedGloveData = new SenseGlove_Data(this.gloveData, this.glove.communicator.samplesPerSecond, 
                        this.glove.TotalCalibrationSteps(), this.glove.TotalCalibrationSteps());
                    this.SetupWrist();
                    this.calibratedWrist = false;
                    this.gloveReady = true;
                    if (runSetup)
                    {
                        this.originalLengths = this.gloveData.handModel.GetFingerLengths();
                        this.originalJoints = this.gloveData.handModel.GetJointPositions();
                        SenseGlove_Debugger.Log("Sense Glove " + this.convertedGloveData.deviceID + " is ready!");
                        this.GloveLoaded();  //raise the event!
                    }
                }
            }
            else //glove != null && gloveReady!
            {
                this.CheckCalibration();

                //Update to the latest GloveData.
                this.UpdateGloveData();

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

    // Called at a fixed rate.
    void FixedUpdate()
    {
        if (this.GloveReady())
        {
            this.CheckCalibration();

            //move the UpdateGloveData() method here to give your SenseGlove a fixed update rate.
            //this.UpdateGloveData();
        }
    }

    // OnApplicationQuit is called when the game shuts down.
    void OnApplicationQuit()
    {
        this.CancelCalibration();
        if (glove != null && glove.IsConnected())
        {
            glove.StopBrakes();
            glove.StopBuzzMotors();
            glove.Disconnect();
            SenseGlove_Debugger.Log("Disconnected the SenseGlove on " + glove.communicator.Address());
        }
        SenseGloveCs.DeviceScanner.CleanUp();
    }

    #endregion Monobehaviour

    //------------------------------------------------------------------------------------------------------------------------------------
    // Communication methods.

    #region Communication

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
        if (glove != null) { this.glove.OnFingerCalibrationFinished -= Glove_OnFingerCalibrationFinished; }
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
            if (!SenseGloveCs.DeviceScanner.IsScanning())
            {
                SenseGloveCs.DeviceScanner.pingTime = 200;
                SenseGloveCs.DeviceScanner.scanDelay = 500;
                SenseGloveCs.DeviceScanner.StartScanning(true);
            }
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
                    this.glove.OnFingerCalibrationFinished += Glove_OnFingerCalibrationFinished;
                }
                else if (canReport)
                {
                    SenseGlove_Debugger.Log("ERROR: Could not connect to " + this.address);
                    canReport = false;
                }
            }
            else if (canReport)
            {
                SenseGlove_Debugger.Log("ERROR: " + this.address + " is not a valid address.");
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

    /// <summary>
    /// Check if this SenseGlove_Object is (still) connected.
    /// </summary>
    /// <returns></returns>
    public bool IsConnected()
    {
        if (glove != null)
        {
            return glove.IsConnected();
        }
        return false;
    }

    #endregion Communication

    //------------------------------------------------------------------------------------------------------------------------------------
    // Data Retrieval

    #region Data

    /// <summary>
    /// Retrieve the UNCONVERTED GloveData from this SenseGlove. Use GetData instead if you wish to access Unity-friendly data.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Returns unconverted data. Use GloveData() instead, which returns a wrapper with more user-friendly Unity (Vector3 and Quaternion) variables.")]
    public GloveData GetGloveData()
    {
        return this.gloveData;
    }

    /// <summary>
    /// Retrieve the GloveData of this SenseGlove, which has been converted to Unity Variables.
    /// </summary>
    /// <returns></returns>
    public SenseGlove_Data GloveData()
    {
        return this.convertedGloveData;
    }

    /// <summary>
    /// Retrieve the internal SenseGlove Object of the DLL, which can be used for calibration or connection purposes.
    /// </summary>
    /// <returns></returns>
    public SenseGlove GetSenseGlove()
    {
        return this.glove;
    }

    /// <summary> Get the lastest (converted and unconverted) glove data from the SenseGlove. </summary>
    private void UpdateGloveData()
    {
        if (this.glove != null && !this.standBy && this.gloveReady)
        {
            //Update to the latest GloveData.
            Quaternion lowerArm = this.foreArm != null ? this.foreArm.transform.rotation : Quaternion.identity;
            this.gloveData = this.glove.Update(this.updateTo, this.solver, this.limitFingers, this.updateWrist, SenseGlove_Util.ToQuaternion(lowerArm), this.limitWrist, this.checkGestures);
            this.convertedGloveData = new SenseGlove_Data(this.gloveData, this.glove.communicator.samplesPerSecond,
                this.glove.TotalCalibrationSteps(), this.glove.CurrentCalibrationStep());
        }
    }

    #endregion Data

    
    //------------------------------------------------------------------------------------------------------------------------------------
    // Manual Calibration methods

    #region Calibration

    /// <summary> Check whether or not this SenseGlove_Object is currently collecting calibration data. </summary>
    /// <returns></returns>
    public bool IsCalibrating()
    {
        return this.calibrating;
    }

    /// <summary> Reset all finger lengths to their original sizes and positions to their original positions. </summary>
    public void ResetFingers()
    {
        float[][] oldLengths = this.GetFingerLengths();
        Vector3[] oldPos = this.GetStartJointPositions();

        this.SetFingerLengths(this.originalLengths);
        this.SetStartJointPositions(this.originalJoints);
        //this.fireCalibration = true;

        this.FireCalibration(oldLengths, this.originalLengths, oldPos, this.GetStartJointPositions());
    }


    /// <summary>
    /// Set the finger lengths used by this sense glove as a 5x3 array, 
    /// which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order.
    /// </summary>
    /// <param name="newFingerLengths"></param>
    public void SetFingerLengths(float[][] newFingerLengths)
    {
        if (this.glove != null && newFingerLengths != null)
        {
            float[][] oldLengths = this.GetFingerLengths();
            this.glove.SetHandLengths(newFingerLengths);
            //this.fireCalibration = true;
            this.FireCalibration(oldLengths, newFingerLengths);
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
        if (this.glove != null && this.GloveReady())
        {
            return this.gloveData.handModel.GetFingerLengths();
        }
        return new float[][] { };
    }

    /// <summary>
    /// Get the positions of the starting finger joints, the CMC or MCP joints.
    /// </summary>
    /// <returns></returns>
    public Vector3[] GetStartJointPositions()
    {
        if (this.glove != null)
        {
            return SenseGlove_Util.ToUnityPosition(this.glove.GetJointPositions());
        }
        return new Vector3[] { };
    }

    /// <summary>
    /// Set the positions of the starting finger joints, the CMC or MCP joints.
    /// </summary>
    /// <returns></returns>
    public void SetStartJointPositions(float[][] positions)
    {
        if (this.glove != null && positions != null)
        {
            Vector3[] oldPositions = this.GetStartJointPositions();
            this.glove.SetJointPositions(positions);
            //this.fireCalibration = true;
            this.FireCalibration(oldPositions, this.GetStartJointPositions());
        }
    }

    /// <summary>
    /// Set the positions of the starting finger joints, the CMC or MCP joints.
    /// </summary>
    /// <returns></returns>
    public void SetStartJointPositions(Vector3[] positions)
    {
        if (this.glove != null)
        {
            Vector3[] oldPositions = this.GetStartJointPositions();
            this.glove.SetJointPositions(SenseGlove_Util.ToPosition(positions));
            //this.fireCalibration = true;
            this.FireCalibration(oldPositions, this.GetStartJointPositions());
        }
    }

    /// <summary> Retrieve the compensation (in mm) from the thimble to the fingertip </summary>
    /// <returns></returns>
    public Vector3[] GetThimbleComp()
    {
        if (this.glove != null)
        {
            return SenseGlove_Util.ToUnityPosition(this.glove.GetThimbleComp());
        }

        return new Vector3[5]
        {
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
        };
    }

    /// <summary> Manually set the compensation (in mm) from the thimble to the fingertip </summary>
    /// <param name="newCompensations"></param>
    public void SetThimbleComp(Vector3[] newCompensations)
    {
        if (this.glove != null)
        {
            this.glove.SetThimbleComp(SenseGlove_Util.ToPosition(newCompensations));
        }
    }

    /// <summary> Calculate the Joint positions based on a known set of finger lengths. </summary>
    /// <remarks> Use this to (re)calculate the joint positions after loading the finger lengths. </remarks>
    /// <param name="fingerLengths"></param>
    public void CalculateJointPositions(float[][] fingerLengths = null)
    {
        if (this.glove != null && this.gloveReady)
        {
            float[][] oldLengths = this.GetFingerLengths();
            Vector3[] oldPos = this.GetStartJointPositions();

            this.glove.CalculateJointPositions(fingerLengths);
            //this.fireCalibration = true;

            this.FireCalibration(oldLengths, this.GetFingerLengths(), oldPos, this.GetStartJointPositions());
        }
    }

    /// <summary>
    /// Calibrate the Wrist, based on the orientation of the foreArm.
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

    

    //------------------------------------------------------------------------------------------------------------------------------------
    // Manual Calibration Steps


    /// <summary> Call the CalibrationFinished event when the calculations within the DLL are finished. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Glove_OnFingerCalibrationFinished(object source, FingerCalibrationArgs args)
    {
        //this.gloveData = glove.GetData(false); //retrieve the latest gloveData, which contains the new lengths.
        //this.convertedGloveData = new SenseGlove_Data(this.gloveData, this.glove.communicator.samplesPerSecond);
        this.calibrationArguments = new CalibrationArgs(args);
        this.fireCalibration = true;
        //SenseGlove_Debugger.Log("Internal Calibration has finished!");
        //this.CalibrationFinished(arguments);
        //this.ReadyCalibration(new CalibrationArgs(args));
    }

    /// <summary>
    /// Fire a OnCalibrationFinished event for only the fingers
    /// </summary>
    /// <param name="oldLengths"></param>
    /// <param name="newLengths"></param>
    private void FireCalibration(float[][] oldLengths, float[][] newLengths)
    {
        Vector3[] pos = this.GetStartJointPositions();
        this.FireCalibration(oldLengths, newLengths, pos, pos);
    }

    /// <summary>
    /// Fire an OnCalibrationFinished event for only the joint positions
    /// </summary>
    /// <param name="oldPositions"></param>
    /// <param name="newPositions"></param>
    private void FireCalibration(Vector3[] oldPositions, Vector3[] newPositions)
    {
        float[][] lengths = this.GetFingerLengths();
        this.FireCalibration(lengths, lengths, oldPositions, newPositions);
    }

    /// <summary>
    /// Fire an OnCalibrationFinished Event.
    /// </summary>
    /// <param name="oldLengths"></param>
    /// <param name="newLengths"></param>
    /// <param name="oldPositions"></param>
    /// <param name="newPositions"></param>
    private void FireCalibration(float[][] oldLengths, float[][] newLengths, Vector3[] oldPositions, Vector3[] newPositions)
    {
        CalibrationArgs newArgs = new CalibrationArgs
        (
            oldLengths,
            newLengths,
            oldPositions,
            newPositions
        );
        this.CalibrationFinished(newArgs);
    }

    /// <summary> Check whether or not to fire the calibrationFinished event during the main loop. </summary>
    /// <remarks>Placed indside a seprate method so we can call it during both Update and LasteUpdate.</remarks>
    private void CheckCalibration()
    {
        if (this.fireCalibration)
        {
            //SenseGlove_Debugger.Log("Firecalibration");
            if (this.calibrationArguments != null)
            {
                this.CalibrationFinished(this.calibrationArguments);
                this.calibrating = false;
            }
            this.fireCalibration = false;
        }
    }

    /// <summary> Reset the Calibration of the glove if, for instance, something went wrong. </summary>
    public void CancelCalibration()
    {
        if (glove != null)
        {
            glove.StopCalibration();
            this.calSteps = 0;
            this.calibrating = false;
            SenseGlove_Debugger.Log("Canceled Calibration");
        }
    }

    /// <summary> Start a new calibration, based on the parameters set via the inspector. </summary>
    /// <param name="whichFingers"></param>
    /// <returns></returns>
    public bool StartCalibration(bool[] whichFingers = null)
    {
        if (this.GloveReady())
        {
            if (whichFingers == null) { whichFingers = new bool[] { false, true, true, true, true }; }

            CalibrationAlgorithm algorithm = null;
            switch (this.calibrationAlgorithm)
            {
                case Algorithm.PointsOnACircle2D: algorithm = new Circle2D(whichFingers); break;
            }
            if (algorithm != null)
            {
                CalibrationMethod method = null;
                switch (this.calibrationMethod)
                {
                    case CalibrationType.Manual: method         = new ManualCalibration(algorithm, this.async); break;
                    case CalibrationType.SemiAutomatic: method  = new SemiAutoCalibration(algorithm, this.async, 12, 0.6f, 5); break;
                    case CalibrationType.Automatic: method      = new AutoCalibration(algorithm, this.async); break;
                }
                if (method != null)
                {
                    this.glove.StartCalibration(method);
                    this.calibrating = true;
                }
            }
        }
        return false;
    }

    /// <summary> Initialize Manual Calibration of the chosen fingers and the chosen complexity </summary>
    /// <param name="whichFingers"></param>
    /// <param name="simpleCalibration"></param>
    /// <returns></returns>
    public bool StartCalibration(bool[] whichFingers, bool calibrateLengths = true, bool calibrateJoints = true)
    {
        if (glove != null && glove.IsConnected())
        {
            CalibrationAlgorithm algorithm = new Circle2D(whichFingers, calibrateLengths, calibrateJoints);
            CalibrationMethod method = new ManualCalibration(algorithm);
            this.glove.StartCalibration(method);
            this.calibrating = true;
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
                this.StartCalibration(new bool[] { false, true, true, true, true }, true);
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
                SenseGlove_Debugger.Log("Step 3 completed. Calibration has finished.");
                calibrating = false;
                calSteps = 0;
            }
        }

    }

    /// <summary> Test a specific Calibration Algorithm, </summary>
    [Obsolete("Used for testing new calibration methods. Will be removed in v1.0.")]
    public void TestCalibration()
    {
        if (this.glove != null && this.gloveReady)
        {
            int[][][] angles = new int[3][][];
            //fill with zeroes
            for (int i=0; i<angles.Length; i++)
            {
                angles[i] = new int[5][];
                for (int f=0; f<angles[i].Length; f++)
                {
                    angles[i][f] = new int[] { 0, 0, 0 };
                }
            }
            angles[0][1] = new int[] { 0, 0, 0 };
            angles[1][1] = new int[] { 1, 100, 90 };
            angles[2][1] = new int[] { 80, 100, 80 };

            CalibrationAlgorithm testAlgorithm = new ThreeGestures2D(new bool[] { false, true, true, true, true }, angles);
            CalibrationMethod testMethod = new SemiAutoCalibration(testAlgorithm, true, 10, 2.0f, 5);

            this.glove.StartCalibration(testMethod);
            this.calibrating = true;
        }
    }



    //------------------------------------------------------------------------------------------------------------------------------------
    // (Semi)Automatic Calibration Steps

    #region AutoCalibration

    /// <summary> Start a semi-automatic calibration of the thumb, using thumb abduction. </summary>
    public void CalibrateThumb()
    {
        if (this.GloveReady())
        {
            SenseGlove_Debugger.Log("Calibrating Thumb");
            CalibrationAlgorithm algorithm = new Circle2D(new bool[] { true, false, false, false, false });
            this.glove.StartSemiAutoCalibration(algorithm, true, 10, 1f, 5);
            this.calibrating = true;
        }
    }

    /// <summary> Start a new Semi-Automatic finger calibration: The DLL will decide when to add new points based on the parameters set. </summary>
    /// <param name="calculateAsync"></param>
    /// <param name="distinctDistance"></param>
    /// <param name="steadyTime"></param>
    /// <param name="steadyDistance"></param>
    public void SemiAutoCalibrateFingers(bool calculateAsync = true, float distinctDistance = 15, float steadyTime = 1.5f, float steadyDistance = 2)
    {
        if (this.GloveReady())
        {
            CalibrationAlgorithm algorithm = new Circle2D(new bool[] { true, false, false, false, false });
            this.glove.StartSemiAutoCalibration(algorithm, calculateAsync, distinctDistance, steadyTime, steadyDistance);
            this.calibrating = true;
        }
    }

    /// <summary> Collect a number of snapshots, then select the most likely ones and perform a calibration step. </summary>
    /// <param name="processAsync"></param>
    /// <param name="pointLimit"></param>
    /// <param name="timeLimit"></param>
    /// <param name="minPoints"></param>
    public void AutoCalibrateFingers(bool processAsync = true, int pointLimit = 100, float timeLimit = 4, int minPoints = 3)
    {
        if (this.GloveReady())
        {
            CalibrationAlgorithm algorithm = new Circle2D(new bool[] { true, false, false, false, false });
            this.glove.StartAutoCalibration(algorithm, processAsync, pointLimit, timeLimit, minPoints);
            this.calibrating = true;
        }
    }

    #endregion AutoCalibration

    #endregion Calibration

    //------------------------------------------------------------------------------------------------------------------------------------
    // (Haptic) Feedback

    #region Feedback

    /// <summary>
    /// Verify if this SenseGlove has a particular functionality (buzz motors, haptic feedback, etc)
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

    /// <summary>
    /// Send a simple brake PWM command to the SenseGlove.
    /// </summary>
    /// <param name="commands"></param>
    /// <returns></returns>
    public bool SendBrakeCmd(int[] commands)
    {
        if (this.glove != null && this.glove.IsConnected())
        {
            return this.glove.BrakeCmd(commands);
            //return this.glove.ByteBrake(commands);
        }
        return false;
    }


    /// <summary>
    /// Send a simple brake PWM command to the SenseGlove.
    /// </summary>
    /// <param name="thumbCmd"></param>
    /// <param name="indexCmd"></param>
    /// <param name="middleCmd"></param>
    /// <param name="ringCmd"></param>
    /// <param name="pinkyCmd"></param>
    /// <returns></returns>
    public bool SendBrakeCmd(int thumbCmd, int indexCmd, int middleCmd, int ringCmd, int pinkyCmd)
    {
        return this.SendBrakeCmd(new int[] { thumbCmd, indexCmd, middleCmd, ringCmd, pinkyCmd });
    }

    /// <summary>  Stop all brakes on this SenseGlove_Object </summary>
    public void StopBrakes()
    {
        SendBrakeCmd(0, 0, 0, 0, 0);
    }

    /// <summary> Send BuzzMotor commands to specific fingers. </summary>
    /// <param name="thumb"></param>
    /// <param name="index"></param>
    /// <param name="middle"></param>
    /// <param name="ring"></param>
    /// <param name="pinky"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(int thumb, int index, int middle, int ring, int pinky)
    {
        return this.SendBuzzCmd(new int[] { thumb, index, middle, ring, pinky });
    }

    /// <summary> Send a simple constant buzzMotor command that will vibrate for the default amount of time (400ms) </summary>
    /// <param name="magnitudes"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(int[] magnitudes)
    {
        if (this.GloveReady() && this.IsConnected())
        {
            return this.glove.BuzzMotorCmd(magnitudes);
        }
        return false;
    }

    /// <summary> Send one buzzmotor command to specific fingers. </summary>
    /// <param name="fingers"></param>
    /// <param name="magn"></param>
    /// <param name="dur"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(bool[] fingers, int magnitude = 100, int duration = 400, BuzzMotorPattern pattern = BuzzMotorPattern.Constant)
    {
        return this.SendBuzzCmd(
            fingers, 
            new int[] { duration, duration, duration, duration, duration },
            new int[] { magnitude, magnitude, magnitude, magnitude, magnitude }, 
            new BuzzMotorPattern[] { pattern, pattern, pattern, pattern, pattern }
        );
    }

    /// <summary> Send a buzz-motor command to the Sense Glove, with optional parameters. </summary>
    /// <param name="fingers"></param>
    /// <param name="durations"></param>
    /// <param name="magnitudes"></param>
    /// <param name="patterns"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(bool[] fingers, int[] durations = null, int[] magnitudes = null, BuzzMotorPattern[] patterns = null)
    {
        if (this.GloveReady() && this.IsConnected())
        {
            return this.glove.BuzzMotorCmd(fingers, durations, magnitudes, patterns);
        }
        return false;
    }

    /// <summary> Stop all Buzz Motors from vibrating. </summary>
    public void StopBuzzMotors()
    {
        SendBuzzCmd(new int[] { 0, 0, 0, 0, 0 });
    }

    #endregion Feedback


    //------------------------------------------------------------------------------------------------------------------------------------
    // Backwards Compatibility Methods

    #region BackwardsComp
        
    /// <summary> Manually assign IMU Correction for old firmware versions. </summary>
    private void SetupWrist()
    {
        if (this.glove != null && this.glove.gloveData.dataLoaded)
        {
            string ID = this.gloveData.deviceID;

            if (ID.Contains("220102"))
            {
                this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(Mathf.PI, 0, Mathf.PI / 2.0f)); //correction for glove 1
                SenseGlove_Debugger.Log("Red Glove Compensation");
            }

            string[] gloveVersion = this.gloveData.firmwareVersion.Split('.');
            if (gloveVersion[0][0] == 'v') { gloveVersion[0] = gloveVersion[0].Substring(1); } //if there is a v in front of it, remove this.
            int mainVersion = int.Parse(gloveVersion[0]);
            int subVersion = int.Parse(gloveVersion[1]);
            if (mainVersion <= 2 && subVersion <= 19)
            {
                if (ID.Contains("120206"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(Mathf.PI / 2.0f, 0, Mathf.PI)); //correction for glove 1
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120101"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(Mathf.PI, 0, 0)); //correction for glove 1
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120203"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI / 2.0f)); //correction?
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120307") || ID.Contains("120304") || ID.Contains("120310") || ID.Contains("120309") || ID.Contains("120311") || ID.Contains("120312"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI)); //correction for glove 7 & 4?  
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
            }
        }
    }

    #endregion BackwardsComp

}

/// <summary> The way this object connects to SenseGlove_Objects detected on this system. </summary>
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


/// <summary>
/// CalibrationArguments, containing both old an new finger lengths and joint positions.
/// </summary>
public class CalibrationArgs : System.EventArgs
{
    /// <summary> The old phalange lengths, in mm, from thumb to pinky, from proximal to distal. </summary>
    public float[][] oldFingerLengths;

    /// <summary> The new phalange lengths, in mm, from thumb to pinky, from proximal to distal. </summary>
    public float[][] newFingerLengths;

    /// <summary> The old joint positions, in mm, relative to the common origin, from thumb to pinky, from proximal to distal. </summary>
    public Vector3[] newJointPositions;

    /// <summary> The new joint positions, in mm, relative to the common origin, from thumb to pinky, from proximal to distal. </summary>
    public Vector3[] oldJointPositions;

    /// <summary> The fingers that were altered / changed during this calibration sequence. </summary>
    public bool[] whichFingers;

    /// <summary> Convert a FingerCalibrationArgs, coming from the DLL, into the Unity-friendly CalibrationArgs. </summary>
    /// <param name="arguments"></param>
    public CalibrationArgs(SenseGloveCs.FingerCalibrationArgs arguments)
    {
        this.whichFingers = arguments.whichFingers;
        this.oldFingerLengths = arguments.oldFingerLengths;
        this.newFingerLengths = arguments.newFingerLengths;
        this.oldJointPositions = SenseGlove_Util.ToUnityPosition(arguments.oldJointPositions);
        this.newJointPositions = SenseGlove_Util.ToUnityPosition(arguments.newJointPositions);    
    }


    
    /// <summary> Create new calibration arguments from within the SenseGlove_Object, so that any handmodel can properly resize. </summary>
    /// <param name="oldLengths"></param>
    /// <param name="newLengths"></param>
    /// <param name="oldPositions"></param>
    /// <param name="newPositions"></param>
    public CalibrationArgs(float[][] oldLengths, float[][] newLengths, Vector3[] oldPositions, Vector3[] newPositions)
    {
        this.whichFingers = new bool[5] { true, true, true, true, true };
        this.oldFingerLengths = oldLengths;
        this.newFingerLengths = newLengths;
        this.oldJointPositions = oldPositions;
        this.newJointPositions = newPositions;
    }
    
}
