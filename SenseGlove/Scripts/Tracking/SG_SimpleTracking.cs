using UnityEngine;

namespace SG
{
    /// <summary> Attached to a GameObject to make it follow a 'target' during a specific Update function. </summary>
    public class SG_SimpleTracking : MonoBehaviour
    {
        /// <summary> When the position of this GameObject is updated. </summary>
        public enum UpdateDuring
        {
            LateUpdate,
            FixedUpdate,
            Update,
            Off
        }

        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> A transform to follow during the simulation. Offsets are determined during Start() of this script </summary>
        [Header("Tracking Settings")]
        [SerializeField] protected Transform trackingTarget;

        /// <summary> Determines when an instance of this script updates its position. </summary>
        public UpdateDuring updateTime = UpdateDuring.LateUpdate;

        /// <summary> Position offset between this object and the target transform </summary>
        protected Vector3 positionOffset = Vector3.zero;
        /// <summary> Rotation offset between this object and the target transform </summary>
        protected Quaternion rotationOffset = Quaternion.identity;


        //----------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Enable/Disable the MeshRenderer connected to this script's GameObject </summary>
        public virtual bool DebugEnabled
        {
            get
            {
                MeshRenderer renderComponent = this.gameObject.GetComponent<MeshRenderer>();
                return renderComponent != null && renderComponent.enabled;
            }
            set
            {
                MeshRenderer renderComponent = this.gameObject.GetComponent<MeshRenderer>();
                if (renderComponent != null) { renderComponent.enabled = value; }
            }
        }

        /// <summary> Returns the supposed, absolute position of this GameObject, based on its offsets. </summary>
        public Vector3 TargetPosition
        {
            get { return trackingTarget.transform.position + (this.trackingTarget.rotation * positionOffset); }
        }

        /// <summary> Returns the supposed, absolute rotation of this GameObject, based on its offsets. </summary>
        public Quaternion TargetRotation
        {
            get { return trackingTarget.transform.rotation * rotationOffset; }
        }

        /// <summary> Returns true if this script has a target it can follow </summary>
        public bool HasTarget { get { return this.trackingTarget != null; } }


        //----------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Ignore collision between this object and another collider </summary>
        /// <param name="col"></param>
        public virtual void SetIgnoreCollision(Collider col, bool ignoreCollision)
        {
            //Debug.Log("Ignore Collision between " + this.name + " and " + col.name);
            Collider[] myColliders = this.GetComponents<Collider>();
            for (int i = 0; i < myColliders.Length; i++)
            {
                Physics.IgnoreCollision(col, myColliders[i], ignoreCollision);
            }
        }


        /// <summary> Set a new tracking target for this script, which also calculates new offsets </summary>
        /// <param name="newTarget"></param>
        public virtual void SetTrackingTarget(Transform newTarget, bool calculateNewOffsets)
        {
            this.trackingTarget = newTarget;
            if (trackingTarget != null && calculateNewOffsets)
            {
                SG.Util.SG_Util.CalculateOffsets(this.transform, trackingTarget, out this.positionOffset, out this.rotationOffset);
            }
        }


        /// <summary> Update the transform of this script to its TragetPosition and Rotation </summary>
        protected virtual void UpdatePosition()
        {
            if (trackingTarget != null && trackingTarget.gameObject.activeInHierarchy) //don;t track if the object is disabled(?)
            {
                this.transform.rotation = TargetRotation;
                this.transform.position = TargetPosition;
            }
        }


        //----------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Awake()
        {
            SetTrackingTarget(this.trackingTarget, true); //do it once during the beginning.
        }

        protected virtual void Update()
        {
            if (updateTime == UpdateDuring.Update) { UpdatePosition(); }
        }

        protected virtual void LateUpdate()
        {
            if (updateTime == UpdateDuring.LateUpdate) { UpdatePosition(); }
        }

        protected virtual void FixedUpdate()
        {
            if (updateTime == UpdateDuring.FixedUpdate) { UpdatePosition(); }
        }

    }
}