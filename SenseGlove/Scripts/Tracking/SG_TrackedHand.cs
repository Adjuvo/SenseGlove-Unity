using UnityEngine;

namespace SG
{
    /// <summary> A hand model with different layers, that follows a GameObject with a configurable offset </summary>
    public class SG_TrackedHand : MonoBehaviour
    {

        /// <summary> The hardware this hand is tracked with. Used to calculate offsets. </summary>
        public enum TrackingHardware
        {
            /// <summary> Custom tracking hardware is used, so offsets are calculated during Start(). </summary>
            Custom,
            /// <summary> SenseGlove Vive Tracker Mount </summary>
            ViveTracker,
            /// <summary> Oculus Rift S Controller using 3D Printed SG Mount; trackedObject should be the Left/RightControllerAnchor in the OVRCameraRig plugin. </summary>
            RiftSController
        }

        /// <summary> The way the tracking is estableshed. </summary>
        public enum TrackingMethod
        {
            /// <summary> The hand matches the trackedObject's position and rotations, with offsets. </summary>
            Default,
            /// <summary> The hand gets a rigidbody, which attempts to reach its targetRotation and -position </summary>
            PhysicsBased,
            /// <summary> This script does not handle any tracking. Use this when making the hand a child of your trackedObject. </summary>
            Disabled
        }

        /// <summary> The hand tracking hardware used to animae / link this TrackedHand. </summary>
        public SG_SenseGloveHardware hardware;

        /// <summary> The object that this script will attempt to follow. </summary>
        [SerializeField] protected Transform trackedObject;

        /// <summary> The hardware that controls the trackedObject's position. Used to calultae offsets. </summary>
        public TrackingHardware trackingHardware = TrackingHardware.ViveTracker;
        /// <summary> How the position of this TrackedHand is determined. </summary>
        public TrackingMethod trackingMethod = TrackingMethod.Default;

        /// <summary> Information of the 3D model of the hand this script represents. </summary>
        public SG_HandModelInfo handModel;

        /// <summary> The script that animates this trackedHand </summary>
        [Header("Hand Layers")]
        public SG_HandAnimator handAnimation;
        /// <summary> The script responsble for collecting force-feedback from objects to this hardware. </summary>
        public SG_HandFeedback feedbackScript;
        /// <summary> The script responsible for grabbing and manipulating objects. </summary>
        public SG_GrabScript grabScript;
        /// <summary> The script that allows this hand to push objects away. </summary>
        public SG_HandRigidBodies rigidBodyLayer;
        /// <summary> The script that prevents this hand from passing through non-trigger colliders. </summary>
        public SG_HandRigidBodies physicsTrackingLayer;

        /// <summary> If set to true, this hand will ignore collisions with SG_Interactable objects that its rigidbody collides with.</summary>
        /// <remarks> The PhysicsTrackingLayer bodies have no rigidbodies of their own, and so their OnCollisionEnter events fire here. </remarks>
        protected bool ignoreGrabables = false;

        /// <summary> The position offset between this trackedHand and its trackedObject. </summary>
        protected Vector3 positionOffset = Vector3.zero;
        /// <summary> The rotation offset between this trackedHand and its trackedObject. </summary>
        protected Quaternion rotationOffset = Quaternion.identity;

        /// <summary> This object's Rigidbody, used when dealing with Physics-based tracking. </summary>
        protected Rigidbody handRB = null;
        /// <summary> The rotation speed of the Rigidbody, when using Physics-based tracking. </summary>
        protected static float physRotationSpeed = 25;


        /// <summary> The position that this trackedHand should be in, based on its trackedObject and offsets. </summary>
        public Vector3 TargetPosition
        {
            get { return trackedObject != null ? trackedObject.transform.position + (this.trackedObject.rotation * positionOffset) : this.transform.position; }
        }

        /// <summary> The rotation that this trackedHand should be in, based on its trackedObject and offsets. </summary>
        public Quaternion TargetRotation
        {
            get { return trackedObject != null ? trackedObject.transform.rotation * rotationOffset : this.transform.rotation; }
        }


        /// <summary> Returns true if this Script is set up to track a right hand. </summary>
        /// <returns></returns>
        public virtual bool TracksRightHand
        {
            get
            {
                if (hardware != null)
                {
                    if (Application.isPlaying && hardware.GloveReady)
                    {
                        return this.hardware.IsRight;
                    }
                    else
                    {   //we're in the editor, so lets base if off the SG_SenseGloveHardware
                        return this.hardware.connectionMethod != SG_SenseGloveHardware.ConnectionMethod.NextLeftHand;
                    }
                }
                return this.name.ToLower().Contains("right"); //last ditch effort
            }
        }


        /// <summary> Link relevant scripts to this trackedHand, if they have not been assinged yet. </summary>
        protected void CheckForScripts()
        {
            if (this.hardware == null) { this.hardware = this.gameObject.GetComponent<SG_SenseGloveHardware>(); }
            if (this.handModel == null) { this.handModel = this.GetComponentInChildren<SG_HandModelInfo>(); }

            if (this.grabScript == null) { this.grabScript = this.GetComponentInChildren<SG_GrabScript>(); }
            if (this.feedbackScript == null) { this.feedbackScript = this.GetComponentInChildren<SG_HandFeedback>(); }
            if (this.handAnimation == null) { this.handAnimation = this.GetComponentInChildren<SG_HandAnimator>(); }

            //Since both RB and PhysicsTrackingLayers have the same component, assing whichever one we haven't done yet.
            if (this.rigidBodyLayer == null || this.physicsTrackingLayer == null)
            {
                SG_HandRigidBodies[] components = this.GetComponentsInChildren<SG_HandRigidBodies>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (this.rigidBodyLayer == null  //we don't yet have a RigidBody Layer
                        && (this.physicsTrackingLayer == null || !GameObject.ReferenceEquals(this.physicsTrackingLayer.gameObject, components[i].gameObject)))
                    {
                        rigidBodyLayer = components[i];
                    }
                    if (this.physicsTrackingLayer == null  //we don't yet have a RigidBody Layer
                        && (this.rigidBodyLayer == null || !GameObject.ReferenceEquals(this.rigidBodyLayer.gameObject, components[i].gameObject)))
                    {
                        physicsTrackingLayer = components[i];
                    }
                }
            }
        }

        /// <summary> Setup and/or change the tracking variables of this hand. </summary>
        /// <param name="newTarget"></param>
        /// <param name="trackType"></param>
        /// <param name="trackMethod"></param>
        /// <param name="rightHand"></param>
        protected virtual void SetupTracking(Transform newTarget, TrackingHardware trackType, TrackingMethod trackMethod, bool rightHand)
        {
            this.trackedObject = newTarget;
            this.trackingHardware = trackType;
            this.trackingMethod = trackMethod;

            //Calculate appropriate offsets
            if (trackedObject != null)
            {
                if (trackingHardware != TrackingHardware.Custom)
                {
                    int LR = rightHand ? 1 : -1;
                    Vector3 posOffs = Vector3.zero;
                    Quaternion rotOffs = Quaternion.identity;
                    if (trackingHardware == TrackingHardware.ViveTracker)
                    {
                        posOffs = new Vector3(0.01f * LR, 0.04f, -0.085f);
                        rotOffs = Quaternion.Euler(0, -90, -90);
                        Debug.Log("Setting up " + (rightHand ? "right" : "left") + " hand for Vive Tracker");
                    }
                    else if (trackingHardware == TrackingHardware.RiftSController)
                    {
                        posOffs = new Vector3(LR*-0.03f, -0.125f, -0.065f);
                        rotOffs = Quaternion.Euler(0, -90, 20);
                        Debug.Log("Setting up " + (rightHand ? "right" : "left") + " hand for Oculus Rift S Controller");
                    }
                    this.positionOffset = posOffs;
                    this.rotationOffset = rotOffs;
                }
                else
                {
                    SG_Util.CalculateOffsets(this.transform, this.trackedObject, out this.positionOffset, out this.rotationOffset);
                }
            }

            //Ignore collisions of the hand itself
            if (this.rigidBodyLayer != null)
            {
                if (this.physicsTrackingLayer != null) { this.rigidBodyLayer.SetIgnoreCollision(this.physicsTrackingLayer, true); }
                if (this.feedbackScript != null) { this.feedbackScript.SetIgnoreCollision(this.rigidBodyLayer, true); }
            }
            if (this.feedbackScript != null && this.physicsTrackingLayer != null)
            {
                this.feedbackScript.SetIgnoreCollision(this.physicsTrackingLayer, true);
            }

            //Apply the appropriate RB method
            if (trackingMethod == TrackingMethod.PhysicsBased && physicsTrackingLayer != null)
            {
                physicsTrackingLayer.RemoveRigidBodies();
                this.handRB = SG_Util.TryAddRB(this.gameObject, false, false);
                ignoreGrabables = true;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(true); }
            }
            else
            {
                ignoreGrabables = false;
                SG_Util.TryRemoveRB(this.gameObject); //should prevent calling OnCollisionEnter
                this.handRB = null;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(false); }
            }
        }


        /// <summary> Swap the tracking targets between this hand an another one. </summary>
        /// <param name="otherHand"></param>
        public virtual void SwapTracking(SG_TrackedHand otherHand)
        {
            if (otherHand != null)
            {
                Transform myTrackedObject = this.trackedObject;
                this.trackedObject = otherHand.trackedObject;
                otherHand.trackedObject = myTrackedObject;
            }
        }



        /// <summary> Update this script's transform by applying a position and rotation directly. </summary>
        public void UpdateTransformDefault()
        {
            if (trackedObject != null)
            {
                this.transform.rotation = TargetRotation;
                this.transform.position = TargetPosition;
            }
        }

        /// <summary> Update this script's transform by applying a velocity to its rigidbody. </summary>
        public void UpdateTransformPhysics()
        {
            if (this.trackedObject != null && this.handRB != null)
            {
                SG_Util.TransformRigidBody(ref this.handRB, this.TargetPosition, this.TargetRotation, physRotationSpeed);
            }
        }




        protected virtual void Awake()
        {
            CheckForScripts();
        }

        protected virtual void Start()
        {
            if (this.handAnimation != null && trackingHardware != TrackingHardware.Custom) { handAnimation.updateWrist = false; }
            SetupTracking(this.trackedObject, this.trackingHardware, this.trackingMethod, this.TracksRightHand);

            if (this.handAnimation != null) { this.handAnimation.senseGlove = this.hardware; }
            if (this.grabScript != null) { this.grabScript.hardware = this.hardware; }
            if (this.feedbackScript != null) { this.feedbackScript.connectedGlove = this.hardware; }

        }


        protected void Update()
        {
            if (this.trackingMethod == TrackingMethod.Default)
            {
                UpdateTransformDefault();
            }
        }

        protected void FixedUpdate()
        {
            if (this.trackingMethod == TrackingMethod.PhysicsBased)
            {
                UpdateTransformPhysics();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            //If we have a Physics layer, it's colliders, without RB, will pass on their OnCollisionEnter to this Object
            //If we have no Physics Layer, this function will not even trigger.
            if (ignoreGrabables)
            {
                SG_Interactable interactable;
                if (SG_HoverCollider.GetInteractableScript(collision.collider, out interactable))
                {
                    this.physicsTrackingLayer.SetIgnoreCollision(collision.collider, true);
                }
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            CheckForScripts();
        }
#endif

    }
}