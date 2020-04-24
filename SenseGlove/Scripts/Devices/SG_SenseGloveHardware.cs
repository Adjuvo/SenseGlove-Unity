﻿using System.Collections.Generic;
using UnityEngine;
using SenseGloveCs;
using SenseGloveCs.Kinematics;

namespace SG
{

    /// <summary> After being linked to a proper Sense Glove via the SenseGlove_DeviceManager, this script is
    /// responsible for updating SG_SenseGloveData every frame, and for exposing feedback - and calibration methods. </summary>
    public class SG_SenseGloveHardware : SG_DeviceLink
    {

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
            public SG_SenseGloveData oldData;

            /// <summary> 'Snapshot' of the new data, with updated parameters </summary>
            public SG_SenseGloveData newData;

            /// <summary> Creates a new instance of the unity-friendly calibration args. </summary>
            /// <param name="args"></param>
            public GloveCalibrationArgs(SenseGloveCs.CalibrationArgs args)
            {
                this.oldData = new SG_SenseGloveData(args.before);
                this.newData = new SG_SenseGloveData(args.after);
            }


            /// <summary> Creates a new instance of the unity-friendly calibration args. </summary>
            /// <param name="args"></param>
            public GloveCalibrationArgs(SG_SenseGloveData oldD, SG_SenseGloveData newD)
            {
                this.oldData = oldD;
                this.newData = newD;
            }

        }



        public enum HapticSendMode
        {
            OnChange, //only send FFB when a difference is detected
            OnFrame, //every frame
            Off,
            OnChangeRepeat, //send max of x commands onchange, helps improve performance for WLK.
        }


        public class BuzzCmd
        {
            public bool[] fingers;
            public float[] durations;
            public float[] times;
            public int[] magnitudes;
            public static readonly BuzzMotorPattern[] patterns = new BuzzMotorPattern[5]
            { BuzzMotorPattern.Constant, BuzzMotorPattern.Constant,
                BuzzMotorPattern.Constant, BuzzMotorPattern.Constant, BuzzMotorPattern.Constant };

            protected int elapsed = 0;

            public bool FullyElapsed
            {
                get { return elapsed >= 5; }
            }

            public BuzzCmd(bool[] fin, int[] magn, int[] dur)
            {
                fingers = new bool[5];
                durations = new float[5];
                magnitudes = new int[5];
                for (int f = 0; f < 5; f++)
                {
                    fingers[f] = fin[f];
                    durations[f] = dur[f] / 1000f; //in s
                    magnitudes[f] = magn[f];
                    if (!fin[f] || durations[f] <= 0) { elapsed++; } //if we don't want a finger or havent given it a duration
                }
                times = new float[5];
            }

            public void Update(float deltaTime)
            {
                for (int f = 0; f < 5; f++)
                {
                    if (times[f] < durations[f])
                    {
                        times[f] = times[f] + deltaTime;
                        if (times[f] >= durations[f])
                        {
                            elapsed++;
                        }
                    }
                }
            }

            public void Merge(ref int[] buffer) //merge this buzzCmd with a buffer.
            {
                for (int f = 0; f < 5; f++)
                {
                    if (fingers[f] && times[f] < durations[f])
                    {
                        buffer[f] = Mathf.Max(buffer[f], magnitudes[f]);
                    }
                }
            }

        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Properties


        /// <summary> The way with which the SG_SenseGloveHardware connects to a glove. </summary>
        [Header("Connection Settings")]
        public ConnectionMethod connectionMethod = ConnectionMethod.NextGlove;

        /// <summary> Allows this Sense Glove_Object to manage its own connection status </summary>
        [SerializeField] protected bool autoConnect = true;

        /// <summary> When Haptic Commands are sent to the SenseGlove Hardware. </summary>
        protected HapticSendMode sendHaptics = HapticSendMode.OnChangeRepeat;

        public bool FFB_Enabled = true;
        /// <summary> </summary>
        public bool buzz_Enabled = true;

        /// <summary> The Solver used to calculate this Sense Glove's hand model each frame. </summary>
        //[Header("Kinematics Settings")]
        protected Solver solver = Solver.Interpolate4Sensors;

        /// <summary> Whether or not to apply natural limits to the fingers. </summary>
        /// <remarks> Marked as protected since these wil likely always be true during normal use. </remarks>
        protected bool limitFingers = true;

        /// <summary> Whether or not to update the wrist model of the Sense Glove. </summary>
        /// <remarks> We will always update it, but calibrate it at hand-model level. </remarks>
        protected bool updateWrist = true;

        /// <summary> Unity Even that fires when this script is assigned to a Sense Glove </summary>
        [Header("Events")]
        [Tooltip("Unity Event that fires when this script is assigned to a Sense Glove ")]
        public SGEvent OnGloveLoad;

        /// <summary> The Internal Sense Glove object that is linked to this monobehaviour Object </summary>
        protected SenseGlove linkedGlove = null;

        /// <summary> The last data from the linked glove. </summary>
        protected SG_SenseGloveData linkedGloveData = SG_SenseGloveData.Empty; //an empty data struct

        /// <summary> Queued Calibration Command from the fingers, which will fire during Unity's next LateUpdate() (so as to allow acces to transforms) </summary>
        protected List<GloveCalibrationArgs> calibrationArguments = new List<GloveCalibrationArgs>();

        /// <summary> Whether or not the linked glove was connected the last time we checked. </summary>
        protected bool wasConnected = false;

        /// <summary> Command queue for the brakes, which is flushed at the end of every Update function. </summary>
        protected List<int[]> brakeQueue = new List<int[]>();
        protected int nextThump = (int)ThumperEffect.None;

        /// <summary> The last sent brake command </summary>
        protected int[] lastBrakeLvls = new int[5];


        protected List<BuzzCmd> buzzQueue = new List<BuzzCmd>();
        protected int[] lastBuzzLvls = new int[5];
        protected int lastThump = (int)ThumperEffect.None;

        //Static properties

        /// <summary> Saves setup time for multiple SenseGlove_Objects checking for DeviceManager's existance. </summary>
        public static bool deviceScannerPresent = false;

        protected static int maxBrakeCmds = 10;
        protected static int maxBuzzCmds = 20;


        protected bool newLinkMade = false;

        protected int cmdsSend = 0;
        protected int maxCmdRepeat = 2; //command is sent 2x max

        /// <summary> If the average force-feedback levels are above this threshold, we should not fire Thumper Commands. </summary>
        protected static int thumpFFBThreshold = 70;
        protected static int thumpBuzzThreshold = 70;

        //--------------------------------------------------------------------------------------------------------------------------
        // Basic Accessors, Gets


        /// <summary> If true, this Sense Glove is connected to a right hand. Otherwise, it is connected to a left hand. </summary>
        public bool IsRight
        {
            get { return this.linkedGlove != null && this.linkedGlove.IsRight(); }
        }

        /// <summary> Determines if this glove is ready and linked to the hardware. </summary>
        public bool GloveReady
        {
            get { return this.IsLinked && this.newLinkMade; }
        }

        /// <summary> Check if the Sense Glove is connected. </summary>
        public override bool IsConnected
        {
            get { return this.linkedGlove != null && this.linkedGlove.IsConnected(); }
        }

        /// <summary> Retrieve Unity-Friendly Glove Data from the Sense Glove. </summary>
        public SG_SenseGloveData GloveData
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

        protected override bool CanLinkTo(IODevice device)
        {
            if (device != null && device is SenseGloveCs.SenseGlove)
            {
                if (this.connectionMethod == ConnectionMethod.NextGlove)
                    return true;

                SenseGlove glove = ((SenseGlove)device);
                return (glove.IsRight() && this.connectionMethod == ConnectionMethod.NextRightHand)
                    || (!glove.IsRight() && this.connectionMethod == ConnectionMethod.NextLeftHand);
            }
            return false;
        }

        protected override void SetupDevice()
        {
            this.linkedGlove = (SenseGlove)this.linkedDevice;
            //this.linkedGlove.OnFingerCalibrationFinished += LinkedGlove_OnCalibrationFinished;
            //uodating once so glovedata is available, but not through the UpdateHand command, as that one needs the glove to already be connected.
            SenseGloveCs.GloveData rawData = this.linkedGlove.GetData(false);
            this.linkedGloveData = new SG_SenseGloveData(rawData);

            //Device has been linked, now retrieve calibration.
            string serialized;
            if (SG.Calibration.SG_CalibrationStorage.LoadInterpolation(SenseGloveCs.DeviceType.SenseGlove, linkedGloveData.gloveSide, out serialized))
            {
                //Debug.Log("Loaded Calibration for the " + (linkedGloveData.gloveSide == GloveSide.LeftHand ? "Left Hand" : "Right Hand"));
                linkedGlove.SetInterpolationValues(serialized); //sets internal values 
            }
        }

        /// <summary> Unlink this glove from the manager. </summary>
        protected override void DisposeDevice()
        {
            // this.CancelCalibration();
            this.wasConnected = false;

            this.OnGloveUnLink(); //fire unlink event
                                  //if (this.linkedGlove != null)
                                  //    this.linkedGlove.OnFingerCalibrationFinished -= LinkedGlove_OnCalibrationFinished;


        }


        /// <summary> Check if a device manager is currently active within the Scene. If not, create an instance to manager our connection(s). </summary>
        protected void CheckForDeviceManager()
        {
            SG_DeviceManager manager = null;
            if (!SG_SenseGloveHardware.deviceScannerPresent)
            {
                object deviceManagerObj = GameObject.FindObjectOfType(typeof(SG_DeviceManager));
                if (deviceManagerObj != null) //it exists
                {
                    manager = (SG_DeviceManager)deviceManagerObj;
                    if (!manager.isActiveAndEnabled || !manager.gameObject.activeInHierarchy)
                    {
                        Debug.LogWarning("WARNING: The SenseGlove_DeviceManager is not active in the scene. Objects will not connect until it is active.");
                    }
                }
                else
                {
                    GameObject newdeviceManager = new GameObject("[SG DeviceManager]");
                    manager = newdeviceManager.AddComponent<SG_DeviceManager>();
                }
                SG_SenseGloveHardware.deviceScannerPresent = true; //we have either found it or created it.
            }
            else //it already exists
            {
                manager = (SG_DeviceManager)GameObject.FindObjectOfType(typeof(SG_DeviceManager));
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
            //Debug.Log(this.name + " Linked to Sense Glove " + this.linkedGlove.DeviceID());
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
            //Debug.Log(this.name + " Unlinked");
            if (GloveUnLoaded != null)
            {
                GloveUnLoaded(this, null);
            }
        }


        // Connect - WIP

        ///// <summary> Called when the glove linked to this SG_SenseGloveHardware (re)Connects to the system. </summary>
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

        ///// <summary> Called when the glove linked to this SG_SenseGloveHardware disconnects. </summary>
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
            return StopBrakes() && StopBuzzMotors();
        }




        /// <summary> Update and flush all commands for this Sense Glove. </summary>
        protected void FlushCmds() //only write brakecommands if we have recieved any.
        {
            int[] finalBrakeCmd = new int[5];
            int[] finalBuzzCmd = new int[5];

            if (FFB_Enabled) //update finalBrakeCmd
            {
                if (brakeQueue.Count > 0)
                {
                    for (int i = 0; i < brakeQueue.Count; i++)
                    {
                        for (int f = 0; f < 5; f++)
                            finalBrakeCmd[f] = Mathf.Max(finalBrakeCmd[f], brakeQueue[i][f]);
                    }
                }
                else
                {
                    finalBrakeCmd = lastBrakeLvls; //no new cmds, so lets stay as we are.
                }
            }
            brakeQueue.Clear(); //clear buffer for next frame.


            for (int i = 0; i < buzzQueue.Count;)
            {
                buzzQueue[i].Update(Time.deltaTime);
                if (buzzQueue[i].FullyElapsed) //no more relevant cmds
                {
                    buzzQueue.RemoveAt(i); //do nothing
                }
                else
                {
                    if (buzz_Enabled)
                    {
                        buzzQueue[i].Merge(ref finalBuzzCmd); //add to final cmd
                    }
                    i++;
                }
            }

            int finalThump = this.nextThump;
            if (finalThump != (int)ThumperEffect.None && finalThump != (int)ThumperEffect.TurnOff
                && (SG_Util.Average(finalBrakeCmd) >= thumpFFBThreshold || SG_Util.Average(finalBuzzCmd) >= thumpBuzzThreshold))
            {
                Debug.Log("Too much other jazz, reset Thump");
                finalThump = (int)ThumperEffect.None; //do not sent thumper commands if there is too much FFB active.
            }

            if (sendHaptics == HapticSendMode.OnChange)
            {
                if (finalThump != (int)ThumperEffect.None || !AlreadySent(finalBrakeCmd, lastBrakeLvls) || !AlreadySent(finalBuzzCmd, lastBuzzLvls))
                {
                    WriteHaptics(finalBrakeCmd, finalBuzzCmd, finalThump);
                }
            }
            else if (sendHaptics == HapticSendMode.OnChangeRepeat)
            {
                if (finalThump != (int)ThumperEffect.None || !AlreadySent(finalBrakeCmd, lastBrakeLvls) || !AlreadySent(finalBuzzCmd, lastBuzzLvls))
                {
                    cmdsSend = 1; //its a new command
                    WriteHaptics(finalBrakeCmd, finalBuzzCmd, finalThump);
                }
                else if (cmdsSend < maxCmdRepeat)
                {
                    cmdsSend++;
                    WriteHaptics(finalBrakeCmd, finalBuzzCmd, finalThump);
                }
            }
            else if (sendHaptics == HapticSendMode.OnFrame)
            {
                WriteHaptics(finalBrakeCmd, finalBuzzCmd, finalThump);
            }
        }

        protected bool WriteHaptics(int[] brakeLvls, int[] buzzLvls, int thumperEffect)
        {
            if (linkedGlove != null && linkedGlove.IsConnected())
            {
                bool sent = linkedGlove.SendHaptics(brakeLvls, buzzLvls, (ThumperEffect)thumperEffect);
                if (sent)
                {
                    lastBrakeLvls = new int[brakeLvls.Length];
                    for (int i = 0; i < lastBrakeLvls.Length; i++) { lastBrakeLvls[i] = brakeLvls[i]; } //deep copy

                    lastBuzzLvls = new int[buzzLvls.Length];
                    for (int i = 0; i < lastBuzzLvls.Length; i++) { lastBuzzLvls[i] = buzzLvls[i]; } //deep copy

                    lastThump = thumperEffect;
                    nextThump = (int)ThumperEffect.None;
                }
                return sent;
            }
            return false;
        }

        protected bool AlreadySent(int[] cmds, int[] lastCmd)
        {
            if (cmds.Length != lastCmd.Length)
                return false;
            for (int i = 0; i < cmds.Length; i++)
            {
                if (cmds[i] != lastCmd[i])
                    return false;
            }   //we did not find anything that did not match,
            return true;
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
                bool sent = this.linkedGlove.BrakeCmd(commands);
                if (sent)
                {
                    lastBrakeLvls = new int[commands.Length];
                    for (int i = 0; i < lastBrakeLvls.Length; i++) { lastBrakeLvls[i] = commands[i]; } //deep copy
                }
                return sent;
            }
            return false;
        }






        /// <summary> Send motor commands to the Sense Glove. summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public bool SendBrakeCmd(int[] commands)
        {
            if (IsLinked && commands.Length >= 5)
            {
                this.brakeQueue.Add(commands);
                if (this.brakeQueue.Count > SG_SenseGloveHardware.maxBrakeCmds)
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
            //if (this.linkedGlove != null && this.linkedGlove.IsConnected())
            //{
            //    return this.linkedGlove.BuzzMotorCmd(fingers, durations, magnitudes, patterns); //toto: queue instead of send(?)
            //}
            if (IsLinked)
            {
                buzzQueue.Add(new BuzzCmd(fingers, magnitudes, durations));
                if (buzzQueue.Count > SG_SenseGloveHardware.maxBuzzCmds)
                    buzzQueue.RemoveAt(0); //remove the earliest one.
                return true;
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
            if (GloveData.firmwareVersion >= 4.6f)
            {
                nextThump = (int)effect; //added to queue
                Debug.Log("Queueing " + nextThump.ToString());
                return true;
            }
            else if (linkedGlove != null && linkedGlove.IsConnected())
            {
                if (SG_Util.Average(lastBrakeLvls) < thumpFFBThreshold)
                {
                    return linkedGlove.SendThumperCmd(effect);
                }
            }
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
                    SG_SenseGloveData newData = new SG_SenseGloveData(this.linkedGlove.GetData(false));
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
                    this.linkedGlove.SetJointPositions(SG_Util.ToPosition(value));
                    SG_SenseGloveData newData = new SG_SenseGloveData(this.linkedGlove.GetData(false));
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
                for (int f = 0; f < handLengths.Length; f++)
                {
                    if (handLengths[f].Length > 2)
                    {   //Set Hand Lengths
                        for (int i = 0; i < handLengths[f].Length; i++)
                            this.linkedGlove.gloveData.kinematics.fingers[f].lengths[i] = SG_Util.ToPosition(handLengths[f][i]);
                    }
                }
                Debug.Log(this.name + " Applied hand Lengths to Sense Glove Internal Model");
            }
        }


        /// <summary> Reset the internal handmodel back to the default finger lengths and -positions </summary>
        public void ResetKinematics()
        {
            if (this.linkedGlove != null)
                this.linkedGlove.RestoreHand();
            SG_SenseGloveData newData = new SG_SenseGloveData(this.linkedGlove.GetData(false));
            this.ReadyCalibration(new GloveCalibrationArgs(this.linkedGloveData, newData), false);
        }

        public void SaveHandCalibration()
        {
            //save calibration data
            if (linkedGlove != null)
            {
                SG.Calibration.SG_CalibrationStorage.StoreInterpolation(this.linkedGlove.GetInterpolationValues(),
                    SenseGloveCs.DeviceType.SenseGlove, this.linkedGloveData.gloveSide);
            }
        }


        #endregion CalibrationAccess


        //--------------------------------------------------------------------------------------------------------------------------
        // Internal Calibration Algorithms

        #region InternalCalibration

        ///// <summary> Reset the Calibration of the glove if, for instance, something went wrong, or if we are shutting down. </summary>
        //public void CancelCalibration()
        //{
        //    if (linkedGlove != null)
        //    {
        //        linkedGlove.StopCalibration();
        //        SenseGlove_Debugger.Log("Canceled Calibration");
        //    }
        //}



        ///// <summary> Calibrate a variable related to the glove or a solver, with a specific collection method. </summary>
        ///// <param name="whichFingers"></param>
        ///// <param name="simpleCalibration"></param>
        ///// <returns></returns>
        //public bool StartCalibration(CalibrateVariable whatToCalibrate, CollectionMethod howToCollect)
        //{
        //    if (this.linkedGlove != null)
        //    {
        //        return this.linkedGlove.StartCalibration(whatToCalibrate, howToCollect);
        //    }
        //    return false;
        //}


        ///// <summary> Continue the next calibration steps (no reporting of progress) </summary>
        ///// <returns></returns>
        //public bool NextCalibrationStep()
        //{
        //    if (this.linkedGlove != null)
        //        return this.linkedGlove.NextCalibrationStep();
        //    return false;
        //}


        ///// <summary> Fires when the glove's internal calibration finished, which may have come from a worker thread. </summary>
        ///// <param name="source"></param>
        ///// <param name="args"></param>
        //private void LinkedGlove_OnCalibrationFinished(object source, CalibrationArgs args)
        //{
        //    this.ReadyCalibration(new GloveCalibrationArgs(args), true);
        //}


        #endregion InternalCalibration

        #endregion Calibration

        public bool GetInterpolationProfile(out SenseGloveCs.Kinematics.InterpolationSet_IMU set)
        {
            if (linkedGlove != null)
            {
                set = SenseGloveCs.Kinematics.InterpolationSet_IMU.Deserialize(linkedGlove.GetInterpolationValues(),
                    linkedGlove.IsRight()); //reserialize so that we return a copy, and not a direct reference.
            }
            else { set = null; }
            return set != null;
        }


        public bool SetInterpolationProfile(SenseGloveCs.Kinematics.InterpolationSet_IMU set)
        {
            if (linkedGlove != null)
            {
                linkedGlove.SetInterpolationValues(set.Serialize());
                return true;
            }
            return false;
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
            if (this.linkedGlove != null && !newLinkMade)
            {
                if (this.linkedGlove.gloveData.gloveValues.Length > 0)
                {
                    this.newLinkMade = true;
                    this.OnGloveLink();
                }
            }
            this.CheckConnection(); //must be placed after checkConnection, otherwise a nullref might occur.
        }

        //Fires after all the updates.
        protected virtual void LateUpdate()
        {
            this.CheckCalibration(); //Placed here, so that all scripts that use the Sense Glove durign the Update() have access to the same data.
            if (IsLinked)
                this.FlushCmds();
        }

        protected override void OnDestroy()
        {
            //this.CancelCalibration(); //cancel to free resources for other SenseGlove_Objects
        }

        protected virtual void OnApplicationQuit()
        {
            //this.CancelCalibration(); //cancel to free resources for other SenseGlove_Objects
        }

        #endregion Monobehaviour

    }
}