using System.Collections.Generic;
using UnityEngine;
using SenseGloveCs;
using SenseGloveCs.Kinematics;

/// <summary> The way this object connects to SenseGlove_Objects detected on this system. </summary>
public enum ConnectionMethod
{
    /// <summary> Connect to the first unconnected SenseGlove on the system. </summary>
    NextGlove = 0,
    /// <summary> Connect to the first unconnected Right Handed SenseGlove on the system. </summary>
    NextRightHand,
    /// <summary> Connect to the first unconnected Left Handed SenseGlove on the system. </summary>
    NextLeftHand
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

/// <summary> After being linked to a proper Sense Glove via the SenseGlove_DeviceManager, this script is
/// responsible for updating SenseGlove_Data every frame, and for exposing feedback - and calibration methods. </summary>
public class SenseGlove_Object : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties


    /// <summary> The way with which the SenseGlove_Object connects to a glove. </summary>
    [Header("Connection Settings")]
    public ConnectionMethod connectionMethod = ConnectionMethod.NextGlove;

    /// <summary> Allows this Sense Glove_Object to manage its own connection status </summary>
    [SerializeField] protected bool autoConnect = true;

    /// <summary> The index of the internal Sense Glove that this _Object is connected to. </summary>
    [SerializeField] protected int trackedGloveIndex = -1;

    /// <summary> Just in case someone tries to change via the inspector. </summary>
    protected int actualTrackedIndex = -1;

    /// <summary> The Solver used to calculate this Sense Glove's hand model each frame. </summary>
    [Header("Kinematics Settings")]
    public Solver solver = Solver.Interpolate4Sensors;

    /// <summary> Enables/Diables the force feedback on this _Object. </summary>
    public bool forceFeedback = true;


    /// <summary> Whether or not to apply natural limits to the fingers. </summary>
    /// <remarks> Marked as protected since these wil likely always be true during normal use. </remarks>
    protected bool limitFingers = true;

    /// <summary> Whether or not to update the wrist model of the Sense Glove. </summary>
    /// <remarks> We will always update it, but calibrate it at hand-model level. </remarks>
    protected bool updateWrist = true;

    /// <summary> Unity Even that fires when this script is assigned to a Sense Glove </summary>
    [Header("Events")]
    [Tooltip("Unity Even that fires when this script is assigned to a Sense Glove ")]
    public SGEvent OnGloveLoad;

    /// <summary> The Internal Sense Glove object that is linked to this monobehaviour Object </summary>
    protected SenseGlove linkedGlove = null;

    /// <summary> The last data from the linked glove. </summary>
    protected SenseGlove_Data linkedGloveData = SenseGlove_Data.Empty; //an empty data struct

    /// <summary> Queued Calibration Command from the fingers, which will fire during Unity's next LateUpdate() (so as to allow acces to transforms) </summary>
    protected List<GloveCalibrationArgs> calibrationArguments = new List<GloveCalibrationArgs>();

    /// <summary> Whether or not the linked glove was connected the last time we checked. </summary>
    protected bool wasConnected = false;

    /// <summary> Command queue for the brakes, which is flushed at the end of every Update function. </summary>
    protected List<int[]> brakeQueue = new List<int[]>();


    //Static properties

    /// <summary> Saves setup time for multiple SenseGlove_Objects checking for DeviceManager's existance. </summary>
    protected static bool deviceScannerPresent = false;

    protected static int maxBrakeCmds = 10;

    //--------------------------------------------------------------------------------------------------------------------------
    // Basic Accessors, Gets


    /// <summary> If true, this Sense Glove is connected to a right hand. Otherwise, it is connected to a left hand. </summary>
    public bool IsRight
    {
        get { return this.linkedGlove != null && this.linkedGlove.IsRight(); }
    }

    /// <summary> Returns the index that this Glove uses to access data via the SenseGlove_DeviceManager. </summary>
    public int GloveIndex
    {
        get { return this.actualTrackedIndex; }
    }

    /// <summary> Check if this SenseGlove_Object has been linked to a Sense Glove via the SenseGlove_DeviceManager. </summary>
    public bool IsLinked
    {
        get { return this.actualTrackedIndex > -1; }
    }

    /// <summary> Determines if this glove is ready and linked to the hardware. </summary>
    public bool GloveReady
    {
        get { return this.linkedGlove != null && this.actualTrackedIndex > -1; }
    }

    /// <summary> Check if the Sense Glove is connected. </summary>
    public bool IsConnected
    {
        get { return this.linkedGlove != null && this.linkedGlove.IsConnected(); }
    }

    /// <summary> Retrieve Unity-Friendly Glove Data from the Sense Glove. </summary>
    public SenseGlove_Data GloveData
    {
        get { return this.linkedGloveData; }
    }

    /// <summary> Check if this glove is collection calibration points. </summary>
    public bool IsCalibrating
    {
        get { return this.linkedGlove != null ? this.linkedGlove.CalibrationStarted() : false; }
    }

    [System.Obsolete("Use GloveData Instead")]
    public GloveData InternalGloveData
    {
        get
        {
            if (this.linkedGlove != null)
                return this.linkedGlove.GetData(false);
            return null;
        }
    }



    //--------------------------------------------------------------------------------------------------------------------------
    // Connection Methods

    /// <summary> Link this Sense Glove to one of the SenseGlove_Manager's detected gloves. </summary>
    /// <param name="gloveIndex"></param>
    public bool LinkToGlove(int gloveIndex)
    {
        if (!this.IsLinked)
        {
            this.trackedGloveIndex = gloveIndex;
            this.actualTrackedIndex = gloveIndex;
            this.linkedGlove = SenseGlove_DeviceManager.GetSenseGlove(gloveIndex);

            this.linkedGlove.OnFingerCalibrationFinished += LinkedGlove_OnCalibrationFinished;
            //TODO: Subscribe to more events if needed

            //uodating once so glovedata is available, but not through the UpdateHand command, as that one needs the glove to already be connected.
            SenseGloveCs.GloveData rawData = this.linkedGlove.Update(UpdateLevel.HandPositions, this.solver, this.limitFingers, this.updateWrist, new Quat(0, 0, 0, 1), false);
            this.linkedGloveData = new SenseGlove_Data(rawData);

            this.OnGloveLink(); //Fire glove linked event.

            return true;
        }
        else
        {
            Debug.LogWarning("Could not Link the Sense Glove because it has already been assigned. Unlink it first bu calling the UnlinkGlove() Method.");
        }
        return false;
    }

    /// <summary> Unlink this glove from the manager. </summary>
    public void UnlinkGlove()
    {
        if (this.IsLinked) //only is we are actually linked.
        {
            this.CancelCalibration();
            this.wasConnected = false;

            this.OnGloveUnLink(); //fire unlink event

            this.actualTrackedIndex = -1;
            this.trackedGloveIndex = -1;
            if (this.linkedGlove != null)
            {
                this.linkedGlove.OnFingerCalibrationFinished -= LinkedGlove_OnCalibrationFinished;
            }
            this.linkedGlove = null;
        }
    }

    /// <summary> Check if a device manager is currently active within the Scene. If not, create an instance to manager our connection(s). </summary>
    protected void CheckForDeviceManager()
    {
        SenseGlove_DeviceManager manager = null;
        if (!SenseGlove_Object.deviceScannerPresent)
        {
            object deviceManagerObj = GameObject.FindObjectOfType(typeof(SenseGlove_DeviceManager));
            if (deviceManagerObj != null) //it exists
            {
                manager = (SenseGlove_DeviceManager)deviceManagerObj;
                if (!manager.isActiveAndEnabled || !manager.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("WARNING: The SenseGlove_DeviceManager is not active in the scene. Objects will not connect until it is active.");
                }
            }
            else
            {
                GameObject newdeviceManager = new GameObject("[SG DeviceManager]");
                manager = newdeviceManager.AddComponent<SenseGlove_DeviceManager>();
            }
            SenseGlove_Object.deviceScannerPresent = true; //we have either found it or created it.
        }
        else //it already exists
        {
            manager = (SenseGlove_DeviceManager)GameObject.FindObjectOfType(typeof(SenseGlove_DeviceManager));
        }

        if (manager != null && this.autoConnect) //redundant check.
        {
            manager.AddToWatchList(this); //make sure this glove is added to its watchlist.
        }
    }




    //--------------------------------------------------------------------------------------------------------------------------
    // Class Methods

    /// <summary> Updates this glove's data. </summary>
    protected void UpdateGlove()
    {
        if (this.wasConnected && this.linkedGlove != null && this.IsConnected) //if we are connected then linkedGlove is assumed not null.
        {
            //lower arm rotation is set to identity (n/a) since we will calibrate at _HandModel Level
            SenseGloveCs.GloveData rawData = this.linkedGlove.Update(UpdateLevel.HandPositions, this.solver, this.limitFingers, this.updateWrist,
                new Quat(0, 0, 0, 1), false);
            this.linkedGloveData.UpdateVariables(rawData);
        }
    }

    /// <summary> Check if we should fire an of the connected events. </summary>
    /// <remarks> While connection events also fire from the DLL, these are mostly from another worker thread. This is Unity-Safe. </remarks>
    protected void CheckConnection()
    {
        if (this.linkedGlove != null)
        {
            if (!this.wasConnected && this.linkedGlove.IsConnected())
                this.OnGloveConnect();
            else if (this.wasConnected && !this.linkedGlove.IsConnected())
                this.OnGloveDisconnect();
        }
    }

    // Static methods

    /// <summary> Check whether or not a glove with a particular handed-ness mathces a connection method. </summary>
    /// <param name="rightHand"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public static bool MatchesConnection(bool rightHand, ConnectionMethod method)
    {
        return method == ConnectionMethod.NextGlove
            || (rightHand && method == ConnectionMethod.NextRightHand)
            || (!rightHand && method == ConnectionMethod.NextLeftHand);
    }




    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    #region Events

    //GloveLinked

    /// <summary> Event delegate for the glove events event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void GloveEventHandler(object source, System.EventArgs args);

    /// <summary> Called when this script is assigned a Sense Glove via the SenseGlove_DeviceManager. </summary>
    public event GloveEventHandler GloveLoaded;

    /// <summary> Used to call the GloveLoaded event. </summary>
    protected void OnGloveLink()
    {
        Debug.Log(this.name + " Linked to Sense Glove " + this.linkedGlove.DeviceID());
        if (GloveLoaded != null)
        {
            GloveLoaded(this, null);
        }
        this.OnGloveLoad.Invoke(); //code before inspector.
    }

    //GloveUnLinked

    /// <summary> Called when the SenseGlove_DeviceManager unlinks the Sense Glove from this object. </summary>
    public event GloveEventHandler GloveUnLoaded;

    /// <summary> Used to call the GloveLoaded event. </summary>
    protected void OnGloveUnLink()
    {
        Debug.Log(this.name + " Unlinked");
        if (GloveUnLoaded != null)
        {
            GloveUnLoaded(this, null);
        }
    }


    // Connect - WIP

    ///// <summary> Called when the glove linked to this SenseGlove_Object (re)Connects to the system. </summary>
    //public event GloveEventHandler GloveConnected;

    /// <summary> Used to call the OnGloveLoaded event. </summary>
    protected void OnGloveConnect()
    {
        this.wasConnected = true;
        //Debug.Log(this.name + " Connected to Sense Glove " + this.linkedGlove.DeviceID());
        //if (GloveConnected != null)
        //{
        //    GloveConnected(this, null);
        //}
    }

    // Disconnect - WIP

    ///// <summary> Called when the glove linked to this SenseGlove_Object disconnects. </summary>
    //public event GloveEventHandler GloveDisconnected;

    /// <summary> Used to call the OnGloveLoaded event. </summary>
    protected void OnGloveDisconnect()
    {
        this.wasConnected = false;
        //Debug.Log(this.name + " Disconnected from Sense Glove " + this.linkedGlove.DeviceID());
        //if (GloveDisconnected != null)
        //{
        //    GloveDisconnected(this, null);
        //}
    }


    // Calibration Finished

    /// <summary> Event delegate function for the CalibrateionFinished event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void CalibrationFinishedEventHandler(object source, GloveCalibrationArgs args);

    /// <summary> Occurs when the finger calibration is finished. Passes the old and new GloveData as arguments. </summary>
    public event CalibrationFinishedEventHandler CalibrationFinished;

    /// <summary> Used to call the OnCalibrationFinished event. </summary>
    /// <param name="calibrationArgs"></param>
    protected void FinishCalibration(GloveCalibrationArgs calibrationArgs)
    {
        if (CalibrationFinished != null)
        {
            CalibrationFinished(this, calibrationArgs);
        }
    }


    #endregion Events


    //--------------------------------------------------------------------------------------------------------------------------
    // Feedback Methods

    #region Feedback

    /// <summary> Verify if this SenseGlove has a particular functionality (buzz motors, haptic feedback, etc) </summary>
    /// <param name="function">The function to test for</param>
    /// <returns></returns>
    public bool HasFunction(GloveFunctions function)
    {
        if (this.linkedGlove != null)
        {
            return linkedGlove.HasFunction(function);
        }
        return false;
    }

    /// <summary> Stop all forms of feedback on this Sense Glove. </summary>
    /// <returns></returns>
    public bool StopFeedback()
    {
        return this.StopBrakes() && this.StopBuzzMotors();
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Force-Feedback Methods

    #region ForceFeedback

    /// <summary> Tell the Sense Glove to set its brakes at the desired magnitude [0..100%] for each finger until it recieves a new command. </summary>
    /// <param name="commands"></param>
    /// <returns>Returns true if the command has been succesfully sent.</returns>
    /// <remarks> This is where the magic happens; where the actual command is sent. All other SendBrakeCmd methods are wrappers. </remarks>
    protected bool WriteBrakeCmd(int[] commands)
    {
        if (this.linkedGlove != null && this.linkedGlove.IsConnected())
        {
            return this.linkedGlove.BrakeCmd(commands); //todo; queue
        }
        return false;
    }

    /// <summary> Send the highest, last recieved brake commands recieved by this _Object. </summary>
    protected void FlushBrakeCmds() //only write brakecommands if we have recieved any.
    {
        if (this.forceFeedback && this.brakeQueue.Count > 0)
        {
            int[] finalCommand = new int[] { 0, 0, 0, 0, 0 };
            for (int i = 0; i < this.brakeQueue.Count; i++)
            {
                for (int f = 0; f < 5; f++)
                    finalCommand[f] = Mathf.Max(finalCommand[f], brakeQueue[i][f]);
            }
            this.WriteBrakeCmd(finalCommand);
            this.brakeQueue.Clear(); 
        }
    }

    /// <summary> Send motor commands to the Sense Glove. summary>
    /// <param name="commands"></param>
    /// <returns></returns>
    public bool SendBrakeCmd(int[] commands)
    {
        if (commands.Length >= 5)
        {
            this.brakeQueue.Add(commands);
            if (this.brakeQueue.Count > SenseGlove_Object.maxBrakeCmds)
                this.brakeQueue.RemoveAt(0); //remove the earliest one.
            return true;
        }
        return false;
    }

    /// <summary> Tell the Sense Glove to set its brakes at the desired magnitude [0..100%] for each finger until it recieves a new command. </summary>
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

    /// <summary> Release all brakes of the SenseGlove.  </summary>
    /// <returns></returns>
    public bool StopBrakes()
    {
        return this.WriteBrakeCmd(new int[5]); //send 5x 0% directly.
    }

    #endregion ForceFeedback



    //--------------------------------------------------------------------------------------------------------------------------
    // Vibration

    #region Vibration

    /// <summary> Send a buzz-motor command to the Sense Glove, with optional parameters for each finger. </summary>
    /// <param name="fingers"></param>
    /// <param name="durations"></param>
    /// <param name="magnitudes"></param>
    /// <param name="patterns"></param>
    /// <returns></returns>
    /// <remarks> This is where the command is actually sent, with parameters for the fingers. All other SendBuzzCmd methods are wrappers. </remarks>
    public bool SendBuzzCmd(bool[] fingers, int[] durations = null, int[] magnitudes = null, BuzzMotorPattern[] patterns = null)
    {
        if (this.linkedGlove != null && this.linkedGlove.IsConnected())
        {
            return this.linkedGlove.BuzzMotorCmd(fingers, durations, magnitudes, patterns); //toto: queue instead of send(?)
        }
        return false;
    }

    /// <summary> Send one buzzmotor command to specific fingers, as indicated by the fingers array. </summary>
    /// <param name="fingers">The fingers (from thumb to pinky) to which to actually apply the buzzMotor command.</param>
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

    /// <summary> Tell the Buzz motors to each vibrate on a different magnitude (0..100%) for a specific duration (ms) </summary>
    /// <param name="magnitudes"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(int[] magnitudes, int duration)
    {
        return this.SendBuzzCmd(
            new bool[5] { true, true, true, true, true },
            new int[5] { duration, duration, duration, duration, duration },
            magnitudes);
    }

    /// <summary> Send a buzzmotor command to a specific finger. </summary>
    /// <param name="finger"></param>
    /// <param name="magnitude"></param>
    /// <param name="duration"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public bool SendBuzzCmd(Finger finger, int magnitude, int duration, BuzzMotorPattern pattern = BuzzMotorPattern.Constant)
    {
        bool[] fingers = new bool[5];
        if (finger == Finger.All)
            fingers = new bool[5] { true, true, true, true, true };
        else
            fingers[(int)finger] = true;
        return this.SendBuzzCmd(fingers, magnitude, duration, pattern);
    }

    /// <summary> Stop all vibration feedback on the Sense Glove. </summary>
    /// <returns></returns>
    public bool StopBuzzMotors()
    {
        if (this.linkedGlove != null)
            return this.linkedGlove.StopBuzzMotors(); //using a specific command here so that no 'stop' command is sent again after x seconds.
        return false;
    }


    /// <summary> Play an effect using the Thumper module on this glove (if it has any). </summary>
    /// <param name="effect"></param>
    /// <returns></returns>
    public bool SendThumperCmd(SenseGloveCs.ThumperEffect effect)
    {
        if (this.linkedGlove != null)
            return this.linkedGlove.SendThumperCmd(effect);
        return false;
    }



    #endregion Vibration

    #endregion Feedback


    //--------------------------------------------------------------------------------------------------------------------------
    // Calibration Methods

    #region Calibration

    /// <summary> The Calibration of the Sense Glove should be ready to fire. </summary>
    /// <param name="args"></param>
    /// <param name="fromDLL"></param>
    protected virtual void ReadyCalibration(GloveCalibrationArgs args, bool fromDLL)
    {
        if (fromDLL) //comes from a possibly async thread within the DLL
            this.calibrationArguments.Add(args); //queue
        else
            this.FinishCalibration(args); //Unity's main thread. just fire, no queue.
    }

    /// <summary> Check if we have any CalibrationComplete events queued, then send them. </summary>
    /// <remarks>Placed indside a seprate method so we can call it during both Update and LateUpdate. Should only be fired from these</remarks>
    protected virtual void CheckCalibration()
    {
        if (this.calibrationArguments.Count > 0)
        {
            if (this.linkedGlove != null && this.linkedGloveData != null)
                this.linkedGloveData.UpdateVariables(this.linkedGlove.gloveData); //update before fire.

            for (int i = 0; i < calibrationArguments.Count; i++)
            {
                this.FinishCalibration(this.calibrationArguments[i]);
            }
            this.calibrationArguments.Clear();
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // General Getting / Setting

    #region CalibrationAccess

    /// <summary> The finger lengths used by this sense glove as a 5x3 array, 
    /// which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order, in mm. </summary>
    public float[][] FingerLengths
    {
        get
        {
            if (this.linkedGloveData != null)
            {
                return this.linkedGloveData.GetFingerLengths();
            }
            return new float[][] { };
        }
        set
        {
            if (this.linkedGlove != null && value != null)
            {
                this.linkedGlove.SetHandLengths(value);
                SenseGlove_Data newData = new SenseGlove_Data(this.linkedGlove.GetData(false));
                this.ReadyCalibration(new GloveCalibrationArgs(this.linkedGloveData, newData), false);
            }
        }
    }

    /// <summary> The positions of the starting finger joints, the CMC or MCP joints, relative to the glove origin. </summary>
    /// <returns></returns>
    public Vector3[] StartJointPositions
    {
        get
        {
            if (this.linkedGloveData != null)
            {
                return this.linkedGloveData.GetJointPositions();
            }
            return new Vector3[] { };
        }
        set
        {
            if (this.linkedGlove != null && value != null)
            {
                this.linkedGlove.SetJointPositions(SenseGlove_Util.ToPosition(value));
                SenseGlove_Data newData = new SenseGlove_Data(this.linkedGlove.GetData(false));
                this.ReadyCalibration(new GloveCalibrationArgs(this.linkedGloveData, newData), false);
            }
        }
    }

    /// <summary> Apply hand parameters to the Sense Glove internal model. </summary>
    /// <param name="jointPositions"></param>
    /// <param name="handLengths"></param>
    public void SetHandParameters(Vector3[] jointPositions, Vector3[][] handLengths)
    {
        if (this.linkedGlove != null && this.linkedGlove.IsReady() && jointPositions.Length > 4 && handLengths.Length > 4)
        {
            this.StartJointPositions = jointPositions;
            for (int f=0; f<handLengths.Length; f++)
            {
                if (handLengths[f].Length > 2)
                {   //Set Hand Lengths
                    for (int i = 0; i < handLengths[f].Length; i++)
                        this.linkedGlove.gloveData.kinematics.fingers[f].lengths[i] = SenseGlove_Util.ToPosition(handLengths[f][i]);
                }
            }
            Debug.Log(this.name  + " Applied hand Lengths to Sense Glove Internal Model");
        }
    }


    /// <summary> Reset the internal handmodel back to the default finger lengths and -positions </summary>
    public void ResetKinematics()
    {
        if (this.linkedGlove != null)
            this.linkedGlove.RestoreHand();
        SenseGlove_Data newData = new SenseGlove_Data(this.linkedGlove.GetData(false));
        this.ReadyCalibration(new GloveCalibrationArgs(this.linkedGloveData, newData), false);
    }


    #endregion CalibrationAccess


    //--------------------------------------------------------------------------------------------------------------------------
    // Internal Calibration Algorithms

    #region InternalCalibration

    /// <summary> Reset the Calibration of the glove if, for instance, something went wrong, or if we are shutting down. </summary>
    public void CancelCalibration()
    {
        if (linkedGlove != null)
        {
            linkedGlove.StopCalibration();
            SenseGlove_Debugger.Log("Canceled Calibration");
        }
    }



    /// <summary> Calibrate a variable related to the glove or a solver, with a specific collection method. </summary>
    /// <param name="whichFingers"></param>
    /// <param name="simpleCalibration"></param>
    /// <returns></returns>
    public bool StartCalibration(CalibrateVariable whatToCalibrate, CollectionMethod howToCollect)
    {
        if (this.linkedGlove != null)
        {
            return this.linkedGlove.StartCalibration(whatToCalibrate, howToCollect);
        }
        return false;
    }


    /// <summary> Continue the next calibration steps (no reporting of progress) </summary>
    /// <returns></returns>
    public bool NextCalibrationStep()
    {
        if (this.linkedGlove != null)
            return this.linkedGlove.NextCalibrationStep();
        return false;
    }


    /// <summary> Fires when the glove's internal calibration finished, which may have come from a worker thread. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void LinkedGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        this.ReadyCalibration(new GloveCalibrationArgs(args), true);
    }


    #endregion InternalCalibration

    #endregion Calibration


    protected static char calValDelim = ';';
    protected static char calFDelim = '|';
    protected static char calBlockDelim = ':';


    /// <summary> The the Calibration data from this Sense Glove and serialize it for later use. </summary>
    /// <returns></returns>
    public string SerializeCalibration()
    {
        if (this.linkedGlove != null && this.linkedGloveData != null)
        {
            string FLString = "", JPString = "", IPString = "";


            //gets finger lengths,
            float[][] FL = this.FingerLengths;
            for (int f=0; f<FL.Length; f++)
            {
                for (int j=0; j<FL[f].Length; j++)
                {
                    FLString += FL[f][j];
                    if (j != FL[f].Length - 1)
                        FLString += calValDelim;
                }
                if (f != FL.Length - 1) //except for the one.
                    FLString += calFDelim;
            }

            //gets joint positions
            Vector3[] jointPos = this.StartJointPositions;
            for (int f = 0; f < jointPos.Length; f++)
            {
                JPString += (jointPos[f].x.ToString() + calValDelim + jointPos[f].y + calValDelim + jointPos[f].z.ToString());
                if (f != jointPos.Length - 1) //except for the one.
                    JPString += calFDelim;
            }

            IPString = this.linkedGlove.GetInterpolationValues();

            return FLString + calBlockDelim + JPString + calBlockDelim + IPString;
        }
        return "";
    }

    /// <summary> Load a serialized calibration data from another session. </summary>
    /// <param name="calibrationData"></param>
    public void LoadCalibrationData(string calibrationData)
    {
        if (this.linkedGlove != null && calibrationData != null && calibrationData.Length > 0)
        {
            string[] split = calibrationData.Split(calBlockDelim);
            if (split.Length > 0)
            {
                //load finger lengths
                string[] lFingers = split[0].Split(calFDelim);
                float[][] FL = new float[lFingers.Length][];
                for (int f = 0; f < lFingers.Length; f++)
                {
                    string[] fVals = lFingers[f].Split(calValDelim);
                    FL[f] = new float[fVals.Length];
                    for (int j = 0; j < fVals.Length; j++)
                        FL[f][j] = SenseGloveCs.Values.toFloat(fVals[j]);
                }
                this.FingerLengths = FL;

                if (split.Length > 1)
                {
                    //load joint positions
                    string[] pJoints = split[1].Split(calFDelim);
                    Vector3[] joints = new Vector3[pJoints.Length];
                    for (int f = 0; f < pJoints.Length; f++)
                    {
                        string[] vect = pJoints[f].Split(calValDelim);
                        if (vect.Length > 2)
                        {
                            joints[f] = new Vector3
                            (
                                SenseGloveCs.Values.toFloat(vect[0]),
                                SenseGloveCs.Values.toFloat(vect[1]),
                                SenseGloveCs.Values.toFloat(vect[2])
                            );
                        }
                        else
                            joints[f] = Vector3.zero;
                    }
                    this.StartJointPositions = joints;

                    //Loads interpolation values.
                    if (split.Length > 2 && split[2].Length > 0)
                    {
                        this.linkedGlove.SetInterpolationValues(split[2]);
                    }
                }
            }
            

        }
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start()
    {
        this.CheckForDeviceManager();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        this.UpdateGlove();
        this.CheckConnection(); //must be placed after checkConnection, otherwise a nullref might occur.
    }

    //Fires after all the updates.
    protected virtual void LateUpdate()
    {
        this.CheckCalibration(); //Placed here, so that all scripts that use the Sense Glove durign the Update() have access to the same data.
        this.FlushBrakeCmds();
    }

    protected virtual void OnDestroy()
    {
        this.CancelCalibration(); //cancel to free resources for other SenseGlove_Objects
    }

    protected virtual void OnApplicationQuit()
    {
        this.CancelCalibration(); //cancel to free resources for other SenseGlove_Objects
    }

    //Fires whenever the user changes a value
    protected virtual void OnValidate()
    {
        if (this.trackedGloveIndex != this.actualTrackedIndex)
        {
            this.trackedGloveIndex = this.actualTrackedIndex;
        }
    }

    #endregion Monobehaviour

}
