using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses colliders to detect which object(s) can be touched (and ultimately picked up) by a senseGlove. </summary> 
[RequireComponent(typeof(Collider))] //ensure the other component has a collider.
public class SenseGlove_Touch : MonoBehaviour
{

    [Tooltip("The collider used to determine which object is currently being touched")]
    public Collider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    private GameObject touchedObject;

    /// <summary> The grabscript using these colliders for its logic. </summary>
    private SenseGlove_PhysGrab grabScript;

    void Start ()
    {
        
	}
	
	void FixedUpdate ()
    {
        if (touch != null) { touch.isTrigger = true; } //enure the touch collider is always kinematic.
    }


    void OnTriggerStay(Collider col)
    {
        if (col.GetComponent<SenseGlove_Interactable>() != null)
        {
            if (!this.IsTouching(col.gameObject))
            {
                
            }
            this.touchedObject = col.gameObject;
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (this.touchedObject != null && this.IsTouching(col.gameObject))
        {
            this.touchedObject = null;
            //if (col.gameObject.GetComponent<SenseGlove_Button>())
            //{
            //    col.GetComponent<SenseGlove_Interactable>().EndInteraction(null);
            //}
            //SenseGlove_Debugger.Log(this.name + " is no longer colliding with " + col.name);
        }
    }

    /// <summary>
    /// Check if this SenseGlove_Touch is touching object obj.
    /// </summary>
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


    public GameObject TouchObject()
    {
        return this.touchedObject;
    }

    /// <summary> Tell the SenseGlove_Touch that is is being used by the specified SenseGlove_PhysGrab </summary>
    /// <param name="parentScript"></param>
    public void SetSourceScript(SenseGlove_PhysGrab parentScript)
    {
        this.grabScript = parentScript;
    }

    /// <summary> Get a reference to the grabscript that this SenseGlove_Touch is attached to. </summary>
    /// <returns></returns>
    public SenseGlove_PhysGrab GrabScript()
    {
        return this.grabScript;
    }

}
