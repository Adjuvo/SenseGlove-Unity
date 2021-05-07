using UnityEngine;

namespace SG
{
    /// <summary> A script to manage a set of Rigidbodies that follow parts of the hand. Used to create a physical presence of the hand in 3D spaces. </summary>
    public class SG_HandRigidBodies : SG_HandComponent
    {
        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The managed rigidbody of the wrist </summary>
        [Header("RigidBody Components")]
        public SG_TrackedBody wristObj;

        /// <summary> The managed rigidbody of the fingers, from thumb to pinky. </summary>
        public SG_TrackedBody[] fingerObjs = new SG_TrackedBody[0];

        //----------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Show/Hide the the rigidbodies in this layer. </summary>
        public override bool DebugEnabled
        {
            set
            {
                wristObj.DebugEnabled = value;
                for (int f = 0; f < fingerObjs.Length; f++)
                {
                    fingerObjs[f].DebugEnabled = value;
                }
            }
        }

        /// <summary> Enable/Disable the overall collision of the rigidbodies in this layer. </summary>
        public bool CollisionsEnabled
        {
            set
            {
                wristObj.CollisionEnabled = value;
                for (int f = 0; f < fingerObjs.Length; f++)
                {
                    fingerObjs[f].CollisionEnabled = value;
                }
            }
        }

        //----------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Setup the tracking / parameters of this script's components. </summary>
        protected void SetupSelf()
        {
            CheckForScripts();
            wristObj.SetTrackingTarget(TrackedHand.handModel.wristTransform, true);
            for (int f = 0; f < fingerObjs.Length; f++)
            {
                Transform target;
                if (TrackedHand.handModel.GetFingerTip((SG_HandSection)f, out target))
                {
                    fingerObjs[f].SetTrackingTarget(target, true);
                }
            }
            this.SetIgnoreCollision(this, true); //ignore own colliders
        }


        /// <summary> Set ignoreCollision between this layer and another set of rigidbodies. </summary>
        /// <param name="otherLayer"></param>
        /// <param name="ignoreCollision"></param>
        public void SetIgnoreCollision(SG_HandRigidBodies otherLayer, bool ignoreCollision)
        {
            if (otherLayer != null)
            {
                GameObject wrist = otherLayer.wristObj != null ? otherLayer.wristObj.gameObject : null;
                SetIgnoreCollision(wrist, ignoreCollision);
                for (int f = 0; f < otherLayer.fingerObjs.Length; f++)
                {
                    SetIgnoreCollision(otherLayer.fingerObjs[f].gameObject, ignoreCollision);
                }
            }
        }

        /// <summary> Set the ignoreCollision between this layer and a specific gameobject </summary>
        /// <param name="obj"></param>
        /// <param name="ignoreCollision"></param>
        public void SetIgnoreCollision(GameObject obj, bool ignoreCollision)
        {
            Collider[] colliders = obj != null ? obj.GetComponents<Collider>() : new Collider[0];
            for (int i = 0; i < colliders.Length; i++)
            {
                this.SetIgnoreCollision(colliders[i], ignoreCollision);
            }
        }

        /// <summary> Set the ignoreCollision between this layer and a specific collider </summary>
        /// <param name="obj"></param>
        /// <param name="ignoreCollision"></param>
        public void SetIgnoreCollision(Collider col, bool ignoreCollision)
        {
            if (this.wristObj != null) { this.wristObj.SetIgnoreCollision(col, ignoreCollision); }
            for (int f = 0; f < fingerObjs.Length; f++)
            {
                if (fingerObjs[f] != null) { this.fingerObjs[f].SetIgnoreCollision(col, ignoreCollision); }
            }
        }



        /// <summary> Add Rigidbodies with proper parameters for this layer. </summary>
        /// <param name="useGrav"></param>
        /// <param name="kinematic"></param>
        public void AddRigidBodies(bool useGrav = false, bool kinematic = false)
        {
            this.wristObj.TryAddRB(useGrav, kinematic);
            for (int f = 0; f < this.fingerObjs.Length; f++)
            {
                fingerObjs[f].TryAddRB(useGrav, kinematic);
                fingerObjs[f].updateTime = SG_SimpleTracking.UpdateDuring.FixedUpdate;
            }
        }

        /// <summary> Removes rigidbodies from this layer, so their collision can become part of a different RigidBody. </summary>
        public void RemoveRigidBodies()
        {
            this.wristObj.TryRemoveRB();
            for (int f = 0; f < this.fingerObjs.Length; f++)
            {
                fingerObjs[f].TryRemoveRB();
                fingerObjs[f].updateTime = SG_SimpleTracking.UpdateDuring.LateUpdate;
            }
        }

        //----------------------------------------------------------------------------------------------
        // Monobehaviour

        // Use this for initialization
        protected void Awake()
        {
            SetupSelf();
        }

    }
}