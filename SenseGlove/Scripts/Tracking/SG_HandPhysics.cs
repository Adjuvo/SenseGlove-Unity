using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Used to animate / update / link colliders to a HandPoser </summary>
    public class SG_HandPhysics : SG_HandComponent
    {
        //--------------------------------------------------------------------------------------------------------------------
        // Hand Link Utility Class

        /// <summary> Minor utility class to identify this hand's bones, even as their parenting changes. </summary>
        public class SG_HandLink : MonoBehaviour
        {
            /// <summary> The Hand Physics Layer this collider is linked to. </summary>
            public SG_HandPhysics linkedHand;
        }

        //--------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The PhysicsBody of the hand - Used for movement. </summary>
        [Header("HandPhysics Components")]
        public Rigidbody handRigidbody;

        /// <summary> All colliders that are a part of the Wrist. </summary>
        public Collider[] wristColliders = new Collider[0];

        /// <summary> Colliders to be made part of the fingers. </summary>
        public CapsuleCollider[] fingerColliders = new CapsuleCollider[0];

        /// <summary> Whether or not to autmatically scale the finger colliders to match that of the hand. </summary>
        public bool scaleFingerColliders = true;

        /// <summary> Optional text element to report the hand's distance and snapBack timing. </summary>
        public TextMesh debugElement;

        /// <summary> Speed (m/s) with which the hand moves towards the target location </summary>
        [Header("Movement Parameters")]
        public float handMoveSpeed = 5;
        /// <summary> Speed (deg/s) with which the hand rotates towards the target location  </summary>
		public float handRotationSpeed = 60;
        /// <summary> How the hand is translated </summary>
        public Util.TranslateMode RBTranslation = Util.TranslateMode.ImprovedVelocity;
        /// <summary> Whether or not to set the velocity of the hand to 0 before or after moving it. </summary>
        public bool zeroVelocity = false;
        /// <summary> How the hand is rotated </summary>
        public Util.RotateMode RBRotation = Util.RotateMode.OfficialMoveRotation;
        /// <summary> Whether or not to set the angular velocity of the hand to 0 before or after moving it </summary>
        public bool zeroAngularVelocity = false;

        /// <summary> The distance (in meter) the physics hand is allowed to be from the real hand before we start snapping back </summary>
        public float snapBackDist = 0.5f;
        /// <summary> When the hand is more than snapBackDist away from the real hand for this amount of time, we disable collision for a bit. </summary>
        public float snapBackTime = 0.5f;
        /// <summary> Time since we've ben snapBackDist away from our target. </summary>
        protected float snapTimer = 0;


        /// <summary> The time after which to re-enable physics. Stored as a variable so it can be customized by function call. </summary>
        protected float reEnableAfter = 1.0f;
        /// <summary> The time since we've disabled collision </summary>
        protected float timer_enablePhysics = 0;

        /// <summary> An auto-generated special GameObject that will contain all the hand bones. Used to move all colliders between this RigidBody and that of Grabable objects. </summary>
        protected Transform colliderContainer;
        /// <summary> When a Collider is a child of a GameObject with this name, it's part of our hand. </summary>
        public static string handBonesTag = "sgBones";

        /// <summary> Tracking Scripts to update finger positioning and offsets. </summary>
        protected SG_SimpleTracking[] fingerTracking = new SG_SimpleTracking[0];

        /// <summary> Distance the hand must be from another objetc before collsion kicks back in. </summary>
        protected float uncollisionDist_sqrd = 0.1f * 0.1f;

        //--------------------------------------------------------------------------------------------------------------------
        // Utility Functions

        /// <summary> Safely report Debug information to this script's debugElement. </summary>
        public string DebugText
        {
            get { return debugElement != null ? debugElement.text : ""; }
            set { if (debugElement != null) { debugElement.text = value; } }
        }

        /// <summary> Returns true if Physics Layers are used inside this project. Enables / Disables warnings. </summary>
        public static bool ActiveInProject
        {
            get; private set;
        }

        /// <summary> Returns the current wrist position of this physics layer - as determined by the RealHandPose and this layer's movement settings. </summary>
        public Vector3 WristPosition
        {
            get { return this.handRigidbody.position; }
        }

        /// <summary> Returns the current wrist rotation of this physics layer - as determined by the RealHandPose and this layer's movement settings. </summary>
        public Quaternion WristRotation
        {
            get { return this.handRigidbody.rotation; }
        }


        /// <summary> Collects offsets between a physicsPoser and a series of tracking scripts, and ensures these don't update by themselves. </summary>
        /// <remarks> In a separate function because I'd use them for both wrist and finger colliders. </remarks>
        /// <param name="physicsPoser"></param>
        /// <param name="trackingScripts"></param>
        protected static void SetTargets(SG_HandPoser3D physicsPoser, SG_SimpleTracking[] trackingScripts)
        {
            for (int i = 0; i < trackingScripts.Length; i++)
            {
                Transform target = physicsPoser.GetTransform(trackingScripts[i].linkMeTo);
                trackingScripts[i].SetTrackingTarget(target, true);
                trackingScripts[i].updateTime = SG_SimpleTracking.UpdateDuring.Off;
            }
        }

        /// <summary> Attempt to collect a TrackedHand from a collider through it's HandPhysics Colliders. Used by HandDetector Scripts. </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public static bool TryGetLinkedHand(Collider col, out SG_TrackedHand hand)
        {
            Transform parent = col.transform.parent;
            if (parent != null && parent.name == handBonesTag) //this is once of our handBone colliders.
            {
                SG_HandLink link = parent.GetComponent<SG_HandLink>(); //this should not be null
                if (link != null && link.linkedHand != null) //but I'm paranoid so we're still checking
                {
                    hand = link.linkedHand.TrackedHand;
                    return hand != null;
                }
            }
            //if (col.transform.parent != null && col.transform.parent.name.Contains("Bone"))
            //{
            //    Debug.LogError("Could not retrieve a Hand from " + col.name);
            //}
            hand = null;
            return false;
        }


        /// <summary> Incorporate the HandBone colliders of this layer to a different RigidBody. Used by SG_Grabables. WARNING: This causes glitchyness when other.Transform is non-uniform. </summary>
        /// <param name="other"></param>
        public void SetCollisionParent(Rigidbody other)
        {
            this.colliderContainer.parent = other.transform;
        }

        /// <summary> Incorporate the HandBone colliders of this layer into a different RigidBody, At a psecific local position. </summary>
        /// <param name="other"></param>
        /// <param name="localPos"></param>
        public void SetCollisionParent(Rigidbody other, Vector3 localPos)
        {
            this.colliderContainer.SetParent(other.transform, true);
            this.colliderContainer.localRotation = Quaternion.identity;
            this.colliderContainer.localPosition = localPos;
        }

        /// <summary> Incorporate the HandBone colliders of this layer into a different RigidBody, At a psecific local position and rotation. </summary>
        /// <param name="other"></param>
        /// <param name="localPos"></param>
        /// <param name="localRot"></param>
        public void SetCollisionParent(Rigidbody other, Vector3 localPos, Quaternion localRot)
        {
            this.colliderContainer.SetParent(other.transform, true);
            this.colliderContainer.localRotation = localRot;
            this.colliderContainer.localPosition = localPos;
        }

        /// <summary> Return all colliders associated with this hand Physics layer back to this RigidBody. Used when releasing Grabables. </summary>
        public void ReturnColliders()
        {
            this.colliderContainer.parent = this.handRigidbody.transform;
            this.colliderContainer.localScale = Vector3.one;
            this.colliderContainer.localRotation = Quaternion.identity;
            this.colliderContainer.localPosition = Vector3.zero;
        }

        //--------------------------------------------------------------------------------------------------------------------
        // HandComponent Overrides

        /// <summary> Override from SG_HandComponent. Faster than this.TrackedHand.PhysicsLayer. </summary>
        public override SG_HandPhysics HandPhysics
        {
            get { return this; }
        }

        /// <summary> Collect all MeshColliders attached to my HandBones, as well as the Debug Text element </summary>
        /// <param name="objects"></param>
        /// <param name="renderers"></param>
        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);
            Util.SG_Util.CollectGameObject(this.debugElement, ref objects);
            for (int i = 0; i < this.wristColliders.Length; i++)
            {
                SG.Util.SG_Util.CollectComponent(this.wristColliders[i].gameObject, ref renderers); //grab rendering components
            }
            for (int f = 0; f < this.fingerColliders.Length; f++)
            {
                SG.Util.SG_Util.CollectComponent(this.fingerColliders[f].gameObject, ref renderers); //grab rendering components
            }
        }


        /// <summary> Sets this component up to work as a HandPhysics Layer </summary>
        protected override void CreateComponents()
        {
            base.CreateComponents();

            //Ensure we have a RigidBody at the start
            this.handRigidbody = SG.Util.SG_Util.TryAddRB(this.gameObject, false, false);
            this.handRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            //Create a special container for the HandBones.
            GameObject collWrist = new GameObject(handBonesTag);
            SG_HandLink link = collWrist.AddComponent<SG_HandLink>();
            link.linkedHand = this;
            this.colliderContainer = collWrist.transform;
            ReturnColliders(); //make all colliders part of this colliderContainer.

            //Ensure that all colliders have some sort of tracking, and place them in a array so I can just iterate through them later
            //wristTracking = new SG_SimpleTracking[this.wristColliders.Length];
            for (int i = 0; i < this.wristColliders.Length; i++)
            {
                //wristTracking[i] = SG.Util.SG_Util.TryAddComponent<SG.SG_SimpleTracking>(wristColliders[i].gameObject);
                //wristTracking[i].SetTrackingTarget(this.transform, true);
                SG.Util.SG_Util.TryRemoveComponent<SG.SG_SimpleTracking>(wristColliders[i].gameObject);
                wristColliders[i].transform.parent = this.colliderContainer;
            }
            fingerTracking = new SG_SimpleTracking[this.fingerColliders.Length];
            for (int i = 0; i < this.fingerColliders.Length; i++)
            {
                fingerTracking[i] = SG.Util.SG_Util.TryAddComponent<SG.SG_SimpleTracking>(fingerColliders[i].gameObject);
                fingerTracking[i].transform.parent = this.colliderContainer;
            }

        }

        /// <summary> Collect all colliders from this Script </summary>
        /// <returns></returns>
        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> myColliders = new List<Collider>();
            for (int i = 0; i < this.wristColliders.Length; i++)
            {
                if (!myColliders.Contains(this.wristColliders[i]))
                {
                    myColliders.Add(wristColliders[i]);
                }
            }
            for (int i = 0; i < this.fingerColliders.Length; i++)
            {
                if (!myColliders.Contains(this.fingerColliders[i]))
                {
                    myColliders.Add(fingerColliders[i]);
                }
            }
            return myColliders;
        }

        /// <summary> Links this HandPhysics Layer to a TrackerHand, and collect the releavent offsets.. </summary>
        /// <param name="newHand"></param>
        /// <param name="firstLink"></param>
        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand_Internal(newHand, firstLink);
            SG_HandPoser3D physicsPoser = newHand.GetPoser(SG_TrackedHand.TrackingLevel.RealHandPose); //might as well. We're mainly using it for the offsets.

            //Optional: Scale finger bones
            if (scaleFingerColliders)
            {
                for (int i = 0; i < this.fingerColliders.Length; i++)
                {
                    HandJoint from = fingerTracking[i].linkMeTo;
                    SGCore.Finger finger; int currIndex;
                    SG_HandPoser3D.ToFinger(from, out finger, out currIndex);
                    //between whatever I'm linked to and the next joint. If I'm linked to thefingertip though, we go one back?
                    int nextIndex = currIndex == 3 ? currIndex - 1 : currIndex + 1; //fingertip is the forth joint wiht index 3.
                    HandJoint to = SG_HandPoser3D.ToHandJoint(finger, nextIndex);

                    physicsPoser.StretchCapsule(fingerColliders[i], from, to);
                }
            }

            //Step 2: Link & calculate offsets
            //SetTargets(physicsPoser, this.wristTracking);
            SetTargets(physicsPoser, this.fingerTracking);


            TempDisableCollisions(); //TODO: Finx this to be "until we match the real hand".
        }



        //--------------------------------------------------------------------------------------------------------------------
        // Collision Functions


        /// <summary> Manually enables / disables collision on this hand's RigidBody. </summary>
        public bool CollisionsEnabled
        {
            get 
            { 
                return this.handRigidbody != null && handRigidbody.detectCollisions; 
            }
            set 
            {
                //if (value != this.CollisionsEnabled)
                //{
                //    Debug.Log("Setting " + (this.TrackedHand != null && this.TrackedHand.TracksRightHand() ? "Right" : "Left") + " Hand Collision to " + value);
                //}
                if (this.handRigidbody != null) { handRigidbody.detectCollisions = value; } 
            }
        }


        public void TempDisableCollisions() //TODO: Make the time configurable.
        {
            TempDisableCollisions(1); //1 s
        }

        public void TempDisableCollisions(float enableAfter)
        {
            this.reEnableAfter = enableAfter;
            this.CollisionsEnabled = false;
            this.timer_enablePhysics = 0;
            //S Debug.Log("Disabling Collision for " + reEnableAfter + "s.");
            //TODO: Make this disabled until the hand reaches the target position?
        }


        /// <summary> Colliders to keep track of </summary>
        /// <remarks> Places as a separate object class so we can add / remove whatever we want. </remarks>
        public class CollisionMark
        {
            public Rigidbody linkedBody;

            public Collider[] objColliders;

            public float taggedDistance_squared; //keeping the squared distance so its easier to calculate

            public CollisionMark(Rigidbody physicsBody, Collider[] physicsColliders, float currDist_squared)
            {
                linkedBody = physicsBody;
                objColliders = physicsColliders;
                taggedDistance_squared = currDist_squared;
            }
        }

        protected List<CollisionMark> markedColliders = new List<CollisionMark>();

        /// <summary> Access which MarkedCollider this RigidBody belongs to. </summary>
        /// <param name="rb"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int ListIndex(Rigidbody rb, List<CollisionMark> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (rb == list[i].linkedBody) { return i; }
            }
            return -1;
        }

        /// <summary> Instead of directly returning the Physics collison between the fingers and the object,  </summary>
        public void MarkForUncollision(Rigidbody rb, Collider[] physicsColliders)
        {
            if (rb != null && physicsColliders.Length > 0)
            {
                if (ListIndex(rb, markedColliders) < 0)
                {
                    float dist = (this.handRigidbody.position - rb.position).sqrMagnitude;
                    CollisionMark newMark = new CollisionMark(rb, physicsColliders, dist);
                    markedColliders.Add(newMark);
                    //We do not restore collision until the hand is far enough away(?)
                }
                //it already exists?
            }
            else
            {
                //Else we just restore collision like we normally would
                //Debug.Log("Restoring collision between " + (rb != null ? rb.name : "NULL") + " and " + this.name);
                SetIgnoreCollision(physicsColliders, false);
            }
        }

        public void CheckUncollision()
        {
            for (int i = 0; i < this.markedColliders.Count;)
            {
                if (markedColliders[i].linkedBody != null) //in case it's deleted.
                {
                    float currDist_sq = (this.handRigidbody.position - markedColliders[i].linkedBody.position).sqrMagnitude;
                    if (currDist_sq - markedColliders[i].taggedDistance_squared > this.uncollisionDist_sqrd)
                    {
                        //Debug.Log("Collision between " + markedColliders[i].linkedBody + " should now be active again");
                        SetIgnoreCollision(markedColliders[i].objColliders, false);
                        markedColliders.RemoveAt(i);
                    } //remove it from the list
                    else { i++; }
                }
                else { i++; }
            }
        }




        //--------------------------------------------------------------------------------------------------------------------
        // RigidBody Tracking Functions

        /// <summary> Returns true if the GrabScript connected to this layer's TrackedHand is overriding the positioning. </summary>
        public bool GrabableOverrides
        {
            get { return this.GrabScript != null && this.GrabScript.ControlsHandLocation(); }
        }

        /// <summary> Updates the local location of a single SimpleTracking Script based on that of a HandPose. It does not care about the real location. </summary>
        /// <param name="physicsPose"></param>
        /// <param name="colliderTracking"></param>
		protected void UpdateColliderLocation_Local(SG_HandPose physicsPose, SG_SimpleTracking colliderTracking)
        {
            int finger; int jointIndex;
            SG_HandPoser3D.ToFinger(colliderTracking.linkMeTo, out finger, out jointIndex);
            //we now know which Transform we're linked to.
            Vector3 jointPos = physicsPose.jointPositions[finger][jointIndex]; //relative to the wrist. Wrist is (this).
            Quaternion jointRot = physicsPose.jointRotations[finger][jointIndex];

            Vector3 localPos; Quaternion localRot;
            Util.SG_Util.CalculateTargetLocation(jointPos, jointRot, colliderTracking.PosOffset, colliderTracking.RotOffset, out localPos, out localRot);

            //and finally, calculate the actiual position in world space? Wait, I don't have to. Not if I consider the layer to be the wrist and these are my children...
            colliderTracking.transform.localRotation = localRot;
            colliderTracking.transform.localPosition = localPos;
        }


        /// <summary> Updates all hand collider' local rotation to match that of a HandPose. We cannot set the world position, as that would leave the RidigBody behind. </summary>
        /// <param name="pose"></param>
        public void UpdateHandColliders(SG_HandPose pose)
        {
            for (int i = 0; i < fingerTracking.Length; i++)
            {
                UpdateColliderLocation_Local(pose, fingerTracking[i]);
            }
        }


        /// <summary> Based on a target pose, update this layer's Rigidbody colliders and move it towards the target. </summary>
        /// <param name="pose"></param>
        /// <param name="deltaTime"></param>
        /// <param name="updateColliders"></param>
        public void UpdateRigidbody(SG_HandPose pose, float deltaTime, bool updateColliders = false)
        {
            //Step 1: Update Finger colliders, if desired.
            if (updateColliders)
            {
                this.UpdateHandColliders(pose);
            }

            //Step 2 Move the hand towards the new Wrist Position
            if (this.handRigidbody != null && !this.handRigidbody.isKinematic && !GrabableOverrides)
            {
                SG.Util.SG_Util.MoveRigidBody(this.handRigidbody, pose.wristPosition, pose.wristRotation, deltaTime,
                    this.RBTranslation, this.handMoveSpeed, this.zeroVelocity, this.RBRotation, this.handRotationSpeed, this.zeroAngularVelocity);

                //final res:
                Vector3 finalPosDiff = pose.wristPosition - this.handRigidbody.position;
                float finalDist = finalPosDiff.magnitude;
                if (finalDist >= this.snapBackDist)
                {
                    snapTimer += deltaTime;
                    if (snapTimer >= snapBackTime)
                    {
                        //Debug.Log("SnapBack!");
                        snapTimer = 0;
                        this.transform.position = pose.wristPosition;
                    }
                }
                else
                {
                    snapTimer = 0;
                }
                DebugText = Util.SG_Util.ToString(finalPosDiff, 3) + "\n" + System.Math.Round(finalDist, 3) + "\n" + snapTimer;
            }
            else
            {
                this.transform.rotation = pose.wristRotation;
                this.transform.position = pose.wristPosition;
            }
            //ensure this is properly sry
            if (GrabableOverrides)
            {
                this.colliderContainer.rotation = pose.wristRotation;
                this.colliderContainer.position = pose.wristPosition;
            }
        }



        //--------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        public override void Awake()
        {
            base.Awake();
            DebugText = "";
            ActiveInProject = true;
            Util.SG_PhysicsHelper.AddHandColiders(this); //add this one to a list to disable collision
            TempDisableCollisions(); //TODO: Untill you reach the target?
        }

        /// <summary>  </summary>
        protected void Update()
        {
            if (timer_enablePhysics <= reEnableAfter)
            {
                timer_enablePhysics += Time.deltaTime;
                if (timer_enablePhysics > reEnableAfter)
                {
                    CollisionsEnabled = true; //return back to normal.
                }
            }
            this.CheckUncollision();

            //if (debugEnabled && debugElement != null)
            //{
            //    string db = "";
            //    for (int i = 0; i < this.markedColliders.Count; i++)
            //    {
            //        db = markedColliders[i].linkedBody.name;
            //        if (i < markedColliders.Count - 1)
            //        {
            //            db += "\n";
            //        }
            //    }
            //    DebugText = db;
            //}
        }




    }
}