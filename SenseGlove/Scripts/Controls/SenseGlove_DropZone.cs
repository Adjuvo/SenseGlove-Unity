using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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


/// <summary> Detects SenseGlove_Grabables within its volume. </summary>
[RequireComponent(typeof(Collider))]
public class SenseGlove_DropZone : MonoBehaviour
{
    //---------------------------------------------------------------------------------------------------------------------------
    // DropZone Parameters.

    /// <summary> Properties that assist in object detection. </summary>
    /// <remarks> Placed inside a class to reduce the amount of List<> parameters. </remarks>
    protected class DropProps
    {
        public float insideTime;
        public bool detected;

        public DropProps()
        {
            this.insideTime = 0;
            this.detected = false;
        }
    }
    
    //---------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    /// <summary> The objects that should be inside this DropZone. Leave it empty to snap to all SenseGlove_Grabables. </summary>
    [Tooltip("The objects that should be inside this DropZone. If left empty, this DropZone will detect all kinds of SenseGlove_Grabables..")]
    [SerializeField] protected List<SenseGlove_Grabable> objectsToGet = new List<SenseGlove_Grabable>();

    /// <summary> The time (in s) that a Grabable must be inside this zone before it is considered 'inside'. </summary>
    [Tooltip("The time (in s) that a Grabable must be inside this zone before it is considered 'inside'.")]
    public float detectionTime = 0.2f;

    /// <summary> Determines if objects that are still being held are detected. </summary>
    [Tooltip("Determines if objects that are still being grabbed are detected [true], or if they must be released first [false].")]
    public bool detectHeldObjects = true;

    /// <summary> Fires when an Object is Detected. </summary>
    [Header("Events")]
    [Tooltip("Fires when an object is detected.")]
    [SerializeField] protected SGEvent OnObjectDetected;

    /// <summary> Fires when an Object is Removed. </summary>
    [Tooltip("Fires when an object is removed.")]
    [SerializeField] protected SGEvent OnObjectRemoved;


    /// <summary> An optional highlight for this snapzone that can be turned on or off. </summary>
    [Header("Graphics")]
    [Tooltip("An optional highlight for this snapzone that can be turned on or off.")]
    public MeshRenderer[] highLighters;


    // Internal properties

    /// <summary> Whether ot not this script has run setup before. </summary>
    protected bool setup = false;

    /// <summary> Timer variable to check OnCollisionStay. </summary>
    protected float checkStayTimer = 0;

    /// <summary> The RigidBody connected to this DropZone. </summary>
    protected Rigidbody physicsBody;

    /// <summary> The list of objects currently inside this dropZone </summary>
    protected List<SenseGlove_Grabable> objectsInside = new List<SenseGlove_Grabable>();

    /// <summary> Contains all properties for dropZone logic. </summary>
    protected List<DropProps> dropProperties = new List<DropProps>();

    /// <summary> The time, in seconds, for which to check OnCollisionStay </summary>
    /// <remarks> In case the collider is enabled with an object already inside </remarks>
    protected static float checkStayTime = 0.2f;

    //---------------------------------------------------------------------------------------------------------------------------
    // Accessors

    /// <summary> Get a list of all objects inside this DropZone. </summary>
    /// <returns></returns>
    public SenseGlove_Grabable[] ObjectsInside
    {
        get { return this.objectsInside.ToArray(); }
    }

    public SenseGlove_Grabable[] TargetObjects
    {
        get { return this.objectsToGet.ToArray(); }
    }


    /// <summary> Check the amount of objects within this DropZone. </summary>
    /// <returns></returns>
    public int NumberOfObjects
    {
        get { return this.objectsInside.Count; }
    }

    /// <summary> Check if all desired objects have been detected. </summary>
    /// <returns></returns>
    public bool AllObjectsDetected
    {
        get { return this.objectsInside.Count == this.objectsToGet.Count; }
    }

    #endregion Properties

    //---------------------------------------------------------------------------------------------------------------------------
    // Methods

    #region Methods

    // Setup Logic

    /// <summary> Validates the settings of this DropZone. </summary>
    public virtual void ValidateSettings()
    {
        for (int i = 0; i < this.objectsToGet.Count;) //objectsToGet that are null should be removed.
        {
            if (this.objectsToGet[i] == null)
            {
                this.objectsToGet.RemoveAt(i);
            }
            else { i++; }
        }

        this.ValidateRB();

        Collider[] myColliders = this.gameObject.GetComponents<Collider>();
        for (int i = 0; i < myColliders.Length; i++)
            myColliders[i].isTrigger = true;
    }
    

    /// <summary> Check if all RigidBody settings allow us to pick up objects. </summary>
    protected void ValidateRB()
    {
        this.physicsBody = this.GetComponent<Rigidbody>();
        if (this.physicsBody != null)
        {
            if (this.physicsBody.useGravity)
                SenseGlove_Debugger.LogWarning(this.name + ".DropZone has a rigidbody that uses gravity, and might move from its desired location.");
        }
        else //we don't have a RigidBody, so one of the ObjectsToGet should have it!
        {
            if (this.objectsToGet.Count > 0)
            {
                bool noRBs = true;
                for (int i = 0; i < this.objectsToGet.Count; i++)
                {
                    if (this.objectsToGet[i].physicsBody == null)
                        SenseGlove_Debugger.LogWarning(this.objectsToGet[i].name + " will not be detected by " + this.name 
                            + ".DropZone, as neither have a RigidBody attached.");
                    else
                        noRBs = false;
                }
                if (noRBs)
                {
                    Debug.LogWarning("Since none of the ObjectsToGet in " + this.name + " have a RigidBody attached, one was autmatically attached to the GameObject.");
                    this.physicsBody = this.gameObject.AddComponent<Rigidbody>();
                    this.physicsBody.useGravity = false;
                    this.physicsBody.isKinematic = true;
                }
            }
            else
                SenseGlove_Debugger.LogWarning(this.name + ".DropZone has no RigidBody of its own, and will therefore only " +
                    "detect Grabables with a RigidBody attached.");
        }
    }



    // Detection Logic

    #region Detection

    /// <summary> Retrieve the index of a Grabable within a list of Grabables. </summary>
    /// <param name="obj"></param>
    /// <param name="grabables"></param>
    /// <returns> Returns -1 if obj does not exist in grabables. </returns>
    public static int ListIndex(SenseGlove_Grabable obj, List<SenseGlove_Grabable> grabables)
    {
        if (obj != null)
        {
            for (int i = 0; i < grabables.Count; i++)
            {
                if (GameObject.ReferenceEquals(obj.pickupReference.gameObject, grabables[i].pickupReference.gameObject))
                    return i;
            }
        }
        return -1;
    }

    /// <summary> Check if this Object has already been detected. </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsDetected(SenseGlove_Grabable obj)
    {
        return SenseGlove_DropZone.ListIndex(obj, this.objectsInside) > -1;
    }

    /// <summary> Check if this SenseGlove_Object is one of the "goal" objects; </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsTarget(SenseGlove_Grabable obj)
    {
        if (this.objectsToGet.Count > 0) 
            return SenseGlove_DropZone.ListIndex(obj, this.objectsToGet) > -1;
        return true; //we can accept any kind of Object.
    }

    /// <summary> Add a target object. </summary>
    /// <param name="obj"></param>
    public void AddTarget(SenseGlove_Grabable obj)
    {
        if (obj != null)
            this.objectsToGet.Add(obj);
    }


    /// <summary> Adds an object to this SenseGlove_DropZone. Does not fire the eventTime. </summary>
    /// <param name="grabable"></param>
    public virtual void AddObject(SenseGlove_Grabable grabable)
    {
        this.objectsInside.Add(grabable);
        this.dropProperties.Add(new DropProps());

        if (this.detectionTime == 0 && (!grabable.IsInteracting() || this.detectHeldObjects))
        {
            this.dropProperties[this.dropProperties.Count - 1].detected = true; //mark that we have detected it!
            this.CallObjectDetect(grabable);
        }
    }


    /// <summary> Removes a specific object from this SenseGlove_DropZone </summary>
    /// <param name="grabable"></param>
    public virtual void RemoveObject(SenseGlove_Grabable grabable)
    {
        int objIndex = SenseGlove_DropZone.ListIndex(grabable, this.objectsInside);
        if (objIndex > -1)
            this.RemoveObject(objIndex);
    }


    /// <summary> Raises a removed event and then remove an object from all associated lists </summary>
    /// <param name="index"></param>
    protected virtual void RemoveObject(int index)
    {
        if (this.dropProperties[index].detected) //only fire the event if this object was detected in the first place.
            this.CallObjectRemoved(this.objectsInside[index]);

        this.objectsInside.RemoveAt(index);
        this.dropProperties.RemoveAt(index);
    }


    /// <summary> Clear all objects currently detected within this space. </summary>
    public virtual void ClearObjects()
    {
        int max = this.objectsInside.Count;
        int i = 0;
        while (this.objectsInside.Count > 0 && i < max) //ensures everything is cleared without calling the function more than needed.
        {
            this.RemoveObject(0);
            i++;
        }
        this.checkStayTimer = 0;
    }
    

    /// <summary> Check if a newly incoming object belongs to our targets. </summary>
    /// <param name="obj"></param>
    protected virtual void CheckObjectEnter(GameObject obj)
    {
        SenseGlove_Grabable grabableScript = obj.GetComponent<SenseGlove_Grabable>();
        if (grabableScript != null && this.IsTarget(grabableScript) && !this.IsDetected(grabableScript))
                this.AddObject(grabableScript);
    }

    protected virtual void CheckObjectExit(GameObject obj)
    {
        SenseGlove_Grabable grabableScript = obj.GetComponent<SenseGlove_Grabable>();
        if (grabableScript != null)
            this.RemoveObject(grabableScript); //RemoveObject(Grabable) will check for indices etc.
    }


    /// <summary> Checks Detection times of the Grabables within this zone. </summary>
    protected virtual void CheckDetectionTimes()
    {
        for (int i = 0; i < this.objectsInside.Count; i++)
        {
            if (!this.objectsInside[i].IsInteracting() || this.detectHeldObjects)
            {
                if (this.dropProperties[i].insideTime < this.detectionTime)
                {
                    this.dropProperties[i].insideTime = this.dropProperties[i].insideTime + Time.deltaTime;
                    if (this.dropProperties[i].insideTime >= this.detectionTime)
                    {
                        this.dropProperties[i].detected = true;
                        this.CallObjectDetect(this.objectsInside[i]);
                    }
                }
            }
            else if (!this.dropProperties[i].detected && !this.detectHeldObjects && this.objectsInside[i].IsInteracting())
                this.dropProperties[i].insideTime = 0; //reset the timer, but only if we havent fired the event yet.
        }
    }

    #endregion Detection

    // Other Logic

    /// <summary> Turn the Highlighter(s) of this DropZone on or off. </summary>
    /// <param name="active"></param>
    public void SetHighLight(bool active)
    {
        for (int i=0; i<this.highLighters.Length; i++)
        {
            this.highLighters[i].enabled = active;
        }
    }

    /// <summary> Resets both the zone and its objects to their original state. </summary>
    public virtual void ResetZoneAndObjects()
    {
        for (int i = 0; i < this.objectsToGet.Count; i++)
            this.objectsToGet[i].ResetObject();

        this.ClearObjects();
    }

    //---------------------------------------------------------------------------------------------------------------------------
    // Events

    /// <summary> Event Delegate for DropZones. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void DropZoneEventHandler(object source, DropZoneArgs args);

    /// <summary> Fires when an object has been detected inside this dropZone. </summary>
    public event DropZoneEventHandler ObjectDetected;

    /// <summary> Calls the ObjectDetected event </summary>
    /// <param name="detectedObject"></param>
    protected virtual void CallObjectDetect(SenseGlove_Grabable detectedObject)
    {
        //Debug.Log(this.name + ": " + detectedObject.name + " Detected!");
        if (this.ObjectDetected != null)
            this.ObjectDetected(this, new DropZoneArgs(detectedObject));
        this.OnObjectDetected.Invoke();
    }

    /// <summary> Fires when an object has been removed from this dropZone. </summary>
    public event DropZoneEventHandler ObjectRemoved;

    /// <summary>  Calls the ObjectRemoved event  </summary>
    /// <param name="removedObject"></param>
    protected virtual void CallObjectRemoved(SenseGlove_Grabable removedObject)
    {
        //Debug.Log(this.name + ": " + removedObject.name + " Removed!");
        if (this.ObjectRemoved != null)
            this.ObjectRemoved(this, new DropZoneArgs(removedObject));
        this.OnObjectRemoved.Invoke();
    }

    #endregion Methods

    //---------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Update is called once per frame
    protected virtual void Update ()
    {
		if (!this.setup) //placed in Update so that all other scripts can finish initialization before we begin checking for RigidBodies.
        {
            this.ValidateSettings();
            this.setup = true;
        }
        
        this.CheckDetectionTimes();

        if (this.checkStayTimer < SenseGlove_DropZone.checkStayTime)
            this.checkStayTimer += Time.deltaTime;

    }

    //fires when this script is disabled
    protected virtual void OnDestroy()
    {
        this.ClearObjects();
    }

    //Fires when we've been re-enabled
    protected virtual void OnEnable()
    {
        this.checkStayTimer = 0; //resets OnTriggerStay timer to begin detecting new objects.
    }



    protected virtual void OnTriggerEnter(Collider other)
    {
        this.CheckObjectEnter(other.gameObject);   
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (this.checkStayTimer < SenseGlove_DropZone.checkStayTime)
            this.CheckObjectEnter(other.gameObject);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        this.CheckObjectExit(other.gameObject);
    }

    #endregion Monobehaviour

}
