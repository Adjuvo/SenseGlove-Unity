using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.XR
{

    public class SG_XR_Menu : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Class-Specific Enumerators

        /// <summary> Which parts of anumation is handled by this script. </summary>
        public enum UpdateBehaviour
        {
            /// <summary> This script will not update the menu's location at all. It will still affect scale and animation. /summary>
            None,
            /// <summary> This script controls only the menu's position. Its rotation stays the same. </summary>
            Position,
            /// <summary> This script controls only the meny's rotation. Its position stays the same. </summary>
            Rotation,
            /// <summary> This script controls both position & rotation of the menu. </summary>
            PositionAndRotation
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Which parts of the location this script is allowed to control. </summary>
        public UpdateBehaviour controls = UpdateBehaviour.PositionAndRotation;

        /// <summary> The menu will float above any menu by this amount. </summary>
        public float floatingHeight = 0.15f;

        /// <summary> How fast the menu follows the hands, if position control is enabled. </summary>
        public float menuMoveSpeed = 5.0f;
        /// <summary> How quickly the menu turns to face the user, if rotation control is enabled. </summary>
        public float menuRotateSpeed = 180.0f;


        /// <summary> Pressing this button will recenter the player, if a SG_XR_RoomSetup is in the scene. Otherwise, this button is disabled. </summary>
        [Header("Button Elements")]
        public SG_WorldCanvasButton recenterButton;
        /// <summary> This button resets the simulation when activated. </summary>
        public SG_WorldCanvasButton resetButton;
        /// <summary> This button exits the simulation when activated. </summary>
        public SG_WorldCanvasButton exitButton;

        /// <summary> All buttons in this script collected in one location. Useful for applying logic to all of them. </summary>
        protected SG_WorldCanvasButton[] allButtons = null;

        /// <summary> If true, this Menu starts off hidden. Otherwise, it remains visible until you toggle it again. </summary>
        [Header("Show / Hide Logic")]
        public bool startsHidden = true;
        /// <summary> How  long it will take to fully expand or collapse the menu, in seconds. </summary>
        public float animationTime = 0.25f;
        /// <summary> When connected to a SG_XR_HandUI, the menu will auto-hide when this angle between watch and head is reached. (If set to 180, the menu won't be hidden at all) </summary>
        [Range(0, 180)] public float hideMenuAngle = 90.0f;

        /// <summary> Calculated based on animationTime. </summary>
        protected float scaleSpeed = 1.0f;
        /// <summary> Whether the menu is currently (trying to be) open (true) or closed (false) </summary>
        protected bool menuOpen = true;
        /// <summary> Whether the menu is currently animating. </summary>
        protected bool animating = false;
        /// <summary> The menu's scale on startup - where it will go to when expanding... </summary>
        protected Vector3 baseScale = new Vector3(1.0f, 1.0f, 1.0f);


        /// <summary> The visual parts of the menu, used to ensure it is enabled at the start. </summary>
        [Header("Functional Elements - Auto Detected")]
        public GameObject visualElements;

        /// <summary> The transform of the HMD, used to falculate a facing angle when this script controls rotation. </summary>
        public Transform headCam;
        /// <summary> SenseGlove-Specific script to recenter the user. When not found or assigned, the button will dissapear. </summary>
        public SG_XR_RoomSetup roomSetup;

        /// <summary> Hand UI Connected to the left hand. Auto-assigned. </summary>
        public SG_XR_HandUI leftHandGizmo;
        /// <summary> Hand UI for the right hand. </summary>
        public SG_XR_HandUI rightHandGizmo;

        /// <summary> Which HandUI we should be following at the moment. </summary>
        protected SG_XR_HandUI currentWristActivator;
        /// <summary> The Transform of the menu, used for quick access and/or for changing it later... </summary>
        protected Transform menuTransform = null;

        /// <summary> UnityActions defined for delegate functions, so I can remove the appropriate listener(s). </summary>
        UnityEngine.Events.UnityAction leftButtonTap = null, rightButtonTap = null;


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Returns thrue if the menu is either open or in the process of opening </summary>
        public bool IsOpenOrOpening
        {
            get { return menuOpen; }
        }

        /// <summary> Returns true if this menu is fully open and no longer animating. Used for button activation. </summary>
        public bool FullyActive
        {
            get { return this.menuOpen && !this.animating; }
        }

        /// <summary> Returns true if this menu is fully closed and no longer animating. </summary>
        public bool FullyClosed
        {
            get { return !this.menuOpen && !this.animating; }
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Button Functions


        /// <summary> Tell all buttons to ignore a specific hand. Usually the one connected to the linked HandUI. </summary>
        /// <param name="hand"></param>
        public void SetIgnoreHand(SG_TrackedHand hand)
        {
            for (int i=0; i<this.allButtons.Length; i++)
            {
                allButtons[i].SetIgnoreHand(hand);
            }
        }


        /// <summary> Collect references to all of the (currently) linked buttons. </summary>
        protected void CollectButtons()
        {
            if (this.allButtons == null)
            {
                List<SG_WorldCanvasButton> btns = new List<SG_WorldCanvasButton>();
                if (this.exitButton != null) { btns.Add(exitButton); }
                if (this.resetButton != null) { btns.Add(resetButton); }
                if (this.recenterButton != null) { btns.Add(recenterButton); }
                this.allButtons = btns.ToArray();
            }
        }


        /// <summary> Event that fires when a button is touched. Check multiTouchBehaviour </summary>
        /// <param name="btn"></param>
        protected void ButtonTouched(SG_TrackedHand hand, SG_WorldCanvasButton btn)
        {
            // TODO FOR LATER: Allow only one of these buttons to activate at a time!
        }

        /// <summary> Event that fires when a button is no longer touched. Check multiTouchBehaviour </summary>
        /// <param name="btn"></param>
        protected void ButtonUnTouched(SG_TrackedHand hand, SG_WorldCanvasButton btn)
        {
            // TODO FOR LATER: Allow only one of these buttons to activate at a time!
        }


        /// <summary> Fires when the Exit Button is pressed. </summary>
        public void ExitButtonPressed(SG_TrackedHand hand, SG_WorldCanvasButton btn)
        {
            SG.Util.SG_SceneControl.QuitApplication();
        }

        /// <summary> Fires when the Recenter Button is pressed. </summary>
        public void RecenterButtonPressed(SG_TrackedHand hand, SG_WorldCanvasButton btn)
        {
            if (this.roomSetup != null)
            {
                this.roomSetup.Recenter();
            }
        }

        /// <summary> Fires when the Reset Button is pressed. </summary>
        public void ResetButtonPressed(SG_TrackedHand hand, SG_WorldCanvasButton btn)
        {
            SG.Util.SG_SceneControl.ToFirstScene();
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Tracking Functions

        /// <summary> Calculates where this menu should be, based on whether or not it's linked to a HandUI. </summary>
        /// <returns></returns>
        public Vector3 CalculateTargetPosition()
        {
            if (this.currentWristActivator != null)
            {
                return this.currentWristActivator.transform.position + (Vector3.up * this.floatingHeight); //float a bit above the menu...
            }
            return this.transform.position;
        }

        /// <summary> Calculated the way this menu should rotate, based on its current location. Note, this assumes Z is the normal for your menu! </summary>
        /// <param name="currentLocation"></param>
        /// <returns></returns>
        public Quaternion CalculateTargetRotation(Vector3 currentLocation)
        {
            if (headCam != null)
            {
                Vector3 menuUp = Vector3.up;
                Vector3 camPos = this.headCam.position;
                Vector3 HMDClosest = SG.Util.SG_Util.GetClosestPointOnLine(currentLocation, camPos, camPos + menuUp);
                //calculate rotation
                //Y axis is up, Forward should face toward the XR Rig
                Vector3 menufwd = (HMDClosest - currentLocation).normalized;
                return Quaternion.LookRotation(menufwd, menuUp);
            }
            return this.transform.rotation;
        }


        /// <summary> Updates the menu's scale, position and rotation based on animation state and contol parameters. Needs dT becasue of speed. </summary>
        /// <param name="dT"></param>
        public void UpdateMenuTransform(float dT)
        {
            //Determine Scale based on animation...
            if (this.animating)
            {
                Vector3 target = this.menuOpen ? this.baseScale : Vector3.zero;

                // Debug.Log("Moving from " + this.transform.localScale.ToString() + " to " + target.);

                this.transform.localScale = Vector3.MoveTowards(this.transform.localScale, target, scaleSpeed * dT);
                if ((this.transform.localScale - target).magnitude < 0.01f) //close enough to target
                {
                    this.transform.localScale = target; //actually set it!
                    this.animating = false;
                }
            }

            if (this.controls == UpdateBehaviour.None)
            {
                return;
            }
            Vector3 targetPosition = CalculateTargetPosition(); //used for rotation as well, which is why I'm calculating it first...
            //Determine Rotation
            if (this.controls == UpdateBehaviour.PositionAndRotation || this.controls == UpdateBehaviour.Rotation && this.headCam != null)
            {
                Quaternion toHead = this.CalculateTargetRotation(targetPosition);
                Quaternion nextRot = Quaternion.RotateTowards(this.transform.rotation, toHead, this.menuRotateSpeed * dT);
                this.transform.rotation = nextRot;
            }
            //Determine Position using velocity
            if (this.controls == UpdateBehaviour.PositionAndRotation || this.controls == UpdateBehaviour.Position)
            {
                Vector3 nextPos = Vector3.MoveTowards(this.transform.position, targetPosition, this.menuMoveSpeed * dT);
                this.transform.position = nextPos;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Animation Functions


        /// <summary> Force the menu to open wihthout updating any links to a HandUI. </summary>
        /// <param name="skipAnimation"> If true, the menu does not animate, but is instantly open </param>
        public void OpenMenu(bool skipAnimation = false)
        {
            if (!menuOpen || skipAnimation)
            {
                menuOpen = true;
                animating = !skipAnimation;
                if (skipAnimation)
                {
                    this.transform.localScale = this.baseScale;
                }
            }
        }

        /// <summary> Force the menu to close wihthout updating any links to a HandUI. </summary>
        /// <param name="skipAnimation"> If true, the menu does not animate, but is instantly closed </param>
        public void CloseMenu(bool skipAnimation = false)
        {
            if (menuOpen || skipAnimation)
            {
                menuOpen = false;
                animating = !skipAnimation;
                if (skipAnimation)
                {
                    this.transform.localScale = Vector3.zero;
                    //Debug.Log("Skipping animation. Scale = 0");
                }
            }
        }

        /// <summary> Open the menu through a link with a HandUI, which also ensured its position is correct. </summary>
        /// <param name="tappedObject"></param>
        protected void OpenMenuWith(SG_XR_HandUI tappedObject)
        {
            this.currentWristActivator = tappedObject;
          //  Debug.Log("Linked to " + currentWristActivator.name);

            this.SetIgnoreHand(tappedObject.linkedHand);

            if (this.controls > UpdateBehaviour.None) //we at least want to control _something_
            {
                Vector3 TargetPos = CalculateTargetPosition(); //required for rotation, if any
                if (this.controls == UpdateBehaviour.PositionAndRotation || this.controls == UpdateBehaviour.Rotation)
                {
                    this.transform.rotation = CalculateTargetRotation(TargetPos);
                }
                if (this.controls == UpdateBehaviour.PositionAndRotation || this.controls == UpdateBehaviour.Position)
                {
                    this.transform.position = TargetPos;
                }
            }
            this.OpenMenu();
        }

        /// <summary> Close the menu using a specific HandUI. </summary>
        /// <param name="tappedObject"></param>
        protected void CloseMenuWith(SG_XR_HandUI tappedObject)
        {
        //    Debug.Log("Unlinked from " + tappedObject.name);
            this.currentWristActivator = null; //unlink.
            this.CloseMenu();
        }


        /// <summary> An event that is fired when you tap on one of the HandUI's. Switches or toggles the menu. </summary>
        /// <param name="tappedObject"></param>
        protected void HandUITapped(SG_XR_HandUI tappedObject)
        {
            if (tappedObject == null) { return; }

            if (this.currentWristActivator != null) //already linked ot a tappedObject...
            {
                if (this.currentWristActivator == tappedObject) //tapped the same object. Hide the menu if you aren't already?
                {
            //        Debug.Log("Tapped while linked. Close as normal...");
                    this.CloseMenuWith(tappedObject);
                }
                else //tapped a different menu!
                {
                    this.CloseMenu(true);
                    this.OpenMenuWith(tappedObject);
           //         Debug.Log("Switched hands! Force Close and open somewhere else...");
                }
            }
            else //we weren't tapping anything before.
            {
            //    Debug.Log("Tapped while unlinked. Open as normal...");
                OpenMenuWith(tappedObject);
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Setup Functions


        public void CheckForComponents()
        {
            if (this.leftHandGizmo == null || this.rightHandGizmo == null)
            {
                SG_XR_HandUI[] handUIComponents = GameObject.FindObjectsOfType<SG_XR_HandUI>();
                for (int i=0; i<handUIComponents.Length; i++)
                {
                    if (leftHandGizmo == null && !handUIComponents[i].LinkedToRightHand)
                    {
                        leftHandGizmo = handUIComponents[i];
                    }
                    if (rightHandGizmo == null && handUIComponents[i].LinkedToRightHand)
                    {
                        rightHandGizmo = handUIComponents[i];
                    }
                    if (leftHandGizmo != null && rightHandGizmo != null)
                    {
                        break; //we've finished assigning everything. let's jump out.
                    }
                }
            }

            if (headCam == null && Camera.main != null)
            {
                this.headCam = Camera.main.transform;
            }
            if (this.roomSetup == null)
            {
                this.roomSetup = GameObject.FindObjectOfType<SG_XR_RoomSetup>();
            }
            if (this.menuTransform == null)
            {
                this.menuTransform = this.transform;
            }
            if (visualElements == null)
            {
                this.visualElements = this.menuTransform.childCount > 0 ? this.menuTransform.GetChild(0).gameObject : null;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour Functions


        private void OnEnable()
        {
            CheckForComponents();
            //Subscribe the proper events
            if (leftHandGizmo != null)
            {
                leftButtonTap = () => { HandUITapped(this.leftHandGizmo); };
                leftHandGizmo.OnMenuTapped.AddListener(leftButtonTap);
            }
            if (rightHandGizmo != null)
            {
                rightButtonTap = () => { HandUITapped(this.rightHandGizmo); };
                rightHandGizmo.OnMenuTapped.AddListener(rightButtonTap);
            }

            //Subscribe to press events for the actual events
            if (this.exitButton != null) { this.exitButton.ButtonActivated.AddListener(ExitButtonPressed); }
            if (this.resetButton != null) { this.resetButton.ButtonActivated.AddListener(ResetButtonPressed); }
            if (this.recenterButton != null) { this.recenterButton.ButtonActivated.AddListener(RecenterButtonPressed); }

            //Subscribe to touch events for multiTouchLogic
            CollectButtons();
            for (int i = 0; i < this.allButtons.Length; i++)
            {
                SG_WorldCanvasButton b = allButtons[i];
                allButtons[i].ButtonTouched.AddListener(ButtonTouched);
                allButtons[i].ButtonUnTouched.AddListener(ButtonUnTouched);
            }
        }

        private void OnDisable()
        {
            if (this.leftHandGizmo != null && this.leftButtonTap != null) { this.leftHandGizmo.OnMenuTapped.RemoveListener(leftButtonTap); }
            if (this.rightHandGizmo != null && this.rightButtonTap != null) { this.rightHandGizmo.OnMenuTapped.RemoveListener(rightButtonTap); }

            if (this.exitButton != null) { this.exitButton.ButtonActivated.RemoveListener(ExitButtonPressed); }
            if (this.resetButton != null) { this.resetButton.ButtonActivated.RemoveListener(ResetButtonPressed); }
            if (this.recenterButton != null) { this.recenterButton.ButtonActivated.RemoveListener(RecenterButtonPressed); }

            for (int i = 0; i < this.allButtons.Length; i++)
            {
                allButtons[i].ButtonTouched.RemoveListener(ButtonTouched);
                allButtons[i].ButtonUnTouched.RemoveListener(ButtonUnTouched);
            }
        }


        private void Awake()
        {
            this.baseScale = this.transform.localScale;
            this.scaleSpeed = this.baseScale.magnitude / animationTime;
        }

        // Start is called before the first frame update
        void Start()
        {
            CheckForComponents(); //check for components that have yet to be assigned
            CollectButtons(); //same here.

            //Set these up properly
            if (this.roomSetup == null && this.recenterButton != null) //there is no SG_XR_RoomSetup present in this scene. In that case, we can remove it.
            {
                this.recenterButton.gameObject.SetActive(false);
            }
            if (this.visualElements != null)
            {
                this.visualElements.SetActive(true); //ensure the visuals are turned on.
            }
            if (startsHidden)
            {
                this.CloseMenu(true);
            }
        }

        // Calling in LateUpdate so the currentWristActivator has its latest values.
        void LateUpdate()
        {
            //Check Menu Deactivation when the user turns away their hand
            if (this.currentWristActivator != null)
            {
                //Check for de-activiation when the arm is rotated away
                if (menuOpen && this.currentWristActivator != null && this.currentWristActivator.AngleWithHMD > this.hideMenuAngle)
                {
                    this.CloseMenuWith(this.currentWristActivator); //smoothly close it
                }
            }
            UpdateMenuTransform(Time.deltaTime);
        }
    }
}