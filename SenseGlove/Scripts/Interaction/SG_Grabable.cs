using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> An object that can be moved around using one or more hands. When held by a GrabScript, it calculates a target position and rotation, then moves towards it.
    /// Grabables override the wrist position- and rotation, drawing the hand on the spot you grabbed it. </summary>
    public class SG_Grabable : SG_Interactable
    {
        /// <summary> How the rotation of a Grabable is determined when held with two hands. </summary>
        public enum DualHandMode
        {
            /// <summary> The Grabable stays in the same rotation it was when the second hand joined in. Not realistic, but allows for quick transfers between hands. </summary>
            FreezeRotation,
            /// <summary> Calculates the target rotation for the two hands, then halves it. Works well enough, but has a tendency to flip if when rotating both hands in opposite directions. </summary>
            AverageRotation,
            /// <summary> Use the target rotation of the left hand. </summary>
            LeftHandRotation,
            /// <summary> Use the target rotation of the right hand. </summary>
            RightHandRotation,
            /// <summary> Use the target rotation of whichever hand held on first (note; this changes as you swap hands). </summary>
            FirstHandsRotation,
            /// <summary> Treat the two refrences as pivots. </summary>
            PivotPoints
        }


        //-----------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> How quickly the object moves towards the target position, in m/s </summary>
        [Header("Grabable Options")]
        public float moveSpeed = 100;
        /// <summary> How quickly this object moves to match its target rotation, in deg/s. </summary>
        public float rotateSpeed = 900;

        /// <summary> The starting values of this Object's RigidBody, to which we'll return once we fully release it. </summary>
        protected Util.RigidBodyStats rbDefaults = null;

        /// <summary> How the rotation of this object is determined when held by two hands. </summary>
        protected DualHandMode dualMode = DualHandMode.AverageRotation;

        /// <summary> My position when the hands last changed. </summary>
        protected Vector3 posOnLastChange = Vector3.zero;
        /// <summary> My rotation when the hands last changed. </summary>
        protected Quaternion rotOnLastChange = Quaternion.identity;

        /// <summary> Optional component to snap the hand to this Grabable. </summary>
        public SG_SnapOptions snapOptions;

        /// <summary> If true, this object will still keep track of its own Velocity, even if it's not moved by Physics. </summary>
        public bool alwaysTrackVelocity = false;

        /// <summary> How this Grabable moves towards the TargetPosition when a non-kinematic RigidBody is used. </summary>
        //[Header("RigidBody Manipulation Options")]
        protected Util.TranslateMode translateMode = Util.TranslateMode.ImprovedVelocity;
        /// <summary> Whether or not to Zero the velocity before / after applying the translation behaviour. </summary>
        protected bool zeroVelocity = false;
        /// <summary> How this Grabable moves towards the TargetRotation when a non-kinematic RigidBody is used. </summary>
        protected Util.RotateMode rotateMode = Util.RotateMode.OfficialMoveRotation;
        /// <summary> Whether or not to Zero the angular velocity before / after applying the rotation behaviour. </summary>
        protected bool zeroAngularVelocity = true;


        // This object keeps track of its own speed - if it has a RigidBody attached.

        /// <summary> The maximum frames for which to keep track of velocity. </summary>
        public static int maxVelocityPoints = 10;

        /// <summary> The xyz velocities during the last few frames, used to determine the average impact velocity. </summary>
        protected List<Vector3> velocities = new List<Vector3>(); //storing as xyz vector to allow for greater flexibility in the future.
        /// <summary> The angular velocity during the previous frames. </summary>
        protected List<Vector3> angularVelocities = new List<Vector3>();

        /// <summary> This object's position during the last frame, used to determine velocity. </summary>
        protected Vector3 lastPosition = Vector3.zero;
        /// <summary> The grabReference's rotation during the last frame. </summary>
        protected Quaternion lastRotation = Quaternion.identity;

        /// <summary> Original Parent of the object </summary>
        protected Transform baseStartParent;
        /// <summary> (Local) start position of the BaseTransform. Logged at start. </summary>
        protected Vector3 baseStartPosition;
        /// <summary> (Local) start rotation of the BaseTransform. Logged at start. </summary>
        protected Quaternion baseStartRotation;




        //-----------------------------------------------------------------------------------------------------------------------------------
        // RigidBody / Movement functions

        /// <summary>  Returns if this object has a non-kinematic rigidBody attached. </summary>
        public bool IsMovedByPhysics
        {
            get
            {
                return this.physicsBody != null && this.rbDefaults != null && !this.rbDefaults.wasKinematic; //we were not set to be Kinematic (could this change while being held?)
            }
        }

        /// <summary> Updates the RB defaults (what we return to after releasing) to the Rigidbody's current values. Called once on Awake. </summary>
        public void UpdateRigidbodyDefaults()
        {
            if (this.physicsBody != null) { rbDefaults = new Util.RigidBodyStats(this.physicsBody); }
        }

        /// <summary> Restores the PhysicsBody back to its original paramaters. </summary>
        public void RestorePhysicsBody()
        {
            if (this.rbDefaults != null) { this.SetPhysicsbody(this.rbDefaults); }
        }

        /// <summary> Set this Grabable's PhysicsBody to specific values (but they will return to defaults when released) </summary>
        /// <param name="useGravity"></param>
        /// <param name="isKinematic"></param>
        public void SetPhysicsbody(bool useGravity, bool isKinematic, RigidbodyConstraints constraints)
        {
            if (this.physicsBody != null)
            {
                this.physicsBody.useGravity = useGravity;
                this.IsKinematic = isKinematic; //via the offical setting
                this.physicsBody.constraints = constraints;
            }
        }


        /// <summary> Actually set this Grabable's PhysicsBody to specific values (but they will return to defaults when released)  </summary>
        /// <param name="stats"></param>
        public void SetPhysicsbody(Util.RigidBodyStats stats)
        {
            if (stats != null)
            {
                SetPhysicsbody(stats.usedGravity, stats.wasKinematic, stats.rbConstraints);
            }
        }

        /// <summary> Stores the object's current location as a 'base' </summary>
        public void SaveCurrentLocation()
        {
            Transform baseTransf = this.MyTransform;
            SG.Util.SG_Util.CalculateBaseLocation(baseTransf, out baseStartPosition, out baseStartRotation); //save this for later
            this.baseStartParent = baseTransf.parent;
        }

        /// <summary> Return this object base to it's original position, determined by the SaveCurrentLocation. (Called during Setup) </summary>
        /// <param name="resetRBStats">If true, this paratmeter resets the Rigidbody's stats as well (UseGravity, IsKinematic </param>
        public void ResetLocation(bool resetRBStats)
        {
            Transform myTransf = this.MyTransform;
            Vector3 currBasePos; Quaternion currBaseRot;
            SG.Util.SG_Util.GetCurrentBaseLocation(myTransf, baseStartParent, this.baseStartPosition, this.baseStartRotation, out currBasePos, out currBaseRot);
            if (this.physicsBody != null)
            {
                this.physicsBody.velocity = Vector3.zero;
                this.physicsBody.angularVelocity = Vector3.zero;
                if (resetRBStats && this.rbDefaults != null)
                {
                    this.physicsBody.useGravity = this.rbDefaults.usedGravity;
                    this.physicsBody.isKinematic = this.rbDefaults.wasKinematic;
                }
            }
            myTransf.position = currBasePos;
            myTransf.rotation = currBaseRot;

        }



        //-----------------------------------------------------------------------------------------------------------------------------------
        // Velocity Tracking

        /// <summary> Returns the average velocity over the last few frames. Note: This is only tracked if IsMoveByPhysics or overrideTrackVelocity is true.  </summary>
        /// <returns></returns>
        public Vector3 SmoothedVelocity
        {
            get { return SG.Util.SG_Util.Average(this.velocities); }
        }

        /// <summary> Average angular velocity over the last few frames. Note: This is only tracked if IsMoveByPhysics or overrideTrackVelocity is true. </summary>
        public Vector3 SmoothedAngularVelocity
        {
            get { return SG.Util.SG_Util.Average(this.angularVelocities); }
        }

        /// <summary> Update this object's velocity. </summary>
        public void UpdateVelocity(float dT)
        {
            Transform myTransf = MyTransform;

            Vector3 currPos = myTransf.position;
            Quaternion currRot = myTransf.rotation;

            Vector3 velocity = (currPos - lastPosition) / dT;
            Vector3 angularVelocity = SG.Util.SG_Util.CalculateAngularVelocity(currRot, lastRotation, Time.deltaTime);

            this.velocities.Add(velocity);
            this.angularVelocities.Add(angularVelocity);
            if (velocities.Count > maxVelocityPoints)
            {
                this.velocities.RemoveAt(0);
                this.angularVelocities.RemoveAt(0);
            }

            lastPosition = currPos;
            lastRotation = currRot;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Grabable Functions


        /// <summary> The Grabable always favours the PhysicsBody - which is why we override it. </summary>
        public override Transform MyTransform
        {
            get
            {
                Setup();
                return this.physicsBody != null ? this.physicsBody.transform : base.MyTransform;
            }
        }


        /// <summary> Grabables control hand location of the grabscripts holding on to it. </summary>
        /// <returns></returns>
        public override bool ControlsHandLocation()
        {
            return this.grabbedBy.Count > 0; //I'm controlling the Grab location if I'm held by an object
        }

        /// <summary> Generate grabArguments - which may be overridden if any SnapPoints have been assgined. </summary>
        /// <param name="grabScript"></param>
        /// <returns></returns>
        public override GrabArguments GenerateGrabArgs(SG_GrabScript grabScript)
        {
            if (this.snapOptions != null)
            {
                GrabArguments res;
                if (this.snapOptions.GenerateGrabArgs(this.MyTransform, grabScript, out res)) //allow the SnapPoint to generate them for us.
                {
                    return res;
                }
            }
            //no snapping, so just go.
            return base.GenerateGrabArgs(grabScript);
        }

        protected void UpdateLastGrabLocation()
        {
            Transform myTransf = this.MyTransform;
            this.posOnLastChange = myTransf.position;
            this.rotOnLastChange = myTransf.rotation;
        }

        /// <summary> Called when we have determined that we'll grab this object, but before the grabScript has been added to our list. </summary>
        /// <param name="grabScript"></param>
        /// <param name="grabArgs"></param>
        /// <returns></returns>
        protected override bool StartGrab(SG_GrabScript grabScript, out GrabArguments grabArgs)
        {
            bool imGrabbed = base.StartGrab(grabScript, out grabArgs);
            if (imGrabbed)
            {
                UpdateLastGrabLocation();

                //Update Physics Behaviour when grabbed for the first time
                SG_HandPhysics handPhysics = grabScript.HandPhysics;
                if (handPhysics != null && handPhysics.isActiveAndEnabled) //the hand has a RigidBody + physics colliders
                {
                    if (this.physicsBody != null) // I have a rigidBody. Add Hand Colliders to me as opposed to the hand
                    {
                        //if (this.name.Contains("Coil"))
                        //{
                        //    Debug.Log("Incorporating fingers into my RigidBody");
                        //}
                        handPhysics.SetCollisionParent(this.physicsBody);
                    }
                    else
                    {
                       // Debug.Log("No PhysicsBody: Ignore Collision with Grabable");
                        handPhysics.SetIgnoreCollision(this.GetPhysicsColliders(), true); // Just ignore the collision between this object's colliders and that of the hand.
                    }
                }

            }

            if (imGrabbed && this.grabbedBy.Count == 0) // I'm not yet grabbed by a GrabScript, and this new one was succesfull
            {
                if (this.rbDefaults != null)
                {
                    //must freeze rotation, but keep thine origianl constraints...
                    //disable gravity (for now) so that it actually gets off the floor. Also freeze rotation because it gives the best behaviour from MoveRotation.
                    this.SetPhysicsbody(false, false, RigidbodyConstraints.FreezeRotation | rbDefaults.rbConstraints);
                }
            }
            return true;
        }


        /// <summary> Fired after we determined we're gonna release, but before we remove the GrabArguments form the list. If this is the last one, return the PhyscisBody back </summary>
        /// <param name="grabbedScript"></param>
        /// <returns></returns>
        protected override bool StartRelease(GrabArguments grabbedScript)
        {
            if (grabbedScript != null && grabbedScript.GrabScript != null)
            {
                SG_HandPhysics handPhysics = grabbedScript.GrabScript.HandPhysics;
                if (handPhysics != null) //the hand has a RigidBody + physics colliders
                {
                    if (this.physicsBody != null) // I have a rigidBody. return rigidbodies
                    //if (IsMovedByPhysics) // I have a rigidBody. return rigidbodies
                    {
                        //if (this.name.Contains("Coil"))
                        //{
                        //    Debug.Log("Returning fingers back to physicsCollider");
                        //}
                        //  Debug.Log("Returning fingers back to physicsCollider");
                        handPhysics.SetIgnoreCollision(this.GetPhysicsColliders(), true); //ignore collision until the colliders are no longer near the object - prevent tossing.
                        handPhysics.MarkForUncollision(this.physicsBody, this.GetPhysicsColliders());
                        handPhysics.ReturnColliders();
                    }
                    else
                    {
                        //re-enable collision
                       // Debug.Log("No PhysicsBody: Re-Enabling Collision");
                        handPhysics.SetIgnoreCollision(this.GetPhysicsColliders(), false);
                    }
                }
            }

            if (this.grabbedBy.Count == 1) //this is the last script and is about to be released
            {
                if (this.physicsBody != null)
                {
                    //Unity absolutely refuses to zero any velocity until I set the body to Kinematic
                    //  this.IsKinematic = true;
                    physicsBody.angularVelocity = Vector3.zero;
                    physicsBody.velocity = Vector3.zero;
                    RestorePhysicsBody(); //return it back to the original states with velocities of 0
                    if (!this.physicsBody.isKinematic) //and add my last velocity if I'm not kinemtaics.
                    {
                        //Add veloctiy of either this object or of the hand...
                        this.physicsBody.angularVelocity = this.SmoothedAngularVelocity;
                        this.physicsBody.velocity = this.SmoothedVelocity;
                    }
                }
            }
            else //We're transferring between hands. Might as well zero the velocity 
            {
                if (this.physicsBody != null)
                {
                    //bool currKin = this.physicsBody.isKinematic;
                    // this.IsKinematic = true;
                    physicsBody.angularVelocity = Vector3.zero;
                    physicsBody.velocity = Vector3.zero;
                    // this.IsKinematic = currKin;
                   // Debug.Log("Tranferring. Zeroien velocity");
                }
            }
            //log my pos/rot on the change
            UpdateLastGrabLocation();
            return true;
        }



        /// <summary> Calculate the target position / rotation of this Grabable, as determined bny the hands holding onto it.
        /// If you're looking to make custom behaviours that do not use any internal states, it's easier to override MoveToTargetLocation() instead. </summary>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        protected virtual void CalculateTargetLocation(List<GrabArguments> heldBy, out Vector3 targetPosition, out Quaternion targetRotation)
        {
            Transform myTransf = MyTransform;
            if (heldBy.Count > 1) //we're held by more than one hand
            {
                //Calculate target Rotation qT
                Quaternion qT;
                if (dualMode == DualHandMode.AverageRotation)
                {
                    Vector3 pos; Quaternion qP;
                    heldBy[0].CalculateObjectTarget(myTransf, out pos, out qP);
                    Quaternion qS;
                    heldBy[1].CalculateObjectTarget(myTransf, out pos, out qS);
                    qT = Quaternion.Slerp(qP, qS, 0.5f); //slerp is clamped between 0 ... 1, so 0.5 is the average.
                }
                else if (dualMode == DualHandMode.RightHandRotation)
                {
                    GrabArguments sourc = heldBy[0].GrabScript.IsRight ? heldBy[0] : heldBy[1];
                    Vector3 pos; Quaternion qP;
                    sourc.CalculateObjectTarget(myTransf, out pos, out qP);
                    qT = qP;
                }
                else if (dualMode == DualHandMode.LeftHandRotation)
                {
                    GrabArguments sourc = heldBy[0].GrabScript.IsRight ? heldBy[1] : heldBy[0];
                    Vector3 pos; Quaternion qP;
                    sourc.CalculateObjectTarget(myTransf, out pos, out qP);
                    qT = qP;
                }
                else if (dualMode == DualHandMode.FirstHandsRotation)
                {
                    Vector3 pos; Quaternion qP;
                    heldBy[0].CalculateObjectTarget(myTransf, out pos, out qP);
                    qT = qP;
                }
                else if (dualMode == DualHandMode.PivotPoints)
                {
                    //GrabArguments primary = heldBy[0].GrabScript.IsRight ? heldBy[1] : heldBy[0];
                    //GrabArguments secondary = heldBy[0].GrabScript.IsRight ? heldBy[0] : heldBy[1];
                    throw new System.NotImplementedException("This experimental feature is not yet available...");
                }
                else //if all else fails, I'll grab the secodn hand, why not?
                {
                    qT = heldBy[1].MyRotAtGrab;
                }
                targetRotation = qT;

                //for position, we take the average between what each hand wants me to be.
                Vector3 posSum = Vector3.zero;
                for (int i = 0; i < heldBy.Count; i++)
                {
                    Vector3 targetPos; Quaternion targetRot;
                    heldBy[i].CalculateObjectTarget(myTransf, out targetPos, out targetRot);
                    posSum += targetPos;
                }
                targetPosition = posSum / (float)heldBy.Count;
            }
            else //we're held by a single hand. This is simple
            {
                heldBy[0].CalculateObjectTarget(myTransf, out targetPosition, out targetRotation);
            }
        }


        /// <summary> Actally set this object's position and rotation to the target. Easiest to override this instead of CalculateTargetLocation. </summary>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        /// <param name="dT"></param>
        protected virtual void MoveToTargetLocation(Vector3 targetPosition, Quaternion targetRotation, float dT)
        {
            Transform myTransf = this.MyTransform;
            if (this.physicsBody != null && !this.physicsBody.isKinematic)
            {
                Util.SG_Util.MoveRigidBody(this.physicsBody, targetPosition, targetRotation, dT, this.translateMode, this.moveSpeed, this.zeroVelocity, this.rotateMode, this.rotateSpeed, this.zeroAngularVelocity);
            }
            else
            {
                myTransf.rotation = Quaternion.RotateTowards(myTransf.rotation, targetRotation, rotateSpeed * dT);
                myTransf.position = Vector3.MoveTowards(myTransf.position, targetPosition, moveSpeed * dT);
            }
        }


        /// <summary> Update this Grabable's location. The SG_Interactable script will ensure it's only called once per frame. </summary>
        /// <param name="dT"></param>
        protected override void UpdateLocation(float dT)
        {
            List<GrabArguments> heldBy = this.grabbedBy;
            if (heldBy.Count > 0) //I'm actually grabbed by something
            {
                Vector3 targetPosition; Quaternion targetRotation;
                CalculateTargetLocation(heldBy, out targetPosition, out targetRotation);
                MoveToTargetLocation(targetPosition, targetRotation, dT);
            }
        }



        /// <summary> Link this script to any of its optional components. </summary>
        protected override void SetupScript()
        {
            base.SetupScript();
            this.UpdateRigidbodyDefaults(); //physicsBody should have been assigned in base.
            this.SaveCurrentLocation();
            if (this.snapOptions == null)
            {
                this.snapOptions = this.GetComponent<SG_SnapOptions>();
            }
        }



        //-------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> An addition check on Start for non-uniform scaling. But only warn our devs about it if they are actually using SG_HandPhysics. </summary>
        protected override void Start()
        {
            base.Start();
            if (SG_HandPhysics.ActiveInProject && this.physicsBody != null)
            {
                Vector3 physicsScale = this.physicsBody.transform.lossyScale;
                bool xy = SGCore.Kinematics.Values.FloatEquals(physicsScale.x, physicsScale.y);
                bool yz = SGCore.Kinematics.Values.FloatEquals(physicsScale.y, physicsScale.z);
                bool xz = SGCore.Kinematics.Values.FloatEquals(physicsScale.x, physicsScale.z);
                if (!xy || !yz || !xz)
                {
                    Debug.LogWarning(this.name + " has a non-uniform scale set to its Rigidbody, " + physicsScale.ToString() + ", which will break the Physics Tracking Layer. " +
                        "If you are using SG_HandPhysics in your project, attach this SG_Interactable Script to a uniformly scaled RigidBody, and add the model and colliders as a child.");
                }
            }
        }


        protected virtual void FixedUpdate()
        {
            if (safeguardFrame != -1 && safeguardFrame != Time.frameCount)
            {
                //Debug.Log("SGKin: Returned SafeGuard at " + Time.frameCount);
                safeguardFrame = -1;
            }
            //ToDo: Validate destroyed GrabScripts?
            
            if (!this.IsGrabbed()) //Ensure 
            {
                UpdateInteractable();
            }

            if (alwaysTrackVelocity || this.IsMovedByPhysics) //only keep these if we have a non-kinematic rigidBody
            {
                UpdateVelocity(Time.deltaTime);
            }
        }

    }
}
