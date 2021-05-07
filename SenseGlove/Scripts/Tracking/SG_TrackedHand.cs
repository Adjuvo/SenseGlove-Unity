using JetBrains.Annotations;
using SGCore.Kinematics;
using System;
using UnityEngine;

namespace SG
{

    
    /// <summary> A hand model with different layers, that follows one of the hands in the GloveList </summary>
    public class SG_TrackedHand : MonoBehaviour
    {
        //----------------------------------------------------------------------------------------------
        // Tracking Method Enum

        /// <summary> The way this TrackedHand follows its TargetPosition </summary>
        public enum TrackingMethod
        {
            /// <summary> The hand matches the trackedObject's position and rotations, with offsets. </summary>
            Default,
            /// <summary> The hand gets a rigidbody, which attempts to reach its targetRotation and -position </summary>
            PhysicsBased,
            /// <summary> This script does not handle any tracking. Use this when making the hand a child of your trackedObject. </summary>
            Disabled
        }

        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The SG_HapticGlove that this TrackedHand pulls its data from. </summary>
        [Header("Hand Tracking")]
        public SG_HapticGlove gloveHardware;

        /// <summary> The Object that we're trying to track the position / rotation of. Can be auto-assinged via the SG_User script. </summary>
        [Header("Positional Tracking")]
        public Transform trackedObject;
        /// <summary> The tracking hardware used to determine the TrackedObject' position. Can be auto-assinged via the SG_User script. </summary>
        public SGCore.PosTrackingHardware trackingHardware = SGCore.PosTrackingHardware.ViveTracker;

        /// <summary> How the position of this TrackedHand is determined. </summary>
        public TrackingMethod trackingMethod = TrackingMethod.Default;


        [Header("Hand Layers")]
        /// <summary> Information of the 3D model of the hand this script represents. </summary>
        public SG_HandModelInfo handModel;
        /// <summary> The script that animates this trackedHand </summary>
        public SG_HandAnimator handAnimation;
        /// <summary> The script responsble for collecting force-feedback from objects to this hardware. </summary>
        public SG_HandFeedback feedbackScript;
        /// <summary> The script responsible for grabbing and manipulating objects. </summary>
        public SG_GrabScript grabScript;
        /// <summary> The script that allows this hand to push objects away. </summary>
        public SG_HandRigidBodies rigidBodyLayer;
        /// <summary> The script that prevents this hand from passing through non-trigger colliders. </summary>
        public SG_HandRigidBodies physicsTrackingLayer;
        /// <summary> The script that prevents this hand from passing through non-trigger colliders. </summary>
        public SG_GestureLayer gestureLayer;
        /// <summary> A visual indication of the hand state. </summary>
        public SG_HandStateIndicator statusIndicator;
        /// <summary> A script to handle calibration of a HapticGlove. </summary>
        public SG_CalibrationSequence calibration;

        // Internal properties 

        /// <summary> If set to true, this hand will ignore collisions with SG_Interactable objects that its rigidbody collides with.</summary>
        /// <remarks> The PhysicsTrackingLayer bodies have no rigidbodies of their own, and so their OnCollisionEnter events fire here. </remarks>
        protected bool ignoreGrabables = false;
        
        /// <summary> Whether or not this glove was connected when we last checked. </summary>
        protected bool wasConnected = false;

        /// <summary> The position offset between this trackedHand and its trackedObject. </summary>
        protected Vector3 customPosOffset = Vector3.zero;
        /// <summary> The rotation offset between this trackedHand and its trackedObject. </summary>
        protected Quaternion customRotOffset = Quaternion.identity;

        /// <summary> This object's Rigidbody, used when dealing with Physics-based tracking. </summary>
        protected Rigidbody handRB = null;
        /// <summary> The rotation speed of the Rigidbody, when using Physics-based tracking. </summary>
        protected static float physRotationSpeed = 25;

        /// <summary> Whether or not we still need to grab a Wirst location this frame. </summary>
        protected bool wristThisFrame = true;
        /// <summary> The last wrist position determined </summary>
        protected Vector3 lastWristPosition = Vector3.zero;
        /// <summary> The last wrist rotation determined </summary>
        protected Quaternion lastWristRotation = Quaternion.identity;

        /// <summary> Distance in m, before the hand snaps to the targer Position / Rotation </summary>
        public static float handSnapDist = 1.0f;
        /// <summary> If the hand too far from the targetPosition (handSnapDist), it will reset after this time. </summary>
        public static float handSnapTime = 0.5f;
        /// <summary> Individual timer to keep track if the hand needs to snap to the targetPosition </summary>
        protected float snapTimer = 0;


        //----------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Is true if this TrackedHand can determine its position using SenseGlove methods. </summary>
        public bool HasPositionalTracking
        {
            get; protected set;
        }


        /// <summary> Enables / Disables all TrackedHand  functionalities. </summary>
        public bool HandEnabled
        {
            get 
            {
                bool handModel = this.handModel != null && this.handModel.gameObject.activeSelf;
                return handModel;
            }
            set 
            {
                if (this.handModel != null) { this.handModel.gameObject.SetActive(value); }
            }
        }

        /// <summary> Returns true if this Script is set up to track a right hand. </summary>
        /// <returns></returns>
        public virtual bool TracksRightHand
        {
            get
            {
                return this.handModel != null
                    && (this.handModel.handSide != HandSide.LeftHand);
                //return this.hardware != null && this.hardware.hand == GloveSide.Right;
            }
        }

        //----------------------------------------------------------------------------------------------
        // Hand Tracking Methods

        /// <summary> Returns the HandPose from the Glove Hardware linked to this TrackedHand. </summary>
        /// <param name="hand"></param>
        /// <param name="pose"></param>
        /// <returns></returns>
        public virtual bool GetHandPose(out SG_HandPose pose)
        {
            if (this.gloveHardware != null && this.handModel != null)
            {
                SGCore.Kinematics.BasicHandModel HM = handModel.HandKinematics;
                //Debug.Log("TrackedHand: " + HM.ToString());
                return this.gloveHardware.GetHandPose(this.handModel.HandKinematics, out pose, true);
            }
            pose = SG_HandPose.Idle(this.TracksRightHand);
            return false;
        }

        /// <summary> Returns the IMU rotation of the Glove Hardware linked to this TrackedHand. </summary>
        /// <returns></returns>
        public virtual Quaternion GetIMURotation()
        {
            Quaternion res = Quaternion.identity;
            if (this.gloveHardware != null && gloveHardware.GetIMURotation(out res))
            {
                return res;
            }
            return res;
        }

        /// <summary> Retrieve the finger flexions for the linked GloveHarware. </summary>
        /// <param name="flexions"></param>
        /// <returns></returns>
        public virtual bool GetNormalizedFlexions(out float[] flexions)
        {
            if (this.gloveHardware != null && this.handModel != null)
            {
                return this.gloveHardware.GetNormalizedFlexion(out flexions);
            }
            flexions = new float[5];
            return false;
        }



        //----------------------------------------------------------------------------------------------
        // Positional Tracking Methods


        /// <summary> The position that this trackedHand should be in, based on its trackedObject and offsets. </summary>
        public virtual Vector3 TargetPosition
        {
            get
            {
                UpdateWristLocation();
                return lastWristPosition;
            }
        }

        /// <summary> The rotation that this trackedHand should be in, based on its trackedObject and offsets. </summary>
        public virtual Quaternion TargetRotation
        {
            get
            {
                UpdateWristLocation();
                return lastWristRotation;
            }
        }

        /// <summary> Access both target position and rotation of the hand </summary>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        /// <returns></returns>
        public void GetTargets(out Vector3 targetPosition, out Quaternion targetRotation)
        {
            UpdateWristLocation();
            targetPosition = this.lastWristPosition;
            targetRotation = this.lastWristRotation;
        }

        /// <summary> Updates the location of the TrackedHand, based on it's TrackedObject and -Hardware. </summary>
        protected void UpdateWristLocation()
        {
            if (wristThisFrame)
            {
                if (this.trackingHardware == SGCore.PosTrackingHardware.Custom)
                {
                    HasPositionalTracking = this.trackedObject != null;
                    if (HasPositionalTracking)
                    {
                        this.lastWristRotation = SG.Util.SG_Util.CalculateTargetRotation(this.trackedObject, this.customRotOffset);
                        this.lastWristPosition = SG.Util.SG_Util.CalculateTargetPosition(this.trackedObject, this.customPosOffset, this.customRotOffset);
                        //Debug.Log("Updating hand with Custom! Offsets: " + customPosOffset.ToString() + ", " + customRotOffset.eulerAngles.ToString());
                    }
                }
                else if (this.gloveHardware != null)
                {
                    HasPositionalTracking = gloveHardware.GetWristLocation(this.trackedObject, this.trackingHardware, out this.lastWristPosition, out this.lastWristRotation);
                }
                else
                {
                    lastWristPosition = this.trackedObject != null ? trackedObject.position : Vector3.zero;
                    lastWristRotation = this.trackedObject != null ? trackedObject.rotation : Quaternion.identity;
                    HasPositionalTracking = false;
                }
                wristThisFrame = false;
            }
        }



        /// <summary> Setup and/or change the tracking variables of this hand. </summary>
        /// <param name="newTarget"></param>
        /// <param name="trackType"></param>
        /// <param name="trackMethod"></param>
        /// <param name="rightHand"></param>
        public virtual void SetTrackingMethod(TrackingMethod trackMethod)
        {
            this.trackingMethod = trackMethod;

            //Ignore collisions of the hand itself
            if (this.rigidBodyLayer != null)
            {
                if (this.physicsTrackingLayer != null) { this.physicsTrackingLayer.SetIgnoreCollision(this.rigidBodyLayer, true); }
                if (this.feedbackScript != null) { this.feedbackScript.SetIgnoreCollision(this.rigidBodyLayer, true); }
                if (this.grabScript != null) { this.grabScript.SetIgnoreCollision(this.rigidBodyLayer, true); }
            }
            if (this.physicsTrackingLayer != null)
            {
                if (this.rigidBodyLayer != null) { this.rigidBodyLayer.SetIgnoreCollision(this.physicsTrackingLayer, true); }
                if (this.feedbackScript != null) { this.feedbackScript.SetIgnoreCollision(this.physicsTrackingLayer, true); }
                if (this.grabScript != null) { this.grabScript.SetIgnoreCollision(this.physicsTrackingLayer, true); }
            }

            //Apply the appropriate RB method
            if (trackingMethod == TrackingMethod.PhysicsBased && physicsTrackingLayer != null)
            {
                physicsTrackingLayer.RemoveRigidBodies();
                this.handRB = SG.Util.SG_Util.TryAddRB(this.gameObject, false, false);
                ignoreGrabables = true;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(true); }
            }
            else
            {
                ignoreGrabables = false;
                SG.Util.SG_Util.TryRemoveRB(this.gameObject); //should prevent calling OnCollisionEnter
                this.handRB = null;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(false); }
            }
        }

        /// <summary> Update the TrackedObject of this TrackedHand </summary>
        /// <param name="trackedObj"></param>
        public virtual void SetTrackingHardware(Transform trackedObj)
        {
            SetTrackingHardware(trackedObject, this.trackingHardware);
        }

        /// <summary> Update the TrackedObject and -Hardware of this TrackedHand </summary>
        /// <param name="trackedObj"></param>
        /// <param name="hardware"></param>
        public virtual void SetTrackingHardware(Transform trackedObj, SGCore.PosTrackingHardware hardware)
        {
            this.trackedObject = trackedObj;
            this.trackingHardware = hardware;
            //Calculate custom offstes if these are used.
            if (trackedObject != null)
            {
                SG.Util.SG_Util.CalculateOffsets(this.transform, this.trackedObject, out this.customPosOffset, out this.customRotOffset);
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
        public virtual void UpdateTransformDefault()
        {
            if (this.trackedObject != null)
            {
                this.transform.rotation = TargetRotation;
                this.transform.position = TargetPosition;
            }
        }

        /// <summary> Update this script's transform by applying a velocity to its rigidbody. </summary>
        public virtual void UpdateTransformPhysics()
        {
            if (this.trackedObject != null && this.handRB != null)
            {
                Vector3 targetPos = this.TargetPosition;
                if ((this.handRB.position - targetPos).magnitude >= handSnapDist)
                {
                    snapTimer += Time.fixedDeltaTime;
                    if (snapTimer >= handSnapTime)
                    {
                        Debug.Log("Snapping hand because it was too far away (> " + handSnapDist + "m for " + handSnapTime + "s.");
                        snapTimer = 0;
                        this.transform.rotation = TargetRotation;
                        this.transform.position = TargetPosition;
                    }
                    else
                    {
                        SG.Util.SG_Util.TransformRigidBody(ref this.handRB, targetPos, this.TargetRotation, physRotationSpeed);
                    }
                }
                else
                {
                    snapTimer = 0;
                    SG.Util.SG_Util.TransformRigidBody(ref this.handRB, targetPos, this.TargetRotation, physRotationSpeed);
                }
            }
        }



        //----------------------------------------------------------------------------------------------
        // Utility Scripts


        /// <summary> Ensure hand layers don't collide with one another. </summary>
        protected virtual void SetupCollisions()
        {
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
                this.handRB = SG.Util.SG_Util.TryAddRB(this.gameObject, false, false);
                ignoreGrabables = true;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(true); }
            }
            else
            {
                ignoreGrabables = false;
                SG.Util.SG_Util.TryRemoveRB(this.gameObject); //should prevent calling OnCollisionEnter
                this.handRB = null;
                if (physicsTrackingLayer != null) { physicsTrackingLayer.gameObject.SetActive(false); }
            }
        }


        /// <summary> Link relevant scripts to this trackedHand, if they have not been assinged yet. </summary>
        protected void CheckForScripts()
        {
            //if (this.hardware == null) { this.hardware = this.gameObject.GetComponent<SG_HapticGlove>(); }
            if (this.handModel == null) { this.handModel = this.GetComponentInChildren<SG_HandModelInfo>(); }

            if (this.grabScript == null) { this.grabScript = this.GetComponentInChildren<SG_GrabScript>(); }
            if (this.feedbackScript == null) { this.feedbackScript = this.GetComponentInChildren<SG_HandFeedback>(); }
            if (this.handAnimation == null) { this.handAnimation = this.GetComponentInChildren<SG_HandAnimator>(); }
            if (this.gestureLayer == null) { this.gestureLayer = this.GetComponentInChildren<SG_GestureLayer>(); }
            if (this.statusIndicator == null) { this.statusIndicator = this.GetComponentInChildren<SG_HandStateIndicator>(); }

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

        /// <summary> Updates the TrackedHand layers based on the HandState of the linked glove. Called when the CalibrationStage changes. </summary>
        public void UpdateHandState()
        {
            if (gloveHardware != null)
            {
                if (this.statusIndicator != null)
                {
                    //Debug.Log(this.name + ": Have to change hand state to " + gloveHardware.CalibrationStage.ToString());
                    if (gloveHardware.IsConnected)
                    {
                        switch (this.gloveHardware.CalibrationStage)
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
                    if (gloveHardware != null && gloveHardware.CalibrationStage == SGCore.Calibration.CalibrationStage.MoveFingers)
                    {
                        statusIndicator.WristText = "Please move\r\nyour fingers";
                    }
                    else
                    {
                        statusIndicator.WristText = "";
                    }
                }
                //Update Animaytion
                if (this.handAnimation != null)
                {
                    if (this.gloveHardware.CalibrationStage == SGCore.Calibration.CalibrationStage.Done)
                    {
                        //Debug.Log("Re-enabled Hand Tracking as calibration works again");
                        this.handAnimation.enabled = true;
                    }
                    else
                    {
                        //Debug.Log("Disabled hand animation for a reason");
                        this.handAnimation.enabled = false; //diable automated animation
                        this.handAnimation.UpdateHand(SG_HandPose.Idle(this.TracksRightHand));
                    }
                }
            }
        }


        //----------------------------------------------------------------------------------------------
        // Monobehaviour


        protected virtual void OnEnable()
        {
            if (this.gloveHardware != null) 
            {
                this.gloveHardware.CalibrationStateChanged.AddListener(UpdateHandState);
                this.gloveHardware.DeviceConnected.AddListener(UpdateHandState);
            }
        }

        protected virtual void OnDisable()
        {
            this.gloveHardware.CalibrationStateChanged.RemoveListener(UpdateHandState);
            this.gloveHardware.DeviceConnected.RemoveListener(UpdateHandState);
        }

        protected virtual void Awake()
        {
            CheckForScripts();
        }

        protected virtual void Start()
        {
            SetTrackingHardware(this.trackedObject);
            SetTrackingMethod(this.trackingMethod);
            SetupCollisions();
            //UpdateHandState();
        }

        protected virtual void Update()
        {
            if (this.trackingMethod == TrackingMethod.Default)
            {
                UpdateTransformDefault();
            }

            if (this.gloveHardware != null)
            {
                bool isConnected = this.gloveHardware.IsConnected;
                if (isConnected && !wasConnected)
                {
                    //glove hardware is connected for this frame
                    if (this.handAnimation != null)
                    {
                        this.handAnimation.CalibrateWrist();
                        this.SetTrackingHardware(this.trackedObject);
                    }
                }
                wasConnected = isConnected;
            }
        }

        protected void FixedUpdate()
        {
            if (this.trackingMethod == TrackingMethod.PhysicsBased)
            {
                UpdateTransformPhysics();
            }
        }

        protected void LateUpdate()
        {
            wristThisFrame = true;
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

    }
}