using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses colliders to detect which object(s) can be touched (and untimately picked up) by a senseGlove. </summary> 
[RequireComponent(typeof(Collider))] //ensure the other component has a collider.
public class SenseGlove_Touch : MonoBehaviour
{

    [Tooltip("The collider used to determine which object is currently being touched")]
    public Collider touch;

    private GameObject touchedObject;

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
            this.touchedObject = col.gameObject;
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (this.touchedObject != null && this.IsTouching(col.gameObject))
        {
            this.touchedObject = null;
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

}
