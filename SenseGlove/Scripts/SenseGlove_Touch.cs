using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses colliders to detect which object(s) can be touched (and ultimately picked up) by a senseGlove. </summary> 
[RequireComponent(typeof(Collider))] //ensure the other component has a collider.
public class SenseGlove_Touch : MonoBehaviour
{

    //--------------------------------------------------------------------------------------------------------------------------
    // Publicly visible attributes

    [Tooltip("The collider used to determine which object is currently being touched")]
    public Collider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    [Tooltip("The object that is currently touched by this SenseGlove_Touch script.")]
    public GameObject touchedObject;

    /// <summary> The grabscript using these colliders for its logic. </summary>
    private SenseGlove_GrabScript grabScript;
    

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
    public SenseGlove_GrabScript GrabScript()
    {
        return this.grabScript;
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    //Colled when the application starts
    void Start()
    {
        if (this.touch == null) { this.touch = this.GetComponent<Collider>(); }
    }

    // Called once per frame
    void Update()
    {
        this.touch.isTrigger = true;
    }

    // Called during a physics update.
    void FixedUpdate()
    {
        if (touch != null) { touch.isTrigger = true; } //enure the touch collider is always kinematic.

        if (this.touchedObject != null && !this.touchedObject.activeInHierarchy)
        {
            Debug.Log("Object no longer exists. Releasing.");
            this.touchedObject = null;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection / Force Feedback 

    // Called when this object enters the collider of another object
    void OnTriggerEnter(Collider col)
    {
        if (col.GetComponent<SenseGlove_Interactable>() != null && this.touchedObject == null)
        {
            //if (!this.IsTouching(col.gameObject))
            //{
            //    //touching a new object!
            //}
            this.touchedObject = col.gameObject;
        }
    }

    // Called every FixedUpdate while this collider is inside another collider.
    void OnTriggerStay(Collider col)
    {
        //if (this.IsTouching(col.gameObject)) //Check if we're still on the same object?
        //{
            
        //}
    }

    // Called when this object exits the collider of another object
    void OnTriggerExit(Collider col)
    {
        if (this.IsTouching(col.gameObject))
        {
            this.touchedObject = null;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Touch Logic

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

    /// <summary> Returns the object that this collider is touching. Usually to check if it is also touched by other colliders. </summary>
    /// <returns></returns>
    public GameObject TouchObject()
    {
        return this.touchedObject;
    }

}
