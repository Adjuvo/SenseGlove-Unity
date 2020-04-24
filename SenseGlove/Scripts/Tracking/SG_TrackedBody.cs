using UnityEngine;

namespace SG
{
    /// <summary> A Rigidbody that tracks a transform by adding velocity to the body, rather than directly applying positions.
    /// It reverts back to simpleTrackign if no Rigidbody is present. </summary>
    public class SG_TrackedBody : SG_SimpleTracking
    {
        /// <summary> The RigidBody to apply force-feebdack to. </summary>
        public Rigidbody physicsBody;

        /// <summary> Time after which the rigidbody will reset back to its targetposition if it is more than resetDistance away </summary>
        protected static float resetTime = 3;
        /// <summary> Maximum distance between this script and it's target position before we assume the colliders are stuck somewhere. </summary>
        protected static float resetDistance = 1;
        /// <summary> Timer to keep track of how long the collider has been away from its target transform </summary>
        protected float resetTimer = 0;

        /// <summary> Speed at which the rotation is matched </summary>
        protected static float rotationSpeed = 25;



        /// <summary> Try to add a rigidbody to this GameObject, if one isn't already present </summary>
        /// <param name="useGrav"></param>
        /// <param name="isKinematic"></param>
        public void TryAddRB(bool useGrav = false, bool isKinematic = false)
        {
            this.physicsBody = SG_Util.TryAddRB(this.gameObject, useGrav, isKinematic);
        }

        /// <summary> Remove the Rigidbody if one exists. </summary>
        public void TryRemoveRB()
        {
            SG_Util.TryRemoveRB(this.gameObject);
            this.physicsBody = null;
        }

        /// <summary> Enable / Disable collision of this collider in general </summary>
        public bool CollisionEnabled
        {
            get { return physicsBody != null && physicsBody.detectCollisions; }
            set { if (physicsBody != null) { physicsBody.isKinematic = !value; } }
        }

        /// <summary> Update this object's transform by applying a velocity to the rigidbody </summary>
        protected override void UpdatePosition()
        {
            if (trackingTarget != null)
            {
                if (this.physicsBody != null)
                {
                    SG_Util.TransformRigidBody(ref physicsBody, this.TargetPosition, this.TargetRotation, rotationSpeed);
                    if ((this.transform.position - trackingTarget.position).magnitude > resetDistance)
                    {
                        resetTimer += this.updateTime == UpdateDuring.FixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;
                        if (resetTimer >= resetTime)
                        {
                            base.UpdatePosition(); //snaps using simple method
                            resetTimer = 0;
                        }
                    }
                    else { resetTimer = 0; }
                }
                else { base.UpdatePosition(); }
            }
        }


        protected override void Awake()
        {
            base.Awake();
            if (physicsBody == null)
            {
                this.physicsBody = this.gameObject.GetComponent<Rigidbody>();
            }
            if (physicsBody != null)
            {
                physicsBody.useGravity = false;
            }
        }

    }
}