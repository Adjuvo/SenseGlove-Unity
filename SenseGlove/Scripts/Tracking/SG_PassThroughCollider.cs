using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{


    /// <summary> A collider that checks whether or not a finger is obstructed by a non-trigger collider </summary>
	public class SG_PassThroughCollider : SG_SimpleTracking
    {
        //------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Which finger this PassThrough Collider will be locking. </summary>
        public SGCore.Finger locksFinger = SGCore.Finger.Thumb;

        /// <summary> The RIgidBody connected to this PassThrough collider. It needs one to fire OnTriggerEnter for non-trigger colliders without a rigidBody </summary>
		protected Rigidbody physicsBody;

        /// <summary> A list of all Impassable colliders that this collider is currently touching. </summary>
        protected List<Collider> impassablesTouched = new List<Collider>();

        /// <summary> Text element on which we print the colliders that touch this finger. </summary>
        public TextMesh debugTextElement;

        //------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Safely Get / Set the DebugTextElement's text value/. </summary>
        public string DebugText
        {
            get { return debugTextElement != null ? debugTextElement.name : ""; }
            set { if (debugTextElement != null) { debugTextElement.text = value; } }
        }

        //------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Retrieve a list of all non-trigger colliders touched by this script. </summary>
        /// <returns></returns>
        public string PrintTouched(string delim = "\n")
        {
            return SG.Util.SG_Util.PrintContents(this.impassablesTouched, delim);
        }

        /// <summary> Updates the DebugText to show the current list of touched colliders. </summary>
        public void UpdateDebugger()
        {
            DebugText = PrintTouched();
        }

        /// <summary> Returns the amount of colliders that this collider is touching. </summary>
        public int HoveredCount
        {
            get { return this.impassablesTouched.Count; }
        }

        /// <summary> Attempt to add a collider to the list </summary>
        /// <param name="col"></param>
        protected void TryAddCollider(Collider col)
        {
            if (!col.isTrigger)
            {
                bool added = SG.Util.SG_Util.SafelyAdd(col, this.impassablesTouched);
                if (added)
                {
                    this.UpdateDebugger();
                }
            }
        }

        /// <summary> Remove a collider from the list of touched colliders, if we are touching it. </summary>
        /// <param name="col"></param>
        protected void TryRemoveCollider(Collider col)
        {
            bool removed = SG.Util.SG_Util.SafelyRemove(col, this.impassablesTouched);
            if (removed)
            {
                this.UpdateDebugger();
            }
        }

        /// <summary> Ensure that deleted or disabledcolliders are removed from our list. </summary>
        /// <returns></returns>
        public int ValidateDetectedColliders()
        {
            int deleted = 0;
            for (int i = 0; i < this.impassablesTouched.Count;)
            {
                if (impassablesTouched[i] == null || !impassablesTouched[i].enabled
                    || !impassablesTouched[i].gameObject.activeInHierarchy)
                {
                    impassablesTouched.RemoveAt(i);
                    deleted++;
                }
                else
                {
                    i++;
                }
            }
            return deleted;
        }


        //------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        protected override void Awake()
        {
            base.Awake();
            this.physicsBody = SG.Util.SG_Util.TryAddRB(this.gameObject, false, true); //make sure I have a rigidBody to trigger non-trigger colliders that don't have it.
            Collider[] myColliders = this.GetComponents<Collider>();
            for (int i=0; i<myColliders.Length; i++)
            {
                myColliders[i].isTrigger = true;
            }
            UpdateDebugger();
        }


        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            //ToDo: Call a wellness check on existsing objects?
            int deletedElements = this.ValidateDetectedColliders();
            if (deletedElements > 0)
            {
                UpdateDebugger();
            }
        }

        protected virtual void OnTriggerEnter(Collider col)
        {
            TryAddCollider(col);
        }

        protected virtual void OnTriggerExit(Collider col)
        {
            TryRemoveCollider(col);
        }


    }
}
