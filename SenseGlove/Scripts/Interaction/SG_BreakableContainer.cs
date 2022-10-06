using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> A SenseGlove_Breakable that contains objects and optionally spawns shards of itself upon breaking. </summary>
    public class SG_BreakableContainer : SG_Breakable
    {
        //----------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> Contains the GameObjects that represent the shards of the broken object. </summary>
        [Header("Container Components")]
        [Tooltip("Contains the GameObjects that represent the shards of the broken object.")]
        public GameObject shardContainer;

        /// <summary> Contains SG_Interactable objects that will be released upon the container breaking. </summary>
        [Tooltip(" Contains SG_Interactable objects that will be released upon the container breaking.")]
        public GameObject contentsContainer;

        /// <summary> Determines if the contents are placed back into the container when the object is unbroken. </summary>
        [Tooltip("Determines if the contents are placed back into the container when the object is unbroken.")]
        public bool unbreakWithContents = false;

        /// <summary> All GameObjects within the shardsContainer. Will be spawned at the time of breaking.  </summary>
        protected GameObject[] brokenShards = new GameObject[0];

        /// <summary> All SenseGlove_Interactables within the container. Will be set to interactable at the time of breaking. </summary>
        protected SG_Interactable[] contents = new SG_Interactable[0];

        /// <summary> The localRotations of the shards, applied on a reset. </summary>
        protected Quaternion[] contentRotations;
        /// <summary> The localPositions of the shards, applied on reset. </summary>
        protected Vector3[] contentPositions;

        /// <summary> The localRotations of the shards, applied on a reset. </summary>
        protected Quaternion[] shardRotations;
        /// <summary> The localPositions of the shards, applied on reset. </summary>
        protected Vector3[] shardPositions;

        //----------------------------------------------------------------------------------------------------
        // Monobehaviour

        //Collect object data before wakeup.
        protected virtual void Awake()
        {
            if (shardContainer != null)
            {
                this.shardContainer.SetActive(true);

                this.brokenShards = new GameObject[this.shardContainer.transform.childCount];
                this.shardPositions = new Vector3[brokenShards.Length];
                this.shardRotations = new Quaternion[brokenShards.Length];
                for (int i = 0; i < this.brokenShards.Length; i++)
                {
                    this.brokenShards[i] = this.shardContainer.transform.GetChild(i).gameObject;
                    SetRB(this.brokenShards[i], false, true);
                    this.shardRotations[i] = this.brokenShards[i].transform.localRotation;
                    this.shardPositions[i] = this.brokenShards[i].transform.localPosition;
                    this.brokenShards[i].SetActive(false);
                }
                this.ResetShards();
            }

            if (this.contentsContainer != null)
            {
                this.contentsContainer.SetActive(true);

                this.contents = new SG_Interactable[this.contentsContainer.transform.childCount];
                this.contentPositions = new Vector3[contents.Length];
                this.contentRotations = new Quaternion[contents.Length];
                for (int i = 0; i < this.contents.Length; i++)
                {
                    this.contents[i] = this.contentsContainer.transform.GetChild(i).GetComponent<SG_Interactable>();

                    this.contents[i].enabled = false;
                    SetRB(this.contents[i].gameObject, false, true);

                    this.contentRotations[i] = this.contents[i].transform.localRotation;
                    this.contentPositions[i] = this.contents[i].transform.localPosition;
                }
                this.ResetContents();
            }
        }

        //----------------------------------------------------------------------------------------------------
        // Class Methods

        /// <summary> Called when the breakable material of the wholeObject is broken </summary>
        public override void Break()
        {
            if (this.IsBroken() || !breakingAllowed) { return; }

            base.Break(); //make sure it broke first

            this.SpawnContents();
            this.SpawnShards();
        }

        /// <summary> Called when the breakable material is reset. </summary>
        public override void UnBreak()
        {
            this.ResetShards();

            if (this.unbreakWithContents)
                this.ResetContents();

            base.UnBreak();
        }

        /// <summary> This always resets contents, while Unbreak (called within ResetObjects) does not reset the contents. </summary>
        public override void ResetObject()
        {
            base.ResetObject();

            if (!this.unbreakWithContents) //reset the contents if we have not already.
                this.ResetContents();
        }

        //----------------------------------------------------------------------------------------------------
        // Content Methods

        /// <summary> Spawns the Shards when the container breaks. </summary>
        protected void SpawnShards()
        {
            for (int i = 0; i < this.brokenShards.Length; i++)
            {
                if (brokenShards[i] != null)
                {
                    this.brokenShards[i].transform.parent = null;
                    SetRB(this.brokenShards[i], true, false);
                    this.brokenShards[i].SetActive(true); //let them do their thing
                }
            }
        }

        /// <summary> Put all the shards back to their original (local) transform. </summary>
        protected void ResetShards()
        {
            for (int i = 0; i < this.brokenShards.Length; i++)
            {
                if (brokenShards[i] != null)
                {
                    this.brokenShards[i].SetActive(false); //now turn them off.
                    SetRB(this.brokenShards[i], false, true); //stops them from moving
                    this.brokenShards[i].transform.parent = this.shardContainer.transform; //reset parent
                    this.brokenShards[i].transform.localPosition = this.shardPositions[i]; //and local position / rotation
                    this.brokenShards[i].transform.localRotation = this.shardRotations[i];
                }
            }
        }

        /// <summary> Spawns Contents when the container breaks </summary>
        /// <remarks> The contents have been visible all along, they just havent been active. </remarks>
        protected void SpawnContents()
        {
            for (int i = 0; i < this.contents.Length; i++)
            {
                this.contents[i].transform.parent = null;
                this.contents[i].enabled = (true);
                SetRB(this.contents[i].gameObject, true, false);
                SetColliders(this.contents[i].gameObject, false);
            }
        }

        /// <summary> Resets the contents back to their original (local) transforms. </summary>
        protected void ResetContents()
        {
            for (int i = 0; i < this.contents.Length; i++)
            {
                this.contents[i].enabled = (false);
                SetColliders(this.contents[i].gameObject, true);
                SetRB(this.contents[i].gameObject, false, true);
                
                this.contents[i].transform.parent = this.contentsContainer.transform;
                this.contents[i].transform.localPosition = this.contentPositions[i];
                this.contents[i].transform.localRotation = this.contentRotations[i];
            }
        }


        //------------------------------------------------------------------------------------------------------------
        // Static Methods

        /// <summary> Set the Rigidbody options of a particular gameObject, if the object has any. </summary>
        /// <param name="Obj"></param>
        /// <param name="gravity"></param>
        /// <param name="kinematic"></param>
        protected static void SetRB(GameObject obj, bool gravity, bool kinematic)
        {
            Rigidbody BR = obj.GetComponent<Rigidbody>();
            if (BR != null)
            {
                BR.useGravity = gravity;
                BR.isKinematic = kinematic;
            }
        }

        /// <summary> Set the Collider options of a particular GameObject, if the object has any.  </summary>
        /// <param name="obj"></param>
        /// <param name="trigger"></param>
        protected static void SetColliders(GameObject obj, bool trigger)
        {
            Collider[] colliders = obj.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].isTrigger = trigger;
            }
        }


    }
}