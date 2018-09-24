
using UnityEngine;

using SenseGloveCs;
using System;
using SenseGloveCs.Kinematics;

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
    protected GloveSide side = GloveSide.Unknown;


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
    public Solver solver = Solver.Interpolate4Sensors;

    /// <summary> Determine whether the fingers are contrained within natural limits. </summary>
    [Tooltip("Determines whether the fingers are contrained within natural limits.")]
    public bool limitFingers = true;

    /// <summary> Determine wheter or not the relative wrist orientation is constained to natural limits. </summary>
    [Tooltip("Determine wheter or not the relative wrist orientation is constained to natural limits. ")]
    public bool limitWrist = true;

    /// <summary> Check for gestures within the DLL </summary>
    [Tooltip("NOT YET IMPLEMENTED - Gesture recognition")]
    private bool checkGestures = false;


   // /// <summary> Whether or not the (complex) calucation is performed in a separate worker thread. </summary>
   // [Tooltip("Whether or not the (complex) calucation is performed in a separate worker thread.")]
   // public bool async = true;

    //--------------------------------------------------------------------------------------------------------------------------
    // Private attributes

    /// <summary> The SenseGlove object through which we retrieve data and send commands. </summary>
    private SenseGlove glove;
    /// <summary> A 'Briefcase' representing all of the Data that can be obtained from this SenseGlove. </summary>
    private GloveData gloveData;

    /// <summary> GloveData converted to Unity variables. </summary>
    private SenseGlove_Data convertedGloveData;

    /// <summary> The last fired calibration arguments. </summary>
    private GloveCalibrationArgs calibrationArguments;


    /// <summary> Determines whether or not the wrist has been calibrated on startup. </summary>
    private bool calibratedWrist = false;

    /// <summary>  Time that has elapsed since the Setup was called. Can be used to delay the Update Finction by setting it back to 0. </summary>
    private float elapsedTime = 0;

    /// <summary> The time it takes for one out of two steps of the Setup to complete. </summary>
    private static float setupTime = 0.5f;

    /// <summary> Only true during the frame where the setup finishes. </summary>
    private bool gloveReady = false;

    /// <summary> Determines if a connection should be made. </summary>
    private bool standBy = false;

    /// <summary> Ensures we send a debug message only once. </summary>
    private bool canReport = true;

    /// <summary> Original (hard-coded) lengths of the Sense Glove </summary>
    private float[][] originalLengths;

    /// <summary> Original (hard-coded) joint positions of the Sense Glove </summary>
    private Vect3D[] originalJoints;

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

    /// <summary> Event delegate for the GloveReady event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void GloveUnloadEventHandler(object source, System.EventArgs args);

    /// <summary> Fired when the Disconnect button is pressed; used for cleanup of scripts reliant on the SenseGlove_Object. </summary>
    public event GloveReadyEventHandler OnGloveUnloaded;

    /// <summary> Used to call the OnGloveLoaded event. </summary>
    protected void GloveUnloaded()
    {
        if (OnGloveUnloaded != null)
        {
            OnGloveUnloaded(this, null);
        }
    }

    /// <summary> Event delegate function for the CalibrateionFinished event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void CalibrationFinishedEventHandler(object source, GloveCalibrationArgs args);

    /// <summary> Occurs when the finger calibration is finished. Passes the old and new GloveData as arguments. </summary>
    public event CalibrationFinishedEventHandler OnCalibrationFinished;

    /// <summary> Used to call the OnCalibrationFinished event. </summary>
    /// <param name="calibrationArgs"></param>
    protected void CalibrationFinished(GloveCalibrationArgs calibrationArgs)
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
                    Vect3D[] oldStartPositions = null;
                    if (this.gloveData != null)
                    {
                        oldFingerLengths = this.GetFingerLengths();
                        oldStartPositions = SenseGlove_Util.ToPosition(this.GetStartJointPositions());
                    }
                    
                    this.gloveData = this.glove.GetData(false); //get the latest data without calculating anything.

                    if (oldFingerLengths != null) { this.SetFingerLengths(oldFingerLengths); } //re-apply old fingerlengths, if possible.
                    if (oldStartPositions != null) { this.SetStartJointPositions(oldStartPositions); } //re=apply joint positions, if possible.

                    this.convertedGloveData = new SenseGlove_Data(this.gloveData);
                    //this.SetupWrist(); //removed for v0.20, uncomment for backwards compatibility with the oldest protos.
                    this.calibratedWrist = false;
                    this.gloveReady = true;
                    if (runSetup)
                    {
                        this.originalLengths = this.gloveData.kinematics.GetFingerLengths();
                        this.originalJoints = this.gloveData.kinematics.GetJointPositions();
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
        this.Dispose();
        SenseGloveCs.DeviceScanner.CleanUp();
    }

    void OnDestroy()
    {
        this.Dispose();
    }

    /// <summary> Dispose of resources. </summary>
    private void Dispose()
    {
        this.CancelCalibration();
        if (glove != null && glove.IsConnected())
        {
            glove.StopBrakes();
            glove.StopBuzzMotors();
            glove.Disconnect();
            SenseGlove_Debugger.Log("Disconnected the SenseGlove on " + glove.communicator.Address());
        }
        this.Disconnect();
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
                        || (this.connectionMethod == ConnectionMethod.FindNextLeftHand && !tempData.kinematics.isRight)
                        || (this.connectionMethod == ConnectionMethod.FindNextRightHand && tempData.kinematics.isRight) )
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
            this.GloveUnloaded(); //raise event
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

    /// <summary> Check if this glove is a left- or right hand, or if it is still loaded. </summary>
    /// <returns></returns>
    public GloveSide GetSide()
    {
        if (this.convertedGloveData != null)
        {
            return this.convertedGloveData.gloveSide;
        }
        return GloveSide.Unknown;
    }

    #endregion Communication

    //------------------------------------------------------------------------------------------------------------------------------------
    // Data Retrieval

    #region Data

    /// <summary>
    /// Retrieve the UNCONVERTED GloveData from this SenseGlove. Use GetData instead if you wish to access Unity-friendly data.
    /// </summary>
    /// <returns></returns>
   //  [Obsolete("Returns unconverted data. Use GloveData() instead, which returns a wrapper with more user-friendly Unity (Vector3 and Quaternion) variables.")]
    public GloveData GetRawGloveData()
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

    /// <summary> Retrieve the internal SenseGlove Object of the DLL, which can be used for calibration or connection purposes.  </summary>
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
            this.convertedGloveData = new SenseGlove_Data(this.gloveData);
        }
    }


    #endregion Data

    
    //------------------------------------------------------------------------------------------------------------------------------------
    // Manual Calibration methods

    #region Calibration

    /// <summary> Reset all finger lengths to their original sizes and positions to their original positions. </summary>
    public void ResetHand()
    {
        this.SetFingerLengths(this.originalLengths);
        this.SetStartJointPositions(this.originalJoints);

        SenseGlove_Data newData = new SenseGlove_Data( this.glove.GetData(false) ); //oldData is currently contained within this class' property.
        this.ReadyCalibration(new GloveCalibrationArgs( this.convertedGloveData, newData ), false);
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
            this.glove.SetHandLengths(newFingerLengths);
            SenseGlove_Data newData = new SenseGlove_Data(this.glove.GetData(false));
            this.ReadyCalibration(new GloveCalibrationArgs(this.convertedGloveData, newData), false);
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
            return this.gloveData.kinematics.GetFingerLengths();
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
    /// <remarks>Used internally</remarks>
    /// <returns></returns>
    protected void SetStartJointPositions(SenseGloveCs.Kinematics.Vect3D[] positions)
    {
        if (this.glove != null && positions != null)
        {
            this.glove.SetJointPositions(positions);
            SenseGlove_Data newData = new SenseGlove_Data(this.glove.GetData(false));
            this.ReadyCalibration(new GloveCalibrationArgs(this.convertedGloveData, newData), false);
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
            this.glove.SetJointPositions(SenseGlove_Util.ToPosition(positions));
            SenseGlove_Data newData = new SenseGlove_Data(this.glove.GetData(false));
            this.ReadyCalibration(new GloveCalibrationArgs(this.convertedGloveData, newData), false);
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
            glove.CalibrateWrist(null, new Quat(SenseGlove_Util.ToQuaternion(this.foreArm.transform.rotation)));
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
            glove.CalibrateWrist(null, new Quat( SenseGlove_Util.ToQuaternion(lowerArm) ));
            SenseGlove_Debugger.Log("Calibrated Wrist");
            return true;
        }
        return false;
    }

    

    //------------------------------------------------------------------------------------------------------------------------------------
    // Calibration Events


    /// <summary> Call the CalibrationFinished event when the calculations within the DLL are finished. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Glove_OnFingerCalibrationFinished(object source, CalibrationArgs args)
    {
        //comes from the DLL
        this.ReadyCalibration(new GloveCalibrationArgs(args), true);
    }

    /// <summary> The Calibration of the Sense Glove should be ready to fire. </summary>
    /// <param name="args"></param>
    /// <param name="fromDLL"></param>
    private void ReadyCalibration(GloveCalibrationArgs args, bool fromDLL)
    {
        if (fromDLL) //comes from a possibly async thread within the DLL
        {
            this.calibrationArguments = args; //queue
        }
        else
        {
            this.CalibrationFinished(args); //just fire, no queue.
        }
    }

    /// <summary> Check if we have any CalibrationComplete events queued, then send them. </summary>
    /// <remarks>Placed indside a seprate method so we can call it during both Update and LateUpdate. Should only be fired from these</remarks>
    private void CheckCalibration()
    {
        if (this.calibrationArguments != null)
        {
            this.CalibrationFinished(this.calibrationArguments);
            this.calibrationArguments = null;
        }
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Manual Calibration Steps

    /// <summary> Check whether or not this SenseGlove_Object is currently collecting calibration data. </summary>
    /// <returns></returns>
    public bool IsCalibrating()
    {
        return this.glove.CalibrationStarted();
    }


    /// <summary> Reset the Calibration of the glove if, for instance, something went wrong. </summary>
    public void CancelCalibration()
    {
        if (glove != null)
        {
            glove.StopCalibration();
            SenseGlove_Debugger.Log("Canceled Calibration");
        }
    }


    /// <summary> Calibrate a variable related to the glove or a solver, with a specific collection method. </summary>
    /// <param name="whichFingers"></param>
    /// <param name="simpleCalibration"></param>
    /// <returns></returns>
    public bool StartCalibration(CalibrateVariable whatToCalibrate, CollectionMethod howToCollect)
    {
        if (this.GloveReady())
        {
            return this.glove.StartCalibration(whatToCalibrate, howToCollect);
        }
        return false;
    }

    /// <summary> Continue the next calibration steps (no reporting of progress) </summary>
    /// <returns></returns>
    public bool NextCalibrationStep()
    {
        return this.glove.NextCalibrationStep();
    }

    /// <summary> Activate the next step of the Sense Glove's manual calibration sequence; reporting the next step. </summary>
    /// <param name="calibrationKey"></param>
    /// <returns></returns>
    public bool NextCalibrationStep(KeyCode calibrationKey)
    {
        if ( !this.IsCalibrating() )
        {
            // Start a new Calibration
            CalibrateVariable whatTo = CalibrateVariable.FingerVariables;

            //TODO : Make this dependant on solvers

            bool strt = this.StartCalibration(whatTo, CollectionMethod.Manual);
            if (strt)
            {
                SenseGlove_Debugger.Log("Started Calibration. Please stretch your fingers in front of you and press " + calibrationKey.ToString());
                return true;
            }
        }
        else
        {
            
            bool added = this.glove.NextCalibrationStep();
            if (added)
            {
                this.convertedGloveData.calibrationStep = this.convertedGloveData.calibrationStep+ 1;
                if (this.convertedGloveData.calibrationStep == 1)
                {
                    SenseGlove_Debugger.Log("Step 1 completed. Please bend you MCP joint to 45* and press " + calibrationKey.ToString());
                }
                else if (this.convertedGloveData.calibrationStep == 2)
                {
                    SenseGlove_Debugger.Log("Step 2 completed. Please bend you MCP joint to 90* and press " + calibrationKey.ToString());
                }
                else if (this.convertedGloveData.calibrationStep == 3)
                {
                    SenseGlove_Debugger.Log("Step 3 completed. Calculating...");
                }
            }
        }
        return false;
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // (Semi)Automatic Calibration Steps

    #region AutoCalibration

    /// <summary> Start a new Semi-Automatic finger calibration: The DLL will decide when to add new points based on the parameters set. </summary>
    /// <param name="calculateAsync"></param>
    /// <param name="distinctDistance"></param>
    /// <param name="steadyTime"></param>
    /// <param name="steadyDistance"></param>
    public bool SemiAutoCalibrateFingers(CalibrateVariable whatToCalibrate, float distinctDistance = 15, float steadyTime = 1.5f, float steadyDistance = 2)
    {
        if (this.GloveReady())
        {
            return this.glove.StartSemiAutoCalibration(whatToCalibrate, distinctDistance, steadyTime, steadyDistance);
        }
        return false;
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
    /// Tell the Sense Glove to set its brakes at the desired magnitude [0..100%] for each finger until it recieves a new command.
    /// </summary>
    /// <param name="commands"></param>
    /// <returns>Returns true if the command has been succesfully sent.</returns>
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
    /// Tell the Sense Glove to set its brakes at the desired magnitude [0..100%] for each finger until it recieves a new command.
    /// </summary>
    /// <param name="thumbCmd"></param>
    /// <param name="indexCmd"></param>
    /// <param name="middleCmd"></param>
    /// <param name="ringCmd"></param>
    /// <param name="pinkyCmd"></param>
    /// <returns>Returns true if the command has been succesfully sent.</returns>
    public bool SendBrakeCmd(int thumbCmd, int indexCmd, int middleCmd, int ringCmd, int pinkyCmd)
    {
        return this.SendBrakeCmd(new int[] { thumbCmd, indexCmd, middleCmd, ringCmd, pinkyCmd });
    }

    /// <summary> Release all brakes of the SenseGlove. </summary>
    public void StopBrakes()
    {
        SendBrakeCmd(0, 0, 0, 0, 0);
    }

    /// <summary> Send a simple constant buzzMotor command that will vibrate for the default amount of time (400ms) </summary>
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
    [Obsolete("Needed for very few of the oldest Sense Glove prototypes. Will be phsed out for DK1")]
    private void SetupWrist()
    {
        
        if (this.glove != null && this.glove.gloveData.dataLoaded)
        {
            string ID = this.gloveData.deviceID;

            
            if (this.gloveData.firmwareVersion <= 2.19f)
            {
                if (ID.Contains("120206"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quat.FromEuler(Mathf.PI / 2.0f, 0, Mathf.PI)); //correction for glove 1
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120101"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quat.FromEuler(Mathf.PI, 0, 0)); //correction for glove 1
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120203"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quat.FromEuler(0, 0, Mathf.PI / 2.0f)); //correction?
                    SenseGlove_Debugger.Log("Firmware Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120307") || ID.Contains("120304") || ID.Contains("120310") || ID.Contains("120309") || ID.Contains("120311") || ID.Contains("120312"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quat.FromEuler(0, 0, Mathf.PI)); //correction for glove 7 & 4?  
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
public class GloveCalibrationArgs : System.EventArgs
{
    /// <summary> 'Snapshot' of the old data, with old parameters </summary>
    public SenseGlove_Data oldData;

    /// <summary> 'Snapshot' of the new data, with updated parameters </summary>
    public SenseGlove_Data newData;

    /// <summary> Creates a new instance of the unity-friendly calibration args. </summary>
    /// <param name="args"></param>
    public GloveCalibrationArgs(SenseGloveCs.CalibrationArgs args)
    {
        this.oldData = new SenseGlove_Data(args.before);
        this.newData = new SenseGlove_Data(args.after);
    }


    /// <summary> Creates a new instance of the unity-friendly calibration args. </summary>
    /// <param name="args"></param>
    public GloveCalibrationArgs(SenseGlove_Data oldD, SenseGlove_Data newD)
    {
        this.oldData = oldD;
        this.newData = newD;
    }

}

