using SGCore.Haptics;
using SGCore.Kinematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG
{
    /// <summary> Instead of this being a single provider, you can add a list of devices with a specific order. This script will select from available devices, and ensure the proper functions are passed on. </summary>
    public class SG_DeviceSelector : MonoBehaviour, IHandFeedbackDevice, IHandPoseProvider
    {

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Lets the system knwo which hand(s) this script is intended for. Useful for debugging. </summary>
        public HandSide intendedFor = HandSide.RightHand;

        /// <summary> Represents all Device(s) that this Selector may use as an input. </summary>
        [SerializeField] protected MonoBehaviour[] devices = new MonoBehaviour[0];

        /// <summary> Latest handModelToUse. </summary>
        protected SGCore.Kinematics.BasicHandModel handModelToUse;

        /// <summary> All of the Tracking Devices. Can be NULL </summary>
        protected List<IHandPoseProvider> trackingDevices = new List<IHandPoseProvider>();
        /// <summary> All of the HandPoseProviders. Can be NULL </summary>
        protected List<IHandFeedbackDevice> feedbackDevices = new List<IHandFeedbackDevice>();

        /// <summary> Which of the devices has currently been identified as being active... </summary>
        protected int activeDeviceIndex = -1;

        /// <summary> The last frame were we checked for device updates. Using this to catch multi-updates in a single frame </summary>
        protected int lastFrameChecked = -1;
        /// <summary> The last RealTimeSinceStartup where we checked for device updates. using this to ensure we don;t need to check every single frame. </summary>
        protected float lastCheckedTime = -1000;

        /// <summary> The time between device checks, used to save some computational power </summary>
        public const float checkTime = 1.0f;


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Events

        /// <summary> Fires when a new Device is linked to the DeviceSelector. </summary>
        public SG.Util.SGEvent DeviceChanged = new Util.SGEvent();

        /// <summary> Call the DeviceChanged event </summary>
        protected void OnDeviceChanged()
        {
            DeviceChanged.Invoke();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Functions

        /// <summary> Returns the currently active Hand Tracking Device. </summary>
        public IHandPoseProvider CurrentTracking
        {
            get
            {
                CheckDevices();
                return activeDeviceIndex > -1 ? trackingDevices[activeDeviceIndex] : null;
            }
        }

        /// <summary> Returns the currently active Haptic Device. </summary>
        public IHandFeedbackDevice CurrentHaptics
        {
            get
            {
                CheckDevices();
                return activeDeviceIndex > -1 ? feedbackDevices[activeDeviceIndex] : null;
            }
        }

        public void CollectDevices()
        {
            this.feedbackDevices.Clear();
            this.trackingDevices.Clear();
            for (int i = 0; i < this.devices.Length; i++)
            {
                AddDevice(this.devices[i]);
            }
        }

        public void AddDevice(GameObject fromGameObj)
        {
            MonoBehaviour[] allScripts = fromGameObj.GetComponents<MonoBehaviour>();
            for (int i = 0; i < allScripts.Length; i++)
            {
                AddDevice(allScripts[i]);
            }
        }

        public void AddDevice(MonoBehaviour fromScript)
        {
            if (!(fromScript is SG_TrackedHand) && fromScript != this)
            {   //not allowed to add myself, nor the trackedHands that use me as an input.
                IHandPoseProvider trackingComponent = fromScript is IHandPoseProvider ? ((IHandPoseProvider)fromScript) : null;
                IHandFeedbackDevice hapticsComponent = fromScript is IHandFeedbackDevice ? ((IHandFeedbackDevice)fromScript) : null;
                if (this.intendedFor != HandSide.AnyHand)
                {
                    if (trackingComponent.TracksRightHand() != this.TracksRightHand())
                    {
                        Debug.LogWarning(this.name + " is meant for a " + (this.TracksRightHand() ? "right" : "left") + " hands, but you've assigned it a " + (trackingComponent.TracksRightHand() ? "right" : "left") + " hand tracking script. Are you sure this is correct?");
                    }
                }
                AddDevice(trackingComponent, hapticsComponent);
            }
        }

        public void AddDevice(IHandPoseProvider trackingComponent, IHandFeedbackDevice hapticsComponent)
        {
            if (trackingComponent != null || hapticsComponent != null) //at least one of them is not NULL
            {
                int trackIndex = ListIndex(trackingComponent);
                int haptIndex = ListIndex(hapticsComponent);
                if (trackIndex < 0 && haptIndex < 0) //not added either of these yet
                {
                    trackingComponent.SetKinematics(this.GetKinematics());
                    this.trackingDevices.Add(trackingComponent);
                    this.feedbackDevices.Add(hapticsComponent);
                }
                else if (trackIndex > -1 && feedbackDevices[trackIndex] == null) //We have the haptics of a trackedObject that weren't assigned yet
                {
                    feedbackDevices[trackIndex] = hapticsComponent;
                }
                else if (haptIndex > -1 && trackingDevices[haptIndex] != null) //We have the tracking of a haptics that wasnt't assigned yet
                {
                    trackingComponent.SetKinematics(this.GetKinematics());
                    trackingDevices[haptIndex] = trackingComponent;
                }
            }
        }

        public int ListIndex(IHandPoseProvider device)
        {
            if (device == null)
            {
                return -1;
            }
            for (int i = 0; i < this.trackingDevices.Count; i++)
            {
                if (this.trackingDevices[i] == device)
                {
                    return i;
                }
            }
            return -1;
        }

        public int ListIndex(IHandFeedbackDevice device)
        {
            if (device == null)
            {
                return -1;
            }
            for (int i = 0; i < this.feedbackDevices.Count; i++)
            {
                if (this.feedbackDevices[i] == device)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary> Check if we need to look for new devices. If so, go do that and update the activeDeviceIndex </summary>
        public void CheckDevices()
        {
            if (Time.frameCount == this.lastFrameChecked)
            {
                return; //we've already checked this frame, no need to do more complex math
            }
            if (Time.realtimeSinceStartup - this.lastCheckedTime < checkTime) //use Mathf.Abs to check for stackoverflow?
            {
                return; //already checked a little while ago, don't need to do it again either.
            }
            this.lastCheckedTime = Time.realtimeSinceStartup;
            this.lastFrameChecked = Time.frameCount;

            //Actually check the devices
            int lastDevice = this.activeDeviceIndex;
            this.activeDeviceIndex = GetFirstActiveDevice(); //resetted.
            if (lastDevice != this.activeDeviceIndex) //chnaged a device...
            {
                OnDeviceChanged();
            }
        }

        /// <summary> Returns the index of the very first device that is connected </summary>
        /// <returns></returns>
        public int GetFirstActiveDevice()
        {
            CheckDevices();
            for (int i = 0; i < this.trackingDevices.Count; i++)
            {
                if ((trackingDevices[i] != null && trackingDevices[i].IsConnected()) || (feedbackDevices[i] != null && feedbackDevices[i].IsConnected()))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary> Retruns a device of the chosen type, provided that it is linked to this Selector. Returns null if it does not exist in this list </summary>
        /// <returns></returns>
        public T GetDevice<T>() where T : MonoBehaviour
        {
            CheckDevices();
            for (int i = 0; i < this.trackingDevices.Count; i++) //these should be the same length
            {
                if (trackingDevices[i] != null && trackingDevices[i] is T)
                {
                    return (T)trackingDevices[i];
                }
                if (feedbackDevices[i] != null && feedbackDevices[i] is T)
                {
                    return (T)feedbackDevices[i];
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // HandPoseProvider Implementation

        public HandTrackingDevice TrackingType()
        {
            return this.CurrentTracking != null ? this.CurrentTracking.TrackingType() : HandTrackingDevice.Unknown;
        }


        public BasicHandModel GetKinematics()
        {
            if (this.handModelToUse == null) //ensures it exists
            {
                this.handModelToUse = SGCore.Kinematics.BasicHandModel.Default(this.intendedFor == HandSide.LeftHand ? false : true);
            }
            return this.handModelToUse;
        }


        public void SetKinematics(BasicHandModel handModel)
        {
            this.handModelToUse = handModel;
            for (int i = 0; i < this.trackingDevices.Count; i++)
            {
                if (this.trackingDevices != null)
                {
                    this.trackingDevices[i].SetKinematics(handModel);
                }
            }
        }


        public bool IsConnected()
        {
            return this.CurrentTracking != null;
        }

        public bool TracksRightHand()
        {
            return this.intendedFor != HandSide.LeftHand;
        }



        public bool GetHandPose(out SG_HandPose handPose, bool forcedUpdate = false)
        {
            if (this.CurrentTracking != null)
            {
                return this.CurrentTracking.GetHandPose(out handPose, forcedUpdate);
            }
            handPose = null;
            return false;
        }

        public bool GetNormalizedFlexion(out float[] flexions)
        {
            if (this.CurrentTracking != null)
            {
                return this.CurrentTracking.GetNormalizedFlexion(out flexions);
            }
            flexions = new float[5];
            return false;
        }



        public float OverrideGrab()
        {
            if (this.CurrentTracking != null)
            {
                return this.CurrentTracking.OverrideGrab();
            }
            return 0.0f;
        }

        public float OverrideUse()
        {
            if (this.CurrentTracking != null)
            {
                return this.CurrentTracking.OverrideUse();
            }
            return 0.0f;
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            if (this.CurrentTracking != null)
            {
                return this.CurrentTracking.TryGetBatteryLevel(out value01);
            }
            else if (this.CurrentHaptics != null)
            {
                return this.CurrentHaptics.TryGetBatteryLevel(out value01);
            }
            value01 = -1.0f;
            return false;
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // IHandFeedbackDevice Implementation



        public string Name()
        {
            if (this.CurrentHaptics != null)
            {
                return this.CurrentHaptics.Name();
            }
            return "N\\A";
        }


        public void SendCmd(SG_FFBCmd ffb)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(ffb);
            }
        }

        public void SendCmd(SG_TimedBuzzCmd fingerCmd)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(fingerCmd);
            }
        }

        public void SendCmd(TimedThumpCmd wristCmd)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(wristCmd);
            }
        }

        public void SendCmd(ThumperWaveForm waveform)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(waveform);
            }
        }

        public void SendCmd(SG_Waveform waveform)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(waveform);
            }
        }

        public void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendImpactVibration(location, normalizedVibration);
            }
        }

        public void StopAllVibrations()
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.StopAllVibrations();
            }
        }

        public void StopHaptics()
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.StopHaptics();
            }
        }

        public void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SendCmd(customWaveform, location);
            }
        }

        public bool FlexionLockSupported()
        {
            if (this.CurrentHaptics != null)
            {
                return CurrentHaptics.FlexionLockSupported();
            }
            return false;
        }

        public void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            if (this.CurrentHaptics != null)
            {
                this.CurrentHaptics.SetFlexionLocks(fingers, fingerFlexions);
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Use this for initialization
        private void Awake()
        {
            CollectDevices();
            CheckDevices();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                CollectDevices(); //Relink...?
            }
        }

#endif

    }
}