//#define CUSTOM_HANDUI_INSPECTOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * Ideal Logic: This wrist element is always visible, until the first time you look at it [gazed at]. After that, it becomes hidden when you turn your wrist away.
 * Pressing it will raise an event and fire some haptics. This script controls the appearance and functionality of the wrist menu only. It does not do anything with what happens afterward.
 */

namespace SG.XR
{

    /// <summary> A little display that sits near or on the hand, which shows you a small UI when glanced at. It can be tapped to raise an event. </summary>
	public class SG_XR_HandUI : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utility Enumeration

        /// <summary> How this scipt links itself to the hand. </summary>
        public enum TrackingMethod
        {
            /// <summary> The tracking Method is disabled. Use this when you're making this UI a child object. </summary>
            None,
            /// <summary> Automatically find the TrackedHand associated with the handSide of this Script. </summary>
            FindTrackedHand,
            /// <summary> We follow a manually assigned TrackedHand ourselves. </summary>
            ManualTrackedHand
        }


        /// <summary> Location of the Wrist Menu, relative to the wrist location. You can either use a preset location, or a custom one. Your choice.  </summary>
        public enum MenuLocation
        {
            /// <summary> The menu will be located somwhat under the hand palm </summary>
            HandPalm,
            /// <summary> The menu will be located on the back of the hand, where a wrist watch would normally be. </summary>
            BackOfHand,
            /// <summary> The menu will take on the location relative to the wrist of the linkedHand. </summary>
            Custom
        }


        /// <summary> How the wrist gizmo is shown / hidden during the simulation. </summary>
        public enum HideMode
        {
            /// <summary> The wrist gizmo is always visible to your user. </summary>
            AlwaysVisible,
            /// <summary> The wrist gizmo hides itself based on its angle with the headset, after being viewed or tapped. </summary>
            AutoHide
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Values

        /// <summary> The way in which the menu is linked to the (desired) hand </summary>
        [Header("Tracking Parameters")]
        [SerializeField] protected TrackingMethod trackingMethod = TrackingMethod.FindTrackedHand;

        /// <summary> Which hand to (autmatically) link this hand UI to. </summary>
        [SerializeField] protected HandSide linkTo = HandSide.RightHand;

        /// <summary> The (preset) location of the Wrist Gizmo, relative to the wrist of the hand. </summary>
        [SerializeField] protected MenuLocation menuLocation = MenuLocation.HandPalm;

        /// <summary> The SG_Trackedhand from which we grab tracking information. </summary>
        public SG_TrackedHand linkedHand;

        /// <summary> Camera head transform, used to determine your head angle compared to the watch angle. When not assigned, things like "auto-hide" and "functional angle" are ignored. </summary>
        public Transform hmdTransform;
        /// <summary> Which axis of the camera is considered "forward" a.k.a. moving along this axis of the hmdTransform gets you further away. </summary>
        public SG.Util.MoveAxis hmdForward = SG.Util.MoveAxis.Z;

        /// <summary> The normal of the screen or face you're checking against the camera. We use this to determine your angle. </summary>
        public SG.Util.MoveAxis facePlaneNormal = SG.Util.MoveAxis.Y;

        // <summary> Position offset for Custom location </summary>
        protected Vector3 gizmoPos_Custom = Vector3.zero;
        // <summary> Rotation offset for Custom location </summary>
        protected Quaternion gizmoRot_Custom = Quaternion.identity;

        /// <summary> Position offset for HandPalm location </summary>
        protected static readonly Vector3 gizmoPos_HandPalm_R = new Vector3(0.055f, -0.05f, 0.0f);
        /// <summary> Rotation offset for HandPalm location </summary>
        protected static readonly Quaternion gizmoRot_HandPalm_R = Quaternion.Euler(0.0f, -90.0f, 180.0f);

        /// <summary> Position offset for BackOfHand location </summary>
        protected static readonly Vector3 gizmoPos_BackOfHand_R = new Vector3(-0.06f, 0.033f, 0);
        /// <summary> Rotation offset for BackOfHand location </summary>
        protected static readonly Quaternion gizmoRot_BackOfHand_R = Quaternion.identity;





        /// <summary> The last angle between the wrist menu and the camera. </summary>
        protected float lastAngle = 180;

        /// <summary> The menu can only be tapped when the gaze angle is smaller than this angle. Will be ignored if hmdTransform is not assigned. </summary>
        [Range(0, 180)] public float functionalAngle = 30;

        /// <summary> Object(s) that are hidden unless the menu is at the functionalAngle OR when a hand is in proximity. </summary>
        public GameObject[] faceItems = new GameObject[0];


        /// <summary> Another hand entering this zone will activate tap logic. </summary>
        [Header("Tapping Logic")]
        public SG_HandDetector tapZone;

        /// <summary> Cooldown in seconds between tapping and untapping again </summary>
        public float tapCoolDown = 0.5f;

        /// <summary> While tapLocked is true, no event can be played </summary>
        protected bool tapLocked = false;

        /// <summary> The waveform to play when a hand taps this Hand UI. </summary>
        public SG_Waveform tappedWaveform;

        /// <summary> This event fires when this menu is tapped by another SG_TrackedHand. </summary>
        public UnityEngine.Events.UnityEvent OnMenuTapped = new UnityEngine.Events.UnityEvent();


        /// <summary> Whether or not to hide this menu at all. Or if it's always visible. In that case, the other variables need not be assigned. </summary>
        [Header("Visibility Parameters")]
        public HideMode menuVisibility = HideMode.AutoHide;

        //if we're not hiding the UI, all of these are not needed.

        /// <summary> If set to true, this watch will start hidden until you gaze at it the first time. </summary>
        public bool startsHidden = false;

        /// <summary> From this angle and above, the menu is fully hidden. (unless we're close enough with our other hand) </summary>
        [Range(0, 180)] public float hiddenWhenAngleOver = 50;


        /// <summary> Used to smoothly fade the visibility of the Hand UI based on the hand angle </summary>
        public SG.Util.SG_MaterialFader fader;


        /// <summary> Waveform to play when the menu becomes visible / interactable with. </summary>
        public SG_Waveform visibleWaveform;
        /// <summary> If set to true, the visible wafefrom has been fired, and we must wait for the menu to become invisible again before we allow it to fire once more... </summary>
        protected bool visibleFired = false;
        /// <summary> The visibility the menu must drop to before we're allowed to fire another visibleWaveform </summary>
        public const float cooldownVisibility = 0.80f; //80%. visibility allows us to send the waveform back again...

        /// <summary> Determines if a first gaze has been made for each hand throughout the simulation. </summary>
        protected static bool hideAllowed_L = false, hideAllowed_R = false;


        /// <summary> Optional Utility zone to always show the UI when the other hand is close. </summary>
        [SerializeField] protected SG_HandDetector proximityZone;
        /// <summary> When close enough and at this angle, we should show the menu regardless </summary>
        [Range(0, 180)] public float validProximityAngle = 80;
        /// <summary> How long the proximity detector needs to detect a hand </summary>
        public const float proximityTime = 0.100f; //100 ms.

        /// <summary> Collider around the wrist to determine whether or not we're gaing at the wrist menu. </summary>
        public Collider gazeCollider;
        /// <summary> Íf true, we are looking at the menu, and it's making a decent enough angle with the HMD. </summary>
        protected bool validGazing = false;
        /// <summary> Timer to check if we have gazed for long enough... </summary>
        protected float timer_gazing = 0;
        /// <summary> Maximum distance for the rayCast made to check valid Gazes </summary>
        public const float gazeDistance = 1.5f; //1.5m
        /// <summary> How long one must gaze for before we hide the menu again... </summary>
        public const float firstGazeTime = 0.200f; //200 ms


        /// <summary> If set to true, we will update debug info. </summary>
        [Header("Debugging")]
        public bool debug = false;
        /// <summary> Test Element to output Debug Information to. </summary>
        public TextMesh debugTxt;


      

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors



        /// <summary> Determines whether or not the wirst menu is currently visible / tappable </summary>
        public bool MenuFunctional
        {
            get { return this.FaceItemsVisible; }
        }


        /// <summary> The angle this menu face makes relative to the HMD. If 0, they are facing one another perfectly(!) </summary>
        public float AngleWithHMD
        {
            get { return this.lastAngle; }
        }

        /// <summary> Retrieve the hand linked to this script. </summary>
        public SG_TrackedHand LinkedHand
        {
            get { return this.linkedHand; }
        }

        /// <summary> The alpha (visibility) level of the 3D model, using the Fader script. </summary>
        public float UIModelAlpha
        {
            get { return this.fader != null ? this.fader.FadeLevel : 1.0f; }
            set 
            { 
                if (this.fader != null)
                {
                    float val = Mathf.Clamp01(value);
                    this.fader.SetFadeLevel(val); 
                } 
            }
        }

        /// <summary> If true, the GameObjects defined in faceItems are visible. If false, they are hidden. </summary>
        public bool FaceItemsVisible
        {
            get { return this.faceItems.Length > 0 ? this.faceItems[0].activeSelf : false; }
            set
            {
                for (int i = 0; i < this.faceItems.Length; i++)
                {
                    faceItems[i].SetActive(value);
                }
            }
        }

        /// <summary> Sets the text of our  debugText element if there is any assigned </summary>
        public string DebugText
        {
            get { return this.debugTxt != null ? debugTxt.text : ""; }
            set { if (this.debugTxt != null) { this.debugTxt.text = value; } }
        }


        /// <summary> If true, this menu is allowed to be hidden. Otherwise, it needs some form of user acknowledgement to start hiding once more... </summary>
        public bool HidingAllowed
        {
            get
            {
                if (this.linkedHand == null) { return false; }
                return this.linkedHand.TracksRightHand() ? hideAllowed_L : hideAllowed_L;
            }
            set
            {
                if (this.linkedHand != null)
                {
                    if (this.linkedHand.TracksRightHand()) { hideAllowed_L = value; }
                    else { hideAllowed_L = value; }
                }
            }
        }


        public bool LinkedToRightHand
        {
            get 
            {
                CheckForComponents();
                if (this.LinkedHand != null)
                {
                    return this.linkedHand.TracksRightHand();
                }
                return this.linkTo != HandSide.LeftHand;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Hand UI Function

        /// <summary> Check for the existince of useful components, if they are not assigned. </summary>
        public virtual void CheckForComponents()
        {
            if (this.fader == null) //material fader
            {
                this.fader = this.GetComponent<SG.Util.SG_MaterialFader>();
                if (this.fader == null)
                {
                    this.fader = this.GetComponentInChildren<SG.Util.SG_MaterialFader>();
                }
            }
            if (hmdTransform == null && Camera.main != null)
            {
                hmdTransform = Camera.main.transform;
            }
            if (this.linkedHand == null || (this.linkedHand != null && this.trackingMethod == TrackingMethod.FindTrackedHand))
            {
                this.linkedHand = SG.Util.SG_Util.FindHandInScene(this.linkTo);
            }
        }


        /// <summary> Ensure all components are assigned, and set up in terms of physics. </summary>
        public virtual void SetupMenu()
        {
            // Step 1: Find anu missing components
            CheckForComponents();

            // Starting values
            DebugText = "";
            lastAngle = 190.0f; //> 180.
            this.HidingAllowed = HidingAllowed || this.startsHidden; //if we're already starting hidden, it's all good. Otherwise, we check if the user had acknowledged the menu before...

            // Step 2: Ensure those components are setup correctly.
            if (this.gazeCollider != null) { this.gazeCollider.isTrigger = true; }
            if (this.tapZone != null) 
            { 
                this.tapZone.detectionTime = 0;
                Collider[] colliders = tapZone.GetColliders();
                for (int i=0; i<colliders.Length; i++)
                {
                    colliders[i].isTrigger = true;
                }
            }
            if (this.proximityZone != null) 
            { 
                this.proximityZone.detectionTime = proximityTime; 
                Collider[] colliders = proximityZone.GetColliders();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].isTrigger = true;
                }
            }

            // Setup collision & Hand Tracking
            SG_TrackedHand otherHand;
            if (this.linkedHand.GetOtherHand(out otherHand))
            {
                if (this.tapZone != null) { this.tapZone.detectableHands = new List<SG_TrackedHand>() { otherHand }; }
                if (this.proximityZone != null) { this.proximityZone.detectableHands = new List<SG_TrackedHand>() { otherHand }; }

                // Ignoring the wrist collision of the other hand to avoid accidental activation
                if (otherHand.handPhysics != null)
                {
                    Collider[] otherWrist = otherHand.handPhysics.wristColliders;
                    Collider[] tapColliders = tapZone != null ? this.tapZone.GetColliders() : new Collider[0];
                    Collider[] hoverColliders = this.proximityZone != null ? this.proximityZone.GetColliders() : new Collider[0];
                    for (int i = 0; i < otherWrist.Length; i++)
                    {
                        for (int j = 0; j < tapColliders.Length; j++)
                        {
                            Physics.IgnoreCollision(otherWrist[i], tapColliders[j], true);
                        }
                        for (int j = 0; j < hoverColliders.Length; j++)
                        {
                            Physics.IgnoreCollision(otherWrist[i], hoverColliders[j], true);
                        }
                    }
                }
            }
            // Step 3: Calculate offsets & Place the menu at its appropriate location 
            UpdateMenuParent();
            // Step 4: Update the menu's visbility
            CheckMenuVisibility();
        }



        /// <summary> Calculates the angle (in degrees) between a face and a camera. </summary>
        /// <param name="faceTransform"></param>
        /// <param name="faceNormal"></param>
        /// <param name="hmdTransform"></param>
        /// <param name="hmdFwd"></param>
        /// <returns></returns>
        public static float FaceAngle(Transform faceTransform, SG.Util.MoveAxis faceNormal, Transform hmdTransform, SG.Util.MoveAxis hmdFwd)
        {
            Vector3 myNormal = faceTransform.rotation * SG.Util.SG_Util.GetAxis(faceNormal); //3d Representation of my "up" vector.
            Vector3 camInward = hmdTransform.rotation * (SG.Util.SG_Util.GetAxis(hmdFwd) * -1); //We need to invert the camera's "fwd" axis; angle should be 0 if you're directly above it.
            return Vector3.Angle(myNormal, camInward);
        }

        /// <summary> Returns the angle between the hmdTransform and the facePlaneNormal. Is 0 when directly faced, and up to 180 degrees when turned completely away  </summary>
        /// <returns></returns>
        public virtual float CalculateFaceAngle()
        {
            if (this.hmdTransform != null)
            {
                return FaceAngle(this.transform, this.facePlaneNormal, this.hmdTransform, this.hmdForward);
            }
            return 0.0f;
        }


        /// <summary> Update all variables that pertain to the visibility && functionality of the menu. </summary>
        public void UpdateMenuLogic(float dT)
        {
            // Calculate menu angle (used for visibility and tapping)
            this.lastAngle = CalculateFaceAngle();

            if (this.menuVisibility == HideMode.AlwaysVisible)
            {
                return; //I don't care about any other logic if we're always visible.
            }

            // Determine if the user is currently looking at the menu (used for visibility)
            bool isGazing = false;
            //determine whether or not the user is gazing at the menu (and it's reasonably facing us)
            if (this.gazeCollider != null && hmdTransform != null && SG_XR_Devices.HeadsetOnHead()) //HMD must be on head for this to be a valid XR device.
            {
                Vector3 hmdPos = this.hmdTransform.position;
                Vector3 direction = this.hmdTransform.rotation * SG.Util.SG_Util.GetAxis(this.hmdForward);

                Ray ray = new Ray(this.hmdTransform.position, direction);
                isGazing = this.gazeCollider.Raycast(ray, out RaycastHit hit, gazeDistance); //this raycasts specifically for this collider.
                if (debug)
                {
                    Vector3 dLine = direction * gazeDistance;
                    Debug.DrawLine(hmdPos, hmdPos + dLine, Color.red);
                }
            }
            this.validGazing = isGazing && lastAngle <= this.functionalAngle;
            if (!HidingAllowed)
            {
                if (validGazing) //not yet allowed 
                {
                    timer_gazing += dT;
                    if (timer_gazing <= firstGazeTime)
                    {
                        this.HidingAllowed = true; //alright, you've stared at it long enough
                    }
                }
                else { timer_gazing = 0.0f; }
            }
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Wrist Tracking Functions

        /// <summary> Sets the menu to the appropriate parent (and offsets if MenuLocation is not set to Custom). If the trackingMethod is set to "None", does nothing.  </summary>
        public void UpdateMenuParent()
        {
            if (this.trackingMethod == TrackingMethod.None)
                return;

            if (this.linkedHand != null)
            {
                bool rightHand = linkedHand.TracksRightHand();
                Transform wristTransform = linkedHand.GetTransform(SG_TrackedHand.TrackingLevel.RenderPose, HandJoint.Wrist);
                if (wristTransform != null)
                {
                    if (menuLocation != MenuLocation.Custom)
                    {
                        Vector3 localPos; Quaternion localRot;
                        switch (this.menuLocation)
                        {
                            case MenuLocation.HandPalm:
                                localPos = gizmoPos_HandPalm_R;
                                localRot = gizmoRot_HandPalm_R;
                                //localRot = rightHand ? gizmoRot_HandPalm_R : Quaternion.Euler(0.0f, 180.0f, 0.0f) * gizmoRot_HandPalm_R;
                                break;
                            case MenuLocation.BackOfHand:
                                localPos = gizmoPos_BackOfHand_R;
                                localRot = rightHand ? gizmoRot_BackOfHand_R : Quaternion.Euler(0.0f, 180.0f, 0.0f) * gizmoRot_BackOfHand_R;
                                break;
                            default:
                                localPos = Vector3.zero;
                                localRot = rightHand ? Quaternion.identity : Quaternion.Euler(0.0f, 180.0f, 0.0f);
                                break;
                        }

                        Vector3 gPos; Quaternion gRot;
                        SG.Util.SG_Util.CalculateTargetLocation(wristTransform, localPos, localRot, out gPos, out gRot);

                        this.transform.parent = null;
                        this.transform.rotation = gRot;
                        this.transform.position = gPos;
                    }
                    this.transform.parent = wristTransform;
                }
            }
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Tapping Logic


        /// <summary> Fires when the user taps on the face of the Wrist Menu. </summary>
        /// <param name="hand"></param>
        public void MenuTapped(SG_TrackedHand hand)
        {
            if (tapLocked || !MenuFunctional || !this.isActiveAndEnabled) //we're not allowed to do anything
            {
                return;
            }

            HidingAllowed = true; //If we weren't allowed to hide before, we are now, as this is a clear acknowledgement.
            if (tapCoolDown > 0) //does not make sense to start a cooldown timer if no cooldown is desired 
            {
                StartCoroutine(StartCooldown(this.tapCoolDown));
            }
            if (this.tappedWaveform != null)
            {
                hand.SendCmd(this.tappedWaveform);
            }
            this.OnMenuTapped.Invoke();
        }

        /// <summary> Locks the tapCooldown for a specified amount of time. </summary>
        /// <returns></returns>
        protected IEnumerator StartCooldown(float coolDown)
        {
            this.tapLocked = true;
            yield return new WaitForSeconds(coolDown);
            this.tapLocked = false;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Visibility Logic

        /// <summary> Returns the transparancy value of the menu, as determined by several factors. </summary>
        /// <returns></returns>
        public float CalculateTransparency()
        {
            if (this.linkedHand != null)
            {
                if (!this.linkedHand.IsConnected())
                {
                    // Debug.Log(this.linkedHand + " is off, so we're hidden.");
                    return 0.0f; //back off, since there's no glove connected, and thus no TrackedHand.
                }
                else if (linkedHand.grabScript != null && linkedHand.grabScript.IsGrabbing)
                {
                    // Debug.Log(this.linkedHand + " is grabbign an object, so I'm disabling the menu!");
                    return 0.0f;
                }
            }

            if (this.menuVisibility == HideMode.AlwaysVisible || !HidingAllowed) //the menu cannot hide yet because the user must first acknowledge its presence this session...?
            {
               // Debug.Log("Vis = " + this.menuVisibility.ToString() + ", HideAllowed = " + HidingAllowed);
                return 1.0f;
            }

            // From here on out, we want to hide the UI based on two factors: The other hand is nearby, and the angle is correct.
            if (this.proximityZone != null && this.proximityZone.FullyDetctedCount() > 0) //we have a proximity zone and it's detecting the other hand!
            {
                if (!HidingAllowed && SG_XR_Devices.HeadsetOnHead())
                {
                    HidingAllowed = true; //from this moment on, the user has acknowledged the existence of our device.
                }
               // Debug.Log("Another hand in zone. So visible!");
                return 1.0f; //should be visible!
            }
            //the hand is no longer there, so calculate the current visibilty based on the angle.
            float res = SG.Util.SG_Util.Map(AngleWithHMD, this.functionalAngle, this.hiddenWhenAngleOver, 1.0f, 0.0f, true); //when at functionalAngle, the watch model is fully visible.
           // Debug.Log("Mapping Visibility based on angle " + this.AngleWithHMD.ToString("0.00") + " => " + res);
            return res;
        }

        /// <summary> Checks if the menu should be visible based on the current variables (updated with UpdateMenuLogic()) </summary>
        public void CheckMenuVisibility()
        {
            float visibility = CalculateTransparency(); //The transparency of our menu.
            bool faceVisible = visibility >= 0.99f && this.AngleWithHMD <= hiddenWhenAngleOver;
            if (this.visibleWaveform != null && this.linkedHand != null) //Fire a waveform if this is our first time becoming visible
            {
                if (faceVisible && !visibleFired)
                {
                    visibleFired = true; //fire the visibility waveform
                    this.linkedHand.SendCmd(this.visibleWaveform);
                }
                else if (visibleFired && visibility <= cooldownVisibility) // Reset when we drop below cooldownVisibility .
                {
                    visibleFired = false;
                }
            }
            this.FaceItemsVisible = faceVisible; //when roughly 1.0f.
            this.UIModelAlpha = visibility;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        protected virtual void OnEnable()
        {
            if (this.tapZone != null) { this.tapZone.HandDetected.AddListener(MenuTapped); }
            tapLocked = false;
        }

        protected virtual void OnDisable()
        {
            if (this.tapZone != null) { this.tapZone.HandDetected.RemoveListener(MenuTapped); }
        }

        protected virtual void Start()
        {
            SetupMenu();
        }

        protected virtual void Update()
        {
            // UpdateLogic();
            UpdateMenuLogic(Time.deltaTime);
            CheckMenuVisibility();

            //Debug
            if (debug)
            {
                DebugText
                    = "Angle: " + this.lastAngle + "°"
                    //+ "\nValidAngle: " + validAngle.ToString()
                    //+ "\nGazeAt: " + this.isGazing.ToString()
                    + "\nValidGaze: " + this.validGazing.ToString()
                    + "\nHideAllowed: " + this.HidingAllowed.ToString()
                    //+ "\nTapHover: " + this.hoveringOverZone.ToString()
                    + "\nFunctional: " + this.MenuFunctional.ToString()
                   ;
            }
        }




#if UNITY_EDITOR
        /// <summary> Called when the Reset function is called, and when the component is added for the first time... </summary>
        protected virtual void Reset()
        {
            //Check for camera transform
            if (hmdTransform == null)
            {
                //cannot use camera.main as it does not exist in the editor.
                Camera cam = GameObject.FindObjectOfType<Camera>();
                hmdTransform = cam.transform;
            }
            if (this.fader == null) //material fader
            {
                this.fader = this.GetComponent<SG.Util.SG_MaterialFader>();
                if (this.fader == null)
                {
                    this.fader = this.GetComponentInChildren<SG.Util.SG_MaterialFader>();
                }
            }
            if (this.trackingMethod == TrackingMethod.FindTrackedHand)
            {
                this.linkedHand = SG.Util.SG_Util.FindHandInScene(this.linkTo);
            }
        }

        /// <summary> Called when the script is loaded OR when any of the parameters are changed through the inspector. </summary>
        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
            {
                this.UpdateMenuParent();
            }
        }




#if CUSTOM_HANDUI_INSPECTOR

        // Declare type of Custom Editor
        [UnityEditor.CustomEditor(typeof(SG_XR_HandUI))]
        [UnityEditor.CanEditMultipleObjects]
        public class SG_XR_HandUIEditor : UnityEditor.Editor
        {
            private UnityEditor.SerializedProperty m_trackingMethod;
            private UnityEditor.SerializedProperty m_linkTo;
            private UnityEditor.SerializedProperty m_gizmoLocation;
            private UnityEditor.SerializedProperty m_linkedHand;

            private UnityEditor.SerializedProperty m_hmdTransform;
            private UnityEditor.SerializedProperty m_hmdForward;
            private UnityEditor.SerializedProperty m_functionalAngle;
            private UnityEditor.SerializedProperty m_faceItems;

            private UnityEditor.SerializedProperty m_tapZone;
            private UnityEditor.SerializedProperty m_tapCoolDown;
            private UnityEditor.SerializedProperty m_tappedWaveform;
            private UnityEditor.SerializedProperty m_OnMenuTapped;

            private UnityEditor.SerializedProperty m_watchVisibility;
            private UnityEditor.SerializedProperty m_startsHidden;
            private UnityEditor.SerializedProperty m_facePlaneNormal;
            private UnityEditor.SerializedProperty m_proximityDetection;
            private UnityEditor.SerializedProperty m_hiddenWhenAngleOver;
            private UnityEditor.SerializedProperty m_fader;
            private UnityEditor.SerializedProperty m_visibleWaveform;
            private UnityEditor.SerializedProperty m_validProximityAngle;
            private UnityEditor.SerializedProperty m_gazeCollider;

            private UnityEditor.SerializedProperty m_debug;
            private UnityEditor.SerializedProperty m_debugTxt;

            void OnEnable()
            {
                m_trackingMethod = serializedObject.FindProperty("trackingMethod");
                m_linkTo = serializedObject.FindProperty("linkTo");
                m_gizmoLocation = serializedObject.FindProperty("menuLocation");
                m_linkedHand = serializedObject.FindProperty("linkedHand");

                m_hmdTransform = serializedObject.FindProperty("hmdTransform");
                m_hmdForward = serializedObject.FindProperty("hmdForward");
                m_functionalAngle = serializedObject.FindProperty("functionalAngle");
                m_faceItems = serializedObject.FindProperty("faceItems");

                m_tapZone = serializedObject.FindProperty("tapZone");
                m_tapCoolDown = serializedObject.FindProperty("tapCoolDown");
                m_tappedWaveform = serializedObject.FindProperty("tappedWaveform");
                m_OnMenuTapped = serializedObject.FindProperty("OnMenuTapped");

                m_watchVisibility = serializedObject.FindProperty("menuVisibility");
                m_startsHidden = serializedObject.FindProperty("startsHidden");
                m_facePlaneNormal = serializedObject.FindProperty("facePlaneNormal");
                m_hiddenWhenAngleOver = serializedObject.FindProperty("hiddenWhenAngleOver");
                
                m_fader = serializedObject.FindProperty("fader");
                m_visibleWaveform = serializedObject.FindProperty("visibleWaveform");
                m_proximityDetection = serializedObject.FindProperty("proximityZone");
                m_validProximityAngle = serializedObject.FindProperty("validProximityAngle");
                m_gazeCollider = serializedObject.FindProperty("gazeCollider");

                m_debug = serializedObject.FindProperty("debug");
                m_debugTxt = serializedObject.FindProperty("debugTxt");
            }

            // OnInspector GUI
            public override void OnInspectorGUI()
            {
                // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
                serializedObject.Update();

                UnityEditor.EditorGUILayout.PropertyField(m_trackingMethod, new GUIContent("TrackingMethod", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_linkTo, new GUIContent("Link To", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_gizmoLocation, new GUIContent("Menu Location", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_linkedHand, new GUIContent("Linked Hand", "")); //TODO: Tooltips.

                UnityEditor.EditorGUILayout.PropertyField(m_hmdTransform, new GUIContent("HMD Transform", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_hmdForward, new GUIContent("HMD Forward", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_facePlaneNormal, new GUIContent("Menu Face Normal", "")); //TODO: Tooltips.

                UnityEditor.EditorGUILayout.PropertyField(m_functionalAngle, new GUIContent("Functional Angle", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_faceItems, new GUIContent("Face Items", "")); //TODO: Tooltips.

                UnityEditor.EditorGUILayout.PropertyField(m_tapZone, new GUIContent("Tap Zone", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_tapCoolDown, new GUIContent("Tap Cooldown", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_tappedWaveform, new GUIContent("Tapped Waveform", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_OnMenuTapped, new GUIContent("OnMenuTapped", "")); //TODO: Tooltips.

                UnityEditor.EditorGUILayout.PropertyField(m_watchVisibility, new GUIContent("Menu Visibility", "")); //TODO: Tooltips.

                SG_XR_HandUI.HideMode currHideMethod = (SG_XR_HandUI.HideMode)m_watchVisibility.intValue;
                if (currHideMethod == HideMode.AutoHide)
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_startsHidden, new GUIContent("Starts Hidden", "")); //TODO: Tooltips.
                    UnityEditor.EditorGUILayout.PropertyField(m_hiddenWhenAngleOver, new GUIContent("Hidden when angle over", "")); //TODO: Tooltips.
                    UnityEditor.EditorGUILayout.PropertyField(m_fader, new GUIContent("Fader", "")); //TODO: Tooltips.
                    UnityEditor.EditorGUILayout.PropertyField(m_visibleWaveform, new GUIContent("VisibleWaveform", "")); //TODO: Tooltips.
                    
                    
                    UnityEditor.EditorGUILayout.PropertyField(m_proximityDetection, new GUIContent("Proximity Zone", "")); //TODO: Tooltips.
                    UnityEditor.EditorGUILayout.PropertyField(m_validProximityAngle, new GUIContent("Valid Proximity Angle", "")); //TODO: Tooltips.
                    UnityEditor.EditorGUILayout.PropertyField(m_gazeCollider, new GUIContent("Gaze Collider", "")); //TODO: Tooltips.
                }
                UnityEditor.EditorGUILayout.PropertyField(m_debug, new GUIContent("Debug", "")); //TODO: Tooltips.
                UnityEditor.EditorGUILayout.PropertyField(m_debugTxt, new GUIContent("DebugText", "")); //TODO: Tooltips.

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif


#endif

    }
}



