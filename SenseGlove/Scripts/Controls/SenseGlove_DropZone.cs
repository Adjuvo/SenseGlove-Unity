﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Allow users to pick snapped objects back up when takeFromHand is enabled.
//TODO: Apply the correct properties to the Grabable when the grabscript picks them up again (subscribe to OnPickedUp events?)
//TODO: Fire events when the objects have been inside the zone for X amounts of seconds.

/// <summary> Detects SenseGlove_Interactables and snaps them to the desired transform. </summary>
[RequireComponent(typeof(Collider))]
public class SenseGlove_DropZone : MonoBehaviour 
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    //Public properties

    /// <summary> If set to true, the SenseGlove_Interactable(s) within the dropzone will no longer be interactable. </summary>
    [Tooltip("If set to true, the SenseGlove_Interactable(s) within the dropzone will no longer be interactable.")]
    public bool disableInteration = false;

    /// <summary> Determines whether or not to snap object to this collider's origin. </summary>
    [Tooltip("Determines whether or not to snap object to this collider's origin.")]
    public bool snapToMe = false;

    /// <summary> Take objects from the grab script, one does not have to let go of the object. </summary>
    [Tooltip("Take objects from the grab script, one does not have to let go of the object.")]
    public bool takeFromHand = false;

    /// <summary> Optional transform that the object will copy. If left unassigned, the transform of this GameObject is used. </summary>
    [Tooltip("Optional transform that the object will copy. If left unassigned, the transform of this GameObject is used.")]
    public Transform snapTarget;

    /// <summary> The objects that should be inside this DropZone. Leave it empty to snap to all SenseGlove_Grabables. </summary>
    [Tooltip("The objects that should be inside this DropZone. Leave it empty to snap to all SenseGlove_Grabables.")]
    public List<SenseGlove_Grabable> objectsToGet = new List<SenseGlove_Grabable>();

    /// <summary> An optional highlight for this snapzone that can be turned on or off. </summary>
    [Tooltip("An optional highlight for this snapzone that can be turned on or off.")]
    public GameObject highLighter;

    // Private properties

    /// <summary> The list of objects currently inside this dropZone </summary>
    private List<SenseGlove_Grabable> objectsInside = new List<SenseGlove_Grabable>();

    /// <summary> Contains the original parent(s) of the snapped objects </summary>
    private List<Transform> originalParent = new List<Transform>();

    /// <summary> The original properties of the RigidBodies that this DropZone contains </summary>
    private List<bool[]> RBprops = new List<bool[]>();

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    //runs before anything else, used to validate settings
    void Awake()
    {
        for (int i=0; i<this.objectsToGet.Count;) //objectsToGet that are null should be removed.
        {
            if (this.objectsToGet[i] == null)
            {
                this.objectsToGet.RemoveAt(i);
            }
            else { i++; }
        }

        this.gameObject.GetComponent<Collider>().isTrigger = true;
    }

    //A new collider enters this dropzone
    void OnTriggerEnter(Collider col)
    {
        SenseGlove_Grabable grabable = col.GetComponent<SenseGlove_Grabable>();
        if (grabable && (!grabable.IsGrabbed() || this.takeFromHand))
        {   
            int index = this.ListIndex(grabable);
            //SenseGlove_Debugger.Log("Detected an Object with listIndex of " + index);
            if (index < 0) //its a new object, even if it has multiple colliders.
            {
               // SenseGlove_Debugger.Log("A new object!");
                if (this.objectsToGet.Count <= 0 || this.IsTarget(grabable)) //either we require multiple objects or it is one of the goal objects.
                {
                    this.AddObject(grabable);
                }
            }
        }
    }

    //a collider exists in the dropZone
    void OnTriggerStay(Collider col)
    {
        SenseGlove_Grabable grabable = col.GetComponent<SenseGlove_Grabable>();
        if (grabable && (!grabable.IsGrabbed() || this.takeFromHand))
        {
            int index = this.ListIndex(grabable);
            if (index < 0)
            {
                if (this.objectsToGet.Count <= 0 || this.IsTarget(grabable))
                {
                    //SenseGlove_Debugger.Log("A new object inside the dropZone!");
                    this.AddObject(grabable);   
                }
            }
        }
    }

    //A collider exits this dropZone
    void OnTriggerExit(Collider col)
    {
        SenseGlove_Grabable grabable = col.GetComponent<SenseGlove_Grabable>();
        if (grabable)
        {
            int index = this.ListIndex(grabable);
            //SenseGlove_Debugger.Log("Object with index " + index + " is leaving this dropzone");
            this.RemoveObject(index);
            
        }
    }

    #endregion Monobehaviour

    //--------------------------------------------------------------------------------------------------------------------------
    // Dropzone Logic

    #region DropzoneLogic

    /// <summary> Check if a SenseGlove_Grabable is (already) inside this dropzone. </summary>
    /// <param name="obj"></param>
    /// <returns>The index of obj in the objectsInside list. -1 if it does not exist inside this list.</returns>
    public int ListIndex(SenseGlove_Grabable obj)
    {
        for (int i=0; i<this.objectsInside.Count; i++)
        {
            if (GameObject.ReferenceEquals(obj.gameObject, this.objectsInside[i].gameObject))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary> Check if this SenseGlove_Object is one of the "goal" objects; </summary>
    /// <param name="obj"></param>
    /// <returns>The index of obj in the objectsToGet list. -1 if it does not exist inside this list.</returns>
    public bool IsTarget(SenseGlove_Grabable obj)
    {
        for (int i = 0; i < this.objectsToGet.Count; i++)
        {
            if (GameObject.ReferenceEquals(obj.gameObject, this.objectsToGet[i].gameObject))
            {
                return true;
            }
        }
        return false;
    }

    
    /// <summary> Add an object to this DropZone, and apply the desired settings. </summary>
    /// <param name="obj"></param>
    public void AddObject(SenseGlove_Grabable obj)
    {
        SenseGlove_Debugger.Log("Adding " + obj.name + " to the DropZone!");

        // remember original parent
        if (obj.IsGrabbed())  { this.originalParent.Add(obj.GetOriginalParent()); }
        else  { this.originalParent.Add(obj.transform.parent); }
        
        bool[] props = null;

        if (this.snapToMe && (this.takeFromHand || !obj.IsGrabbed()))
        {
            obj.EndInteraction();
            Transform zoneParent = this.snapTarget;
            if (zoneParent == null)
            {
                zoneParent = this.gameObject.transform;
            }

            obj.gameObject.transform.parent = zoneParent;
            obj.gameObject.transform.localPosition = Vector3.zero;
            obj.gameObject.transform.localRotation = Quaternion.identity;

            Rigidbody RB = obj.physicsBody;
            if (RB != null)
            {
                if (obj.IsGrabbed())  { props = obj.GetRBProps(); }
                else  { props = new bool[2] { RB.useGravity, RB.isKinematic }; }
                RB.useGravity = false;
                RB.isKinematic = true;
            }

        }

        this.RBprops.Add(props);
        this.objectsInside.Add(obj);
        obj.SetInteractable(!this.disableInteration); //enable / disable interactions.
        this.OnObjectDetected(obj);

    }

    /// <summary> Remove an object from this dropzone and restore its original settings. </summary>
    /// <param name="objectIndex"></param>
    public void RemoveObject(int objectIndex)
    {
        //SenseGlove_Debugger.Log("The script wishes to remove " + objectIndex);
        if (objectIndex >= 0 && objectIndex < this.objectsInside.Count)
        {
            SenseGlove_Debugger.Log("removing " + this.objectsInside[objectIndex].name + " from the DropZone!");
            SenseGlove_Grabable obj = this.objectsInside[objectIndex];

            SenseGlove_Debugger.Log("RBProps.lengh = " + RBprops.Count);

            if (obj.GetComponent<Rigidbody>() != null && RBprops[objectIndex] != null)
            {   //if it is currently picked up, we assign the previous properties to its grabscript, which will then apply them once it lets go.
                SenseGlove_Debugger.Log("It has a physicsBody");
                if (obj.IsGrabbed())
                {
                    obj.SetOriginalParent(this.originalParent[objectIndex]);
                    obj.SetRBProps(this.RBprops[objectIndex][0], this.RBprops[objectIndex][1]);
                }
                else
                {
                    obj.transform.parent = this.originalParent[objectIndex];
                    obj.physicsBody.useGravity = this.RBprops[objectIndex][0];
                    obj.physicsBody.isKinematic = this.RBprops[objectIndex][1];
                }
                
            }

            this.objectsInside.RemoveAt(objectIndex);
            SenseGlove_Debugger.Log("Removed it from ObjectsInside!");
            this.RBprops.RemoveAt(objectIndex);
            this.originalParent.RemoveAt(objectIndex);

            obj.SetInteractable(true); //now the function can also be used to force removal of the object.
            this.OnObjectRemoved(obj);
        }
    }

    
    //--------------------------------------------------------------------------------------------------------------------------
    // External Logic Methods

    

    /// <summary> Get a list of all objects inside this DropZone. </summary>
    /// <returns></returns>
    public SenseGlove_Grabable[] ObjectsInside()
    {
        return this.objectsInside.ToArray();
    }

    /// <summary> Check the amount of objects within this DropZone. </summary>
    /// <returns></returns>
    public int NumberOfObjects()
    {
        return this.objectsInside.Count;
    }

    /// <summary> Check if all desired objects have been detected. </summary>
    /// <returns></returns>
    public bool AllObjectsDetected()
    {
        return this.objectsInside.Count == this.objectsToGet.Count;
    }

    #endregion DropzoneLogic

    /// <summary>
    /// Enable / Disable the highlighter(s) of this dropzone.
    /// </summary>
    /// <param name="active"></param>
    public void SetHighLight(bool active)
    {
        if (this.highLighter != null)
        {
            this.highLighter.SetActive(active);
        }
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    #region Events


    //ObjectDetected
    public delegate void ObjectDetectedEventHandler(object source, DropZoneArgs args);
    /// <summary> Fires when one of the desired SenseGlove_Grabable objects is detected. </summary>
    public event ObjectDetectedEventHandler ObjectDetected;

    protected void OnObjectDetected(SenseGlove_Grabable obj)
    {
        if (ObjectDetected != null)
        {
            ObjectDetected(this, new DropZoneArgs(obj));
        }
    }


    //ObjectRemoved
    public delegate void ObjectRemovedEventHandler(object source, DropZoneArgs args);
    /// <summary> Fires when one of the SenseGlove_Grabables that was detected earlier is removed. </summary>
    public event ObjectDetectedEventHandler ObjectRemoved;

    protected void OnObjectRemoved(SenseGlove_Grabable obj)
    {
        if (ObjectRemoved != null)
        {
            ObjectRemoved(this, new DropZoneArgs(obj));
        }
    }

    #endregion Events

}

//--------------------------------------------------------------------------------------------------------------------------
// Event Arguments

public class DropZoneArgs : System.EventArgs
{
    /// <summary> The object that was detected or removed. </summary>
    public SenseGlove_Grabable grabable;

    /// <summary> Create a new instance of the DropZoneArgs. </summary>
    /// <param name="obj"></param>
    public DropZoneArgs(SenseGlove_Grabable obj)
    {
        this.grabable = obj;
    }

}

