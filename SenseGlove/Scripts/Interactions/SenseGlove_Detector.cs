using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A class to detect a SenseGlove based on its SenseGlove_Feedback colliders </summary>
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class SenseGlove_Detector : MonoBehaviour 
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Public Properties.

    /// <summary> How many SenseGlove_Touch colliders can enter the Detector before the GloveDetected event is raised. </summary>
    [Tooltip("How many SenseGlove_Touch colliders can enter the Detector before the GloveDetected event is raised.")]
    public int activationThreshold = 1;

    /// <summary> Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called. </summary>
    [Tooltip("Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called. Set to 0 to ignore.")]
    public float activationTime = 0;

    /// <summary> If set to true, the detector will not raise events if a second grabscript joins in.  </summary>
    [Tooltip("If set to true, the detector will not raise events if a second grabscript joins in.")]
    public bool singleGlove = false;

    //--------------------------------------------------------------------------------------------------------------------------
    // Internal Properties.

    /// <summary> All of the grabscripts currently interacting with this detector, in order of appearance. </summary>
    private List<SenseGlove_HandModel> detectedGloves = new List<SenseGlove_HandModel>();

    /// <summary> The amount of SenseGlove_Touch colliders of each grabscript that are currently in the detection area </summary>
    private List<int> detectedColliders = new List<int>();

    /// <summary> Used to keep track of the time that each glove have been inside this detector. </summary>
    private List<float> detectionTimes = new List<float>();

    /// <summary> Used to determine if the activationtheshold had been reached before. Prevents the scipt from firing multiple times. </summary>
    private List<bool> eventFired = new List<bool>();

    /// <summary> The collider of this detection area. Assigned on startup </summary>
    private Collider myCollider;
    /// <summary> The rigidbody of this detection area. Assigned on StartUp </summary>
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

    // Updates every frame, used to raise event(s).
    void LateUpdate()
    {
        for (int i = 0; i < this.detectedGloves.Count; i++)
        {
            if (this.detectedColliders[i] >= this.activationThreshold)
            {
                this.detectionTimes[i] += Time.deltaTime;
                if (this.detectionTimes[i] >= this.activationTime && !(eventFired[i]) && !(this.singleGlove && this.detectedGloves.Count > 1))
                {
                    this.OnGloveDetected(this.detectedGloves[i]);
                    this.eventFired[i] = true;
                }
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection

    void OnTriggerEnter(Collider col)
    {
        SenseGlove_Feedback touch = col.GetComponent<SenseGlove_Feedback>();
        if (touch && touch.handModel) //needs to have a grabscript attached.
        {
            int scriptIndex = this.HandModelIndex(touch.handModel);

            //#1 - Check if it belongs to a new or existing detected glove.
            if (scriptIndex < 0)
            {
                //Debug.Log("New Grabscript entered.");
                this.AddEntry(touch.handModel);
                scriptIndex = this.detectedGloves.Count - 1;
            }
            else
            {   
                //Debug.Log("Another collider for grabscript " + scriptIndex);
                this.detectedColliders[scriptIndex]++;
            }
            
            //if no time constraint is set, raise the event immediately!
            if (this.activationTime <= 0 && this.detectedColliders[scriptIndex] == this.activationThreshold)
            {
                //Debug.Log("ActivationThreshold Reached!");
                if (!(eventFired[scriptIndex]) && !(this.singleGlove && this.detectedGloves.Count > 1))
                {
                    this.OnGloveDetected(this.detectedGloves[scriptIndex]);
                    this.eventFired[scriptIndex] = true;
                }
            }

        }
    }

    void OnTriggerExit(Collider col)
    {
        SenseGlove_Feedback touch = col.GetComponent<SenseGlove_Feedback>();
        if (touch && touch.handModel) //must have a grabscript attached.
        {
            //Debug.Log("Collider Exits");
            int scriptIndex = this.HandModelIndex(touch.handModel);
            if (scriptIndex < 0)
            {
                //Debug.Log("Something went wrong with " + this.gameObject.name);
                //it is likely the palm collider.
            }
            else
            {   //belongs to an existing SenseGlove.
                this.detectedColliders[scriptIndex]--;
                if (this.detectedColliders[scriptIndex] <= 0)
                {
                    //raise release event.
                    //Debug.Log("Escape!");
                    if (eventFired[scriptIndex] && !(this.singleGlove && this.detectedGloves.Count > 1)) //only fire if the last glove has been removed.
                    {
                        this.OnGloveRemoved(this.detectedGloves[scriptIndex]);
                    }             
                    this.RemoveEntry(scriptIndex);
                }
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Accessing Lists

    /// <summary>
    /// 
    /// </summary>
    /// <param name="grab"></param>
    /// <returns></returns>
    private int HandModelIndex(SenseGlove_HandModel model)
    {
        for (int i = 0; i < this.detectedGloves.Count; i++)
        {
            if (GameObject.ReferenceEquals(model, this.detectedGloves[i])) { return i; }
        }
        return -1;
    }

    private void AddEntry(SenseGlove_HandModel model)
    {
        this.detectedGloves.Add(model);
        this.detectionTimes.Add(0);
        this.detectedColliders.Add(1); //already add one.
        this.eventFired.Add(false);
    }

    private void RemoveEntry(int scriptIndex)
    {
        if (scriptIndex > -1 && scriptIndex < detectedGloves.Count)
        {
            this.detectedColliders.RemoveAt(scriptIndex);
            this.detectedGloves.RemoveAt(scriptIndex);
            this.detectionTimes.RemoveAt(scriptIndex);
            this.eventFired.RemoveAt(scriptIndex);
        }
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void GloveDetectedEventHandler(object source, GloveDetectionArgs args);
    /// <summary> Fires when a new SenseGlove_Grabscript enters this detection zone. </summary>
    public event GloveDetectedEventHandler GloveDetected;

    protected void OnGloveDetected(SenseGlove_HandModel model)
    {
        if (GloveDetected != null)
        {
            GloveDetected(this, new GloveDetectionArgs(model));
        }
    }

    public delegate void OnGloveRemovedEventHandler(object source, GloveDetectionArgs args);
    /// <summary>Fires when a SenseGlove_Grabscript exits this detection zone.  </summary>
    public event OnGloveRemovedEventHandler GloveRemoved;

    protected void OnGloveRemoved(SenseGlove_HandModel model)
    {
        if (GloveRemoved != null)
        {
            GloveRemoved(this, new GloveDetectionArgs(model));
        }
    }

    /// <summary> Returns true if there is a Sense Glove contained within this collider. </summary>
    /// <returns></returns>
    public bool ContainsSenseGlove()
    {
        return this.detectedGloves.Count > 0;
    }

    /// <summary> Get a list of all gloves within this detection area. </summary>
    /// <returns></returns>
    public SenseGlove_HandModel[] GlovesInside()
    {
        return this.detectedGloves.ToArray();
    }

}

/// <summary> EventArgs fired when a glove is detected in or removed from a SenseGlove_Detector. </summary>
public class GloveDetectionArgs : System.EventArgs
{
    /// <summary> The Grabscript that caused the event to fire. </summary>
    public SenseGlove_HandModel handModel;

    /// <summary> Create a new instance of the SenseGlove Detection Arguments </summary>
    /// <param name="grab"></param>
    public GloveDetectionArgs(SenseGlove_HandModel model)
    {
        this.handModel = model;
    }

}
