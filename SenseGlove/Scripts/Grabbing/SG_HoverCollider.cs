using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> A script that keeps track of multiple SG_Interactable objects it collides with. </summary>
    public class SG_HoverCollider : SG_SimpleTracking
    {
        //----------------------------------------------------------------------------------------------
        // Detection Arguments

        /// <summary> A class containing everything related to a SG_Interactable we're touching.  </summary>
        public class DetectionArgs
        {
            /// <summary> The interactable we're currently hovering over. </summary>
            public SG_Interactable Interactable
            {
                get; private set;
            }

            /// <summary> How many colliders of Interactable we're currently touching. </summary>
            public int CollidersInside
            {
                get; private set;
            }

            /// <summary> Create a new instance of the DetectionArgs </summary>
            /// <param name="iScript"></param>
            /// <param name="collidersNowInside"></param>
            public DetectionArgs(SG_Interactable iScript, int collidersNowInside = 1)
            {
                Interactable = iScript;
                CollidersInside = collidersNowInside;
            }


            /// <summary> Returns true if our Interactable is the same as otherScript. </summary>
            /// <remarks> Placed in a separate function so we can optionally so some checks on a script level. </remarks>
            /// <param name="otherScript"></param>
            /// <returns></returns>
            public bool SameScript(SG_Interactable otherScript/*, bool debug*/)
            {
                //if (debug)
                //    Debug.Log("Comparing " + Interactable.name + " to " + otherScript.name + ", result will be " + (this.Interactable == otherScript ? "TRUE" : "FALSE"));
                return this.Interactable == otherScript;
            }


            /// <summary> Retruns true if we're still touching at least one collider of this same Interactable.  </summary>
            public bool StillHovering
            {
                get { return CollidersInside > 0; }
            }


            /// <summary> Returns true if this Interactable can no longer be located, or if it is disabled. </summary>
            public bool IsMissing
            {
                get
                {
                    if (this.Interactable != null)
                    {
                        return !this.Interactable.gameObject.activeInHierarchy;
                    }
                    return true; //otherwise it's been destroyed
                }
            }


            /// <summary> Notify we're touching (another) collider of this same Interactable. </summary>
            public void AddCollider()
            {
                CollidersInside = CollidersInside + 1;
            }

            /// <summary> Notify we're no longer touching a collider of this same Interactable.  </summary>
            public void RemoveCollider()
            {
                CollidersInside = CollidersInside - 1;
            }

        }

        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The list of interactables that are currently being touched. </summary>
        protected List<DetectionArgs> interactablesTouched = new List<DetectionArgs>();


        //----------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> The interactable objects that this script is currently touching </summary>
        public SG_Interactable[] TouchedObjects
        {
            get
            {
                SG_Interactable[] res = new SG_Interactable[interactablesTouched.Count];
                for (int i=0; i< interactablesTouched.Count; i++)
                {
                    res[i] = interactablesTouched[i].Interactable;
                }
                return res;
            }
        }

        //----------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Return treu if this script is touching an object </summary>
        /// <returns></returns>
        public bool IsTouching() { return interactablesTouched.Count > 0; }


        /// <summary> Returns true if this script is touching a specific SG_Interactable. </summary>
        /// <param name="interactable"></param>
        /// <returns></returns>
        public bool IsTouching(SG_Interactable interactable)
        {
            for (int i = 0; i < interactablesTouched.Count; i++)
            {
                if (interactablesTouched[i].SameScript(interactable/*, false*/)) { return true; }
            }
            return false;
        }


        /// <summary> Returns a list of interactables that are touched by both this hoverCollider and another hoverCollider </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SG_Interactable[] MatchingObjects(SG_HoverCollider other)
        {
            List<SG_Interactable> bothTouch = new List<SG_Interactable>();
            SG_Interactable[] touchedOther = other.TouchedObjects;
            for (int i = 0; i < touchedOther.Length; i++)
            {
                if (this.IsTouching(touchedOther[i]))
                {
                    bothTouch.Add(touchedOther[i]);
                }
            }
            return bothTouch.ToArray();
        }


        /// <summary> Clear this scripts references to other scripts. </summary>
        public void ClearTouchedObjects()
        {
            interactablesTouched.Clear();
        }


        /// <summary> Retrieve a SG_Interactable object from a collider. Returns true if one is found. </summary>
        /// <param name="col"></param>
        /// <param name="interactable"></param>
        /// <param name="favourSpecific"></param>
        /// <returns></returns>
        public static bool GetInteractableScript(Collider col, out SG_Interactable interactable, bool favourSpecific = true)
        {
            SG_Interactable myScript = col.gameObject.GetComponent<SG_Interactable>();
            if (myScript != null && favourSpecific) //we favour the 'specific' material over a global material.
            {
                interactable = myScript;
                return true;
            }
            //myMat might exist, but we favour the connected one if possible.
            SG_Interactable connectedScript = col.attachedRigidbody != null ?
                col.attachedRigidbody.gameObject.GetComponent<SG_Interactable>() : null;

            if (connectedScript == null) { interactable = myScript; } //the connected body does not have a material, so regardless we'll try the specific one.
            else { interactable = connectedScript; }
            return interactable != null;
        }


        /// <summary> Returns the index of an SG_Interactable in this script's touchedObjects. </summary>
        /// <param name="iScript"></param>
        /// <returns></returns>
        protected int ListIndex(SG_Interactable iScript)
        {
            for (int i = 0; i < interactablesTouched.Count; i++)
            {
                if (interactablesTouched[i].SameScript(iScript/*, this.name.Contains("Index")*/))
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary> Remove an Interactable at this position in our list. Returns ture if succesful. </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected bool RemoveFromList(int index)
        {
            if (index > -1 && index < interactablesTouched.Count)
            {
                interactablesTouched.RemoveAt(index);
                return true;
            }
            return false;
        }


        /// <summary> Check for missing objects (that no longer exist, etc. Clear them automatically.. </summary>
        protected void CheckMissingObjects()
        {
            for (int i=0; i<this.interactablesTouched.Count; ) //no i++
            {
                if (interactablesTouched[i].IsMissing) 
                {
                    bool removed = RemoveFromList(i);
                    if ( !removed ) { i++; }  //if we were unable to remove the object, still increment, otherwise, we're stuck in a loop..
                }
                else { i++; }
            }
        }

        /// <summary> Add (another) collider of a particular Interactable to our list. </summary>
        /// <param name="iScript"></param>
        public void AddCollider(SG_Interactable iScript)
        {
            int index = ListIndex(iScript);
            if (index > -1) //this is an object in the list
            {
                this.interactablesTouched[index].AddCollider();
            }
            else
            {
                this.interactablesTouched.Add(new DetectionArgs(iScript));
            }
        }


        /// <summary> Remove a collider of a particular Interactable to our list. Remove the Interactable if we're no longer touching it. </summary>
        /// <param name="iScript"></param>
        public void RemoveCollider(SG_Interactable iScript)
        {
            int index = ListIndex(iScript);
            if (index > -1) //this is an object in the list
            {
                interactablesTouched[index].RemoveCollider();
                if (!interactablesTouched[index].StillHovering)
                {
                    this.interactablesTouched.RemoveAt(index);
                }
            }
        }


        //----------------------------------------------------------------------------------------------
        // Monobehaviour


        protected override void Awake()
        {
            base.Awake();
            Util.SG_Util.TryAddRB(this.gameObject, false, true);
        }

        protected override void Update()
        {
            base.Update();
            CheckMissingObjects();
        }


        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.activeInHierarchy)
            {
                SG_Interactable iScript;
                if (GetInteractableScript(other, out iScript)) //this is an interactable object
                {
                    AddCollider(iScript);
                }
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            SG_Interactable iScript;
            if (GetInteractableScript(other, out iScript)) //this is an interactable object
            {
                RemoveCollider(iScript);
            }
        }

    }
}