using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> A script that keeps track of multiple SG_Interactable objects it collides with. </summary>
    public class SG_HoverCollider : SG_SimpleTracking
    {
        /// <summary> The list of interactables that are currently being touched. </summary>
        protected List<SG_Interactable> interactablesTouched = new List<SG_Interactable>();
        /// <summary> The number of colliders for each interactable that this script is touching. </summary>
        protected List<int> collidersInside = new List<int>();


        /// <summary> The interactable objects that this script is currently touching </summary>
        public SG_Interactable[] TouchedObjects
        {
            get { return interactablesTouched.ToArray(); }
        }

        /// <summary> Return treu if this script is touching an object </summary>
        /// <returns></returns>
        public bool IsTouching() { return interactablesTouched.Count > 0; }

        /// <summary> Returns true if this script is touching a specific GameObject. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsTouching(GameObject obj)
        {
            for (int i = 0; i < interactablesTouched.Count; i++)
            {
                if (GameObject.ReferenceEquals(interactablesTouched[i].gameObject, obj)) { return true; }
            }
            return false;
        }

        /// <summary> Returns true if this script is touching a specific SG_Interactable. </summary>
        /// <param name="interactable"></param>
        /// <returns></returns>
        public bool IsTouching(SG_Interactable interactable)
        {
            for (int i = 0; i < interactablesTouched.Count; i++)
            {
                if (interactablesTouched[i] == interactable) { return true; }
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
            collidersInside.Clear();
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


        /// <summary> Checks if a collider is connected to a specific touchedScript </summary>
        /// <param name="col"></param>
        /// <param name="touchedScript"></param>
        /// <returns></returns>
        public static bool SameScript(Collider col, SG_Interactable touchedScript)
        {
            if (touchedScript != null && col != null)
            {
                if (GameObject.ReferenceEquals(col.gameObject, touchedScript.gameObject))
                    return true; //this is the touched object.
                                 // at this line, col does not have the same material, but perhaps its attachedRigidbody does.
                return col.attachedRigidbody != null && GameObject.ReferenceEquals(col.attachedRigidbody.gameObject,
                    touchedScript.gameObject);
            }
            return false;
        }


        /// <summary> Returns the index of an SG_Interactable in this script's touchedObjects. </summary>
        /// <param name="iScript"></param>
        /// <returns></returns>
        protected int ListIndex(SG_Interactable iScript)
        {
            for (int i = 0; i < interactablesTouched.Count; i++)
            {
                if (GameObject.ReferenceEquals(iScript.gameObject, interactablesTouched[i].gameObject))
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary> Add a new (collider of) an SG_Interactable script to this script's touchedObjects </summary>
        /// <param name="script"></param>
        protected void AddToList(SG_Interactable script)
        {
            int index = ListIndex(script);
            if (index > -1) //different collider of existing object
            {
                collidersInside[index] = collidersInside[index] + 1; //don;t think += 1 works here...?
            }
            else //collider of a new object
            {
                interactablesTouched.Add(script);
                collidersInside.Add(1);
            }
        }

        /// <summary> Remove a collider from this script's touchedObjects </summary>
        /// <param name="col"></param>
        protected void RemoveFromList(Collider col)
        {
            for (int i = 0; i < interactablesTouched.Count;)
            {
                if (SameScript(col, interactablesTouched[i]))
                {  //removed this script
                    collidersInside[i] = collidersInside[i] - 1; //don;t think -= 1 works here...?
                    if (collidersInside[i] <= 0)
                    {
                        interactablesTouched.RemoveAt(i);
                        collidersInside.Remove(i);
                    }
                    else { i++; }
                }
                else { i++; }
            }
        }




        protected override void Awake()
        {
            base.Awake();
            SG_Util.TryAddRB(this.gameObject, false, true);
        }


        protected virtual void OnTriggerEnter(Collider other)
        {
            SG_Interactable iScript;
            if (GetInteractableScript(other, out iScript)) //this is an interactable object
            {
                AddToList(iScript);
            }
        }


        protected virtual void OnTriggerExit(Collider other)
        {
            RemoveFromList(other);
        }

    }
}