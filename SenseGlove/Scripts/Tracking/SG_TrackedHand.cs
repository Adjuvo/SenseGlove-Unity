using SGCore.Haptics;
using SGCore.Kinematics;
using UnityEngine;

namespace SG
{


    /// <summary> Responsible for coordinating updates and animation between its different 'layers', and ensuring they do not interfere with one another. 
    /// It does not implement its own IHandPoseProvider or IHandFeedbackDevice, but passes the implementation to its linked object(s). </summary>
	public class SG_TrackedHand : MonoBehaviour, IHandPoseProvider, IHandFeedbackDevice
    {
        /// <summary> Different poses to acces from this TrackedHand. Used to determine which SG_HandPoser3D to grab. </summary>
        public enum TrackingLevel
        {
            /// <summary> The pose of the real hand as determined by our Hand Tracking Source. Use this for intent or distance-calculations. </summary>
            RealHandPose,
            /// <summary> The wrist location as determined by our Physics / Grabables plus the real finger tracking </summary>
            VirtualPose,
            /// <summary> The wrist location as determined by our Physics / Grabables plus the finger tracking from our passThrough / pose overrides.  </summary>
            RenderPose,
        }

        /// <summary> Access the hand layers </summary>
        public enum HandLayer
        {
            HandModel,
            Animation,
            Grab,
            Feedback,
            Physics,
            Calibration,
            Gestures,
            Projection,
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        //      /// <summary> The GameObject from which we will try to take an IHandPoseProvider interface. Can be a SG_HapticGlove, or whaterver else your heart desires </summary>
        //      /// <remarks>Has to be a GameObject because Unity does not allow inspector references to Interfaces, only to Monobehaviour Classes.</remarks>
        //      [Header("Device I/O Components")]
        //      public GameObject handRealHandSource;
        //      /// <summary> The GameObject from which we will try to take an IHandFeedbackDevice interface. Can be a SG_HapticGlove, or whaterver else your heart desires </summary>
        //      /// <remarks>Has to be a GameObject because Unity does not allow inspector references to Interfaces, only to Monobehaviour Classes.</remarks>
        //      public GameObject hapticsSource;

        //      /// <summary> The actual source of real-life hand data. Used to gain access to the real hand pose, which is passed to the relevant scripts. </summary>
        //      public IHandPoseProvider realHandSource
        //      {
        //          get; private set;
        //      }

        //      /// <summary> The actual device to send Haptics to. Calling Haptic cmds sent ot this script will be passed to the source. </summary>
        //public IHandFeedbackDevice hapticHardware
        //      {
        //          get; private set;
        //      }

        

        public SG_DeviceSelector deviceSelector;

        /// <summary> The 3D Hand Model that is  actually rendered to the screen. The script purely holds information on which transforms control which joint. </summary>
        /// <remarks> Required because we use it as in input to geerate the correct HandPoser offsets for the other layers. </remarks>
        [Header("Hand Layers (Important)")]
        public SG_HandModelInfo handModel;

        /// <summary> Script responsible for animating the 3D model of the hand based on a SG_HandPose. When linked to the TrackedHand, it updates the Animator at the end of each Update using the Render Pose. </summary>
        /// <remarks> You can access the pose it uses through the TrackedHand's GetPose(TrackingLevel.RenderPose); function. </remarks>
        public SG_HandAnimator handAnimation;

        /// <summary> Script that is responsible for collecting Force-Feedback on the fingers, and sending it to the hapticHardware. When linked to a TrackedHand, its colliders are updated each Update using the Virtual Pose</summary>
        [Header("Hand Layers (Optional)")]
        public SG_HandFeedback feedbackLayer;

        /// <summary> The Script that is responsible for detecting "Grabbing" and "Releasing" of objects. When linked to the TrackedHand, it is used to overide physics behaviour of the hand when needed. </summary>
        public SG_GrabScript grabScript;

        /// <summary> A Script keeping track of a RigidBody and a series of colliders that represent the hand's Physics shape for the Physics Engine. When linked to the TrackedHand, 
        /// it updates the colliders and coodinates collision together with the grabScript. </summary>
        public SG_HandPhysics handPhysics;

        public SG_GestureLayer gestureLayer;

        //    /// <summary> An optional component to 'lock' finger flexion if the fingers would pass through a non-trigger collider. </summary>
        //    public SG_FingerPassThrough passThoughLayer;

        /// <summary> An optional component that locks the finger flexion when it passes through collider(s). Also used to predict force-feedback activation. </summary>
        public SG_HandProjection projectionLayer;

        /// <summary> The calibration layer of this hand, only required when attached to a Haptic Glove. Without it, you'll need to activate calibration from elsewhere. </summary>
        public SG_CalibrationSequence calibration;

        /// <summary> A visual indication of the hand state. </summary>
        public SG_HandStateIndicator statusIndicator;

        /// <summary> Represents the real world hand- and finger tracking, as Transforms in your Unity Scene. Used as a refrence for moving colliders. </summary>
        /// <remarks> Does not take into account anything the virtual world, and should mainly be used for intention / tracking. </remarks>
        protected SG_HandPoser3D realHandPoser;

        /// <summary> Represents the hand location as determined by physics and grabables, combined with the real finger tracking as Transforms in your Unity Scene. Used as a refrence for moving colliders (FFB, PassThrough). </summary>
        /// <remarks> Without a Physics- or Grab Layer, this is essentallty the Real Hand Pose. </remarks>
        protected SG_HandPoser3D virtualHandPoser;

        /// <summary> Represents the 'final' hand- and finger tracking used for rendering, as Transforms in your Unity Scene. Can be used as input for your virtual logic. </summary>
        /// <remarks> When no Passthrough Layer is present, and we're not holding only anything that overrides finger tracking, this is the same as the virtual HandPose </remarks>
        protected SG_HandPoser3D renderPoser;

        /// <summary> If true, this TrackedHand uses its own transform as the Wrist location. </summary>
        [Header("Control Parameters")]
        public bool overrideWristLocation = false;

        /// <summary> Whether or not to visualize the Real Hand Poser (red) using Linerenderers </summary>
        [Header("Debug Components")]
        public bool showRealPose = false;
        /// <summary> Whether or not to visualize the Virtual Hand Poser (green) using Linerenderers </summary>
        public bool showVirtualPose = false;
        /// <summary> Whether or not to visualize the Render Poser (blue) using Linerenderers </summary>
        public bool showRenderPose = false;

        /// <summary> If true, we can still run setup on this script. Used to prevent calling it twice. </summary>
        protected bool setup = true;
        /// <summary> ensures I don't link the calibration twice. </summary>
        protected bool calibrationLink = false;

        /// <summary> The latest "real" hand pose of the trackingSource. Collected and used during Update, and also used in FixedUpdate. Which is why we cache it. </summary>
        protected SG_HandPose l_realHandPose;
        /// <summary> The latest Collider pose, a.k.a. where Physics Determines the wrist to be combine with the real hand  </summary>
        protected SG_HandPose l_virtualPose;
        /// <summary> The latest render pose as determined by the Hand Physics/Grabable combination. Updated in PixedUpdate, used in (late)Update. </summary>
        protected SG_HandPose l_renderPose;




        /// <summary> Assigned manually </summary>
        protected IHandPoseProvider manualPoseProvider = null;

        /// <summary> The device or source used for hand tracking </summary>
        public IHandPoseProvider RealHandSource
        {
            get 
            { 
                if (manualPoseProvider != null)
                {
                    return this.manualPoseProvider;
                }
                return this.deviceSelector != null ? this.deviceSelector.CurrentTracking : null;
            }
            set 
            {
                this.manualPoseProvider = value;
                if (manualPoseProvider != null) //I could set it to NULL to clear it.
                {
                    this.manualPoseProvider.SetKinematics(this.GetKinematics());
                }
            }
        }


        /// <summary> Can be assigned manually to override the typical HapticHardware </summary>
        protected IHandFeedbackDevice manualHapticHardware = null;


        /// <summary> The deivce through whcih to play back Haptc Effects </summary>
        public IHandFeedbackDevice HapticHardware
        {
            get 
            {
                if (manualHapticHardware != null)
                {
                    return this.manualHapticHardware;
                }
                return this.deviceSelector != null ? this.deviceSelector.CurrentHaptics : null;
            }
            set
            {
                manualHapticHardware = value;
            }
        }



        /// <summary> Optional User that this hand is connected to. </summary>
        public SG_User User
        {
            get; set;
        }

        public bool GetUser(out SG_User user)
        {
            user = this.User;
            return user != null;
        }

        /// <summary> If this is the right hand, get the left hand, and vice versa. </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool GetOtherHand(out SG_TrackedHand otherHand)
        {
            if (this.User != null)
            {
                otherHand = this.TracksRightHand() ? this.User.leftHand : this.User.rightHand;
            }
            else
            {
                otherHand = null;
            }
            return otherHand != null;
        }


        //public void SetTrackingProvider(IHandPoseProvider provider)
        //{
        //    Setup(); //ensure setup is properly performed(!)
        //    this.realHandSource = provider;
        //    provider.SetKinematics(this.GetHandModel()); //Ensure this provider keeps the same output.
        //    Debug.Log(this.name + "Linked!");
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Setup Functions

        public void Setup()
        {
            if (setup)
            {
                setup = false;
                CreateComponents();
                LinkLayers();
            }
        }


        /// <summary> Creates all (missing) components to this SG_TackedHand. </summary>
        protected void CreateComponents()
        {
            // Create the Poser components.

            //Add a RealHandPoser if we don't have one yet
            Util.SG_Util.TryAddHandPoser(ref this.realHandPoser, "RealHandPose", this.transform, Color.red, showRealPose);

            //Add a virtual hand poser if we don't have one yet
            Util.SG_Util.TryAddHandPoser(ref this.virtualHandPoser, "VirtualPose", this.transform, Color.green, showVirtualPose);

            ////Add a PhysicsPoser if we don't have one yet
            Util.SG_Util.TryAddHandPoser(ref this.renderPoser, "RenderPose", this.transform, Color.blue, showRenderPose);

            // Make sure these match properly for other script to setup on Start()
            realHandPoser.MatchJoints(this.handModel);
            virtualHandPoser.MatchJoints(this.handModel);
            renderPoser.MatchJoints(this.handModel);
            
        }


        /// <summary> Attempt to link scripts to this TrackedHand and set up their behaviours. </summary>
        protected void LinkLayers()
        {
            //Try to link other layers if they exist in my children
            if (this.handModel == null)
            {
                this.handModel = this.GetComponentInChildren<SG_HandModelInfo>();
            }
            if (this.handAnimation == null)
            {
                this.handAnimation = this.GetComponentInChildren<SG_HandAnimator>();
            }
            if (this.calibration == null)
            {
                this.calibration = this.GetComponentInChildren<SG_CalibrationSequence>();
            }
            if (this.deviceSelector == null)
            {
                this.deviceSelector = this.GetComponent<SG_DeviceSelector>();
            }

            //// Grab the Interfaces off the objects
            //if (handRealHandSource != null)
            //{
            //    IHandPoseProvider[] providers = handRealHandSource.GetComponents<IHandPoseProvider>();
            //    for (int i = 0; i < providers.Length; i++)
            //    {
            //        if (!(providers[i] is SG_TrackedHand && ((SG_TrackedHand)providers[i]) == this)) //ensure we don;t assign ourselves(!)
            //        {
            //            this.realHandSource = providers[i];
            //            break;
            //        }
            //    }
            //    if (this.realHandSource == null) { Debug.LogError("The handRealHandSource assigned to " + this.name + " does not have an IHandPoseProvider script attached. It's best to assign one or leave this field blank.", this); }
            //}

            //if (hapticsSource != null)
            //{
            //    IHandFeedbackDevice[] providers = hapticsSource.GetComponents<IHandFeedbackDevice>();
            //    for (int i = 0; i < providers.Length; i++)
            //    {
            //        if (!(providers[i] is SG_TrackedHand && ((SG_TrackedHand)providers[i]) == this)) //ensure we don;t assign ourselves(!)
            //        {
            //            this.hapticHardware = providers[i];
            //            break;
            //        }
            //    }
            //    if (this.hapticHardware == null) { Debug.LogError("The hapticsSource assigned to " + this.name + " does not have an IHandFeedbackDevice script attached. It's best to assign one or leave this field blank.", this); }
            //}

            // Generate a HandModel from this script.
            SGCore.Kinematics.BasicHandModel handModel = this.GetHandModel();
            // Debug.Log(this.name + " Collected a HandModel: " + handModel.ToString());

            if (this.deviceSelector != null)
            {
                //Tell the realHandSource which kinematic model to use for forward kinematics. 
                this.deviceSelector.SetKinematics(handModel);
            }
            else
            {
                Debug.Log(this.name + " has no tracking source (yet)");
            }

            //TODO: Setup all layers.

            //Setup the grabScript, if any, and link it to this hand's posers.
            if (this.grabScript != null)
            {
                grabScript.LinkToHand(this, true);
                grabScript.SetIgnoreCollision(this.handPhysics, true);
                grabScript.SetIgnoreCollision(this.projectionLayer, true);
                grabScript.SetIgnoreCollision(this.feedbackLayer, true);
            }

            //Setup the passThoughLayer, if any, and link it to this hand's posers.
            if (this.projectionLayer != null)
            {
                projectionLayer.LinkToHand(this, true);
                projectionLayer.SetIgnoreCollision(this.handPhysics, true);
                //passThoughLayer.SetIgnoreCollision(this.grabScript, true); //should have been done if it exists
                projectionLayer.SetIgnoreCollision(this.feedbackLayer, true);
            }

            //Setup the handPhysics, if any, and link it to this hand's posers.
            if (this.handPhysics != null)
            {
                this.handPhysics.LinkToHand(this, true);
                //handPhysics.SetIgnoreCollision(this.grabScript, true); //should have been done if it exists
                //handPhysics.SetIgnoreCollision(this.passThoughLayer, true); //should have been done if it exists
                handPhysics.SetIgnoreCollision(this.feedbackLayer, true);
            }

            //Setup the feedbackLayer, if any, and link it to this hand's posers.
            if (this.feedbackLayer != null)
            {
                this.feedbackLayer.LinkToHand(this, true);
                //feedbackLayer.SetIgnoreCollision(this.handPhysics, true); ////should have been done if it exists
                //feedbackLayer.SetIgnoreCollision(this.passThoughLayer, true); //should have been done if it exists
                //grabScript.SetIgnoreCollision(this.grabScript, true); //should have been done if it exists
            }

            if (this.statusIndicator != null) //ensure this indicator is here.
            {
                this.statusIndicator.LinkToHand(this, true);
            }

            if (this.gestureLayer != null)
            {
                this.gestureLayer.LinkToHand(this, true);
            }

            if (this.calibration != null) //we have a calibration layer, and it's linked to a Hapitc Glove.
            {
                this.calibration.LinkHand(this);
                this.LinkCalibrationEvents();
            }
            this.UpdateHandState();


            // Generate an Idle pose that all poses start as
            SG_HandPose idlePose = new SG_HandPose(SGCore.HandPose.DefaultIdle(this.TracksRightHand(), handModel));
            l_realHandPose = idlePose;
            l_virtualPose = idlePose;
            l_renderPose = idlePose;

            //finally, now that all elements are linked we can set these posers in the correct pose.
            realHandPoser.UpdateHandPoser(l_realHandPose);
            virtualHandPoser.UpdateHandPoser(l_virtualPose);
            renderPoser.UpdateHandPoser(l_renderPose);
        }


        /// <summary> Enable / Disable collision between this TrackedHand + Layers and those of another TrackedHand. Should be done to minimize glitchy behaviours between hands. </summary>
        /// <param name="otherHand"></param>
        /// <param name="ignoreCollision">if true, no more collision will take place</param>
        public void SetIgnoreCollision(SG_TrackedHand otherHand, bool ignoreCollision)
        {
            if (otherHand != this)
            {
                //ignore collision between passThroughs, which is the most important one
                if (this.projectionLayer != null)
                {
                    this.projectionLayer.SetIgnoreCollision(otherHand.projectionLayer, ignoreCollision);
                    this.projectionLayer.SetIgnoreCollision(otherHand.handPhysics, ignoreCollision); // My passThrough + other's HandPhysics
                }
                //Ignore the others' hand colliders as they jump around between parents.
                if (otherHand.projectionLayer != null)
                {
                    otherHand.projectionLayer.SetIgnoreCollision(this.handPhysics, ignoreCollision); //my handPhysics + other hand's passthrough
                }
                //Ignore each other's hand physics- as supposedly you should be stopped by your real hands.
                if (this.handPhysics != null)
                {
                    this.handPhysics.SetIgnoreCollision(otherHand.handPhysics, ignoreCollision);
                }
                //ignore grab layers becasue they get confused about finger bone collidrrs.
                if (this.grabScript != null)
                {
                    this.grabScript.SetIgnoreCollision(otherHand.handPhysics, ignoreCollision);
                }
                if (otherHand.grabScript != null)
                {
                    otherHand.grabScript.SetIgnoreCollision(this.handPhysics, ignoreCollision);
                }
            }
        }


        protected void LinkCalibrationEvents()
        {
            if (!calibrationLink && this.calibration != null && this.calibration.linkedGlove != null) //subscribe
            {
                this.calibration.linkedGlove.CalibrationStateChanged.AddListener(UpdateHandState);
                calibrationLink = true;
            }
        }

        protected void UnlinkCalibrationEvents()
        {
            if (this.calibrationLink && this.calibration != null && this.calibration.linkedGlove != null)
            {
                this.calibrationLink = false;
                this.calibration.linkedGlove.CalibrationStateChanged.RemoveListener(UpdateHandState);
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utility Functions

        /// <summary> Enable / Disable the hand model </summary>
        public bool HandModelEnabled
        {
            get
            {
                return this.handModel != null && this.handModel.gameObject.activeSelf;
            }
            set
            {
                if (this.handModel != null)
                {
                    this.handModel.gameObject.SetActive(value);
                }
                if (this.handPhysics != null)
                {
                    this.handPhysics.CollisionsEnabled = value; //only turn it back on if the hand wants it to be on.
                }
                UpdateDebugLines(); //turn on / off the debug lines
            }
        }

        public void UpdateDebugLines()
        {
            if (this.realHandPoser != null) { this.realHandPoser.LinesEnabled = this.showRealPose && this.HandModelEnabled; }
            if (this.virtualHandPoser != null) { this.virtualHandPoser.LinesEnabled = this.showVirtualPose && this.HandModelEnabled; }
            if (this.renderPoser != null) { this.renderPoser.LinesEnabled = this.showRenderPose && this.HandModelEnabled; }
        }

        /// <summary> Safely enable / disable a hand layer. </summary>
        /// <param name="layer"></param>
        /// <param name="active"></param>
        public void SetLayer(HandLayer layer, bool active)
        {
            switch (layer)
            {
                case HandLayer.Animation:
                    if (this.handAnimation != null) { this.handAnimation.gameObject.SetActive(active); }
                    break;
                case HandLayer.Calibration:
                    if (this.calibration != null) { this.calibration.gameObject.SetActive(active); }
                    break;
                case HandLayer.Feedback:
                    if (this.feedbackLayer != null) { this.feedbackLayer.gameObject.SetActive(active); }
                    break;
                case HandLayer.Gestures:
                    if (this.gestureLayer != null) { this.gestureLayer.gameObject.SetActive(active); }
                    break;
                case HandLayer.Grab:
                    if (this.grabScript != null) { this.grabScript.gameObject.SetActive(active); }
                    break;
                case HandLayer.HandModel:
                    if (this.handModel != null) { this.handModel.gameObject.SetActive(active); }
                    break;
                case HandLayer.Projection:
                    if (this.projectionLayer != null) { this.projectionLayer.gameObject.SetActive(active); }
                    break;
                case HandLayer.Physics:
                    if (this.handPhysics != null) { this.handPhysics.gameObject.SetActive(active); }
                    break;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Tracking Functions

        /// <summary> Returns the latest HandPose as determined by this TrackedHand of the chosen level. </summary>
        /// <param name="pose"></param>
        /// <returns></returns>
        public SG_HandPose GetHandPose(TrackingLevel pose)
        {
            switch (pose)
            {
                case TrackingLevel.VirtualPose:
                    return l_virtualPose;
                case TrackingLevel.RenderPose:
                    return l_renderPose;
            }
            return this.l_realHandPose;
        }


        /// <summary> Access the HandModel associated with this TrackedHand's 3D Model. Used as input to setup the different layers. </summary>
        /// <returns></returns>
        public SGCore.Kinematics.BasicHandModel GetHandModel()
        {
            return this.handModel != null ? this.handModel.HandKinematics : SGCore.Kinematics.BasicHandModel.Default(this.TracksRightHand());
        }

        /// <summary> Returns one of this hand's Posers; collections of Transforms for each joint. Posers can then be used to access specific hand sections. </summary>
        /// <param name="forTrackingLevel"></param>
        /// <returns></returns>
        public SG_HandPoser3D GetPoser(TrackingLevel forTrackingLevel)
        {
            Setup(); //if we haven't already
            switch (forTrackingLevel)
            {
                case TrackingLevel.RenderPose:
                    return this.renderPoser;
                case TrackingLevel.VirtualPose:
                    return this.virtualHandPoser;
                default:
                    return this.realHandPoser;
            }
        }

        /// <summary> Returns the Transform of a specific part of the hand, of a specific poser. Use this to link your object to a specific poser. </summary>
        /// <param name="level"></param>
        /// <param name="handSection"></param>
        /// <returns></returns>
        public Transform GetTransform(TrackingLevel level, HandJoint handSection)
        {
            return GetPoser(level).GetTransform(handSection);
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Tracking Synchronization

        /// <summary> Called during Update(). Returns the most up-to-date ColliderPose. Because the hand and/or object I'm holding moves in (bewtween) the FixedUpdate, we need a way to get the up-to-date variables in the Update </summary>
        /// <param name="currWristPos"></param>
        /// <param name="currWristRot"></param>
        protected SG_HandPose GetLatestColliderPose(SG_HandPose realHandPose)
        {
            //latest real hand combined with the up-to-date wrist
            Vector3 updatedWristPos;
            Quaternion updatedWristRot;
            if (this.grabScript != null && this.grabScript.isActiveAndEnabled && grabScript.ControlsHandLocation())
            {
                this.grabScript.GetHandLocation(realHandPose, out updatedWristPos, out updatedWristRot);
            }
            else if (this.handPhysics != null && this.handPhysics.isActiveAndEnabled)
            {
                updatedWristPos = this.handPhysics.WristPosition;
                updatedWristRot = this.handPhysics.WristRotation;
            }
            else
            {
                return realHandPose; //nothing special is happening, so just give it the latest collider pose.
            }
            return SG_HandPose.Combine(updatedWristPos, updatedWristRot, realHandPose, false); //combine the wrist tracking with the latest hand pose
        }

        /// <summary> Called during Update(). Returns the finger tracking for rendering, with prioriy as Grabable > PassThrough > RealHand </summary>
        /// <param name="realHandPose"></param>
        /// <param name="colliderPose"></param>
        /// <returns></returns>
        protected SG_HandPose GetLatestFingerPose(SG_HandPose realHandPose, SG_HandPose colliderPose)
        {
            if (this.grabScript != null && this.grabScript.isActiveAndEnabled && this.grabScript.ControlsFingerTracking())
            {
                SG_HandPose grabPose;
                this.grabScript.GetFingerTracking(realHandPose, this.GetHandModel(), out grabPose);
                return grabPose;
            }
            else if (this.projectionLayer != null && this.projectionLayer.isActiveAndEnabled && this.projectionLayer.useForAnimation)
            {
                SG_HandPose passPose = projectionLayer.GetConstrainedPose(colliderPose, this.handModel);
                if (passPose != null)
                {
                    return passPose;
                }
            }
            return colliderPose;
        }





        protected void UpdateRealHandPose()
        {
            if (this.RealHandSource != null) //if true, l_realHandPose has been assigned and can be used in other updates.
            {
                SG_HandPose nextPose;
                if (this.RealHandSource.GetHandPose(out nextPose)) //it's assigned, but we need to 
                {
                    l_realHandPose = nextPose;
                }
            }
            if (this.overrideWristLocation)
            {
                l_realHandPose.wristPosition = this.transform.position;
                l_realHandPose.wristRotation = this.transform.rotation;
            }
            // Update the tracking refrences for the real hand.
            SG_HandPoser3D.UpdatePoser(this.realHandPoser, l_realHandPose);
        }


        protected void UpdateVirtualPose() // is the real wold finger tracking + wrist determined by grab / physics logic.
        {
            if (l_realHandPose == null)
                return;

            // Wrist Position to use for the hand Physics
            Vector3 wristPosition = Vector3.zero;
            Quaternion wristRotation = Quaternion.identity;
            bool wristAssigned = false; //set this to true whenever you set the wristPosition/Rotation.

            if (this.grabScript != null && this.grabScript.isActiveAndEnabled)
            {
                if (this.grabScript.ControlsHandLocation())
                {
                    this.grabScript.GetHandLocation(l_realHandPose, out wristPosition, out wristRotation);
                    //Debug.Log("VP: GrabScript Source");
                    wristAssigned = true;
                }
            }
            // We're not grabbing on to anything, OR whatever we are grabbing does not influence hand location
            if (!wristAssigned && this.handPhysics != null && this.handPhysics.isActiveAndEnabled)
            {
                wristPosition = this.handPhysics.WristPosition;
                wristRotation = this.handPhysics.WristRotation;
                // Debug.Log("VP: Physics Source");
                wristAssigned = true;
            }
            // Neither Grabbing nor Physics is influencing the wrist position.
            if (!wristAssigned)
            {
                wristPosition = l_realHandPose.wristPosition;
                wristRotation = l_realHandPose.wristRotation;
                //Debug.Log("VP: RealHand Source");
            }
            l_virtualPose = SG_HandPose.Combine(wristPosition, wristRotation, l_realHandPose, true);
            SG_HandPoser3D.UpdatePoser(this.virtualHandPoser, l_virtualPose); //Finally, Update the poser
        }

        protected void UpdateRenderPose()
        {
            if (l_realHandPose == null || l_virtualPose == null)
                return;

            // Render the hand using the latest pose / overrides.
            SG_HandPose fingerTracking = GetLatestFingerPose(this.l_realHandPose, this.l_virtualPose);
            l_renderPose = SG_HandPose.Combine(l_virtualPose, fingerTracking, true); // combine finger tracking with the latest virtual pose's wrist

            SG_HandPoser3D.UpdatePoser(renderPoser, l_renderPose); //update transforms
            if (this.handAnimation != null && this.handAnimation.isActiveAndEnabled) //animate it if required.
            {
                this.handAnimation.UpdateHand(l_renderPose, false);
            }
        }





        /// <summary> Called during Update(). Updates the latest RealHandPose for FixedUpdate(), a few trigger colliders and renders the hand via the Animator. </summary>
        /// <param name="deltaTime"></param>
        protected void UpdateHandTracking(float deltaTime)
        {
            // Get the latest real hand pose.
            // UpdateRealHandPose();


            // Update the latest collider pose - since the wrist position updates on Physics Step, it will have changed since the PhysicsUpdate.
            //l_virtualPose = GetLatestColliderPose(l_realHandPose);
            //SG_HandPoser3D.UpdatePoser(this.virtualHandPoser, l_virtualPose);

            // Update any layers that need to match the colliderPose exactly
            // PassThrough Layer - Tracking only.
           
            // FFB colliders
            //if (this.feedbackLayer != null && this.feedbackLayer.isActiveAndEnabled)
            //{
            //    this.feedbackLayer.UpdateColliders();
            //}

            //UpdateRenderPose();
        }





        /// <summary> Called in FixedUpdate(). Updates the physics and logic of the hand. Manipulation, Physics Movement etc. Stores the ColliderPose. </summary>
        /// <param name="deltaTime"></param>
        protected void UpdateHandPhysics(float deltaTime)
        {
            if (l_realHandPose == null) //we do not update if we have not a single handPose. Redundant? Maybe.
            {
                return;
            }

            // FingerTracking to use for the hand Physics
            SG_HandPose fingerTracking = null;
            // Wrist Position to use for the hand Physics
            Vector3 wristPosition = Vector3.zero;
            Quaternion wristRotation = Quaternion.identity;
            bool wristAssigned = false; //set this to true whenever you set the wristPosition/Rotation.

            // Are we controlled by a grabbed / hovered object?
            if (this.grabScript != null && this.grabScript.isActiveAndEnabled)
            {
                this.grabScript.UpdateGrabLogic(deltaTime);
                this.grabScript.UpdateGrabbedObjects();

                if (grabScript.ControlsHandLocation())
                {
                    this.grabScript.GetHandLocation(this.l_realHandPose, out wristPosition, out wristRotation);
                    wristAssigned = true;
                }
                if (grabScript.ControlsFingerTracking())
                {
                    // then give me the finger pose
                    SGCore.Kinematics.BasicHandModel handDimension = this.GetHandModel();
                    this.grabScript.GetFingerTracking(l_realHandPose, handDimension, out fingerTracking);
                }
            }

            if (fingerTracking == null) //Finger tracking is NOT determined by something we're holding / hovering ver
            {
                if (this.projectionLayer != null && this.projectionLayer.isActiveAndEnabled && this.projectionLayer.useForAnimation)
                {
                    fingerTracking = projectionLayer.GetConstrainedPose(l_virtualPose, this.handModel);
                }
                else
                {
                    fingerTracking = l_realHandPose;
                }
                //if (this.passThoughLayer != null && this.passThoughLayer.isActiveAndEnabled && passThoughLayer.LatestPose != null) //try the passthrough layer
                //{
                //    fingerTracking = passThoughLayer.LatestPose; //this one is updated during Update, so should be up to date
                //}
                //else //no fingerPassThrough Layer. Use the real hand instead.
                //{
                //    fingerTracking = l_realHandPose;
                //}
            }

            // At this point, FingerTracking IS assigned. We might still be missing the wrist tracking, and the physics layer needs an update still.
            if (this.handPhysics != null && this.handPhysics.isActiveAndEnabled)
            {
                //For now, the target is either the grabbed obj or the real world location.
                SG_HandPose physicsTarget = wristAssigned ? SG_HandPose.Combine(wristPosition, wristRotation, fingerTracking, false) : SG_HandPose.Combine(l_realHandPose, fingerTracking, false);

                this.handPhysics.UpdateRigidbody(physicsTarget, deltaTime, true); //might as well update colliders in the same function (true)
                //if (!wristAssigned)
                //{
                //    wristPosition = handPhysics.WristPosition;
                //    wristRotation = handPhysics.WristRotation;
                //    wristAssigned = true;
                //}
            }

            //if (!wristAssigned) //just_in_case
            //{
            //    wristPosition = l_realHandPose.wristPosition;
            //    wristRotation = l_realHandPose.wristRotation;
            //    wristAssigned = true;
            //}

            //l_virtualPose = SG_HandPose.Combine(wristPosition, wristRotation, l_realHandPose, true);
            //SG_HandPoser3D.UpdatePoser(this.virtualHandPoser, l_virtualPose); //Update the pose
            ////I don't need to assign any render poses, since they'll be updated later
        }




        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // IHandPoseProvider functions


        /// <summary> Returns true if this SG_TrackedHand Script has been set up to track the right hand. </summary>
        /// <returns></returns>
        public bool TracksRightHand()
        {
            if (this.handModel != null) //by default, our 3D model should determine which hand it follows
            {
                return this.handModel.handSide != HandSide.LeftHand;
            }
            else if (this.RealHandSource != null) //in case we don;t have a 3D model to animate, we default to whatever our hand source is linekd to
            {
                this.RealHandSource.TracksRightHand();
            }
            throw new System.MissingMemberException("You're trying to find out which hand " + this.name + " tracks, but it's not linked to a 3D model or Tracking Input!");
        }


        /// <summary> Returns true if this script's realHandSource is linked and connected </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (RealHandSource != null)
            {
                return RealHandSource.IsConnected();
            }
            return false;
        }

        /// <summary> Set the Hand dimensions used by this script's HandPoseProvider. </summary>
        /// <param name="handModel"></param>
        public void SetKinematics(BasicHandModel handModel)
        {
            if (RealHandSource != null) { RealHandSource.SetKinematics(handModel); }
        }

        /// <summary> If this script has a realHandSource Assigned, we return the Hand Dimensions set to it. Otherwise, we return this script's HandModelInfo dimensions. If you want that one specifically, We recommend using GetHandModel instead. </summary>
        /// <returns></returns>
        public BasicHandModel GetKinematics()
        {
            return this.GetHandModel();
        }

        /// <summary> Returns the last Real Hand Pose determined by the realHandSource of this script. If you require something else, use the GetHandPose(TrackingLevel) instead. </summary>
        /// <param name="handPose"></param>
        /// <returns></returns>
        public bool GetHandPose(out SG_HandPose handPose, bool forceUpdate = false)
        {
            if (RealHandSource != null)
            {
                return RealHandSource.GetHandPose(out handPose, forceUpdate);
            }
            handPose = null;
            return false;
        }

        /// <summary> Return the last Normalized Flexion determined by the realHandSource of this script. If you require something else, use the GetHandPose(TrackingLevel, out handPose) instead. </summary>
        /// <param name="flexions"></param>
        /// <returns></returns>
        public bool GetNormalizedFlexion(out float[] flexions)
        {
            if (RealHandSource != null)
            {
                return RealHandSource.GetNormalizedFlexion(out flexions);
            }
            flexions = new float[5];
            return false;
        }


        /// <summary> Returns true if this script's realHandSource wishes to force Grab Behaviour. </summary>
        /// <returns></returns>
        public float OverrideGrab()
        {
            if (RealHandSource != null) { return RealHandSource.OverrideGrab(); }
            return 0;
        }

        /// <summary> Returns true if this script's realHandSource wishes to force Use Behaviour. </summary>
        /// <returns></returns>
        public float OverrideUse()
        {
            if (RealHandSource != null) { return RealHandSource.OverrideUse(); }
            return 0;
        }

        public HandTrackingDevice TrackingType()
        {
            if (RealHandSource != null) { return RealHandSource.TrackingType(); }
            return HandTrackingDevice.Unknown;
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            if (RealHandSource != null) { return RealHandSource.TryGetBatteryLevel(out value01); }
            value01 = -1.0f;
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // IHandFeedbackDevice functions

        /// <summary> Returns the name of this device's connected HapticHardware, if it is linked. </summary>
        /// <returns></returns>
        public string Name()
        {
            return this.name + "(" + (this.HapticHardware != null ? this.HapticHardware.Name() : "UNLINKED") + ")";
        }

        /// <summary> Send a Force-Feedback Command to this script's hapticHardware </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_FFBCmd ffb)
        {
            if (HapticHardware != null) { HapticHardware.SendCmd(ffb); }
        }

        /// <summary> Cease all vibrations on script's hapticHardware </summary>
        public void StopAllVibrations()
        {
            if (HapticHardware != null) { HapticHardware.StopAllVibrations(); }
        }

        /// <summary> Stop all haptics (vibration and force-feedback) on this script's hapticHardware </summary>
        public void StopHaptics()
        {
            if (HapticHardware != null) { HapticHardware.StopHaptics(); }
        }


        /// <summary> Send a command to the finger vibrotactile actuators, if any </summary>
        /// <param name="fingerCmd"></param>
        public void SendCmd(SGCore.Haptics.SG_TimedBuzzCmd fingerCmd)
        {
            if (HapticHardware != null) { HapticHardware.SendCmd(fingerCmd); }
        }

        /// <summary> Send a command to the Wrist vibrotactile actuators. </summary>
        /// <param name="wristCmd"></param>
        public void SendCmd(SGCore.Haptics.TimedThumpCmd wristCmd)
        {
            if (HapticHardware != null) { HapticHardware.SendCmd(wristCmd); }
        }

        /// <summary> Send an impact vibration to this script's hapticHardware. </summary>
        /// <param name="location"></param>
        /// <param name="normalizedVibration"></param>
        public void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            if (HapticHardware != null)
            {
                HapticHardware.SendImpactVibration(location, normalizedVibration);
            }
        }

        /// <summary> Sends a Waveform to this script's linked hardware. </summary>
        /// <param name="waveform"></param>
        public void SendCmd(SG_Waveform waveform)
        {
            if (HapticHardware != null)
            {
                HapticHardware.SendCmd(waveform);
            }
        }

        public void SendCmd(ThumperWaveForm waveform)
        {
            if (HapticHardware != null)
            {
                HapticHardware.SendCmd(waveform);
            }
        }

        public void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location)
        {
            if (this.HapticHardware != null)
            {
                this.HapticHardware.SendCmd(customWaveform, location);
            }
        }

        public bool FlexionLockSupported()
        {
            if (this.HapticHardware != null)
            {
                return this.HapticHardware.FlexionLockSupported();
            }
            return false;
        }

        public void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            if (this.HapticHardware != null)
            {
                this.HapticHardware.SetFlexionLocks(fingers, fingerFlexions);
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> Updates the TrackedHand layers based on the HandState of the linked glove. Called when the CalibrationStage changes. </summary>
        public void UpdateHandState()
        {
            //Debug.Log(this.name + ": HandState has changed to " + this.gloveHardware.CalibrationStage.ToString());
            if (this.calibration != null && this.calibration.linkedGlove != null)
            {
                if (this.statusIndicator != null)
                {
                    //Debug.Log(this.name + ": Have to change hand state to " + gloveHardware.CalibrationStage.ToString());
                    if (this.calibration.linkedGlove.IsConnected())
                    {
                        switch (this.calibration.linkedGlove.GetCalibrationStage())
                        {
                            case SGCore.Calibration.CalibrationStage.MoveFingers:
                                this.statusIndicator.SetMaterials(SG_HandStateIndicator.HandState.CheckRanges);
                                break;
                            case SGCore.Calibration.CalibrationStage.CalibrationNeeded:
                                this.statusIndicator.SetMaterials(SG_HandStateIndicator.HandState.CalibrationNeeded);
                                break;
                            case SGCore.Calibration.CalibrationStage.Calibrating:
                                this.statusIndicator.SetMaterials(SG_HandStateIndicator.HandState.Calibrating);
                                break;
                            default:
                                this.statusIndicator.SetMaterials(SG_HandStateIndicator.HandState.Default);
                                break;
                        }
                    }
                    else { this.statusIndicator.SetMaterials(SG_HandStateIndicator.HandState.Disconnected); }

                    //Update Text
                    if (calibration.linkedGlove != null && calibration.linkedGlove.GetCalibrationStage() == SGCore.Calibration.CalibrationStage.MoveFingers)
                    {
                        statusIndicator.WristText = "Please move\r\nyour fingers";
                    }
                    else
                    {
                        statusIndicator.WristText = "";
                    }
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Ensures this script has Posers so that independent scripts can ask for things on Start
        void Awake()
        {
            Setup();
        }

        //  Linking at Start instead of Awake so the other scripts have time to setup in Awake.
        void Start()
        {
            UpdateHandState();
        }

        // Handle Physics Behaviours during FixedUpdate
        void FixedUpdate()
        {
            //UpdatePhsics should be fine
            UpdateRealHandPose();
            UpdateVirtualPose();
            if (projectionLayer != null && projectionLayer.isActiveAndEnabled)
            {
                projectionLayer.UpdateProjections(l_virtualPose);
            }
            UpdateHandPhysics(Time.fixedDeltaTime);
            //l_virtualPose = l_realHandPose; //Supposedly, this is the only time I _can_ update it...
            //UpdateVirtualPose();
        }

        // And all trigger behaviours in Update
        // Update creates the smoothest experience
        void Update()
        {
            // UpdateHandTracking(Time.deltaTime);
            UpdateRealHandPose();
            // UpdateHandPhysics(Time.deltaTime);
            UpdateVirtualPose();
            UpdateHandTracking(Time.deltaTime);
            UpdateRenderPose();
        }

        void LateUpdate()
        {
            // UpdateRealHandPose();
        }

        void OnDisable()
        {
            LinkCalibrationEvents();
        }

        void OnEnabled()
        {
            UnlinkCalibrationEvents();
        }

        void OnApplicationQuit()
        {
            StopHaptics();
        }

#if UNITY_EDITOR

        //Toggle Debugs during play in the editor.
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateDebugLines();
            }
        }
#endif


    }
}