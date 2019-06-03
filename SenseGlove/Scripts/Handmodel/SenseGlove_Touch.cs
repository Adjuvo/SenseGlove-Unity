using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses colliders to detect which object(s) can be touched (and ultimately picked up) by the SenseGlove_PhysGrab. </summary> 
[RequireComponent(typeof(Collider))] //ensure the other component has a collider.
public class SenseGlove_Touch : MonoBehaviour
{

    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    /// <summary> The collider used to determine which object is currently being touched. </summary>
    [Tooltip("The collider used to determine which object is currently being touched")]
    public Collider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    protected GameObject touchedObject;

    /// <summary> Script of touched object </summary>
    protected SenseGlove_Interactable touchedScript;

    /// <summary> The grabscript using these colliders for its logic. Only PhysGrab uses these. </summary>
    protected SenseGlove_GrabScript grabScript;
    

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Get / Set attributes for Construction

    /// <summary> Tell the SenseGlove_Touch that is is being used by the specified SenseGlove_PhysGrab. Copy its ForceFeedbackType. </summary>
    /// <param name="parentScript"></param>
    public void SetSourceScript(SenseGlove_GrabScript parentScript)
    {
        this.grabScript = parentScript;
    }

    /// <summary> Get a reference to the grabscript that this SenseGlove_Touch is attached to. </summary>
    /// <returns></returns>
    public SenseGlove_GrabScript GrabScript { get { return this.grabScript; } }

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    //Colled when the application starts
    protected virtual void Start()
    {
        if (this.touch == null) { this.touch = this.GetComponent<Collider>(); }
    }

    // Called once per frame
    protected virtual void Update()
    {
        this.touch.isTrigger = true;
    }

    // Called during a physics update.
    protected virtual void FixedUpdate()
    {
        if (touch != null) { touch.isTrigger = true; } //enure the touch collider is always kinematic.

        if ( this.touchedObject == null || (this.touchedObject != null && !this.touchedObject.activeInHierarchy) 
            || (this.touchedScript != null && !this.touchedScript.CanInteract()) )
        {
            //SenseGlove_Debugger.Log("Object no longer exists. Releasing.");
            this.touchedObject = null;
            this.touchedScript = null;
        }
    }




    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection / Force Feedback 

    // Called when this object enters the collider of another object
    protected virtual void OnTriggerEnter(Collider col)
    {
        SenseGlove_Interactable interact = col.attachedRigidbody != null ? col.attachedRigidbody.GetComponent<SenseGlove_Interactable>()
            : col.GetComponent<SenseGlove_Interactable>();
        if (interact != null)
        {
            interact.TouchedBy(this);
            if (this.touchedObject == null)
            {
                //if (!this.IsTouching(col.gameObject))
                //{
                //    //touching a new object!
                //}
                this.touchedObject = interact.gameObject;
                this.touchedScript = interact;
            }
        }
    }

    // Called every FixedUpdate while this collider is inside another collider.
    protected virtual void OnTriggerStay(Collider col)
    {
    }

    // Called when this object exits the collider of another object
    protected virtual void OnTriggerExit(Collider col)
    {
        GameObject gObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;

        SenseGlove_Interactable interact = gObj.GetComponent<SenseGlove_Interactable>();
        if (interact != null)
            interact.UnTouchedBy(this);

        if (this.IsTouching(gObj))
        {
            this.touchedScript.UnTouchedBy(this);

            this.touchedObject = null;
            this.touchedScript = null;
        }
    }

    #endregion Monobehaviour

    //--------------------------------------------------------------------------------------------------------------------------
    // Touch Logic

    #region TouchLogic

    /// <summary> Check if this SenseGlove_Touch is touching any objects </summary>
    /// <returns></returns>
    public bool IsTouching()
    {
        return this.touchedObject != null;
    }

    /// <summary> Check if this SenseGlove_Touch is touching object obj. </summary>
    /// <param name="obj"></param>
    public bool IsTouching(GameObject obj)
    {
        if (this.touchedObject != null && obj != null)
        {
            bool t = GameObject.ReferenceEquals(this.touchedObject, obj);
            //return this.touchedObject.Equals(obj);
            //return this.touchedObject == obj;
            //SenseGlove_Debugger.Log("The thumb is touching " + touchedObject.name + " and Index is touching " + obj.name + ". Equals = " + t);
            return t;
        }
        return false;
    }

    /// <summary> Check if two SenseGlove_Touch scripts are touching the same object </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsTouching(SenseGlove_Touch other)
    {
        if (other == null)
        {
            SenseGlove_Debugger.LogError("Other is Null"); //If this occurs then our grabScript(s) aren't up to scratch.
        }
        else if (this.touchedObject != null && other.touchedObject != null)
        {
            return GameObject.ReferenceEquals(this.touchedObject, other.touchedObject);
        }
        return false;
    }

    /// <summary> Returns the object that this collider is touching. Usually to check if it is also touched by other colliders. </summary>
    /// <returns></returns>
    public GameObject TouchObject()
    {
        return this.touchedObject;
    }

    /// <summary> Retrieve the Interactable script if an object is currently being touched. </summary>
    /// <returns></returns>
    public SenseGlove_Interactable TouchedScript()
    {
        if (this.touchedObject != null)
        {
            return this.touchedObject.GetComponent<SenseGlove_Interactable>();
        }
        return null;
    }

    /// <summary> Clear the object currently touched by this script </summary>
    public void ClearTouchedObjects()
    {
        this.touchedObject = null;
    }

    #endregion TouchLogic
    
}
