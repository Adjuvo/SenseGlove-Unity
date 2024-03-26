using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> Attach this to an SG_Interactable script to activate a wrist squeeze to a desired level while the object is grabbed. </summary>
    public class SG_SqueezeOnGrab : MonoBehaviour
    {
        /// <summary> Attached Interactable Script </summary>
        [SerializeField] protected SG_Interactable linkedInteractible;

        /// <summary> The (current) squeeze Level (0..1) that will be queued each frame while the objetc is grabbed. </summary>
        [Range(0.0f, 1.0f)] [SerializeField] protected float squeezeLevel = 1.0f;
        

        /// <summary> Accessor for the (current) squeeze Level (0..1) that will be queued each frame while the objetc is grabbed. </summary>
        public virtual float SqueezeLevel
        {
            get { return this.squeezeLevel; }
            set { this.squeezeLevel = Mathf.Clamp01(squeezeLevel); }
        }

        /// <summary> Fires when this object is grabbed by a GrabScript. Queues a Wrist Squeeze command that will be sent at the end of this frame. </summary>
        /// <param name="thisObj"></param>
        /// <param name="grabbedByHand"></param>
        protected virtual void ObjectGrabbed(SG_Interactable thisObj, SG_GrabScript grabbedByHand)
        {
            linkedInteractible.QueueWristSqueeze(this.SqueezeLevel);
        }


        /// <summary> Fires when this object stops being grabbed by a grabscript. Ends wrist squeeze on the hand </summary>
        /// <param name="thisObj"></param>
        /// <param name="grabbedByHand"></param>
        protected virtual void ObjectReleased(SG_Interactable thisObj, SG_GrabScript grabbedByHand)
        {
            if (grabbedByHand != null && grabbedByHand.TrackedHand != null)
                grabbedByHand.TrackedHand.QueueWristSqueeze(0.0f); //end the wirst squeeze on the hand, since it might have already left the Grabable's scope
        }

        /// <summary> Fires each frame while the object is grabbed. We'll keep queueing QueueWristSqueeze(SqueezeLevel) through the linkedInteractable. </summary>
        protected virtual void UpdateGrabbedHaptics()
        {
            linkedInteractible.QueueWristSqueeze(this.SqueezeLevel);
        }

        protected virtual void OnEnable()
        {
            if (linkedInteractible == null) { this.linkedInteractible = this.GetComponent<SG_Interactable>(); }
            if (linkedInteractible != null)
            {
                linkedInteractible.ObjectGrabbed.AddListener(ObjectGrabbed);
                linkedInteractible.ObjectReleased.AddListener(ObjectReleased);
            }
        }

        protected virtual void OnDisable()
        {
            if (linkedInteractible != null)
            {
                linkedInteractible.ObjectGrabbed.RemoveListener(ObjectGrabbed);
                linkedInteractible.ObjectReleased.RemoveListener(ObjectReleased);
            }
        }

        protected virtual void Update()
        {
            if (linkedInteractible != null && linkedInteractible.IsGrabbed())
            {
                UpdateGrabbedHaptics();
            }
        }

    }

}