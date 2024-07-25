//#define CV_ENABLED
#define HG_CUSTOM_INSPECTOR
//#define GRAB_WT_FROM_SETTINGS

using SGCore.Kinematics;
using UnityEngine;
using UnityEngine.Events;
using SGCore.Haptics;
using SGCore.CV;
using System.Collections;

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
        AnyHand
    }


    /// <summary> Interface for a left- or right handed glove built by SenseGlove. Usually either a SenseGlove DK1 or a Nova Glove. </summary>
    public class SG_HapticGlove : MonoBehaviour, IHandPoseProvider, IHandFeedbackDevice
    {
        /// <summary> Method to determine a HapticGlove's wrist location, as SenseGlove device do not come with their own. </summary>
        public enum WristTracking
        {
            /// <summary> Use UnityXR's positioning System. Assign an origin value to move the hands with the origin </summary>
            UnityXR,
            /// <summary> Use UnityXR's positioning system, but use manual offsets. </summary>
            UnityXRManualOffsets,
            /// <summary> Follow a specific transform, with tracking offsets determined by the devices detected via SG_XR_Devices. </summary>
            FollowObjectAutoOffsets,
            /// <summary> Follow a specific transform, but using tracking Offsets determined at the start. </summary>
            FollowObjectManualOffsets,
            /// <summary> Follow a GameObject without using any offsets. Useful to override any wrist location(s) in example scenes. </summary>
            FollowObjectNoOffsets,
            /// <summary> This GameObject's own Transform to determine the wrist rotation / position. Useful when you're using parenting to determine hand location(s). </summary>
            MyGameObject,
        }

        //--------------------------------------------------------------------------------------------------------
        // Variables

        // Inspector Variables.

        /// <summary> What kind of hand sides this SG_HapitcGlove is allowed to connect to. </summary>
        public HandSide connectsTo = HandSide.RightHand;

        /// <summary> The method by which to determine Wrist Position of this SG_HapticGlove when no Optical Tracking is available. </summary>
        public WristTracking wristTrackingMethod = WristTracking.UnityXR;

        /// <summary> Optional. When using UnityXR's wrist tracking, the wrist will move relative to this origin. </summary>
        public Transform origin;

        /// <summary> GameObject controller by a 3rd party tracking script, that is used to take over wrist tracking. </summary>
        public Transform wristTrackingObj;
        /// <summary> The hardware family of our 3rd party wrist trackers. Determines wrist offsets. If set to Custom, we will use the offsets recorded during Start(). </summary>
        public SGCore.PosTrackingHardware wristTrackingOffsets = SGCore.PosTrackingHardware.Custom;

        /// <summary> The last Calibration State of this glove </summary>
        public SGCore.HG_CalibrationState LastCalibrationState { get; protected set; }


        // Events

        /// <summary> Fires when this HapticGlove connects to this simulation. Switching Scene causes this to fire again. </summary>
        public UnityEvent DeviceConnected = new UnityEvent();

        /// <summary> Fires when this HapticGlove disconnects from the system. </summary>
        public UnityEvent DeviceDisconnected = new UnityEvent();

        /// <summary> Fires when this glove's calibration state changes. </summary>
        public UnityEvent CalibrationStateChanged = new UnityEvent();



        // Connection Related

        /// <summary> The glove we were connected to since the last "Hardware Tick". If null, we were not connected. </summary>
        protected SGCore.HapticGlove lastGlove = null;
        /// <summary> How ofter we check for new hardware (states). Static so it may be changed from other settings.</summary>
        public static float hwTimer = 0.5f;
        /// <summary> Timer to activate HardwareTick. </summary>
        protected float timer_checkHW = 0;
        /// <summary> Connection state during the last Hardware Tick. Used to fire Connected/Disconnected events. </summary>
        protected bool wasConnected = false;

        /// <summary> If true, we can still check for the XRRig </summary>
        protected bool checkOrigin = true;


        // Tracking Related

        /// <summary> The HandDimensions used to generate our HandModel. can be get/set. When requesting a HandPose while it's NULL, we'll set it to a default one. </summary>
        protected SGCore.Kinematics.BasicHandModel handDimensions = null;
        /// <summary> The Last HandPose calculated by this glove. Cached to we can retrun it when mulitple requests are made each frame. </summary>
        protected SG_HandPose lastHandPose = null;
        /// <summary> If true, we can retrieve a new HandPose this frame. If false, we've already calculated one </summary>
        protected bool newPoseNeeded = true;

#if CV_ENABLED
        /// <summary> The last calculated CV pose. Kept in memory so we only calculate it one per frame (since time since last sample doesn't change). </summary>
        protected CV_ProcessedHandData lastCVData = null;

        // Internal cv stuff

        /// <summary> If true, we can retrieve a new CV data this frame. If false, we've already calculated one </summary>
        protected bool newCVNeeded = true;
        /// <summary> How much of the CV we're using for Tracking. </summary>
        protected CV_Tracking_Depth cvLevel = CV_Tracking_Depth.WristOnly;
#endif

        // Haptics Related

        /// <summary> Optionally bypasses Haptic sending. Diables Unity side of our Haptics System </summary>
        protected bool bypassingHaptics = false;

        public bool BypassHaptics
        {
            get { return bypassingHaptics; }
            set { bypassingHaptics = value; }
        }

        ///// <summary> Coroutines to maintain legacy support for DK1 and Nova 1.X glove fingers, who still work with 'streaming' </summary>
        //protected Coroutine[] fingerVibros = new Coroutine[5];
        ///// <summary> Coroutines to maintain legacy support for Nova 1.X glove thumper, which may still work with 'streaming' </summary>
        //protected Coroutine wristVibro = null;

        /// <summary> Contains legacy vibration commands for Nova 1.0 and DK1.0 </summary>
        protected LegacyCommand[] legacyFingers = new LegacyCommand[5];
        protected LegacyCommand legacyWrist = null;




        //--------------------------------------------------------------------------------------------------------
        // Connection Functions

        /// <summary> Returns the first valid instance of a SGCore.HapticGlove object, based on HandSide parameters </summary>
        /// <param name="handSide"></param>
        /// <param name="gloveInstance"></param>
        /// <returns></returns>
        public static bool GetGloveInstance(HandSide handSide, out SGCore.HapticGlove gloveInstance)
        {
            if (handSide == HandSide.AnyHand)
            {
                return SGCore.HapticGlove.GetGlove(out gloveInstance); //returns the first connected glove
            }
            return SGCore.HapticGlove.GetGlove(handSide == HandSide.RightHand, out gloveInstance); //returns the firce connected glove that matches left/right parameters.
        }

        /// <summary> The Internal HapticGlove that this glove is linked to.Is null when not connected. </summary>
        public SGCore.HapticGlove InternalGlove
        {
            get { return this.lastGlove; }
        }

        /// <summary> The DeviceType of this glove. Use this to distinguish between SenseGlove and Nova, for example. </summary>
        public SGCore.DeviceType DeviceType
        {
            get; private set;
        }

        /// <summary> Updates this glove's DeviceType at various stages </summary>
        private void UpdateDeviceType()
        {
            this.DeviceType = this.lastGlove != null ? this.lastGlove.GetDeviceType() : SGCore.DeviceType.UNKNOWN;
        }

        /// <summary> Updates the connection status and fires events if needed</summary>
        protected virtual void UpdateConnection()
        {
            SGCore.HapticGlove uGlove;
            if (GetGloveInstance(this.connectsTo, out uGlove))
            {
                lastGlove = uGlove; //updates the refrence
                if (!wasConnected) //glove has reconnected
                {
                    UpdateDeviceType();
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

        //--------------------------------------------------------------------------------------------------------
        // Tracking Functions

        /// <summary> Retrieve the IMU rotation of this glove  in Unity notation. This is an uncalibrated rotation, and is purely what the sensor itself passes through. </summary>
        /// <param name="imuRotation"></param>
        /// <returns></returns>
        public virtual bool GetIMURotation(out Quaternion imuRotation)
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

        public void SwapTracking(SG_HapticGlove otherHand)
        {
            if (otherHand != null)
            {
                Transform myTrackedObject = this.wristTrackingObj;
                this.wristTrackingObj = otherHand.wristTrackingObj;
                otherHand.wristTrackingObj = myTrackedObject;
            }
        }

        public void SetTrackingHardware(Transform trackingRefrence, SGCore.PosTrackingHardware hardwareType)
        {
            wristTrackingObj = trackingRefrence;
            wristTrackingOffsets = hardwareType;
        }


        /// <summary> Calculates the 3D position of the glove hardware origin. Mostly used in representing a 3D model of the hardware. If you wish to know where the hand is; use GetWristLocation instead. </summary>
        /// <param name="trackedObject"></param>
        /// <param name="hardware"></param>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public virtual bool GetGloveLocation(Transform trackedObject, SGCore.PosTrackingHardware hardware, out Vector3 glovePos, out Quaternion gloveRot)
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
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public bool GetWristLocation(out Vector3 wristPos, out Quaternion wristRot)
        {
            if (this.wristTrackingMethod == WristTracking.MyGameObject)
            {
                wristPos = this.transform.position;
                wristRot = this.transform.rotation;
                return true;
            }
            else if (this.wristTrackingMethod == WristTracking.FollowObjectNoOffsets)
            {
                wristPos = this.wristTrackingObj != null ? this.wristTrackingObj.position : this.transform.position;
                wristRot = this.wristTrackingObj != null ? this.wristTrackingObj.rotation : this.transform.rotation;
                return true;
            }

            // Validate Offsets
            if (this.wristTrackingMethod == WristTracking.FollowObjectAutoOffsets || this.wristTrackingMethod == WristTracking.UnityXR)
            {

#if GRAB_WT_FROM_SETTINGS
                this.wristTrackingOffsets = SG_Core.Settings.GetInternalTrackerType(this.TracksRightHand()); //grab it from Settings
#else
                if (SG_XR_Devices.GetTrackingHardware(this.TracksRightHand(), out SGCore.PosTrackingHardware updatedOffsets)) //grab from SG_XR_Devices
                {
                    this.wristTrackingOffsets = updatedOffsets;
                }
#endif
            }


            SGCore.PosTrackingHardware offsets = this.wristTrackingOffsets;

            //UnityXR = Whatever Unity returns, but also with the Origin, if any
            if (this.wristTrackingMethod == WristTracking.UnityXR || wristTrackingMethod == SG_HapticGlove.WristTracking.UnityXRManualOffsets)
            {
                Vector3 trackerPos;
                Quaternion trackerRot;
                if (SG_XR_Devices.GetTrackingReferenceLocation(this.TracksRightHand(), out trackerPos, out trackerRot))
                {
                    //Calculate world position by using the Origin property.
                    Quaternion worldRot = origin != null ? origin.rotation * trackerRot : trackerRot;
                    Vector3 worldPos = origin != null ? origin.position + (origin.rotation * trackerPos) : trackerPos;
                    return GetWristLocation(worldPos, worldRot, offsets, out wristPos, out wristRot);
                }
                wristPos = Vector3.zero;
                wristRot = Quaternion.identity;
                return false;
            }
            // If we get here, we're using either one of the offsets
            return GetWristLocation(this.wristTrackingObj, offsets, out wristPos, out wristRot);
        }

        /// <summary> Calculate a 3D position of the wrist, based on an existing tracking hardware and its location. </summary>
        /// <param name="trackedObject"></param>
        /// <param name="hardware"></param>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public bool GetWristLocation(Transform trackedObject, SGCore.PosTrackingHardware hardware, out Vector3 wristPos, out Quaternion wristRot)
        {
            if (trackedObject == null)
            {
                wristPos = this.transform.position;
                wristRot = this.transform.rotation;
                return false;
            }
            return GetWristLocation(trackedObject.position, trackedObject.rotation, hardware, out wristPos, out wristRot);
        }

        /// <summary> Calculate a 3D position of the wrist, based on an existing tracking hardware and its location. </summary>
        /// <param name="trackedObject"></param>
        /// <param name="hardware"></param>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        /// <returns></returns>
        public virtual bool GetWristLocation(Vector3 trackedObjPos, Quaternion trackedObjRot, SGCore.PosTrackingHardware hardware, out Vector3 wristPos, out Quaternion wristRot)
        {
            if (this.lastGlove == null || hardware == SGCore.PosTrackingHardware.Custom)
            {
                wristPos = trackedObjPos;
                wristRot = trackedObjRot;
                return true;
            }

            //if we get here, lastGlove is not null. We know what we're dealing with
            SGCore.Kinematics.Vect3D trackedPos = SG.Util.SG_Conversions.ToPosition(trackedObjPos, true);
            SGCore.Kinematics.Quat trackedRot = SG.Util.SG_Conversions.ToQuaternion(trackedObjRot);

            SGCore.Kinematics.Vect3D wPos; SGCore.Kinematics.Quat wRot;
            lastGlove.GetWristLocation(trackedPos, trackedRot, hardware, out wPos, out wRot);

            wristPos = SG.Util.SG_Conversions.ToUnityPosition(wPos);
            wristRot = SG.Util.SG_Conversions.ToUnityQuaternion(wRot);
            return true;
        }



        /// <summary> Updates the LastHandPose if needed. At the end of this function, LastHandModel is known. </summary>
        /// <param name="forceUpdate"></param>
        public void UpdateLastHandPose(bool forceUpdate)
        {
#if CV_ENABLED
            UpdateCVData(forceUpdate); 
            if (lastCVData != null) 
            {
                //CV Data is available, but how old is it?

                float currTime = CV_HandLayer.GetSimulationTime(); //the current time
                //Debug.Log("Retrieved Data for a glove at " + cvData.timeStamp.ToString() + " (d= " + (currTime - lastCVData.timeStamp).ToString() + ")");
                lastHandPose = new SG_HandPose( SGCore.HandPose.FromHandAngles( lastCVData.handAngles, lastCVData.rightHand, this.GetKinematics() ));
                lastHandPose.wristPosition = SG.Util.SG_Conversions.ToUnityPosition( lastCVData.wristWorldPosition );
                lastHandPose.wristRotation = SG.Util.SG_Conversions.ToUnityQuaternion( lastCVData.wristWorldRotation );
                return;
            }
#endif
            if (forceUpdate || newPoseNeeded)
            {
                newPoseNeeded = false; //we no longer need a pose this frame
                SG_HandPose newPose;
                if (this.GetHandPose(this.GetKinematics(), out newPose))
                {
                    lastHandPose = newPose; //only re-assign it if you were succesful in calculating it.
                    return;
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------
        // Calbration Functions

        /// <summary> (Re)start the glove's calibration. This is all running internally, though if a CalibrationLayer is attached, instructions can be updated. </summary>
        /// <returns></returns>
        public bool StartCalibration()
        {
            if (this.lastGlove != null)
            {
                this.lastGlove.ResetCalibration();
                return true;
            }
            return false;
        }

        /// <summary> End any active calibration on the glove. </summary>
        /// <returns></returns>
        public bool CompleteCalibration()
        {
            if (this.lastGlove != null)
            {
                this.lastGlove.EndCalibration();
                return true;
            }
            return false;
        }


        //--------------------------------------------------------------------------------------------------------
        // CV Related

#if CV_ENABLED
        /// <summary> Calculate CV hand pose and position if we haven't already this frame. </summary>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        protected void UpdateCVData(bool forceUpdate = false)
        {
            if (this.lastGlove != null)
            {
                if (forceUpdate || newCVNeeded) //we need a new pose
                {
                    newCVNeeded = false; //we'll only check once per frame.
                    CV_ProcessedHandData cvData;
                    if (CV_HandLayer.TryGetPose(this.TracksRightHand(), SGCore.DeviceType.NOVA, "", out cvData)) //NOVA FOR NOW BUT THIS MUST BE THE ACTUAL DEVICE!
                    {
                        lastCVData = cvData;
                    }
                    else
                    {
                        lastCVData = null; //so that the GetData knows it's not (or no longer) available.
                    }
                }
            }
        }
#endif


        //--------------------------------------------------------------------------------------------------------
        // HandPoseProvider Interface Functions

        /// <summary> Returns the tracking Type of this device </summary>
        /// <returns></returns>
        public HandTrackingDevice TrackingType()
        {
            return HandTrackingDevice.HapticGlove; //If other HapitcGlove devices appear, we can use those instead.
        }

        /// <summary> Returns true if this Haptic Glove is configured to connect to the left / right hands. </summary>
        /// <returns></returns>
        public bool TracksRightHand()
        {
            return this.connectsTo != HandSide.LeftHand;
        }

        /// <summary> Sets the default handKinematics used by this glove's solver. Can be left empty, at which point it will use the default. You can also override it when requesting a HandPose </summary>
        /// <param name="handModel"></param>
        public void SetKinematics(BasicHandModel handModel)
        {
            this.handDimensions = handModel;
            this.newPoseNeeded = true; //we definitely need to recalculate
            if (this.lastHandPose == null) //was never generated!
            {
                lastHandPose = ToUnityPose(SGCore.HandPose.DefaultIdle(this.TracksRightHand(), this.handDimensions));
            }
        }

        /// <summary> Retrieve the hand dimensions used for this glove's default  </summary>
        /// <returns></returns>
        public BasicHandModel GetKinematics()
        {
            if (this.handDimensions == null)
            {
                this.handDimensions = SGCore.Kinematics.BasicHandModel.Default(this.TracksRightHand());
            }
            return this.handDimensions;
        }


        /// <summary> Returns true if this HapticGlove is connected to the system.  </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return this.lastGlove != null;
        }


        public bool TryGetBatteryLevel(out float value01)
        {
            if (this.lastGlove != null && this.lastGlove.GetBatteryLevel(out float unconv))
            {
                value01 = Mathf.Clamp01(unconv + 0.01f); //our battery caps out at 99%. So until I fix this, this is the best we can do.
                return true;
            }
            value01 = -1.0f;
            return false;
        }

        /// <summary> Returns the last handPose retrieved this frame </summary>
        /// <param name="handPose"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        public bool GetHandPose(out SG_HandPose handPose, bool forceUpdate = false)
        {
            this.UpdateLastHandPose(forceUpdate);
            handPose = this.lastHandPose;
            return handPose != null;
        }


        /// <summary> Properly calculates a new HandPose with SenseGlove-Specific inputs. This one does not get stored in the lastHandpose </summary>
        /// <param name="handDimensions"></param>
        /// <param name="handProfile"></param>
        /// <param name="handPose"></param>
        /// <returns></returns>
        public bool GetHandPose(SGCore.Kinematics.BasicHandModel handDimensions, out SG_HandPose handPose)
        {
            handPose = null;
            if (this.lastGlove != null)
            {
                SGCore.HandPose iPose;
                if (this.lastGlove.GetHandPose(handDimensions, out iPose))
                {
                    handPose = ToUnityPose(iPose);
                }
            }
            return handPose != null;
        }

        /// <summary> Convert and internal SenseGlove Pose into a Unity one, where I also add the wrist location </summary>
        /// <param name="iPose"></param>
        /// <returns></returns>
        protected SG_HandPose ToUnityPose(SGCore.HandPose iPose)
        {
            SG_HandPose handPose = new SG_HandPose(iPose); //convert this into Unity Notation
            //and add the position / rotation of the wrist-taken from Unity.

            //Vector3 nWristPos; Quaternion nWristRot; //'normal' location from the API
            this.GetWristLocation(out handPose.wristPosition, out handPose.wristRotation); //the same

            //Vector3 posOffset = this.TracksRightHand() ? wristPosCorrection_R : wristPosCorrection_L;
            //Quaternion rotOffset = this.TracksRightHand() ? wristRotCorrection_R : wristRotCorrection_L;

            //SG.Util.SG_Util.CalculateTargetLocation(nWristPos, nWristRot, posOffset, rotOffset, out handPose.wristPosition, out handPose.wristRotation);

            return handPose;
        }

        /// <summary> Return the finger flexions as normalized values [0 = Extended, 1 = fully flexed]. </summary>
        /// <param name="flexions"></param>
        /// <returns></returns>
        public bool GetNormalizedFlexion(out float[] flexions)
        {
            this.UpdateLastHandPose(false);
            if (lastHandPose != null)
            {
                flexions = this.lastHandPose.normalizedFlexion;
                return true;
            }
            flexions = new float[5];
            return false;
        }

        /// <summary> Since this is not a controller, we don't need to map it. </summary>
        /// <returns></returns>
        public float OverrideGrab()
        {
            return 0;
        }


        /// <summary> Since this is not a controler, we don't need to map it. </summary>
        /// <returns></returns>
        public float OverrideUse()
        {
            return 0;
        }



        //--------------------------------------------------------------------------------------------------------
        // HandFeedbackDevice Interface Functions

        /// <summary> Returns the name of this GameObjects summary>
        /// <returns></returns>
        public string Name()
        {
            return this.gameObject.name;
        }

        /// <summary> Stop any vibrotactile effects currently playing on this glove. </summary>
        public virtual void StopAllVibrations()
        {
            //hapticStream.ClearVibrations();
            if (this.lastGlove != null && !this.bypassingHaptics)
            {
                lastGlove.StopVibrations();
            }
        }

        /// <summary> Stop both force- and vibrotactile feedback to this glove. </summary>
        public virtual void StopHaptics()
        {
            //hapticStream.ClearAll();
            if (this.lastGlove != null && !this.bypassingHaptics)
            {
                lastGlove.StopHaptics();
            }
        }


        /// <summary> Send a Force-Feedback command to this glove. </summary>
        /// <param name="ffb"></param>
        public virtual void QueueFFBCmd(float[] values01)
        {
            if (this.lastGlove != null)
            {
                //Debug.Log("queued " + SG.Util.SG_Util.ToString(values01));
                this.lastGlove.QueueFFBLevels(values01);
            }
        }

        /// <summary> Send a Force-Feedback command to this glove. </summary>
        /// <param name="ffb"></param>
        public virtual void QueueFFBCmd(SGCore.Finger finger, float value01)
        {
            if (this.lastGlove != null)
            {
                this.lastGlove.QueueFFBLevel(finger, value01);
            }
        }



        public bool HasVibrationMotor(VibrationLocation atLocation)
        {
            switch (this.DeviceType)
            {
                case SGCore.DeviceType.NOVA:
                    return atLocation == VibrationLocation.WholeHand || atLocation == VibrationLocation.Index_Tip || atLocation == VibrationLocation.Thumb_Tip;

                case SGCore.DeviceType.NOVA_2_GLOVE:
                    return atLocation == VibrationLocation.WholeHand || atLocation == VibrationLocation.Index_Tip || atLocation == VibrationLocation.Thumb_Tip
                        || atLocation == VibrationLocation.Palm_IndexSide || atLocation == VibrationLocation.Palm_PinkySide;

                case SGCore.DeviceType.SENSEGLOVE:
                    return atLocation == VibrationLocation.Index_Tip || atLocation == VibrationLocation.Middle_Tip || atLocation == VibrationLocation.Ring_Tip
                        || atLocation == VibrationLocation.Pinky_Tip || atLocation == VibrationLocation.Thumb_Tip;

                default:
                    return false;
            }
        }


        public void SendVibrationCmd(VibrationLocation location, float amplitude, float duration, float frequency)
        {
            if (this.lastGlove != null)
            {
                if (this.lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA)
                {
                    SGCore.Nova.NovaGlove nova = (SGCore.Nova.NovaGlove)lastGlove;
                    if (nova.SupportsCustomWaveforms)
                    {
                        SGCore.CustomWaveform wf = new SGCore.CustomWaveform(amplitude, duration, frequency);
                        SG_CustomWaveform.CallCorrectWaveform(lastGlove, wf, location);
                    }
                    else
                    {
                        if (location == VibrationLocation.Palm_IndexSide || location == VibrationLocation.Palm_PinkySide || location == VibrationLocation.WholeHand)
                        {
                            this.legacyWrist = new LegacyCommand(amplitude, duration, -Time.deltaTime); //thumper effect.
                        }
                        else //assume finger.
                        {
                            int index = SG_CustomWaveform.ToFingerIndex(location);
                            if (index > -1)
                            {
                                this.legacyFingers[index] = new LegacyCommand(amplitude, duration, -Time.deltaTime);
                            }
                        }
                    }
                }
                else if (this.lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA_2_GLOVE)
                {
                    SGCore.CustomWaveform wf = new SGCore.CustomWaveform(amplitude, duration, frequency);
                    SG_CustomWaveform.CallCorrectWaveform(lastGlove, wf, location);
                }
                else if (this.lastGlove.GetDeviceType() == SGCore.DeviceType.SENSEGLOVE)
                {
                    SGCore.SG.SenseGlove dk1Glove = (SGCore.SG.SenseGlove)lastGlove;
                    if (location == VibrationLocation.Palm_IndexSide || location == VibrationLocation.Palm_PinkySide)
                    {
                        dk1Glove.QueueWristCommand(amplitude > 0.5f ? SGCore.SG.SG_ThumperCmd.Impact_Thump_100 : SGCore.SG.SG_ThumperCmd.Impact_Thump_30);
                    }
                    else //assume finger.
                    {
                        int index = SG_CustomWaveform.ToFingerIndex(location);
                        if (index > -1)
                        {
                            this.legacyFingers[index] = new LegacyCommand(amplitude, duration, -Time.deltaTime);
                        }
                    }
                }
            }
        }



        /// <summary> Send a Legacy Waveform command that is Supported by Nova 1.0 and SenseGlove DK1 only </summary>
        /// <param name="waveform"></param>
        public virtual void SendLegacyWaveform(SG_Waveform waveform)
        {
            SendLegacyWaveform(waveform, waveform.amplitude, waveform.duration_s, waveform.intendedLocation);
        }

        /// <summary> Send a Wavefrom from the Inspector to the glove. </summary>
        /// <param name="waveform"></param>
        /// <param name="location"></param>
        /// <param name="amplitude"></param>
        /// <param name="duration"></param>
        public virtual void SendLegacyWaveform(SG_Waveform waveform, float amplitude, float duration, VibrationLocation location)
        {
            if (location == VibrationLocation.Unknown || amplitude <= 0.0f || duration <= 0.0f)
            {
                return;
            }

            if (lastGlove != null)
            {
                int finger = SG_CustomWaveform.ToFingerIndex(location);
                if (finger > -1) //this is a finger.
                {
                    Debug.Log(Time.time + ": Finger Effect Added: " + amplitude + ", " + duration + ", " + location.ToString());
                    this.legacyFingers[finger] = new LegacyWaveform(waveform.amplitude, waveform.duration_s, waveform.waveForm, -Time.deltaTime);
                }
                else if (lastGlove is SGCore.Nova.NovaGlove   //only Nova Gloves support Legacy effects on the Wrist
                    && location == VibrationLocation.WholeHand)
                {
                    Debug.Log(Time.time + ": Wrist Effect Added: " + amplitude + ", " + duration + ", " + location.ToString());
                    legacyWrist = new LegacyWaveform(waveform.amplitude, waveform.duration_s, waveform.waveForm, -Time.deltaTime);
                }
            }
            //Convert this from a Monobehaviour class into a class that extends off the Timed Buzz/Waveform Cmd.
            //if (Util.SG_Util.OneTrue(waveform.FingersArray))
            //{
            //    SG_WaveFormCmd iFingerBuzz = new SG_WaveFormCmd(waveform.waveForm, waveform.duration_s, waveform.magnitude, waveform.FingersArray, -Time.deltaTime); // -deltaTime so when we evaluate at the end of this frame, they start at 0s.
            //    this.hapticStream.AddCmd(iFingerBuzz);
            //}
            //if (waveform.wrist && this.DeviceType != SGCore.DeviceType.SENSEGLOVE)
            //{
            //    ThumperWaveForm iWristCmd = new ThumperWaveForm(waveform.magnitude, waveform.duration_s, waveform.waveForm, -Time.deltaTime); // -deltaTime so when we evaluate at the end of this frame, they start at 0s.
            //    this.hapticStream.AddCmd(iWristCmd);
            //}
        }

        ///// <summary> Send a Thumper command that is defined by a WaveForm to this Glove. </summary>
        ///// <param name="waveform"></param>
        //public virtual void SendCmd(ThumperWaveForm waveform)
        //{
        //    waveform.elapsedTime = -Time.deltaTime;
        //    this.hapticStream.AddCmd(waveform);
        //}

        /// <summary> Send an impact vibration to this HapitcGlove, along with the location and a vibration impact level 0...1. </summary>
        /// <param name="location"></param>
        /// <param name="normalizedVibration"></param>
        public virtual void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            if (this.lastGlove != null)
            {
                if (lastGlove.GetDeviceType() == SGCore.DeviceType.SENSEGLOVE)
                {
                    SGCore.SG.SG_ThumperCmd effect = normalizedVibration > 0.5f ? SGCore.SG.SG_ThumperCmd.Impact_Thump_100 : SGCore.SG.SG_ThumperCmd.Impact_Thump_30;
                    ((SGCore.SG.SenseGlove)lastGlove).QueueWristCommand(effect);
                }
                else if (lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA)
                {
                    float wL = (0.1f) + (normalizedVibration * 0.70f); //10 ... 80
                    SendVibrationCmd(VibrationLocation.WholeHand, wL, 0.2f, 80);
                }
                else if (lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA_2_GLOVE)
                {
                    SGCore.CustomWaveform wf = new SGCore.CustomWaveform(normalizedVibration, 0.2f, 80.0f);
                    SGCore.Nova.Nova2Glove n2g = (SGCore.Nova.Nova2Glove)this.lastGlove;
                    n2g.SendCustomWaveform(wf, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmIndexSide);
                    n2g.SendCustomWaveform(wf, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmPinkySide);
                }
            }
        }


        public virtual void SendCustomWaveform(SG_CustomWaveform customWaveform, VibrationLocation location)
        {
            if (this.lastGlove != null)
            {
                if ((lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA && !((SGCore.Nova.NovaGlove)lastGlove).SupportsCustomWaveforms)
                    || (lastGlove.GetDeviceType() == SGCore.DeviceType.SENSEGLOVE))
                {
                    //we're dealing with a Nova 1.0 that does not have Custom waveforms or a DK1
                    SendVibrationCmd(location, customWaveform.amplitude, customWaveform.Duration, customWaveform.startFrequency);
                    return;
                }

                VibrationLocation[] allLocations = new VibrationLocation[1] { location };
                if (lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA)
                {   //this is an effect not meant for Nova one.
                    if (location == VibrationLocation.Palm_IndexSide || location == VibrationLocation.Palm_PinkySide)
                    {
                        allLocations = new VibrationLocation[1] { VibrationLocation.WholeHand };
                    }
                }
                else if (lastGlove.GetDeviceType() == SGCore.DeviceType.NOVA_2_GLOVE)
                {
                    if (location == VibrationLocation.WholeHand)
                    {
                        allLocations = new VibrationLocation[2] { VibrationLocation.Palm_IndexSide, VibrationLocation.Palm_PinkySide };
                    }
                }
                foreach (VibrationLocation loc in allLocations)
                {
                    SG_CustomWaveform.CallCorrectWaveform(this.lastGlove, customWaveform.GetWaveform(), loc);
                }
                //((SGCore.Nova.NovaGlove)this.lastGlove).SendCustomWaveform(customWaveform.GetWaveform(), location);
            }
        }

        public virtual bool FlexionLockSupported()
        {
            if (this.InternalGlove != null && this.InternalGlove is SGCore.Nova.NovaGlove)
            {
                return ((SGCore.Nova.NovaGlove)this.InternalGlove).SupportsThresholds;
            }
            return false;
        }


        public virtual void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            if (this.InternalGlove != null && this.InternalGlove is SGCore.Nova.NovaGlove)
            {
                ((SGCore.Nova.NovaGlove)this.InternalGlove).QueueThresholdCmd_Flexions(fingers, fingerFlexions);
            }
        }


        public virtual void QueueWristSqueeze(float squeezeLevel01)
        {
            if (this.InternalGlove != null && this.InternalGlove is SGCore.Nova.Nova2Glove)
            {
                ((SGCore.Nova.Nova2Glove)this.InternalGlove).QueueSqueezeLevel(squeezeLevel01); //will be sent in the next update.
            }
        }

        public virtual void StopWristSqueeze()
        {
            if (this.InternalGlove != null && this.InternalGlove is SGCore.Nova.Nova2Glove)
            {
                ((SGCore.Nova.Nova2Glove)this.InternalGlove).StopSqueezeFeedback();
            }
        }


        /// <summary> Process all haptics that were sent this frame and that are still active. </summary>
        public virtual void UpdateHaptics(float dT)
        {
            //SG_FFBCmd newFFB; SG_BuzzCmd newBuzz; ThumperCmd newThumper;
            //if (this.hapticStream.FlushHaptics(dT, this.lastFFBLevels, lastBuzzCmd, lastThumperCmd, out newFFB, out newBuzz, out newThumper)) //processes Hapitcs in queue, and returns true if any of the new effects are different from the last
            //{
            //    //Debug.Log(this.name + ": Compiled " + newFFB.ToString() + ", " + newBuzz.ToString() + ", " + newThumper.ToString());
            //    //I see no reson to update these unless they were different
            //    this.lastFFBLevels = newFFB;
            //    this.lastBuzzCmd = newBuzz;
            //    this.lastThumperCmd = newThumper;
            //    if (this.lastGlove != null && !this.bypassingHaptics) //we'll process the timing regardless of whether or not we are connected. So this check comes after.
            //    {
            //        //actually properly queue the force-feedback.
            //        for (int f=0; f<5; f++)
            //        {
            //            SGCore.Finger finger = (SGCore.Finger)f;
            //            lastGlove.QueueFFBLevel(finger, lastFFBLevels.GetLevel(f) / 100.0f);
            //            lastGlove.QueueVibroLevel(finger, lastBuzzCmd.GetLevel(f) / 100.0f);
            //        }
            //        if (lastGlove is SGCore.Nova.NovaGlove)
            //        {
            //            SGCore.Nova.NovaGlove nova = (SGCore.Nova.NovaGlove)lastGlove;
            //            float properWrist = this.lastThumperCmd.magnitude / 100.0f; //div 100 because the bettwe magnitue is 0 .. 1
            //            nova.QueueWristLevel(properWrist);
            //        }

            //        //this.lastGlove.SendHaptics(newFFB, newBuzz, newThumper); //actually send these.
            //    }
            //}

            for (int f = 0; f < this.legacyFingers.Length; f++)
            {
                if (UpdateAndEvaluate(ref legacyFingers[f], dT, out float amplitude))
                {
                    if (lastGlove != null)
                    {
                        this.lastGlove.QueueVibroLevel((SGCore.Finger)f, amplitude);
                    }
                }
            }
            if (UpdateAndEvaluate(ref legacyWrist, dT, out float amp))
            {
                if (lastGlove != null && lastGlove is SGCore.Nova.NovaGlove)
                {
                    ((SGCore.Nova.NovaGlove)lastGlove).QueueWristLevel(amp);
                }
            }

            if (this.lastGlove != null)
            {
                bool sent = this.lastGlove.SendHaptics(); // Shushing everything!
                //if (sent)
                //{
                //    Debug.Log(Time.time + ": Sent Haptics to Glove!");
                //}
            }
        }


        /// <summary> Update and evaluates a Legacy Command. </summary>
        /// <param name="cmd"></param>
        /// <param name="dT"></param>
        /// <param name="amplitude"></param>
        /// <returns></returns>
        public static bool UpdateAndEvaluate(ref LegacyCommand cmd, float dT, out float amplitude)
        {
            if (cmd != null)
            {
                cmd.UpdateEffect(dT);
                if (cmd.HasElapsed())
                {
                    cmd = null;
                    amplitude = 0.0f;
                    //Debug.LogError(Time.time + ": Effect has elapsed. Ending it");
                    return true; //we need to update back to 0.0f!
                }
                else
                {
                    //Debug.LogError(Time.time + ": Effect Updated to " + cmd.Amplitude + " at " + cmd.ElapsedTime);
                    amplitude = cmd.Amplitude;
                    return true;
                }
            }
            amplitude = 0.0f;
            return false;
        }


        //--------------------------------------------------------------------------------------------------------
        // Utility Functions 

        ///// <summary> Access the latest FFB level. </summary>
        //public float[] LastFFBCommand
        //{
        //    get { return this.lastFFBLevels; }
        //}

        private void CheckCalibrationState()
        {
            SGCore.HG_CalibrationState newState = SGCore.HandLayer.GetCalibrationState(this.TracksRightHand());
            if (newState != this.LastCalibrationState)
            {
                this.LastCalibrationState = newState;
                this.CalibrationStateChanged.Invoke();
            }
        }

        //--------------------------------------------------------------------------------------------------------
        // Monobehavior 

        protected void Awake()
        {
            LastCalibrationState = SGCore.HG_CalibrationState.Unknown;
        }

        protected void Start()
        {
            SG.Util.SG_Connections.SetupConnections();
            UpdateDeviceType();
            timer_checkHW = hwTimer; //ensures we check it next frame!
        }


        protected virtual void Update()
        {
            timer_checkHW += Time.deltaTime;
            if (timer_checkHW >= hwTimer)
            {
                UpdateConnection();
                timer_checkHW = 0;
                CheckCalibrationState();
            }
            if (this.LastCalibrationState != SGCore.HG_CalibrationState.Unknown && this.LastCalibrationState != SGCore.HG_CalibrationState.CalibrationLocked)
            {
                CheckCalibrationState();
            }
        }

        protected virtual void LateUpdate()
        {
            newPoseNeeded = true; //next frame we're allowed to update the pose again.
#if CV_ENABLED
            newCVNeeded = true; // We're allowed to collect CV data again
#endif
            UpdateHaptics(Time.deltaTime);
        }


        protected virtual void OnApplicationQuit()
        {
            StopHaptics(); //Ensure no Haptics continue after the application quits.
        }

    }

#if HG_CUSTOM_INSPECTOR && UNITY_EDITOR

    // Declare type of Custom Editor
    [UnityEditor.CustomEditor(typeof(SG_HapticGlove))]
    [UnityEditor.CanEditMultipleObjects]
    public class SG_HapticGloveEditor : UnityEditor.Editor
    {
        public const float vSpace = 150f;

        private UnityEditor.SerializedProperty m_connectsTo;

        private UnityEditor.SerializedProperty m_wristTrackingMethod;
        private UnityEditor.SerializedProperty m_origin;
        private UnityEditor.SerializedProperty m_wristTrackingObj;
        private UnityEditor.SerializedProperty m_wristTrackingOffsets;


        private UnityEditor.SerializedProperty m_DeviceConnected;
        private UnityEditor.SerializedProperty m_DeviceDisconnected;
        private UnityEditor.SerializedProperty m_CalibrationStateChanged;


        void OnEnable()
        {
            m_connectsTo = serializedObject.FindProperty("connectsTo");

            m_wristTrackingMethod = serializedObject.FindProperty("wristTrackingMethod");
            m_origin = serializedObject.FindProperty("origin");
            m_wristTrackingObj = serializedObject.FindProperty("wristTrackingObj");
            m_wristTrackingOffsets = serializedObject.FindProperty("wristTrackingOffsets");

            m_DeviceConnected = serializedObject.FindProperty("DeviceConnected");
            m_DeviceDisconnected = serializedObject.FindProperty("DeviceDisconnected");
            m_CalibrationStateChanged = serializedObject.FindProperty("CalibrationStateChanged");
        }

        // OnInspector GUI
        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            GUILayout.Label("Connection Settings", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.PropertyField(m_connectsTo, new GUIContent("Connects To", "What kind of hand sides this SG_HapticGlove is allowed to connect to"));

            UnityEditor.EditorGUILayout.Space();
            GUILayout.Label("Wrist Tracking Settings", UnityEditor.EditorStyles.boldLabel);

            GUIContent l_wristTracking = new GUIContent("Wrist Tracking Method", "The method by which to determine Wrist Position of this SG_HapticGlove when no Optical Tracking is available.");
            UnityEditor.EditorGUILayout.PropertyField(m_wristTrackingMethod, l_wristTracking);

            SG_HapticGlove.WristTracking currWTMethod = (SG_HapticGlove.WristTracking)m_wristTrackingMethod.intValue;
            if (currWTMethod != SG_HapticGlove.WristTracking.MyGameObject)
            {
                if (currWTMethod == SG_HapticGlove.WristTracking.UnityXR || currWTMethod == SG_HapticGlove.WristTracking.UnityXRManualOffsets)
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_origin, new GUIContent("Origin", "Optional. When using UnityXR's wrist tracking, the wrist will move relative to this origin."));
                }
                if (currWTMethod == SG_HapticGlove.WristTracking.FollowObjectManualOffsets || currWTMethod == SG_HapticGlove.WristTracking.FollowObjectAutoOffsets || currWTMethod == SG_HapticGlove.WristTracking.FollowObjectNoOffsets)
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_wristTrackingObj, new GUIContent("Wrist Tracking Obj", "The GameObject to follow, using offsets"));
                }
                if (currWTMethod == SG_HapticGlove.WristTracking.FollowObjectManualOffsets || currWTMethod == SG_HapticGlove.WristTracking.UnityXRManualOffsets)
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_wristTrackingOffsets, new GUIContent("Wrist Tracking Offsets", "The hardware offsets from tracking reference to the wrist."));
                }
            }

            UnityEditor.EditorGUILayout.Space();
            GUILayout.Label("Events", UnityEditor.EditorStyles.boldLabel);

            UnityEditor.EditorGUILayout.PropertyField(m_DeviceConnected, new GUIContent("Device Connected", "Fires when this HapticGlove connects to this simulation. Switching Scene causes this to fire again."));
            UnityEditor.EditorGUILayout.PropertyField(m_DeviceDisconnected, new GUIContent("Device Disconnected", "Fires when this HapticGlove disconnects from the system."));
            UnityEditor.EditorGUILayout.PropertyField(m_CalibrationStateChanged, new GUIContent("Calibration State Changed", "Fires when this glove's calibration state changes."));

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}