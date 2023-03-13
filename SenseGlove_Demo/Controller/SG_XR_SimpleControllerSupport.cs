#define SGXR_CONTROLLER_CUSTOM_INSPECTOR

using UnityEngine;
using UnityEngine.XR;
using SGCore.Kinematics;

/*
 * A way to use the SenseGlove Interaction System using XR controllers through UnityXR's Button inputs. It does not use whatever hand tracking these devices have.
 * @author: Max Lammers
 */

namespace SG.XR
{

    /// <summary> Provides simple SenseGlove Hand Tracking via an XR Controller. Relies purely on button inputs, and not on UnityXR's Hand Tracking data. It is therefore not scalable to many pieces of hardware... </summary>
    public class SG_XR_SimpleControllerSupport : MonoBehaviour, IHandPoseProvider
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Enums

        /// <summary> Generic button to use as input. </summary>
        public enum ControlBtn
        {
            /// <summary> Button on the side of the controller, usually controller by the middle and/or ring finger(s). </summary>
            Grip,
            /// <summary> Trigger button that is most commonly controlled with the index finger. </summary>
            Trigger,
            /// <summary> Button on the controller face, commonly used for confirm actions. E.g. "A" button </summary>
            PrimaryFaceButton,
            /// <summary> Button on the controller face, commonly used for cancel actions e.g. "B" button. </summary>
            SecondaryFaceButton,
            /// <summary> Activated by clicking in the thumb stick. For teleportation, for example. </summary>
            AnalogStick
        }

        /// <summary> How the hand location of this controller is determined. </summary>
        public enum WristTrackingMethod
        {
            /// <summary> Wrist Tracking is determined using this GameObject's location. Useful for debugging, or when you're using non-standard offsets. </summary>
            ThisGameObject,
            /// <summary> Take controller data from UnityXR, but place the hand at the controller's origin (to see how badly it's mapped). </summary>
            UnityXR_ControllerLocation,          
            /// <summary> If known offsets exist to map the SenseGlove hand to a controller, use those. </summary>
            UnityXR_AutoOffsets,
            /// <summary> Take controller data from UnityXR, and force the hand to use specific offsets </summary>
            UnityXR_ManualOffsets,
        }

        /// <summary> Which controller offsets to use (if any). </summary>
        public enum ControllerHardware
        {
            /// <summary> Our system cannot determine which controller you're using. Instead, we just place the hand at the controller's origin. </summary>
            Unknown,
            /// <summary> This is a Quest-like Controller. </summary>
            OculusQuest
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> If true, this script takes input from the Right Controller, otherwise, it takes input from the left controller. </summary>
        public bool rightHand = true;

        /// <summary> The button that is used to Grab objects in the SenseGlove System </summary>
        [Header("Finger Tracking Inputs")]
        public ControlBtn grabButton = ControlBtn.Grip;
        
        /// <summary> The button for activating whatever it is in your hand (Power tools, toggle siwtches, etc) </summary>
        public ControlBtn useButton = ControlBtn.Trigger;

        /// <summary> How the wirst location is determined. </summary>
        [Header("Wrist Tracking Inputs")]
        public WristTrackingMethod wristTracking = WristTrackingMethod.UnityXR_AutoOffsets;

        /// <summary> The hardware that was manually set - used when UnityXR_ManualOffsets </summary>
        public ControllerHardware manualHardware = ControllerHardware.Unknown;

        /// <summary> When using the UnityXR Input system, we only get the local position. Assign your VR Rig to this field to have them move with your player. </summary>
        public Transform origin;


        /// <summary> The last 'level' at which the HandPose was generated. Has a range of 0-1 where 0 represents an open hand, and where 1 represents a closed fist.  </summary>
        protected float currentPoseLevel = -1; //range 0-1

        /// <summary> Hand Dimensions used for forward kinematics. </summary>
        protected SGCore.Kinematics.BasicHandModel handModel = null;
        /// <summary> The last HandPose that was calculated for this script. </summary>
        protected SG_HandPose lastPose = null;

        /// <summary> The latest battery level retrieved through the controller. </summary>
        protected float lastBatteryLevel = -1.0f;

        /// <summary> Interpolation angles ? </summary>
        protected static Vect3D[][] openAngles_left = null, openAngles_right = null;
        protected static Vect3D[][] closedAngles_left = null, closedAngles_right = null;

        /// <summary> Unity XR Input Device to collect controller information </summary>
        protected UnityEngine.XR.InputDevice xrDevice;
        /// <summary> Whether or we have found a controller for this script to link to. If we cannot find one, this device is not "Connected" </summary>
        protected bool controllerLinked = false;

        /// <summary> Controller Hardware determined by the SenseGlove System. </summary>
        protected ControllerHardware controllerHardware = ControllerHardware.Unknown;

        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Controller Offsets - UnityXR_AutoOffsets

        /// <summary> Position offset from UnityXR's Quest 2 right controller origin to the SenseGlove Wrist </summary>
        public static Vector3 quest2R_PosOffset = new Vector3(0.045f, -0.025f, -0.115f);
        /// <summary> Rotation offset from UnityXR's Quest 2 right controller origin to the SenseGlove Wrist </summary>
        public static Quaternion quest2R_RotOffset = Quaternion.Euler(-80, 0, -90);
        /// <summary> Position offset from UnityXR's Quest 2 left controller origin to the SenseGlove Wrist </summary>
        public static Vector3 quest2L_PosOffset = new Vector3(-0.045f, -0.025f, -0.115f);
        /// <summary> Rotation offset from UnityXR's Quest 2 left controller origin to the SenseGlove Wrist </summary>
        public static Quaternion quest2L_RotOffset = Quaternion.Euler(80, -180, -90);



        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Latest Controller values (not hand tracking; the actual device)

        /// <summary> The last position of the Controller's Origin </summary>
        public Vector3 LastControllerPos
        {
            get; protected set;
        }

        /// <summary> The last rotation of the Controller's Origin </summary>
        public Quaternion LastControllerRot
        {
            get; protected set;
        }

        /// <summary> The last value of the "Grab" button (0 .. 1) </summary>
        public float GrabBtnPressure
        {
            get; protected set;
        }

        /// <summary> The last value of the "Use" button (0..1) </summary>
        public float UseBtnPressure
        {
            get; protected set;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Controller / Hardware Linking

        /// <summary> Update the link to the controller. </summary>
        public void CheckDeviceLink()
        {
            bool nowLinked = SG_XR_Devices.GetHandDevice(this.TracksRightHand(), out this.xrDevice);
            if (!controllerLinked && nowLinked)
            {
                // xrDevice is now assigned and has manifacturer ids etc.
                this.controllerHardware = ControllerHardware.Unknown; //we don't know what this is, yet...
                if (xrDevice.name.ToLower().Contains("oculus") || xrDevice.name.ToLower().Contains("meta"))
                {
                    this.controllerHardware = ControllerHardware.OculusQuest;
                }
                Debug.Log("Linked " + this.name + " to " + this.xrDevice.name + ", which uses " + this.controllerHardware.ToString() + " offsets.");
            }
            else if (controllerLinked && !nowLinked)
            {
                Debug.Log("Lost Connection to " + this.xrDevice.name);
            }
            controllerLinked = nowLinked;
        }


        /// <summary> Turns a button that has only an on/off boolean state, and turn it into a pressure value; from 0...1 </summary>
        /// <param name="device"></param>
        /// <param name="feature"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool PressToPressure(InputDevice device, UnityEngine.XR.InputFeatureUsage<bool> feature, out float value)
        {
            value = -1.0f;
            bool pressed;
            if (device.TryGetFeatureValue(feature, out pressed))
            {
                value = pressed ? 1.0f : 0.0f;
            }
            return value > -1.0f;
        }


        /// <summary> Retrieve a pressure value (0..1) from a button of an Input device. </summary>
        /// <param name="device"></param>
        /// <param name="button"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool GetPressureValue(InputDevice device, ControlBtn button, out float value)
        {
            switch (button)
            {
                case ControlBtn.Trigger:
                    return device.TryGetFeatureValue(CommonUsages.trigger, out value);

                case ControlBtn.Grip:
                    return device.TryGetFeatureValue(CommonUsages.grip, out value);

                case ControlBtn.SecondaryFaceButton:
                    return PressToPressure(device, CommonUsages.secondaryButton, out value);

                case ControlBtn.AnalogStick:
                    return PressToPressure(device, CommonUsages.primary2DAxisClick, out value);

                default:
                    return PressToPressure(device, CommonUsages.primaryButton, out value);
            }
        }



        /// <summary> Update the variables </summary>
        public void UpdateControllerValues()
        {
            CheckDeviceLink();
            if (controllerLinked)
            {
                Vector3 pos;
                if (xrDevice.TryGetFeatureValue(CommonUsages.devicePosition, out pos))
                {
                    this.LastControllerPos = pos;
                }
                Quaternion rot;
                if (xrDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
                {
                    this.LastControllerRot = rot;
                }

                float grabBtnPressure;
                if (GetPressureValue(xrDevice, grabButton, out grabBtnPressure))
                {
                    this.GrabBtnPressure = grabBtnPressure;
                }

                float usePressure;
                if (GetPressureValue(xrDevice, useButton, out usePressure))
                {
                    this.UseBtnPressure = usePressure;
                }

                float BL;
                if (xrDevice.TryGetFeatureValue(CommonUsages.batteryLevel, out BL))
                {
                    this.lastBatteryLevel = BL;
                }
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // IHandPoseProvider interface implementation

        /// <summary> Returns true if this device tracks a right hand. </summary>
        /// <returns></returns>
        public virtual bool TracksRightHand()
        {
            return this.rightHand;
        }

        /// <summary> Returns true if a suitable controller is linked to this device. </summary>
        /// <returns></returns>
        public virtual bool IsConnected()
        {
            CheckDeviceLink();
            return controllerLinked;
        }

        /// <summary> Set the HandModel that will be used to calculate the positions / rotations of the hand. Up </summary>
        /// <param name="handModel"></param>
        public virtual void SetKinematics(BasicHandModel handModel)
        {
            this.handModel = handModel;
            CalculateFingerPoseAt(this.currentPoseLevel); //updates pose with the new hand model.
        }

        /// <summary> Get the Hand Model that is being used to calculate the positions / rotations of the hand  </summary>
        /// <returns></returns>
        public virtual BasicHandModel GetKinematics()
        {
            if (this.handModel == null)
            {
                this.handModel = SGCore.Kinematics.BasicHandModel.Default(this.TracksRightHand());
            }
            return this.handModel;
        }

        /// <summary> Retrieve the hand pose generated from controller inputs. Returns true if a pose is available (the Controller could be off, for example). </summary>
        /// <param name="handPose"></param>
        /// <param name="forcedUpdate"></param>
        /// <returns></returns>
        public virtual bool GetHandPose(out SG_HandPose handPose, bool forcedUpdate = false)
        {
            if (this.lastPose == null)
            {
                this.lastPose = SG_HandPose.Idle(this.TracksRightHand());
            }
            handPose = this.lastPose;
            return this.lastPose != null;
        }

        /// <summary> Returns the normalized finger flexions generated by this script. In this case, it's linked to the Grab Button. </summary>
        /// <param name="flexions"></param>
        /// <returns></returns>
        public virtual bool GetNormalizedFlexion(out float[] flexions)
        {
            SG_HandPose pose;
            if (this.GetHandPose(out pose))
            {
                flexions = SG.Util.SG_Util.ArrayCopy(pose.normalizedFlexion);
                return true;
            }
            flexions = new float[0];
            return false;
        }

        /// <summary> Returns a value between 0..1, representing a desire to grab an object - In this case; the pressure of the Grab button </summary>
        /// <returns></returns>
        public virtual float OverrideGrab()
        {
            UpdateControllerValues();
            return this.GrabBtnPressure;
        }

        /// <summary> Returns a value between 0..1, representing a desire to activate an object - In this case; the pressure of the Use button </summary>
        /// <returns></returns>
        public virtual float OverrideUse()
        {
            UpdateControllerValues();
            return this.UseBtnPressure;
        }

        /// <summary>  </summary>
        /// <returns></returns>
        public HandTrackingDevice TrackingType()
        {
            return HandTrackingDevice.Controller6DoF;
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            value01 = this.lastBatteryLevel;
            return value01 > 1.0f;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // HandPose Calculation

        // Wrist Tracking

        /// <summary> Retrieve controller offsets based on the tracking hardware </summary>
        /// <param name="rightHand"></param>
        /// <param name="hardware"></param>
        /// <param name="posOffset"></param>
        /// <param name="rotOffset"></param>
        public static void GetControllerOffsets(bool rightHand, ControllerHardware hardware, out Vector3 posOffset, out Quaternion rotOffset)
        {
            if (hardware == ControllerHardware.OculusQuest)
            {
                posOffset = rightHand ? quest2R_PosOffset : quest2L_PosOffset;
                rotOffset = rightHand ? quest2R_RotOffset : quest2L_RotOffset;
                return;
            }
            posOffset = Vector3.zero;
            rotOffset = Quaternion.identity;
        }

        /// <summary> Calculate the wrist location based on this script's parameters. </summary>
        /// <param name="wristPos"></param>
        /// <param name="wristRot"></param>
        public void CalculateWristLocation(out Vector3 wristPos, out Quaternion wristRot)
        {
            if (this.wristTracking == WristTrackingMethod.ThisGameObject)
            {
                wristPos = this.transform.position;
                wristRot = this.transform.rotation;
                return;
            }
            // For the others, I need at least the current controller position
            Quaternion controllerRot = origin != null ? origin.rotation * LastControllerRot : LastControllerRot;
            Vector3 controllerPos = origin != null ? origin.position + (origin.rotation * LastControllerPos) : LastControllerPos;

            if (this.wristTracking == WristTrackingMethod.UnityXR_ControllerLocation)
            {
                wristPos = controllerPos;
                wristRot = controllerRot;
                return;
            }

            Vector3 posOffset; Quaternion rotOffset;
            GetControllerOffsets(rightHand, (this.wristTracking == WristTrackingMethod.UnityXR_ManualOffsets ? this.manualHardware : this.controllerHardware), out posOffset, out rotOffset);
            wristRot = controllerRot * rotOffset;
            wristPos = controllerPos + (controllerRot * posOffset);
        }

        // Finger Tracking

        /// <summary> Retrieve internal Joint Angles for use in interpolation </summary>
        /// <param name="rightHand"></param>
        /// <param name="handAngles0"></param>
        /// <param name="handAngles1"></param>
        public static void GetInterpolationInput(bool rightHand, out Vect3D[][] handAngles0, out Vect3D[][] handAngles1)
        {
            if (openAngles_right == null) { openAngles_right = SGCore.HandPose.DefaultIdle(true).handAngles; }
            if (closedAngles_right == null) { closedAngles_right = SGCore.HandPose.Fist(true).handAngles; }
            if (openAngles_left == null) { openAngles_left = SGCore.HandPose.DefaultIdle(false).handAngles; }
            if (closedAngles_left == null) { closedAngles_left = SGCore.HandPose.Fist(false).handAngles; }

            handAngles0 = rightHand ? openAngles_right : openAngles_left;
            handAngles1 = rightHand ? closedAngles_right : closedAngles_left;
        }


        /// <summary> Calculates the HandPose at a specific step '0 .. 1'. </summary>
        /// <param name="level"></param>
        protected virtual void CalculateFingerPoseAt(float level)
        {
            this.currentPoseLevel = Mathf.Clamp01(level);

            Vect3D[][] handAngles0, handAngles1;
            GetInterpolationInput(this.TracksRightHand(), out handAngles0, out handAngles1);

            Vect3D[][] newAngles = SGCore.Kinematics.Values.InterpolateHandAngles_WithJointAngles(currentPoseLevel, 0, 1, handAngles0, handAngles1, this.TracksRightHand(), true);
            SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(newAngles, this.TracksRightHand(), this.GetKinematics());

            this.lastPose = new SG_HandPose(iPose);
        }

        // Hand Tracking.

        /// <summary> Update this script's HandPose, if required. </summary>
        /// <param name="dT">Could be used later to make finger movement less instantaneous. </param>
        public void UpdateHandPose(float dT)
        {
            UpdateControllerValues(); //ensure you have the latest controller values.
            if (this.IsConnected())
            {
                float newPressure = GrabBtnPressure;
                if (newPressure != this.currentPoseLevel) //its a different value
                {
                    CalculateFingerPoseAt(this.GrabBtnPressure);
                }
                //even if the fingers don't update, the wrist will(!)
                if (this.lastPose != null) //unless there was never a pose because we coulnd not get the pressure?
                {
                    CalculateWristLocation(out this.lastPose.wristPosition, out this.lastPose.wristRotation);
                }
                //At this Stage, LastPose contains the appropriate Finger Tracking and Wrist location.
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Update is called once per frame
        protected void Update()
        {
            UpdateHandPose(Time.deltaTime);
        }
    }



#if SGXR_CONTROLLER_CUSTOM_INSPECTOR && UNITY_EDITOR

    // Declare type of Custom Editor
    [UnityEditor.CustomEditor(typeof(SG_XR_SimpleControllerSupport))]
    [UnityEditor.CanEditMultipleObjects]
    public class SG_XR_SimpleControllerEditor : UnityEditor.Editor
    {
        public const float vSpace = 150f;

        private UnityEditor.SerializedProperty m_rightHand;
        private UnityEditor.SerializedProperty m_grabBtn;
        private UnityEditor.SerializedProperty m_useBtn;
        
        private UnityEditor.SerializedProperty m_wristTracking;
        private UnityEditor.SerializedProperty m_manualHW;
        private UnityEditor.SerializedProperty m_origin;

        void OnEnable()
        {
            m_rightHand = serializedObject.FindProperty("rightHand");
            m_grabBtn = serializedObject.FindProperty("grabButton");
            m_useBtn = serializedObject.FindProperty("useButton");

            m_wristTracking = serializedObject.FindProperty("wristTracking");
            m_manualHW = serializedObject.FindProperty("manualHardware");
            m_origin = serializedObject.FindProperty("origin");
        }

        // OnInspector GUI
        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            UnityEditor.EditorGUILayout.PropertyField(m_rightHand, new GUIContent("Right Hand", "If enabled, this script collects data from the right controller. Otherwise, it collects from the left controller."));

            UnityEditor.EditorGUILayout.PropertyField(m_grabBtn, new GUIContent("Grab Button", "Button used to grasp objects.")); //this one comes with its own header.
            UnityEditor.EditorGUILayout.PropertyField(m_useBtn, new GUIContent("Use Button", "Button used to activate objects in one's hand."));

            UnityEditor.EditorGUILayout.PropertyField(m_wristTracking, new GUIContent("Wrist Tracking", "How to determine where to place the hand's wrist - Required because this isn't using UnityXR's Hand Tracking.")); //comes with the header...

            SG_XR_SimpleControllerSupport.WristTrackingMethod currWTMethod = (SG_XR_SimpleControllerSupport.WristTrackingMethod)m_wristTracking.intValue;
            if (currWTMethod != SG_XR_SimpleControllerSupport.WristTrackingMethod.ThisGameObject) //using this gameobject means we don't need any other options...
            {
                UnityEditor.EditorGUILayout.PropertyField(m_origin, new GUIContent("Origin", "When using the UnityXR Input system, we only get the local position. Assign your VR Rig to this field to have them move with your player."));
                if (currWTMethod == SG_XR_SimpleControllerSupport.WristTrackingMethod.UnityXR_ManualOffsets)
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_manualHW, new GUIContent("Manual Offsets", "Force this script to use these offsets for wrist tracking."));
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

#endif


}