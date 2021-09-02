using SGCore.Calibration;
using SGCore.Haptics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG
{
    /// <summary> The hardware this hand is tracked with. Used to set up connections and calculate offsets. </summary>
    public enum HandSide
    {
        /// <summary> This script only connects to left-handed gloves. </summary>
        LeftHand,
        /// <summary> This script only connects to right-handed gloves. </summary>
        RightHand,
        /// <summary> This script connects to the first glove this is connected to the system. </summary>
        Any
    }

    /// <summary> Interface for a left- or right handed glove built by SenseGlove. Usually either a SenseGlove DK1 or a Nova Glove. </summary>
    public class SG_HapticGlove : MonoBehaviour
	{
        /// <summary> How much of the CV tracking is being used. </summary>
        public enum CV_Tracking_Depth
        {
            /// <summary> No computer-vision is enabled. </summary>
            Disabled = 0,
            /// <summary> Use CV only for the wrist position / rotation </summary>
            WristOnly,
            /// <summary> Use CV for both the wirst location and Hand Position  </summary>
            WristAndHandPose
        }

        //--------------------------------------------------------------------------------------------------------
        // Variables

        /// <summary> Which side of the hand this glove connects to </summary>
        public HandSide connectsTo = HandSide.Any;

        /// <summary> If true, the user must move their hands at the beginning of the play session to make sure the glove is properly calibrated. Though only if this is a Nova Glove. </summary>
        public bool checkCalibrationOnStart = false; //todo: Write a check that allows us to skip this if a scene change occured.


        /// <summary>  Fires when this HapitcGlove connect from the system while this application is running. </summary>
        public UnityEvent DeviceConnected;

        /// <summary> Fires when this HapitcGlove disconnects from the system while this application is running. </summary>
        public UnityEvent DeviceDisconnected;

        /// <summary> Fires when this glove's calibration state changes. </summary>
		public UnityEvent CalibrationStateChanged;

        /// <summary> The current Caliobration sequenced using this glove. Used tolimit the amount of calibration sequences that a glove can have active </summary>
        protected SG_CalibrationSequence currentSequence = null;

        /// <summary> This JapticGlove's calibration Stage. </summary>
        public CalibrationStage CalibrationStage
        {
            get; private set;
        }

        /// <summary> Whether or not this glove is currently being calibrated by a Calibration Sequence. </summary>
        public bool CalibrationLocked
        {
            get; set;
        }



        /// <summary> The glove we were connected to since the last "Hardware Tick" </summary>
        protected SGCore.HapticGlove lastGlove = null;
        /// <summary> How ofter we check for new hardware (states). </summary>
        protected float hwTimer = 0.5f;
        /// <summary> Timer to activate HardwareTick. </summary>
        protected float checkTime = 0;
        /// <summary> Connection state during the last Hardware Tick. Used to fire Connected/Disconnected events. </summary>
        protected bool wasConnected = false;


        /// <summary> If true, we can retrieve a new HandPose this frame. </summary>
        private bool newPoseNeeded = true;
        /// <summary> HandPose as calculated in the first GetHandPose function of this frame. Used to prevent redundant calculations. </summary>
		protected SG_HandPose lastHandPose = null;
        /// <summary> The last handKinematcs used for the GetHandPose function. Used so you can request HandPoses without needing access to HandKinematics. </summary>
		protected SGCore.Kinematics.BasicHandModel lastHandModel = null;

        // Internal cv stuff

        /// <summary> How much of the CV we're using for Tracking. </summary>
        protected CV_Tracking_Depth cvLevel = CV_Tracking_Depth.WristOnly;


        // Internal haptics Stuff.

        /// <summary> Enables / Disables Force-Feedback on individual fingers. </summary>
        public bool[] ffbEnabled = new bool[5] { true, true, true, true, true };
        /// <summary> Enables / Disables Vibrotactile-Feedback on individual fingers. </summary>
		public bool[] vibroEnabled = new bool[5] { true, true, true, true, true };

        protected static readonly int maxQueue = 25;
        protected List<SGCore.Haptics.SG_FFBCmd> ffbQueue = new List<SGCore.Haptics.SG_FFBCmd>(maxQueue);
        protected List<SGCore.Haptics.SG_TimedBuzzCmd> buzzQueue = new List<SGCore.Haptics.SG_TimedBuzzCmd>(maxQueue);
        protected SGCore.Haptics.SG_FFBCmd lastFFB = SGCore.Haptics.SG_FFBCmd.Off;
        protected SGCore.Haptics.SG_BuzzCmd lastBuzz = SGCore.Haptics.SG_BuzzCmd.Off;

        protected List<TimedThumpCmd> thumperQueue = new List<TimedThumpCmd>();
        protected SGCore.Haptics.ThumperCmd lastThumpCmd = ThumperCmd.Off;

        /// <summary> Used to check if your Haptic Glove needs calibration. </summary>
        protected SGCore.Calibration.HG_CalCheck calibrationCheck = new HG_CalCheck(null);


        /// <summary> The DeviceType of this glove. Use this to distinguish between SenseGlove and Nova. Is unknown if the glove is not connected. </summary>
        public SGCore.DeviceType DeviceType
        {
            get; private set;
        }


        //--------------------------------------------------------------------------------------------------------
        // Accessors


        /// <summary> Returns true if this HapticGlove is currently connected. </summary>
        public bool IsConnected
        {
            get
            {
                return this.lastGlove != null && this.lastGlove.IsConnected();
            }
        }


        /// <summary> Returns true if this HapticGlove is currently connected to a right hand. </summary>
        public bool IsRight
        {
            get
            {
                if (this.IsConnected)
                {
                    return this.lastGlove.IsRight();
                }
                return this.connectsTo != HandSide.LeftHand;
            }
        }


        /// <summary> The Internal HapticGlove that this glove is linked to. Is null when not connected. </summary>
        public SGCore.HapticGlove InternalGlove
        {
            get  { return this.lastGlove; }
        }


        /// <summary> Enable / Disable all Force-Feedack. </summary>
        public bool ForceFeedbackEnabled
        {
            get
            {
                for (int f = 0; f < ffbEnabled.Length; f++)
                {
                    if (!ffbEnabled[f]) { return false; };
                }
                return true;
            }
            set { for (int f = 0; f < ffbEnabled.Length; f++) { ffbEnabled[f] = value; } }
        }


        /// <summary> Enables / Disables all vibrotatcile feedback. </summary>
        public bool VibroTactileEnabled
        {
            get
            {
                for (int f = 0; f < vibroEnabled.Length; f++)
                {
                    if (!vibroEnabled[f]) { return false; };
                }
                return true;
            }
            set { for (int f = 0; f < vibroEnabled.Length; f++) { vibroEnabled[f] = value; } }
        }



        //--------------------------------------------------------------------------------------------------------
        // Connection Functions


        /// <summary> Returns the first valid instance of a SGCore.HapticGlove object, based on HandSide parameters </summary>
        /// <param name="handSide"></param>
        /// <param name="gloveInstance"></param>
        /// <returns></returns>
        public static bool GetGloveInstance(HandSide handSide, out SGCore.HapticGlove gloveInstance)
        {
            if (handSide == HandSide.Any)
            {
                return SGCore.HapticGlove.GetGlove(out gloveInstance);
            }
            return SGCore.HapticGlove.GetGlove(handSide == HandSide.RightHand, out gloveInstance);
        }

        /// <summary> Updates the connection status and fires events if needed </summary>
        protected void UpdateConnection()
        {
            SGCore.HapticGlove uGlove;
            if (GetGloveInstance(this.connectsTo, out uGlove))
            {
                lastGlove = uGlove; //updates the refrence
                if (!wasConnected) //glove has reconnected
                {
                    UpdateDeviceType();
                    this.CheckForCalibration();
                    this.DeviceConnected.Invoke();
                }
                wasConnected = true;
            }
            else //No valid glove(s) found
            {
                if (wasConnected)
                {
                    lastGlove = null;
                    UpdateDeviceType();
                    this.DeviceDisconnected.Invoke();
                }
                wasConnected = false;
            }
        }

        /// <summary> Updates this glove's DeviceType at various stages </summary>
        private void UpdateDeviceType()
        {
            this.DeviceType = this.lastGlove != null ? this.lastGlove.GetDeviceType() : SGCore.DeviceType.UNKNOWN;
        }


        //--------------------------------------------------------------------------------------------------------
        // Tracking


        /// <summary> Calculates the 3D position of the glove hardware origin. Mostly used in representing a 3D model of the hardware. If you wish to know where the hand is; use GetWristLocation instead. </summary>
        /// <param name="trackedObject"></param>
        /// <param name="hardware"></param>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public bool GetGloveLocation(Transform trackedObject, SGCore.PosTrackingHardware hardware, out Vector3 glovePos, out Quaternion gloveRot)
        {
            if (this.lastGlove != null)
            {
                SGCore.Kinematics.Vect3D trackedPos = SG.Util.SG_Conversions.ToPosition(trackedObject.position, true);
                SGCore.Kinematics.Quat trackedRot = SG.Util.SG_Conversions.ToQuaternion(trackedObject.rotation);

                SGCore.Kinematics.Vect3D wPos; SGCore.Kinematics.Quat wRot;
                lastGlove.GetGloveLocation(trackedPos, trackedRot, hardware, out wPos, out wRot);

                glovePos = SG.Util.SG_Conversions.ToUnityPosition(wPos);
                gloveRot = SG.Util.SG_Conversions.ToUnityQuaternion(wRot);
                return true;
            }
            glovePos = trackedObject.position;
            gloveRot = trackedObject.rotation;
            return false;
        }


        /// <summary> Calculate a 3D position of the wrist, based on an existing tracking hardware and its location. </summary>
        /// <param name="trackedObject"></param>
        /// <param name="hardware"></param>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public bool GetWristLocation(Transform trackedObject, SGCore.PosTrackingHardware hardware, out Vector3 wristPos, out Quaternion wristRot)
        {
            if (this.lastGlove != null)
            {
                SGCore.Kinematics.Vect3D trackedPos = SG.Util.SG_Conversions.ToPosition(trackedObject.position, true);
                SGCore.Kinematics.Quat trackedRot = SG.Util.SG_Conversions.ToQuaternion(trackedObject.rotation);

                SGCore.Kinematics.Vect3D wPos; SGCore.Kinematics.Quat wRot;
                lastGlove.GetWristLocation(trackedPos, trackedRot, hardware, out wPos, out wRot);

                wristPos = SG.Util.SG_Conversions.ToUnityPosition(wPos);
                wristRot = SG.Util.SG_Conversions.ToUnityQuaternion(wRot);
                return true;
            }
            wristPos = trackedObject.position;
            wristRot = trackedObject.rotation;
            return false;
        }


        /// <summary> Returns a Hand Pose containing everything you'd want to animate a hand model in Unity. Using the global HandProfile. </summary>
        /// <param name="handModel"></param>
        /// <param name="pose"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
		public bool GetHandPose(SGCore.Kinematics.BasicHandModel handModel, out SG_HandPose pose, bool forceUpdate = false)
        {
            return GetHandPose(handModel, SG_HandProfiles.GetProfile(this.IsRight), out pose, forceUpdate);
        }

        /// <summary> Returns a Hand Pose containing everything you'd want to animate a hand model in Unity. Using a custom HandProfile. </summary>
        /// <returns></returns>
        public bool GetHandPose(SGCore.Kinematics.BasicHandModel handDimensions, SGCore.HandProfile userProfile, out SG_HandPose pose, bool forceUpdate = false)
        {
            if (this.lastGlove != null)
            {
                if (forceUpdate || newPoseNeeded) //we need a new pose
                {
                    lastHandModel = handDimensions;
                    if (this.CalibrationStage == CalibrationStage.MoveFingers)
                    {
                        lastHandPose = SG_HandPose.Idle(this.IsRight);
                        newPoseNeeded = false; //we don't need a new pose this frame, thank you.
                    }
                    else if (this.CalibrationLocked && this.currentSequence != null)
                    {
                        //we should gram it from the calibrationSequence instead.
                        if (this.currentSequence.GetCalibrationPose(out lastHandPose))
                        {
                            newPoseNeeded = false;
                        }
                    }
                    else
                    {
                        SGCore.HandPose iPose;
                        if (lastGlove.GetHandPose(handDimensions, userProfile, out iPose))
                        {
                            lastHandPose = new SG_HandPose(iPose);
                            newPoseNeeded = false; //we don't need a new pose this frame, thank you.
                        }
                    }
                }
                // else we already have one, and don't want to force-update.
                pose = lastHandPose;
                return lastHandPose != null;
            }
            else if (lastHandPose == null) { lastHandPose = SG_HandPose.Idle(this.IsRight); } //only if we dont yet have a LastHandPose.

            //otherwise we shouldn't bother
            pose = lastHandPose;
            return false;
        }


        /// <summary> Retrieve flexion angles, normalized to [0...1] or [fingers extended ... fingers flexed] </summary>
        /// <param name="flexions"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
		public bool GetNormalizedFlexion(out float[] flexions, bool forceUpdate = false)
        {
            if (lastHandModel == null) { lastHandModel = SGCore.Kinematics.BasicHandModel.Default(this.IsRight); }
            //if (lastProfile == null) { lastProfile = SGCore.HandProfile.Default(this.IsRight); }
            return GetNormalizedFlexion(lastHandModel, SG_HandProfiles.GetProfile(this.IsRight), out flexions, forceUpdate);
        }


        /// <summary> Retrieve flexion angles, normalized to [0...1] or [fingers extended ... fingers flexed] </summary>
        /// <param name="handModel"></param>
        /// <param name="userProfile"></param>
        /// <param name="flexions"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        public bool GetNormalizedFlexion(SGCore.Kinematics.BasicHandModel handModel, SGCore.HandProfile userProfile, out float[] flexions, bool forceUpdate = false)
        {
            SG_HandPose pose;
            if (GetHandPose(handModel, userProfile, out pose, forceUpdate))
            {
                flexions = pose.normalizedFlexion;
                return true;
            }
            flexions = new float[5];
            return false;
        }


        /// <summary> Retrieve the IMU rotation of this glove. </summary>
        /// <param name="imuRotation"></param>
        /// <returns></returns>
		public bool GetIMURotation(out Quaternion imuRotation)
        {
            if (this.lastGlove != null)
            {
                SGCore.Kinematics.Quat Q;
                if (lastGlove.GetIMURotation(out Q))
                {
                    imuRotation = SG.Util.SG_Conversions.ToUnityQuaternion(Q);
                    return true;
                }
            }
            imuRotation = Quaternion.identity;
            return false;
        }


        //--------------------------------------------------------------------------------------------------------
        // Haptic Feedback


        /// <summary> Send a new Force-Feedback command for this frame. </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SGCore.Haptics.SG_FFBCmd ffb)
        {
            if (this.isActiveAndEnabled)
            {
                this.ffbQueue.Add(ffb.Copy());
                if (this.ffbQueue.Count > maxQueue) { ffbQueue.RemoveAt(0); }
            }

        }

        /// <summary> Send a new timed vibration command for this frame. </summary>
        /// <param name="buzz"></param>
        public void SendCmd(SGCore.Haptics.SG_TimedBuzzCmd buzz)
        {
            
            if (this.isActiveAndEnabled)
            {
                this.buzzQueue.Add((SGCore.Haptics.SG_TimedBuzzCmd)buzz.Copy());
                buzzQueue[buzzQueue.Count - 1].elapsedTime = -Time.deltaTime; //since the time is in ms, and we will be adding deltatime for the first update.
                if (this.buzzQueue.Count > maxQueue) { buzzQueue.RemoveAt(0); }
            }
        }


        /// <summary> Send a so-called Timed Thumper Command, like a finger vibrotactile command, but for the Nova Glove's wrist actuator. </summary>
        /// <param name="thumper"></param>
        public void SendCmd(TimedThumpCmd thumper)
        {
            if (this.isActiveAndEnabled)
            {
                this.thumperQueue.Add(thumper.Copy());
                this.thumperQueue[this.thumperQueue.Count - 1].elapsedTime = -Time.deltaTime;
                if (this.thumperQueue.Count > maxQueue) { thumperQueue.RemoveAt(0); }
            }
        }


        /// <summary> Send a vibration command based on an AnimationCurve. With optional override parameters. </summary>
        /// <param name="buzz"></param>
        public void SendCmd(SG_Waveform buzz)
        {
            if (buzz != null)
            {
                this.SendCmd(buzz, buzz.fingers, buzz.magnitude, buzz.duration_s);
            }
        }

        /// <summary> Send a vibration command based on an AnimationCurve. With optional override parameters. </summary>
        /// <param name="buzz"></param>
        /// <param name="overrideFingers"></param>
        public void SendCmd(SG_Waveform buzz, bool[] overrideFingers)
        {
            if (buzz != null)
            {
                this.SendCmd(buzz, overrideFingers, buzz.magnitude, buzz.duration_s);
            }
        }

        /// <summary> Send a vibration command based on an AnimationCurve. With optional override parameters. </summary>
        /// <param name="buzz"></param>
        /// <param name="overrideFingers"></param>
        /// <param name="magnitude"></param>
        public void SendCmd(SG_Waveform buzz, bool[] overrideFingers, int magnitude)
        {
            if (buzz != null)
            {
                this.SendCmd(buzz, overrideFingers, magnitude, buzz.duration_s);
            }
        }

        /// <summary> Send a vibration command based on an AnimationCurve. With optional override parameters. </summary>
        /// <param name="buzz"></param>
        /// <param name="overrideFingers"></param>
        /// <param name="magnitude"></param>
        /// <param name="duration_s"></param>
        public void SendCmd(SG_Waveform buzz, bool[] overrideFingers, int magnitude, float duration_s)
        {
            if (this.isActiveAndEnabled && this.lastGlove != null && buzz != null)
            {
                if (buzz.wrist && this.DeviceType != SGCore.DeviceType.SENSEGLOVE)
                {
                    //Debug.Log("Converted a Waveform into a thumper cmd");
                    ThumperWaveForm cmd = new ThumperWaveForm(magnitude, duration_s, buzz.waveForm, -Time.deltaTime);
                    this.thumperQueue.Add(cmd);
                    if (this.thumperQueue.Count > maxQueue) { thumperQueue.RemoveAt(0); }
                }
                else
                {
                    //Debug.Log("Converted a Waveform into a buzzCmd");
                    SG_WaveFormCmd iCmd = new SG_WaveFormCmd(buzz.waveForm, duration_s, magnitude, overrideFingers, -Time.deltaTime);
                    this.buzzQueue.Add(((SGCore.Haptics.SG_TimedBuzzCmd)iCmd.Copy()));
                    if (this.buzzQueue.Count > maxQueue) { buzzQueue.RemoveAt(0); }
                }
            }
        }





        /// <summary> Ceases all vibrotactile feedback only </summary>
        public void StopAllVibrations()
        {
            this.buzzQueue.Clear();
            if (this.lastGlove != null) { lastGlove.SendHaptics(SGCore.Haptics.SG_BuzzCmd.Off); }
        }

        /// <summary> Ceases all haptics on this glove. </summary>
        public void StopHaptics()
        {
            this.ffbQueue.Clear();
            this.buzzQueue.Clear();
            if (this.lastGlove != null) { lastGlove.StopHaptics(); }
        }


        /// <summary> Go through any active haptic effects this frame, update any timers and flush everything as a single set of commands. </summary>
        protected void UpdateHaptics()
        {
            if (this.ffbQueue.Count > 0 || this.buzzQueue.Count > 0 || thumperQueue.Count > 0)
            {
                int startBuzz = this.buzzQueue.Count;

                SGCore.Haptics.SG_FFBCmd finalBrakeCmd = SGCore.Haptics.SG_FFBCmd.Off;
                SGCore.Haptics.SG_BuzzCmd finalBuzzCmd = SGCore.Haptics.SG_BuzzCmd.Off;

                if (ffbQueue.Count > 0)
                {
                    for (int i = 0; i < ffbQueue.Count; i++)
                    {
                        finalBrakeCmd = finalBrakeCmd.Merge(ffbQueue[i]);
                    }
                }
                else { finalBrakeCmd = lastFFB; }

                ffbQueue.Clear(); //clear buffer for next frame.

                float dTSec = Time.deltaTime;

                //Buzz motor
                for (int i = 0; i < buzzQueue.Count;)
                {
                    buzzQueue[i].UpdateTiming(dTSec);
                    if (buzzQueue[i].TimeElapsed()) //no more relevant cmds
                    {
                        buzzQueue.RemoveAt(i); //do nothing
                    }
                    else
                    {
                        finalBuzzCmd = buzzQueue[i].Merge(finalBuzzCmd); //add to final cmd
                        i++;
                    }
                }

                //evaluate thumper
                int finalThump = 0;
                for (int i = 0; i < thumperQueue.Count;)
                {
                    thumperQueue[i].Update(dTSec);
                    if (thumperQueue[i].Elapsed)
                    {
                        thumperQueue.RemoveAt(i);
                    }
                    else
                    {
                        finalThump = Mathf.Max(finalThump, thumperQueue[i].magnitude);
                        i++;
                    }
                }

                //evaluate based on FFBEnabled BuzzEnabled
                int[] fForceLevels = finalBrakeCmd.Levels;
                int[] fBuzzLevels = finalBuzzCmd.Levels;
                for (int f = 0; f < 5; f++)
                {
                    if (!ffbEnabled[f]) { fForceLevels[f] = 0; }
                    if (!vibroEnabled[f]) { fBuzzLevels[f] = 0; }
                }
                finalBrakeCmd.Levels = fForceLevels;
                finalBuzzCmd.Levels = fBuzzLevels;

                //ToDo: Only send if there is a change?

                if (this.lastGlove != null)
                {
                    //Debug.Log("Sending Hatpics: " + finalBrakeCmd.ToString() + ", " + finalBuzzCmd.ToString() + ", [" + finalThump + "]");
                    this.lastGlove.SendHaptics(finalBrakeCmd, finalBuzzCmd, new ThumperCmd(finalThump));
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------
        // Calibration

        /// <summary> Checks the calibration stage of this glove when it connects. </summary>
        protected void CheckForCalibration()
        {
            if (this.checkCalibrationOnStart && SGCore.Calibration.HG_CalCheck.NeedsCheck(this.DeviceType))
            {
                SGCore.Calibration.SensorRange lastRange;
                if (SG_HandProfiles.LoadLastRange(this.lastGlove, out lastRange))
                {
                    //Debug.Log("Loaded Last Range: " + lastRange.ToString(true));
                    Debug.Log("Please move your " + (this.IsRight ? "right" : "left") + " hand so we can determine of calibration is required");
                    this.CalibrationStage = CalibrationStage.MoveFingers; //todo: Check for Nova or SenseGlove DK1
                    this.calibrationCheck = new HG_CalCheck(lastRange);
                }
                else
                {
                    //Debug.Log("There's no range from last time, so we start off with calibration regardless!");
                    this.CalibrationStage = CalibrationStage.CalibrationNeeded;
                }
            }
            else
            {
                this.CalibrationStage = CalibrationStage.Done;
            }
            //CalibrationLocked = this.CalibrationStage == CalibrationStage.Calibrating; //only locked if we're calibrating
            this.CalibrationStateChanged.Invoke();
        }


        /// <summary> If the user needs to move their fingers, determine if they need calibration or not. </summary>
        /// <param name="deltaTime"></param>
        protected void CheckFingerMovement(float deltaTime)
        {
            if (this.lastGlove != null && this.CalibrationStage == CalibrationStage.MoveFingers)
            {
                SGCore.Kinematics.Vect3D[] calVals;
                if (lastGlove.GetCalibrationValues(out calVals))
                {
                    calibrationCheck.CheckRange(calVals, deltaTime, this.DeviceType); //update checker.
                    if (calibrationCheck.ReachedConclusion)
                    {
                        this.CalibrationStage = calibrationCheck.CalibrationStage;
                        CalibrationLocked = false; //unlocks calibration
                        this.CalibrationStateChanged.Invoke();
                    }
                }
            }
        }

        /// <summary> Lock this glove to a SG_ClibrationSequence so that no others can interfere. Also lets us know this glove is currently being calibrated. </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public bool LockCalibration(SG_CalibrationSequence sequence)
        {
            if (sequence != null && !CalibrationLocked)
            {
                this.currentSequence = sequence;
                this.CalibrationLocked = true;
                this.CalibrationStage = CalibrationStage.Calibrating;
                this.CalibrationStateChanged.Invoke();
                return true;
            }
            return false;
        }

        /// <summary> Unlocks the glove from this calibration sequence, and let others know it's finished. </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public bool UnlockCalibraion(SG_CalibrationSequence sequence)
        {
            if (this.currentSequence == null || this.currentSequence == sequence)
            {
                this.currentSequence = null;
                this.CalibrationLocked = false;
                this.CalibrationStage = CalibrationStage.Done;
                this.CalibrationStateChanged.Invoke();
                return true;
            }
            return false;
        }


        /// <summary> Takes a sensor range taked from a Calibration sequence and calibrates the hand accordingly.. </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool CalibrateHand(SGCore.Calibration.SensorRange range)
        {
            SGCore.HandProfile newProfile;
            SGCore.Calibration.HG_CalibrationSequence.CompileProfile(range, this.DeviceType, this.IsRight, out newProfile);
            SG_HandProfiles.SetProfile(newProfile);

            //this.lastRange = new SGCore.Calibration.SensorRange(range); //deep copy
            if (SG_HandProfiles.SaveLastRange(range, this.lastGlove))
            {
                //Debug.Log("Saved Range: " + range.ToString(true));
            }

            if (this.CalibrationStage != CalibrationStage.Done)
            {
                this.CalibrationStage = CalibrationStage.Done;
                this.CalibrationStateChanged.Invoke();
            }
            return true;
        }



        //--------------------------------------------------------------------------------------------------------
        // Monobehaviour


        // Use this for initialization
        void Start()
        {
            if (this.DeviceConnected == null) { this.DeviceConnected = new UnityEvent(); }
            if (this.DeviceDisconnected == null) { this.DeviceDisconnected = new UnityEvent(); }
            this.CalibrationStage = CalibrationStage.MoveFingers;
            SG.Util.SG_Connections.SetupConnections();
            UpdateDeviceType();
            checkTime = hwTimer; //ensures we check it next frame!
        }

        void Update()
        {
            checkTime += Time.deltaTime;
            if (checkTime >= hwTimer) { UpdateConnection(); checkTime = 0; }
            CheckFingerMovement(Time.deltaTime);
        }

        void LateUpdate()
        {
            newPoseNeeded = true; //next frame we're allowed to update the pose again.
            UpdateHaptics();
        }

        void OnApplicationQuit()
        {
            StopHaptics();
        }

    }
}