using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    //---------------------------------------------------------------------------------------------------------------------------------------------
    // Detection Arguments

    /// <summary> Base Class to override when you've got additional paramaters that must be added. Nested inside this script because it's useless outside of it.</summary>
    public class DetectArguments
    {
        /// <summary> The original script attached to the first collider, that may have other colliders attached to it. </summary>
        public MonoBehaviour script;

        /// <summary> All of the colliders this script is touching. </summary>
        protected List<Collider> touchedColliders = new List<Collider>();

        /// <summary> Empty so we can do weird stuff with subclasses. </summary>
        protected DetectArguments() { }
        

        /// <summary> Creates a new instance of DetectArguments </summary>
        /// <param name="touchedScript"></param>
        /// <param name="firstCollider"></param>
        public DetectArguments(MonoBehaviour touchedScript, Collider firstCollider)
        {
            script = touchedScript;
            touchedColliders.Add(firstCollider);
        }


        /// <summary> Check how many colliders this script has touched. </summary>
        public int CollidersTouched
        {
            get { return this.touchedColliders.Count; }
        }

        /// <summary> Simple ToString representation for debugging </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.script != null ? this.script.name + ": " + this.CollidersTouched + " Colliders" : "NULL";
        }

        /// <summary> Print all colliders in a readable format for debugging purposes. </summary>
        /// <returns></returns>
        public string PrintContents()
        {
            if (this.script != null)
            {
                string res = this.script.name + ": ";
                for (int i = 0; i < this.touchedColliders.Count; i++)
                {
                    res += touchedColliders[i].name;
                    if (i < touchedColliders.Count - 1) { res += ", "; }
                }
                return res;
            }
            return "NULL";
        }

        /// <summary> Adds a new collider to this script's touchedcolliders, provided it doesn't already have a refrence to it </summary>
        /// <param name="col"></param>
        public void Add(Collider col)
        {
            if (!touchedColliders.Contains(col))
            {
                touchedColliders.Add(col);
            }
        }

        /// <summary> Remove a specific collider from the touchedColliders. </summary>
        /// <param name="col"></param>
        public void Remove(Collider col)
        {
            touchedColliders.Remove(col);
        }

        /// <summary> Remove a collider at a specific index. </summary>
        /// <param name="colliderIndex"></param>
        public void RemoveAt(int colliderIndex)
        {
            touchedColliders.RemoveAt(colliderIndex);
        }

        /// <summary> Retrieve the index of a collider inside this script's list. -1 if it's not a part of this one. </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public int ColliderIndex(Collider col)
        {
            return Util.SG_Util.ListIndex(touchedColliders, col);
        }

        /// <summary> Cheks if colliders are deleted. Returns the amount of elements that were deleted. </summary>
        /// <returns></returns>
        public int ValidateColliders()
        {
            int deleted = 0;
            for (int i = 0; i < this.touchedColliders.Count;)
            {
                if (touchedColliders[i] == null)
                {
                    Debug.Log("A Collider belonging to " + (this.script != null ? this.script.name : "N\\A") + " was deleted. Removing it from the list.");
                    touchedColliders.RemoveAt(i);
                    deleted++;
                }
                else
                {
                    i++;
                }
            }
            return deleted;
        }

        public List<Collider> GetColliders()
        {
            return this.touchedColliders;
        }

    }

    /// <summary> Keeps track of all the colliders of GameObejcts we're touching, provided these contain a particular script. </summary>
    /// <remarks> Since I have so many Script to detect physics stuff, I'm hoping to standardize this behaviour. </remarks>
    public class SG_ScriptDetector<T> where T : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------
        // SG_ScriptDetector Code

        /// <summary> When detecting a collider with a script on both itself and on its attached rigidBody (on another object), which of the two is preferred? 
        /// If true, the collider, if false, the attachedRigidBody.  </summary>
        public bool favourSpecificScripts = true;

        /// <summary> All detected Scripts </summary>
        public List<DetectArguments> detectedScripts = new List<DetectArguments>();


        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Construction

        /// <summary> Creates a new Instance of a SG_ScriptDetector. </summary>
        public SG_ScriptDetector() { }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Utility

        /// <summary> Returns the amount of scripts detected by this ScriptDetector. </summary>
        public int DetectedCount() { return this.detectedScripts.Count; }

        /// <summary> Returns the number of colliders touching a particular script. If we haven't detected it yet, we'll return 0. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public int ColliderCount(T script)
        {
            int scriptIndex = ListIndex(script, this.detectedScripts);
            return scriptIndex > -1 ? this.detectedScripts[scriptIndex].CollidersTouched : 0;
        }

        /// <summary> Returns true if this ScriptDetector has detected a specific script. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public bool IsDetected(T script)
        {
            return ListIndex(script, this.detectedScripts) > -1;
        }

        /// <summary> Returns the index of script in this object's list. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public int DetectionIndex(T script)
        {
            return ListIndex(script, this.detectedScripts);
        }

        /// <summary> Returns a list of all unique detected scripts currently in this Detector. </summary>
        /// <returns></returns>
        public T[] GetDetectedScripts()
        {
            T[] res = new T[this.detectedScripts.Count];
            for (int i=0; i<this.detectedScripts.Count; i++)
            {
                res[i] =( (T) this.detectedScripts[i].script ); 
            }
            return res;
        }

        public List<Collider> GetColliders()
        {
            //TODO: Optimize this by keeping track of a list of all colliders in the zone(?)
            List<Collider> res = new List<Collider>(this.detectedScripts.Count);
            for (int i=0; i<this.detectedScripts.Count; i++)
            {
                res.AddRange( detectedScripts[i].GetColliders());
            }
            return res;
        }


        /// <summary> Print all detected scripts and their colliders in  </summary>
        /// <returns></returns>
        public string PrintContents(string delim = "\n", bool listColliders = true)
        {
            string res = "";
            for (int i=0; i<detectedScripts.Count; i++)
            {
                res += listColliders ? detectedScripts[i].PrintContents() : detectedScripts[i].ToString();
                if (i < detectedScripts.Count - 1) { res += delim; }
            }
            return res;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Adding / Removing Colliders

        /// <summary> Returns the index of a specific script. </summary>
        /// <remarks> Implemented separately from SG_Util.ListIndex, since I'm checking for the validity of .script, and not detected[i]. </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static int ListIndex(MonoBehaviour foundScript, List<DetectArguments> alreadydetected)
        {
            for (int i=0; i< alreadydetected.Count; i++)
            {
                if (alreadydetected[i].script == foundScript)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary> Generates a brand new DetectionArgument. Override this if you wish to add a different subclass of DetectArguments. </summary>
        /// <param name="firstCollider"></param>
        /// <param name="detectedScript"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        protected virtual DetectArguments CreateArguments(Collider firstCollider, T detectedScript, MonoBehaviour source)
        {
            return new DetectArguments(detectedScript, firstCollider);
        }


        /// <summary> Attempt to add a collider to the list that may or may not have a Component T attached to it. 
        /// When 2 is returned, it's a new script we didn't encounter yet.
        /// When 1,is returned, the collider was added to an existsing script,
        /// When 0 is returned, this is a useless collider. </summary>
        /// <param name="col"></param>
        /// <param name="source"> From which script this object comes. Used to generate any new items </param>
        /// <param name="touchedScript"> optional paramater to pass if you want to do something if it's added. (code 1 or 2) </param>
        /// <returns></returns>
        public virtual int TryAddList(Collider col, MonoBehaviour source, out T touchedScript)
        {
            //step 1: Extract the script we're looking for.
            if (Util.SG_Util.GetScript(col, out touchedScript))
            {
                //the script exists.
                int listIndex = ListIndex(touchedScript, this.detectedScripts);
                if (listIndex < 0) 
                {
                    DetectArguments args = CreateArguments(col, touchedScript, source);
                    this.detectedScripts.Add(args);
                    return 2;
                }
                //we detected it already, this is a second collider.
                this.detectedScripts[listIndex].Add(col);
                return 1;
            }
            return 0;
        }

        /// <summary> Attempt to add a collider to the list that may or may not have a Component T attached to it. 
        /// When 2 is returned, it's a new script we didn't encounter yet.
        /// When 1,is returned, the collider was added to an existsing script,
        /// When 0 is returned, this is a useless collider.</summary>
        /// <param name="col"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public int TryAddList(Collider col, MonoBehaviour source)
        {
            T temp;
            return this.TryAddList(col, source, out temp);
        }


        /// <summary> Add a script-collider combination to this one's list. Used after you've already found the monobehavior.  </summary>
        /// <param name="touchedScript"></param>
        /// <param name="col"></param>
        /// <param name="source"></param>
        /// <returns> The amount of colliders of T in my list after the operation </returns>
        public virtual int AddToList(T touchedScript, Collider col, MonoBehaviour source)
        {
            //the script exists.
            int listIndex = ListIndex(touchedScript, this.detectedScripts);
            if (listIndex < 0)
            {
                DetectArguments args = CreateArguments(col, touchedScript, source);
                this.detectedScripts.Add(args);
                return args.CollidersTouched;
            }
            //we detected it already, this is a second collider.
            this.detectedScripts[listIndex].Add(col);
            return this.detectedScripts[listIndex].CollidersTouched;
        }


        /// <summary> Attempt to remove a collider from our list. 
        /// Returns 0 if it wasn't removed. 
        /// 1 if it was removed, but we still have other coliders in memory, 
        /// 2 if it caused us to no longer have the script in memory. </summary>
        /// <param name="col"></param>
        /// <param name="removedItem"> Optional paramater to pass if you want to do something if a collider is removed (code 1 or 2) </param>
        /// <returns></returns>
        public virtual int TryRemoveList(Collider col, out T removedItem)
        {
            for (int i=0; i<this.detectedScripts.Count; i++)
            {
                int index = detectedScripts[i].ColliderIndex(col);
                if (index > -1) //the collider exists within this script.
                {
                    removedItem = (T) detectedScripts[i].script; //I think I'm only able to cast like this because I explicitly stated T : Monobehaviour earlier?
                    detectedScripts[i].RemoveAt(index); //removes this collider specifically from detectedScripts[i].
                    if (detectedScripts[i].CollidersTouched == 0) //it's now empty
                    {
                        detectedScripts.RemoveAt(i); //remove it from the list entirely.
                        return 2;
                    }
                    return 1;
                }
            }
            removedItem = null;
            return 0;
        }

        public virtual int RemoveFromList(Collider colToRemove, T associatedScript)
        {
            int listIndex = ListIndex(associatedScript, this.detectedScripts); 
            if (listIndex > -1) //this is a script in my list
            {
                //remove said collider
                this.detectedScripts[listIndex].Remove(colToRemove);
                if (detectedScripts[listIndex].CollidersTouched == 0) //it's now empty
                {
                    detectedScripts.RemoveAt(listIndex); //remove it from the list entirely.
                    return 2;
                }
                return 1;
            }
            return 0;
        }


        /// <summary> Try to remove something from the list, but you don't need a reference to the script that may have been removed. 
        /// Returns 0 if it wasn't removed. 
        /// 1 if it was removed, but we still have other coliders in memory, 
        /// 2 if it caused us to no longer have the script in memory. </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public int TryRemoveList(Collider col)
        {
            T temp;
            return this.TryRemoveList(col, out temp);
        }

        /// <summary> Remove everything related to a specific script off this detector </summary>
        /// <returns></returns>
        public bool ClearCollisions(T script)
        {
            int index = ListIndex(script, this.detectedScripts);
            if (index > -1) //this script is in the list
            {
                detectedScripts.RemoveAt(index); //cleared.
            }
            return false;
        }


        /// <summary> Return the script associated with the chosen collider. </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool GetAssociatedScript(Collider col, out MonoBehaviour connectedScript)
        {
            for (int i=0; i<this.detectedScripts.Count; i++)
            {
                if (detectedScripts[i].ColliderIndex(col) > -1)
                {
                    connectedScript = detectedScripts[i].script;
                    return true;
                }
            }
            connectedScript = null;
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Check if the object should still be alive

        /// <summary> Ensures that objects no longer in existence are cleaned up. Returns the amount of deleted elements (both colliders AND scripts). </summary>
        /// <param name="source"></param>
        /// <param name="validateColliders"></param>
        /// <returns></returns>
        public virtual int ValidateDetectedObjects(MonoBehaviour source, bool validateColliders = true)
        {
            int deleted = 0;
            for (int i=0; i<detectedScripts.Count;)
            {
                if (detectedScripts[i].script == null)
                {
                    Debug.Log(source.name + " found a deleted script in a ScriptDetector. Clearing it.");
                    detectedScripts.RemoveAt(i);
                    deleted++;
                }
                else if (!detectedScripts[i].script.gameObject.activeInHierarchy)
                {
                   // Debug.Log(source.name + ": " + detectedScripts[i].script.name + " is no longer active in the scene. Clearing it.");
                    detectedScripts.RemoveAt(i);
                    deleted++;
                }
                else if (validateColliders)
                {
                    //object is ok, let's check its colliders;
                    deleted += detectedScripts[i].ValidateColliders(); //calidateColliders returns the number of elements that were deleted.
                    if (detectedScripts[i].CollidersTouched == 0)
                    {
                        detectedScripts.RemoveAt(i);
                        deleted++;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
            return deleted;
        }

        /// <summary> Remove any scripts or colliders that have been deleted or disabled. the out variables tell you how many were deleted </summary>
        /// <param name="source"></param>
        /// <param name="deletedScripts">The number of scripts that were removed from the list.</param>
        /// <param name="validateColliders">If false, we do not check colliders. Faster, but you may miss a deleted one.</param>
        /// <param name="collidersMustChange">If true, whe a script has 0 colliders remaining, we only delete it from the list if we were the one to remove the last collider. Used for snapping objects outside of a SnapZone.</param>
        public void ValidateDetectedObjects(MonoBehaviour source, out int deletedScripts, bool validateColliders = true, bool collidersMustChange = false)
        {
            int temp;
            ValidateDetectedObjects(source, out deletedScripts, out temp, validateColliders, collidersMustChange);
        }

        /// <summary> Remove any scripts or colliders that have been deleted or disabled. the out variables tell you how many were deleted </summary>
        /// <param name="source"></param>
        /// <param name="deletedScripts">The number of scripts that were removed from the list.</param>
        /// <param name="deletedColliders">The amount of colliders that were removed from the list.</param>
        /// <param name="validateColliders">If false, we do not check colliders. Faster, but you may miss a deleted one.</param>
        /// <param name="collidersMustChange">If true, whe a script has 0 colliders remaining, we only delete it from the list if we were the one to remove the last collider. Used for snapping objects outside of a SnapZone.</param>
        public void ValidateDetectedObjects(MonoBehaviour source, out int deletedScripts, out int deletedColliders, bool validateColliders = true, bool collidersMustChange = false)
        {
            deletedScripts = 0;
            deletedColliders = 0;
            for (int i = 0; i < detectedScripts.Count;)
            {
                if (detectedScripts[i].script == null)
                {
                    Debug.Log(source.name + " found a deleted script in a ScriptDetector. Clearing it.");
                    detectedScripts.RemoveAt(i);
                    deletedScripts++;
                }
                else if (!detectedScripts[i].script.gameObject.activeInHierarchy)
                {
                    Debug.Log(detectedScripts[i].script.name + " is disabled. Clearing it.");
                    detectedScripts.RemoveAt(i);
                    deletedScripts++;
                }
                else if (validateColliders)
                {
                    //object is ok, let's check its colliders;
                    int collidersBefore = detectedScripts[i].CollidersTouched;
                    deletedColliders += detectedScripts[i].ValidateColliders(); //calidateColliders returns the number of elements that were deleted.
                    if (detectedScripts[i].CollidersTouched == 0 && (!collidersMustChange || detectedScripts[i].CollidersTouched != collidersBefore) ) //that was the last one AND we want to remove on change
                    {
                        detectedScripts.RemoveAt(i);
                        deletedScripts++;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
        }


        /*
        //---------------------------------------------------------------------------------------------------------------------------------------------
        // TODO: Events: So we can listen to a specific ScriptDetector

        public class SD_EventArgs : System.EventArgs
        {
            public DetectArguments Detection
            {
                get; private set;
            }

            public SD_EventArgs(DetectArguments args)
            {
                this.Detection = args;
            }
        }

        // Declare the delegate (if using non-generic pattern).
        public delegate void SD_EventHandler(object sender, SD_EventArgs e);

        // Declare the event(s)
        /// <summary> Raised after a new script has been added. </summary>
        public event SD_EventHandler ScriptAdded;
        /// <summary> Raised after a new script has been removed. </summary>
        public event SD_EventHandler ScriptRemoved;

        /// <summary> Raised whenever there is a change in colliders / objects. </summary>
        public event SD_EventHandler ColliderChange;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        protected virtual void RaiseAddedEvent(DetectArguments args)
        {
            // Raise the event in a thread-safe manner using the ?. operator.
            ScriptAdded?.Invoke(this, new SD_EventArgs(args));
        }
        */

    }
}