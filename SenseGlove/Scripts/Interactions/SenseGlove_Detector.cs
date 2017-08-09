using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class to detect a SenseGlove based on its PhysGrab colliders
/// </summary>
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class SenseGlove_Detector : MonoBehaviour 
{

    /// <summary> How many SenseGlove_Touch colliders can enter the Detector before the GloveDetected event is raised. </summary>
    public int activationThreshold = 1;

    //--------------------------------------------------------------------------------------------------------------------------
    // Internal Properties.

    /// <summary>
    /// The amount of SenseGlove_Touch colliders of each grabscript that are currently in the detection area
    /// </summary>
    private List<int> detectedColliders = new List<int>();
    private List<SenseGlove_PhysGrab> detectedGloves = new List<SenseGlove_PhysGrab>();

    private Collider myCollider;
    private Rigidbody myRigidbody;

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    // Use this for initialization
    void Start () 
	{
		//add a rigidbody if not already present?
        myCollider = this.GetComponent<Collider>();
        myRigidbody = this.GetComponent<Rigidbody>();

        if (myCollider)
        {
            myCollider.isTrigger = true;
        }
        if (myRigidbody)
        {
            myRigidbody.useGravity = false;
            myRigidbody.isKinematic = true;
        }
    }
	

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection

    void OnTriggerEnter(Collider col)
    {
        SenseGlove_Touch touch = col.GetComponent<SenseGlove_Touch>();
        if (touch)
        {
            int scriptIndex = this.GrabScriptIndex(touch.GrabScript());
            if (scriptIndex < 0)
            {   //new grabScript
                //Debug.Log("New Grabscript entered.");
                this.AddGrabScript(touch.GrabScript());
                if (1 >= this.activationThreshold) //We've just added the first collider, so the sount would be 1.
                {
                    Debug.Log("ActivationThreshold Reached!");
                    this.OnGloveDetected();
                }
            }
            else
            {   //belongs to an existing SenseGlove.
                //Debug.Log("Another collider for grabscript " + scriptIndex);
                this.detectedColliders[scriptIndex]++;
                if (this.detectedColliders[scriptIndex] >= this.activationThreshold)
                {
                    //Debug.Log("ActivationThreshold Reached!");
                    this.OnGloveDetected();
                }
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        SenseGlove_Touch touch = col.GetComponent<SenseGlove_Touch>();
        if (touch)
        {
            //Debug.Log("Collider Exits");
            int scriptIndex = this.GrabScriptIndex(touch.GrabScript());
            if (scriptIndex < 0)
            {
                Debug.Log("Something went wrong with " + this.gameObject.name);
            }
            else
            {   //belongs to an existing SenseGlove.
                this.detectedColliders[scriptIndex]--;
                if (this.detectedColliders[scriptIndex] < this.activationThreshold 
                    && this.detectedColliders[scriptIndex] + 1 == this.activationThreshold)
                {
                    //raise release event.
                    //Debug.Log("Escape!");
                    this.OnGloveRemoved();
                }
                if (this.detectedColliders[scriptIndex] <= 0)
                {
                    this.RemoveGrabScript(scriptIndex);
                }
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Accessing Lists

    private int GrabScriptIndex(SenseGlove_PhysGrab grab)
    {
        for (int i = 0; i < this.detectedGloves.Count; i++)
        {
            if (GameObject.ReferenceEquals(grab, this.detectedGloves[i])) { return i; }
        }
        return -1;
    }

    private void AddGrabScript(SenseGlove_PhysGrab grab)
    {
        this.detectedGloves.Add(grab);
        this.detectedColliders.Add(1); //already add one.
    }

    private void RemoveGrabScript(int scriptIndex)
    {
        if (scriptIndex > -1 && scriptIndex < detectedGloves.Count)
        {
            this.detectedColliders.RemoveAt(scriptIndex);
            this.detectedGloves.RemoveAt(scriptIndex);
        }
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void GloveDetectedEventHandler(object source, EventArgs args);
    /// <summary> Fires when a new SenseGlove_Grabscript enters this detection zone. </summary>
    public event GloveDetectedEventHandler GloveDetected;

    protected void OnGloveDetected()
    {
        if (GloveDetected != null)
        {
            GloveDetected(this, null);
        }
    }

    public delegate void OnGloveRemovedEventHandler(object source, EventArgs args);
    /// <summary>Fires when a SenseGlove_Grabscript exits this detection zone.  </summary>
    public event OnGloveRemovedEventHandler GloveRemoved;

    protected void OnGloveRemoved()
    {
        if (GloveRemoved != null)
        {
            GloveRemoved(this, null);
        }
    }


    public bool ContainsSenseGlove()
    {
        return this.detectedGloves.Count > 0;
    }

    /// <summary> Get a list of all gloves within this detection area. </summary>
    /// <returns></returns>
    public SenseGlove_PhysGrab[] GlovesInside()
    {
        return this.detectedGloves.ToArray();
    }

}
