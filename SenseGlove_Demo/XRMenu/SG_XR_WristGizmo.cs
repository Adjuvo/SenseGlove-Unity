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
	public class SG_XR_WristGizmo : MonoBehaviour
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
        public enum GizmoLocation
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
            AllwaysVisible,
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
        [SerializeField] protected GizmoLocation gizmoLocation = GizmoLocation.HandPalm;

        /// <summary> The SG_Trackedhand from which we grab tracking information. </summary>
        public SG_TrackedHand linkedHand;

        /// <summary> Camera head transform, used to determine your head angle compared to the watch angle. When not assigned, things like "auto-hide" and "functional angle" are ignored. </summary>
        public Transform hmdTransform;
        /// <summary> Which axis of the camera is considered "forward" a.k.a. moving along this axis of the hmdTransform gets you further away. </summary>
        [SerializeField] protected SG.Util.MoveAxis hmdForward = SG.Util.MoveAxis.Z;

        // <summary> Position offset for Custom location </summary>
        protected Vector3 gizmoPos_Custom = Vector3.zero;
        // <summary> Rotation offset for Custom location </summary>
        protected Quaternion gizmoRot_Custom = Quaternion.identity;

        /// <summary> Position offset for HandPalm location </summary>
        protected static readonly Vector3 gizmoPos_HandPalm_R = new Vector3(-0.030f, -0.0287f, 0.0f);
        /// <summary> Rotation offset for HandPalm location </summary>
        protected static readonly Quaternion gizmoRot_HandPalm_R = Quaternion.Euler(180.0f, 0.0f, 0.0f);

        /// <summary> Position offset for BackOfHand location </summary>
        protected static readonly Vector3 gizmoPos_BackOfHand_R = new Vector3(-0.06f, 0.033f, 0);
        /// <summary> Rotation offset for BackOfHand location </summary>
        protected static readonly Quaternion gizmoRot_BackOfHand_R = Quaternion.identity;



        /// <summary> The last angle between the wrist menu and the camera. </summary>
        protected float lastAngle = 180;


        /// <summary> Another hand entering this zone will activate tap logic. </summary>
        [Header("Tapping Logic")] 
        public SG_HandDetector tapZone;
        
        /// <summary> The menu can only be tapped when the gaze angle is smaller than this angle. Will be ignored if hmdTransform is not assigned. </summary>
        [Range(0, 180)] public float functionalAngle = 30;
        
        /// <summary> Cooldown in seconds between tapping and untapping again </summary>
        public float tapCoolDown = 0.5f;

        /// <summary> While tapLocked is true, no event can be played </summary>
        protected bool tapLocked = false;

        /// <summary> The waveform to play when a hand taps this Hand UI. </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public SG_Waveform tappedWaveform;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary> This event fires when this menu is tapped by another SG_TrackedHand. </summary>
        public UnityEngine.Events.UnityEvent OnMenuTapped = new UnityEngine.Events.UnityEvent();


        /// <summary> Whether or not to hide this menu at all. Or if it's always visible. In that case, the other variables need not be assigned. </summary>
        [Header("Visibility Parameters")]
        public HideMode watchVisibility = HideMode.AutoHide;

        //if we're not hiding the UI, all of these are not needed.

        /// <summary> The normal of the screen or face you're checking against the camera. We use this to determine your angle. </summary>
        public SG.Util.MoveAxis facePlaneNormal = SG.Util.MoveAxis.Y;

        /// <summary> Used to smoothly fade the visibility of the Hand UI based on the hand angle </summary>
        public SG.Util.SG_MaterialFader fader;

        /// <summary> Object(s) that are hidden unless the menu is at the functionalAngle OR when a hand is in proximity. </summary>
        public GameObject[] faceItems = new GameObject[0];

        /// <summary> Waveform to play when the menu becomes visible / interactable with. </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public SG_Waveform visibleWaveform;
#pragma warning restore CS0618 // Type or member is obsolete

        //if the tap hover is not assigned, hide the UI

        /// <summary> Optional Utility zone to always show the UI when the other hand is close. </summary>
        [SerializeField] protected SG_HandDetector tapHoverZone;
        /// <summary> Whether or not the hand is currently hovering over the UI via the tapHoverZone. Set with events. </summary>
        protected bool hoveringOverZone = false;

        //TODO: Hide this object when the hand is turned off.
        protected bool headsetOnFirst = false;
        /// <summary> Whether or not we're gazing at all. Could still be on the other side of the arm, though... </summary>
        protected bool isGazing = false;
        /// <summary> Whether or not we were gazing at a valid angle </summary>
        protected bool validGazing = false;

        /// <summary> When close enough and at this angle, we should show the menu regardless </summary>
        [Range(0, 180)] public float validProximityAngle = 80;


        //if the gaze is not assigned, hide as well...

        /// <summary> Collider around the wrist to determine whether or not we're gaing at the wrist menu. </summary>
        public Collider gazeCollider;
        /// <summary> How far away the gaze  </summary>
        public float gazeDistance = 2; //how close you need to for the camera to touch the WristMenu
        /// <summary> Determines if a first gaze has been made for each hand throughout the simulation. </summary>
        protected static bool firstGaze_L = false, firstGaze_R = false;

        public bool headsetMustBeWorn = true;

        /// <summary> From this angle and above, the menu is fully hidden. (unless we're close enough with our other hand) </summary>
        [Range(0, 180)] public float hiddenWhenAngleOver = 50;

        /// <summary> If visible Re-Hide the hidden menu items at this offset </summary>
        protected float rehideBufferAngle = 5.0f;

        public float firstGazeTime = 0.1f;
        protected float timer_gazing = 0;



        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors


        /// <summary> Retrieve the hand linked to this script. </summary>
        public SG_TrackedHand LinkedHand
        {
            get { return this.linkedHand; }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Hand UI Function

        /// <summary> Check for the existince of useful components, if they are not assigned. </summary>
        public void CheckForComponents()
        {
            if (this.fader == null) //material fader
            {
                this.fader = this.GetComponent<SG.Util.SG_MaterialFader>();
                if (this.fader == null)
                {
                    this.fader = this.GetComponentInChildren<SG.Util.SG_MaterialFader>();
                }
            }
        }

        /// <summary> Directly set the menu visibility to either fully on, or fully off. </summary>
        /// <param name="active"></param>
        public void SetVisibility(bool active)
        {
            if (this.fader != null)
            {

            }
        }



        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // OLD CODE





        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Wrist Location Functions

        public void UpdateTrackingLocation()
        {
            if (this.linkedHand != null && this.gizmoLocation != GizmoLocation.Custom)
            {
                bool rightHand = linkedHand.TracksRightHand();
                Transform wristTransform = linkedHand.GetTransform(SG_TrackedHand.TrackingLevel.RenderPose, HandJoint.Wrist);
                if (wristTransform != null)
                {
                    Vector3 localPos; Quaternion localRot;
                    switch (this.gizmoLocation)
                    {
                        case GizmoLocation.HandPalm:
                            localPos = gizmoPos_HandPalm_R;
                            localRot = rightHand ? gizmoRot_HandPalm_R : Quaternion.Euler(0.0f, 180.0f, 0.0f) * gizmoRot_HandPalm_R;
                            break;
                        case GizmoLocation.BackOfHand:
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
                    this.transform.parent = wristTransform;
                }
            }
        }

        




        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Functions



        /// <summary> Returns the angle between the hmdTransform and the facePlaneNormal. Is 0 when directly faced, and up to 180 degrees when  </summary>
        /// <returns></returns>
        public virtual float CalculateFaceAngle()
        {
            if (this.hmdTransform != null)
            {
                return FaceAngle(this.transform, this.facePlaneNormal, this.hmdTransform, this.hmdForward);
            }
            return 0.0f;
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



        [Header("Debugging")]
        public bool debug = false;
        public TextMesh debugTxt;
      

        public float GazeAngle
        {
            get { return this.lastAngle; }
        }

        public string DebugText
        {
            get { return this.debugTxt != null ? debugTxt.text : ""; }
            set { if (this.debugTxt != null) { this.debugTxt.text = value; } }
        }

        
        public bool FaceItemsVisible
        {
            get { return this.faceItems.Length > 0 ? this.faceItems[0].activeSelf : false; }
            set
            {
                for (int i=0; i<this.faceItems.Length; i++)
                {
                    faceItems[i].SetActive(value);
                }
            }
        }

        public bool FirstGazedAt
        {
            get
            {
                return this.linkedHand.TracksRightHand() ? firstGaze_R : firstGaze_L;
            }
            set
            {
                if (this.linkedHand.TracksRightHand()) { firstGaze_R = value; }
                else { firstGaze_L = value; }
            }
        }

        /// <summary> Determines whether or not the wirst menu is currently visible / tappable </summary>
        public bool MenuFunctional
        {
            get; private set;
        }


        /// <summary> Fires when the user taps on the wrist menu. </summary>
        /// <param name="hand"></param>
        public void MenuTapped(SG_TrackedHand hand)
        {
            if (tapLocked || !MenuFunctional || !this.isActiveAndEnabled) //we're not allowed to do anything
            {
                return; 
            }

            FirstGazedAt = true; //can start hiding from then on.

          //  Debug.Log("The (Dev) Menu was tapped and valid!");
            if (tapCoolDown > 0) //does not make sense to start a cooldown timer if no cooldown is desired 
            {
                StartCoroutine(StartCooldown());
            }
            if (this.tappedWaveform != null)
            {
                //Debug.LogError("TODO: Implement Timed Waveforms!");
                //hand.SendCmd(this.tappedWaveform);
            }
            this.OnMenuTapped.Invoke();
        }


        protected IEnumerator StartCooldown()
        {
            this.tapLocked = true;
            yield return new WaitForSeconds(tapCoolDown);
            this.tapLocked = false;
        }



        public void UpdateLogic()
        {
            // Step 0 - Check if we can start the main logic, or if the HMD needs to be put on first...
            if (!headsetOnFirst) //we haven' yet had the headset on.
            {
                this.headsetOnFirst = this.headsetMustBeWorn ? SG_XR_Devices.HeadsetOnHead() : true;
                if (headsetOnFirst) //the first time it' turned on
                {
                    // Start with everything is visible
                    FaceItemsVisible = true;
                    if (this.fader != null) { fader.SetFadeLevel(1.0f); }
                }
            }
            if (!headsetOnFirst) //Still have yet to put on the HMD
            {
                DebugText = "Put on Headset...";
                return;
            }
            

            // Step 1: Update Variables that determine whether or not the Menu is Functional.

            this.lastAngle = this.CalculateFaceAngle(); //base calculates it properly
            bool validAngle = this.lastAngle <= this.functionalAngle;
            this.hoveringOverZone = this.tapHoverZone != null && this.tapHoverZone.HandsInZoneCount() > 0;

            isGazing = false;
            //determine whether or not the user is gazing at the menu (and it's reasonably facing us)
            if (this.gazeCollider != null && hmdTransform != null)
            {
                Vector3 hmdPos = this.hmdTransform.position;
                Vector3 direction = this.hmdTransform.rotation * SG.Util.SG_Util.GetAxis(this.hmdForward);

                Ray ray = new Ray(this.hmdTransform.position, direction);
                RaycastHit hit;
                isGazing = this.gazeCollider.Raycast(ray, out hit, this.gazeDistance); //this raycasts specifically for this collider.
                if (debug)
                {
                    Vector3 dLine = direction * gazeDistance;
                    Debug.DrawLine(hmdPos, hmdPos + dLine, Color.red);
                }
            }
            this.validGazing = this.isGazing && validAngle;

            //Evaluate a fist gaze for other logic...
            if (!this.FirstGazedAt)
            {
                if (this.validGazing)
                {
                    timer_gazing += Time.deltaTime;
                    if (timer_gazing >= this.firstGazeTime)
                    {
                        Debug.Log((this.linkedHand.TracksRightHand() ? "R" : "L" ) + ": First proper Gaze. Unlocks the Auto Hide when releavnt.");
                        this.FirstGazedAt = true;
                    }
                }
                else { this.timer_gazing = 0; }
            }

            // Step 3: Evaluate whether or not the menu is (still) functional
            if (MenuFunctional) //Menu is turned on
            {
                bool handNearby = hoveringOverZone && lastAngle <= this.validProximityAngle + rehideBufferAngle; //a hand is nearby, and it' at a somewhat acceptable angle
                bool angleStillOK = this.lastAngle <= this.functionalAngle + rehideBufferAngle;
                MenuFunctional = handNearby || angleStillOK;
                //Logic for re-hiding.
                if (!MenuFunctional) //no longer funcitonal
                {
                    //Debug.Log("Hide Wrist Icons.");
                    this.FaceItemsVisible = false;
                }
            }
            else
            {
                MenuFunctional = validAngle || (hoveringOverZone && lastAngle <= this.validProximityAngle); //we'e hovering over the button
                if (MenuFunctional)
                {
                    this.fader.SetFadeLevel(1.0f); //force visibility
                    this.FaceItemsVisible = true;
                    //Debug.LogError("TODO: Implement Timed Waveforms!");
                  
                    //his.linkedHand.SendCmd(this.visibleWaveform);
                    //Debug.Log("Show Wrist Icons");
                }
            }

            //Step 4 (optional) - hide the menu when no longer functional
            if (this.watchVisibility == HideMode.AutoHide && this.FirstGazedAt && this.fader != null)
            {
                float alpha = this.MenuFunctional ? 1.0f : Mathf.Clamp01(SG.Util.SG_Util.Map(this.lastAngle, this.hiddenWhenAngleOver, this.functionalAngle, 0, 1));
                this.fader.SetFadeLevel(alpha);
            }


            //Debug
            if (debug)
            {
                DebugText
                    = "Angle: " + this.lastAngle + "°"
                    + "\nValidAngle: " +validAngle.ToString()
                    + "\nGazeAt: " + this.isGazing.ToString()
                    + "\nValidGaze: " + this.validGazing.ToString()
                    + "\nFirstGaze: " + this.FirstGazedAt.ToString()
                    + "\nTapHover: " + this.hoveringOverZone.ToString()
                    + "\nFunctional: " + this.MenuFunctional.ToString()
                   ;
            }
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

        /// <summary> Called once when this script is activated. </summary>
        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            this.UpdateTrackingLocation();

            if (hmdTransform == null && Camera.main != null)
            {
                hmdTransform = Camera.main.transform;
            }

            if (this.gazeCollider != null) { this.gazeCollider.isTrigger = true; }
            DebugText = "";


            //Setup colliders
            SG_TrackedHand otherHand;
            if (this.linkedHand.GetOtherHand(out otherHand))
            {
                this.tapZone.detectableHands = new List<SG_TrackedHand>() { otherHand };
                this.tapHoverZone.detectableHands = new List<SG_TrackedHand>() { otherHand };

                //TODO; ignore wrist collision to avoid accidental activations
                if (otherHand.handPhysics != null)
                {
                    Collider[] otherWrist = otherHand.handPhysics.wristColliders;
                    Collider[] tapColliders = this.tapZone.GetColliders();
                    Collider[] hoverColliders = this.tapHoverZone.GetColliders();
                    for (int i=0; i< otherWrist.Length; i++)
                    {
                        for (int j=0; j<tapColliders.Length; j++)
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
            this.tapZone.detectionTime = 0; //
            this.tapHoverZone.detectionTime = 0; //

            //Determine whether or not to turn on / off the menu at the start...
            headsetOnFirst = headsetMustBeWorn ? SG_XR_Devices.HeadsetOnHead() : true;
            MenuFunctional = true;
            FaceItemsVisible = true;
            if (this.fader != null) {  fader.SetFadeLevel(1.0f); }  
        }

        protected virtual void Update()
        {
            UpdateLogic();
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
        }

        /// <summary> Called when the script is loaded OR when any of the parameters are changed through the inspector. </summary>
        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
            {
                this.UpdateTrackingLocation();
            }
        }
#endif

    }
}