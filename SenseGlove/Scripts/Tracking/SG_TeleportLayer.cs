using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace SG
{
    /// <summary> Customizeable Teleport Options for an SG_TrackedHand. </summary>
    public class SG_TeleportLayer : SG_HandComponent
    {
        //---------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> GameObject used as the origin of the beam. If none is assigned, we use the wrist </summary>
        [Header("Teleport Components")]
        public Transform beamOrigin;
        /// <summary> Optional Tracking Script for the Beam Origin - attaching it to the wrist. When not assigned, yoú're reposonsible for linkin it. </summary>
        public SG_SimpleTracking beamOriginTracking;

        /// <summary> Renders a line between the Beam Origin and the Teleport Location </summary>
        public LineRenderer beamRenderer;

        /// <summary> Obtional Object to place where the beam hits. Can be replaced with anything fancy or animated. </summary>
        public Transform beamEndObject;

        /// <summary> SG Gesture used to turn on the teleport beam </summary>
        public SG_BasicGesture activationGesture;
        /// <summary> SG Gesture used to confirm a teleport is required. </summary>
        public SG_BasicGesture teleportGesture;
        
        /// <summary> Controls the visuals to show the progress for teleportation, when needed </summary>
        public GameObject progressVisualObject;
        /// <summary> Script component used to control the progress visual, using a value between 0 and 1. </summary>
        [SG_RequireInterface(typeof(SG.IControlledBy01Value))]
        public MonoBehaviour progessVisualController;

        /// <summary> Debug Text for teleportation </summary>
        public TextMesh debugTextElement;


        /// <summary> If true, only one of the hands (left/right) can have a teleport beam active at onetime. </summary>
        [Header("Teleport Settings")]
        public bool oneHandOnly = true; //If true; only one hand can have the beam active at one point. Otherwise, we don't care.
        /// <summary> If true, in order to activate the teleportation, you must be aiming on (valid) ground. </summary>
        public bool activateOnValidGroundOnly = true;

        /// <summary> Which beam type to use </summary>
        public TeleportBeamType beamType = TeleportBeamType.Bezier;
        /// <summary> How the teleport beam is activated </summary>
        public ActivationType activateBeam = ActivationType.ActivateGesture;
        /// <summary> Onlce activated, this is how we confirm the teleportation </summary>
        public ConfirmType confirmTeleport = ConfirmType.KeepAimingTimer;

        /// <summary> Which Physics Layers one is allowed to teleport to </summary>
        public LayerMask teleportLayers;

        /// <summary> Distance (in meters) how far you can move. </summary>
        public float beamDistance = 20.0f;

        /// <summary> The amount of 'steps' our beam will have (for Curved beams like Bezier only) </summary>
        [Range(2, 100)] public int beamResolution = 32; //resolution for when a Bezier Curve is used.

        /// <summary> Teleports the hands to their new destination location after moving the play, so you don't need to wait on them to catch up. Optional in case you do something custom. </summary>
        public bool teleportHands = true;

        /// <summary> The amount of time it takes to activate the beam with a gesture (if any are used) </summary>
        public float activationTime = 0.2f;
        /// <summary> How long you need to be holding the gesture / aim at the ground etc before the teleportation happens </summary>
        public float teleportConfirmTime = 1.2f;

        /// <summary> After teleporting, we disable the beam(s) and you're not allowed to teleport again for this time </summary>
        public float timeBetweenTeleports = 1.0f;
        
        /// <summary> Basic Teleport Beam Colour, when it's not hitting anything </summary>
        public Color basicBeamColour = Color.red;
        /// <summary> Beam Colour when it hits a valid teleportation object </summary>
        public Color validBeamColour = Color.green;

        /// <summary> If Set to true, this script will not actually move your XR Rig, only raise events on where it wants to go... </summary>
        public bool fireEventsOnly = false;

        /// <summary> Fired when the beam activates </summary>
        [Header("Teleport Events")]
        public UnityEvent BeamActivated = new UnityEvent();
        /// <summary> Fired when the beam deactivated (which also happens on a Teleport) </summary>
        public UnityEvent BeamDeactivated = new UnityEvent();
        /// <summary> Happens just before a teleport occurs. Parameter is the target location </summary>
        public SG_TeleportEvent BeforeTeleport = new SG_TeleportEvent();
        /// <summary> Happens just after a teleport occurs. Parameter is the target location </summary>
        public SG_TeleportEvent AfterTeleport = new SG_TeleportEvent();


        // Private Variables

        /// <summary> The actual transfrom used for teleportation. Separately declared in case BeamOrigin etc are not defined. </summary>
        private Transform originTransform = null;

        /// <summary> Current Teleport State for state driven logic </summary>
        private TeleportState currState = TeleportState.Idle;
        /// <summary> Keeps track of how long we've been waiting to go from Cooldoin -> Idle </summary>
        private float teleportCooldownTimer = 0;
        /// <summary> Timer for how long gestures are made. </summary>
        private float gestureTimer = 0;
        /// <summary> If true, the last beam check hit a valid target </summary>
        private bool hittingValidTarget = false;
        /// <summary> If hittingValidTarget, this value contains the point that was hit.. </summary>
        private Vector3 lastBeamTarget = Vector3.zero;
        /// <summary> (re)generated and used for both raycasts and rendering </summary>
        private Vector3[] beamPoints = new Vector3[0];

        /// <summary> Debug test in case the Debug Text Element is not assinged. </summary>
        private string debug = "";
        /// <summary> Used to check if I'm not holding / hovering over object(s). </summary>
        private SG_GrabScript myGrabScript;
        /// <summary> Úsed to check the other hand's teleport state </summary>
        private SG_TeleportLayer otherhandTeleportScript;
        /// <summary> Controls the progress bar visual of the Teleport Script </summary>
        private SG.IControlledBy01Value progressControlScript;

        /// <summary> Used for Bezier control points </summary>
        private Vector3[] m_ControlPoints = new Vector3[3];
        /// <summary> Used to generate control point in the middle of the beam. This is X meters 'forward' </summary>
        private float controlPointDistance = 5f;
        /// <summary> Used to generate control point in the middle of the beam. This is X meters 'high' </summary>
        private float controlPointHeight = 3f;
        /// <summary> How 'low' the final point on the beam will be from the start height. </summary>
        private float endPointHeight = -5f;



        //---------------------------------------------------------------------------------------------------------------------------
        // Accessors


        /// <summary> Control the beam Line Renderer </summary>
        public bool BeamLineEnabled
        {
            get { return beamRenderer != null && beamRenderer.enabled; }
            set { if (beamRenderer != null) { beamRenderer.enabled = value; } }
        }

        /// <summary> Control the GameObject at the end of the Beam, where the hit occurs </summary>
        public bool BeamEndEnabled //toggled separate from the Teleport Beam it nothing can be hit.
        {
            get { return beamEndObject != null && beamEndObject.gameObject.activeSelf; }
            set { if (beamEndObject != null) { beamEndObject.gameObject.SetActive(value); } }
        }

        /// <summary> If true, we turn the timing element on/off </summary>
        public bool TimerElementEnabled 
        {
            get { return progressVisualObject != null && progressVisualObject.activeSelf; }
            set { if (progressVisualObject != null) { progressVisualObject.SetActive(value); } }
        }

        /// <summary> The Debug Text element contents </summary>
        public string DebugText
        {
            get { return debugTextElement != null ? debugTextElement.text : ""; }
            set { if (debugTextElement != null) { debugTextElement.text = value; } }
        }

        /// <summary> Current Teleport State, used for other hand(s) </summary>
        public TeleportState State
        {
            get { return this.currState; }
        }


        //---------------------------------------------------------------------------------------------------------------------------
        // SG_HandComponent Implementation


        /// <summary> Links this layer to an SG_TrackedHand </summary>
        /// <param name="newHand"></param>
        /// <param name="firstLink"></param>
        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand_Internal(newHand, firstLink);
            if (firstLink)
            {
                //TODO: Set up Tracking and the like
                SG_HandPoser3D trackingTargets = newHand.GetPoser(SG_TrackedHand.TrackingLevel.VirtualPose);

                if (beamOrigin == null || beamOriginTracking == null) //check if we need to grab / update either one
                {
                    if (beamOrigin != null)
                        beamOriginTracking = beamOrigin.GetComponent<SG_SimpleTracking>();
                    else if (beamOriginTracking != null)
                        beamOrigin = beamOriginTracking.transform;
                }
                if (beamOriginTracking != null) //and then do it
                {
                    trackingTargets.ParentObject(beamOriginTracking.transform, beamOriginTracking.linkMeTo); //Instead of following a frame behind, we're childing.
                    beamOriginTracking.updateTime = SG_SimpleTracking.UpdateDuring.Off; //Turn it off.
                }
                if (this.debugTextElement != null)
                    this.debugTextElement.transform.parent = trackingTargets.wrist;

                SG_GestureLayer gestures = newHand.gestureLayer;
                if (gestures == null)
                {
                    if (activateBeam == ActivationType.ActivateGesture || confirmTeleport == ConfirmType.ConfirmGesture)
                        Debug.LogError("Teleport Script relies on Gestures, but no Gesture Scritp is connected to " + TrackedHand.name, this);
                }
                else
                {   //do this regardless in case you change activation at runtime.
                    gestures.AddGesture(this.activationGesture);
                    gestures.AddGesture(this.teleportGesture);
                }

                if (progressVisualObject != null)
                    progressVisualObject.transform.parent = trackingTargets.wrist;
                if (progessVisualController != null && progessVisualController is SG.IControlledBy01Value) //extra sanity check in case the Editor Script(s) fail me.
                    progressControlScript = (SG.IControlledBy01Value)progessVisualController;

                myGrabScript = newHand.grabScript;

                if (newHand.GetOtherHand(out SG_TrackedHand otherHand))
                    otherhandTeleportScript = otherHand.teleportLayer;

                originTransform = beamOrigin;
                if (originTransform == null)
                    originTransform = trackingTargets.wrist;
                if (originTransform == null)
                    originTransform = this.transform; //final option; this.
            }

        }

        /// <summary> Create and setup the components inside this script </summary>
        protected override void CreateComponents()
        {
            base.CreateComponents();
            if (beamRenderer != null)
            {
                beamRenderer.startColor = basicBeamColour;
                beamRenderer.endColor = basicBeamColour;
                switch (beamType)
                {
                    case TeleportBeamType.Straight:
                        beamRenderer.positionCount = 2;
                        break;
                    default:
                        beamRenderer.positionCount = beamResolution;
                        break;
                }
            }
            switch (beamType)
            {
                case TeleportBeamType.Bezier:
                    beamPoints = new Vector3[beamResolution];
                    break;
                default:
                    beamPoints = new Vector3[2];
                    break;
            }
        }


        /// <summary> Add my debug objects to a list so it won't be shown when debug is disabled. </summary>
        /// <param name="objects"></param>
        /// <param name="renderers"></param>
        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);
            if (this.debugTextElement != null)
                objects.Add(this.debugTextElement.gameObject);
        }



        //---------------------------------------------------------------------------------------------------------------------------
        // Teleport Methods - can be called by other scripts

        /// <summary> If not aleasy activated, this turns on the beam. </summary>
        public void ActivateTeleportBeam()
        {
            BeamActivated?.Invoke();
            SetTeleportState(TeleportState.BeamActivated);
        }

        /// <summary> If activated, this disables the beam. </summary>
        public void DisableTeleportBeam()
        {
            BeamDeactivated?.Invoke();
            SetTeleportState(TeleportState.Idle);
        }


        /// <summary> Invoke teleportation where the beam is aiming. But it should be hitting a valid target! </summary>
        public void TeleportToBeamTarget()
        {
            if (!hittingValidTarget)
            {
                Debug.LogWarning("Cannot teleport since no valid target has been hit.");
                if (this.currState != TeleportState.BeamActivated || this.currState != TeleportState.AwaitConfirm)
                {
                    Debug.LogWarning("Cannot teleport since no valid target has been hit. Activate the beam first using ActivateTeleportBeam()");
                }
                return;
            }
            Teleport(lastBeamTarget);
            BeamDeactivated?.Invoke(); //should happen regardless. And since I'm teleporting -to- the beam target, this is fine
        }




        /// <summary> Teleports the player to the target location </summary>
        /// <param name="targetLocation"></param>
        /// <param name="targetHeadForward"></param>
        /// <param name="targetUp"></param>
        public void Teleport(Vector3 targetLocation)
        {
            Debug.Log("TODO: Teleport Logic");

            Transform xrRig, xrHead;
            if (!SG_XR_SceneTrackingLinks.GetXRRigAndHeadTransforms(out xrRig, out xrHead))
            {
                Debug.LogError("This SG_TeleportLayer relies on SG_XR_SceneTrackingLinks to find the XR Rig and Camera. Please make sure said script is in your scene and fully linked to the right GameObjects!", this);
                return;
            }

            this.currState = TeleportState.Teleporting;
            //Move the XR Rig such that one of it's children appears at the chosen location. But the Y of the XR Rig must the that of Location
            BeforeTeleport?.Invoke(targetLocation);

            if (!fireEventsOnly) //skip if we're only calling the event(s) and aren't actually teleporing. In case someone else wants to handle the logic. 
            {
                //Move XR Rig
                Vector3 offset = targetLocation - xrHead.position;
                xrRig.position += new Vector3(offset.x, 0.0f, offset.z);
                xrRig.position = new Vector3(
                    xrRig.position.x,
                    targetLocation.y,
                    xrRig.position.z
                );
            }
            SetTeleportState(TeleportState.Cooldown);
            AfterTeleport?.Invoke(targetLocation);

            //Move Hands if needed
            if (teleportHands)
                StartCoroutine(TeleportPhysicsHands());
        }


        /// <summary> Teleports the Physics Hands to the actual location of the 'real hand' wherever possible. This prevents the 'flying hands' you see </summary>
        /// <returns></returns>
        private IEnumerator TeleportPhysicsHands()
        {
            yield return null;
            if (TrackedHand != null)
            {
                if (TrackedHand.handPhysics != null)
                    TrackedHand.handPhysics.PlaceHandAtRealLocation();
                if (TrackedHand.GetOtherHand(out SG_TrackedHand oppositeHand) && oppositeHand.handPhysics != null)
                {
                    oppositeHand.handPhysics.PlaceHandAtRealLocation();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------
        // Teleport Logic - internal state-driven handling

        /// <summary> If true, the beam is currently allowed to activate. Note: Can be expensive if activateOnValidGroundOnly is true, since this checks physics and raycast. </summary>
        /// <returns></returns>
        public bool IsBeamActivateAllowed()
        {
            // Quickest: Check if I'm holding / hovering over anything grabable.
            if (myGrabScript != null && myGrabScript.IsGrabbing || myGrabScript.IsHovering())
                return false;

            // Quick: Check other hand's state if needed.
            if (oneHandOnly && otherhandTeleportScript != null)
            {
                TeleportState other = otherhandTeleportScript.State;
                if (other != TeleportState.Idle)
                    return false; //the other hand is currently active and/or teleporting.
            }

            // Slow: Update Beam and check if we're hitting a valid target 
            if (activateOnValidGroundOnly)
            {
                bool onTarget = false;
                if (this.beamType == TeleportBeamType.Bezier)
                {
                    UpdateBezierBeamPoints();
                    onTarget = RaycastPoints(this.beamPoints, this.teleportLayers, null, out RaycastHit hit); //passing LineRenderer NULL so I skip updating that one.
                }
                else if (beamType == TeleportBeamType.Straight)
                {
                    onTarget = Physics.Raycast(originTransform.position, originTransform.forward, out RaycastHit hitInfo, beamDistance, teleportLayers, QueryTriggerInteraction.UseGlobal);
                }
                if (!onTarget)    
                    return false; //we're not yet hitting anything.
            }
            return true;
        }

        /// <summary> Sets the Teleport Progress visuals(!) to the selected value </summary>
        /// <param name="value01"></param>
        /// <param name="hideIfZero"></param>
        public void SetTeleportProgressVisuals(float value01, bool hideIfZero)
        {
            if (progressVisualObject != null)
            {
                if (hideIfZero && Mathf.Abs(value01) < 0.01f)
                    progressVisualObject.SetActive(false);
                else
                    progressVisualObject.SetActive(true);
            }
            if (progressControlScript != null)
                progressControlScript.SetControlValue(value01);
        }

        /// <summary> Switch the Teleport State to a new one. (Re)s)ets a bunch of variables. </summary>
        /// <param name="state"></param>
        /// <param name="enforceChange"></param>
        private void SetTeleportState(TeleportState state, bool enforceChange = false)
        {
            if (this.currState == state && !enforceChange)
                return;

            //Debug.Log("Switched Teleport State to " + state.ToString());
            this.currState = state;

            //do these regardless of the state. Caus eI'm re-using them
            teleportCooldownTimer = 0.0f;
            gestureTimer = 0.0f; //reset timer

            bool beamEndOn = false;
            bool beamOn = false; //putting this in a bool since in most cases it will be off!
            bool timerVisualOn = false;
            switch (this.currState)
            {
                case TeleportState.AwaitBeamActivate:
                    break;
                case TeleportState.BeamActivated:
                    hittingValidTarget = false;

                    //So that when we turn it on, its point are alreay at the right location!
                    if (this.beamType == TeleportBeamType.Bezier)
                    {
                        UpdateBezierBeamPoints();
                        RaycastPoints(this.beamPoints, this.teleportLayers, this.beamRenderer, out RaycastHit hit);
                    }
                    beamOn = true;
                    break;
                case TeleportState.AwaitConfirm:
                    beamOn = true;
                    beamEndOn = hittingValidTarget; //keep it on if it was hitting a valid target before. Though to be fair, if we get here, it -should- already be on.
                    timerVisualOn = hittingValidTarget;
                    break;
                case TeleportState.Cooldown:
                    hittingValidTarget = false;
                    break;
                default:
                    break;
            }
            TimerElementEnabled = timerVisualOn;
            BeamLineEnabled = beamOn;
            BeamEndEnabled = beamEndOn;
        }

        /// <summary> Handle Teleport logic for one 'frame' </summary>
        /// <param name="dT"></param>
        private void RunTeleportLogic(float dT)
        {
            switch (this.currState)
            {
                case TeleportState.Idle:
                    
                    if (activateBeam == ActivationType.ActivateGesture) //if acitvated on function calls, we just do nothing here.
                    {
                        if (activationGesture.IsGesturing && IsBeamActivateAllowed())
                            SetTeleportState(TeleportState.AwaitBeamActivate);
                    }
                    break;

                case TeleportState.AwaitBeamActivate:
                    if (activateBeam == ActivationType.ActivateGesture) //if acitvated on function calls, we just do nothing here.
                    {
                        if (activationGesture.IsGesturing)
                        {
                            gestureTimer += dT;
                            if (gestureTimer >= activationTime)
                                ActivateTeleportBeam();
                        }
                        else
                        {
                            gestureTimer -= dT;
                            if (gestureTimer <= 0.0f) //go the other way
                                SetTeleportState(TeleportState.Idle); //todo: add Hysterises
                        }
                    }
                    break;

                case TeleportState.BeamActivated:

                    //Keep checking for valid target(s) and update lastBeamTarget
                    UpdateBeam();

                    //TODO: If gesture or timing based enable or disable when required

                    //Check if de-activated
                    if (activateBeam == ActivationType.ActivateGesture)
                    {
                        if (!activationGesture.IsGesturing)
                        {
                            teleportCooldownTimer += dT;
                            if (teleportCooldownTimer >= activationTime) //tood; make another timer.
                                SetTeleportState(TeleportState.Idle); //back to idle since we're not making it anymore...
                        }
                    }
                    if (confirmTeleport == ConfirmType.ConfirmGesture && teleportGesture.IsGesturing)
                    {
                        SetTeleportState(TeleportState.AwaitConfirm);
                    }
                    else if (confirmTeleport == ConfirmType.KeepAimingTimer)
                    {
                        if (hittingValidTarget)
                        {
                            gestureTimer += dT;
                            float progress = teleportConfirmTime > 0 ? gestureTimer / teleportConfirmTime : 1.0f;
                            SetTeleportProgressVisuals(progress, true);
                            if (gestureTimer >= teleportConfirmTime)
                                TeleportToBeamTarget();
                        }
                        else
                            gestureTimer = 0.0f;
                    }
                    break;

                case TeleportState.AwaitConfirm:

                    if (confirmTeleport == ConfirmType.ConfirmGesture) //if activated on function calls, we just do nothing here.
                    {
                        if (teleportGesture.IsGesturing)
                        {
                            gestureTimer += dT;
                            float progress = teleportConfirmTime > 0 ? gestureTimer / teleportConfirmTime : 1.0f;
                            SetTeleportProgressVisuals(progress, true);
                            if (gestureTimer >= teleportConfirmTime)
                                TeleportToBeamTarget();
                        }
                        else
                        {
                            gestureTimer -= dT;
                            if (gestureTimer <= 0.0f) //go the other way
                                SetTeleportState(TeleportState.BeamActivated); //todo: add Hysterises
                        }
                    }
                    break;

                case TeleportState.Cooldown:

                    teleportCooldownTimer += dT;
                    if (teleportConfirmTime >= timeBetweenTeleports)
                        SetTeleportState(TeleportState.Idle);
                    break;

                default:
                    break;
            }
            UpdateDebugTxt();
        }


        /// <summary> Update the Debug Text (element) if enabled </summary>
        public void UpdateDebugTxt()
        {
            if (!debugEnabled)
                return;
            if (debugTextElement == null)
                return;

            debug = currState.ToString();
            switch (currState)
            {
                case TeleportState.AwaitBeamActivate: //one only gets here when gesture is active
                    debug += $"\n{gestureTimer.ToString("0.00")} / {activationTime.ToString("0.00")}s";
                    break;
                case TeleportState.AwaitConfirm: //one only gets here when gesture is active
                    debug += $"\n{gestureTimer.ToString("0.00")} / {teleportConfirmTime.ToString("0.00")}s";
                    break;
                case TeleportState.BeamActivated:

                    debug += $" / {confirmTeleport.ToString()}";
                    debug += $"\nBeamType: {beamType.ToString()} -> " + (hittingValidTarget ? "HIT" : "No Hit");
                    if (activateBeam == ActivationType.ActivateGesture)
                        debug += $"\nCancel: {teleportCooldownTimer.ToString("0.00")} / {activationTime.ToString("0.00")}s";
                   
                    if (confirmTeleport == ConfirmType.KeepAimingTimer)
                        debug += $"\nKeepAiming: {gestureTimer.ToString("0.00")} / {teleportConfirmTime.ToString("0.00")}s";

                    break;
                case TeleportState.Cooldown:
                    debug += $"\n{teleportCooldownTimer.ToString("0.00")} / {timeBetweenTeleports.ToString("0.00")}s";
                    break;
                case TeleportState.Idle:
                    debug += $" -> {activateBeam.ToString()}";
                    break;
                default:
                    break;
            }
            debugTextElement.text = debug;
        }




        /// <summary> Update beam graphics and hit location </summary>
        private void UpdateBeam()
        {
            bool hitTarget = false;
            Vector3 hitLocation = Vector3.zero;
            switch (beamType)
            {
                case TeleportBeamType.Bezier:

                    hitTarget = false;
                    UpdateBezierBeamPoints();
                    hitTarget = RaycastPoints(beamPoints, teleportLayers, this.beamRenderer, out RaycastHit hit);
                    hitLocation = hitTarget ? hit.point : beamPoints[beamPoints.Length - 1];

                    break;
                default:

                    hitTarget = Physics.Raycast(originTransform.position, originTransform.forward, out RaycastHit hitInfo, beamDistance, teleportLayers, QueryTriggerInteraction.UseGlobal);
                    hitLocation = hitTarget ? hitInfo.point : originTransform.position + (originTransform.forward * beamDistance); //jus go forward beeeuw
                    beamPoints[0] = originTransform.position;
                    beamPoints[1] = hitLocation;
                    if (beamRenderer != null)
                    {
                        beamRenderer.positionCount = 2;
                        beamRenderer.SetPositions(beamPoints);
                    }
                    break;
            }

            //when we get here?
            if (hitTarget && !hittingValidTarget) //started hitting a valid target - toggle stuff
            {
                if (beamRenderer != null)
                {
                    beamRenderer.startColor = validBeamColour;
                    beamRenderer.endColor = validBeamColour;
                }
                BeamEndEnabled = true;
            }
            else if (hittingValidTarget && !hitTarget) //no longer hitting a valid target - toggle stuff
            {
                if (beamRenderer != null)
                {
                    beamRenderer.startColor = basicBeamColour;
                    beamRenderer.endColor = basicBeamColour;
                }
                BeamEndEnabled = false;
            }

            hittingValidTarget = hitTarget;
            lastBeamTarget = hitLocation;
            if (hittingValidTarget)
            {
                if (beamEndObject != null)
                    beamEndObject.position = hitLocation;
            }
        }


        /// <summary> Updates the Control points for a Bezier curve </summary>
        private void UpdateBezierBeamPoints()
        {
            UpdateBezierControlPoints(originTransform.position, originTransform.forward, Vector3.up);
            beamPoints = GenerateBezierPoints(m_ControlPoints[0], m_ControlPoints[1], m_ControlPoints[2], this.beamResolution);
            Debug.DrawLine(m_ControlPoints[0], m_ControlPoints[1], Color.white);
            Debug.DrawLine(m_ControlPoints[1], m_ControlPoints[2], Color.white);
        }

        /// <summary> Raycast along the points of a curve, and report if something is hit. Also update a linerenderer if one is provided. </summary>
        /// <param name="points"></param>
        /// <param name="layerMask"></param>
        /// <param name="lineRenderer"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static bool RaycastPoints(
            Vector3[] points,
            int layerMask,
            LineRenderer lineRenderer,
            out RaycastHit hit)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];

                //Debug.DrawLine(start, end, Color.black);

                Vector3 delta = end - start;
                float distance = delta.magnitude;

                if (distance <= 0f)
                    continue;

                if (Physics.Raycast(
                    start,
                    delta / distance,
                    out hit,
                    distance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
                {
                    // We hit something between points[i] and points[i + 1].

                    // Keep all points up to the segment start,
                    // then add the exact hit position.
                    if (lineRenderer != null)
                    {
                        lineRenderer.positionCount = i + 2;
                        for (int j = 0; j <= i; j++)
                            lineRenderer.SetPosition(j, points[j]);
                        lineRenderer.SetPosition(i + 1, hit.point);
                    }
                    return true;
                }
            }
            // Nothing hit — draw the entire curve (?)
            hit = default;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
            }
            return false;
        }

        
        /// <summary> Calculate the control points required for a Bezier Curve </summary>
        /// <param name="lineOrigin"></param>
        /// <param name="lineDirection"></param>
        /// <param name="curveReferenceUp"></param>
        void UpdateBezierControlPoints(Vector3 lineOrigin, Vector3 lineDirection, Vector3 curveReferenceUp)
        {
            m_ControlPoints[0] = lineOrigin;
            m_ControlPoints[1] = m_ControlPoints[0] + lineDirection * controlPointDistance + curveReferenceUp * controlPointHeight;
            m_ControlPoints[2] = m_ControlPoints[0] + lineDirection * beamDistance + curveReferenceUp * endPointHeight;
        }

        /// <summary> Generates a set of Bezier points based on the three control poitns and a resolution </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static Vector3[] GenerateBezierPoints(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            int resolution)
        {
            Vector3[] points = new Vector3[resolution];

            for (int i = 0; i < resolution; i++)
            {
                float t = (float)i / (resolution - 1);
                float u = 1f - t;

                points[i] =
                    u * u * p0 +
                    2f * u * t * p1 +
                    t * t * p2;
            }
            return points;
        }



        //---------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void Start()
        {
            if (this.TrackedHand != null && this.TrackedHand.GetOtherHand(out SG_TrackedHand otherHand))
                this.otherhandTeleportScript = otherHand.teleportLayer; //linking this -after- start because just in case the Unity Scripts haven't had their chance to set up yet.
            SetTeleportState(TeleportState.Idle, true);
        }

        private void Update()
        {
            RunTeleportLogic(Time.deltaTime);
        }


        private void OnDisable()
        {
            BeamEndEnabled = false;
            BeamLineEnabled = false;
        }

        private void OnEnable()
        {
            SetTeleportState(TeleportState.Idle, true);
        }


        //---------------------------------------------------------------------------------------------------------------------------
        // Enums

        /// <summary> How to avtivate the Teleport Beam </summary>
        public enum ActivationType
        {
            FunctionCall,       //only turn on when a Function is called
            ActivateGesture     //Use a simple pointing gesture (index straight, other finger flexed)
        }

        /// <summary> The type of Teleport Beam (while active) </summary>
        public enum TeleportBeamType
        {
            Straight,           //Straight line (nd RyCast) forward.
            Bezier              //
        }

        /// <summary> How one confirms the teleportation </summary>
        public enum ConfirmType
        {
            FunctionCall,       //only turn on when a Function is called
            KeepAimingTimer,    //Keep aiming until a timer elapses
            ConfirmGesture      //Make a gesture to confirm you want to teleport.
        }

        /// <summary> Mainly used internally for teleportation logic. </summary>
        public enum TeleportState
        {
            Idle,               //Start 
            AwaitBeamActivate,  //Gesture made, now waiting X ms to fully activate, or to disable.
            BeamActivated,      //Activated. Can confirm or 'undo' the teleport
            AwaitConfirm,       //Activated and Gesture made, now waiting X ms to fully Teleport, or to disable.
            Teleporting,        //Currently Teleporting...
            Cooldown,
        }

        //---------------------------------------------------------------------------------------------------------------------------
        // Unity Event
        [System.Serializable] public class SG_TeleportEvent : UnityEvent<Vector3> { }
    }
}