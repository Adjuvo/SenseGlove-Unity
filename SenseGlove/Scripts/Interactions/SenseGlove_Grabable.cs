using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An object that can be picked up and dropped by the SenseGlove. </summary>
public class SenseGlove_Grabable : SenseGlove_Interactable
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Attributes

    /// <summary> Whether or not this object can be picked up from the Sense Glove by another Grabscript. </summary>
    [Tooltip("Whether or not this object can be picked up from the Sense Glove by another Grabscript.")]
    public bool canTransfer = true;

    /// <summary> The gameObject used as a reference for the Grabable's transform updates. </summary>
    private GameObject grabReference;

    //Reset Functionality 
        
    /// <summary> The original position of the SenseGlove_Grabable, used to reset its position. </summary>
    private Vector3 originalPos = Vector3.zero;
    /// <summary> The original rotation of the SenseGlove_Grabable, used to reset its rotation. </summary>
    private Quaternion originalRot = Quaternion.identity;

    //Folllow GrabType Variables
    
    /// <summary> The xyz offset of this Grabable's transform to the grabReference, on the moment it was picked up. </summary>
    private Vector3 grabOffset = Vector3.zero;
    /// <summary> The quaternion offset of this Grabable's transform to the grabReference, on the moment it was picked up.  </summary>
    private Quaternion grabRotation = Quaternion.identity;

    //Parent GrabType Variables

    private Transform originalParent;

    //PhysicsJoint GrabType Variables

    //Object RigidBody Variables

    /// <summary> The rigidBody to which velocity, gravity and kinematic options are applied. </summary>
    [Tooltip("The rigidBody to which velocity, gravity and kinematic options are applied. The script automatically connects to the Rigidbody attached to this GameObject.")]
    public Rigidbody physicsBody;

    /// <summary> Whether this grabable's physicsBody was kinematic before it was picked up. </summary>
    private bool wasKinematic;
    /// <summary> Whether this grabable's physicsBody was used gravity before it was picked up. </summary>
    private bool usedGravity;


    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void PickedUpEventHandler(object source, EventArgs args);
    /// <summary> Fires when this Grabable is picked up. </summary>
    public event PickedUpEventHandler ObjectGrabbed;

    protected void OnPickedUp()
    {
        if (ObjectGrabbed != null)
        {
            ObjectGrabbed(this, null);
        }
    }

    public delegate void ReleasedEventHandler(object source, EventArgs args);
    /// <summary> Fires when this Grabable is released. </summary>
    public event ReleasedEventHandler Released;

    protected void OnReleased()
    {
        if (Released != null)
        {
            Released(this, null);
        }
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    void Start()
    {
        this.originalPos = this.transform.position;
        this.originalRot = this.transform.rotation;
        if (!this.physicsBody) { this.physicsBody = this.gameObject.GetComponent<Rigidbody>(); }

        //Verify the kinematic variables
        if (this.physicsBody)
        {
            this.wasKinematic = this.physicsBody.isKinematic;
            this.usedGravity = this.physicsBody.useGravity;
        }
    }

    void Update()
    {
        if (!this.isInteractable && this.grabReference != null) { this.EndInteraction(null); } //end the interaction if the object is no longer interactable with.
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Class methods

    public override void BeginInteraction(SenseGlove_GrabScript grabScript)
    {
        if (this.isInteractable) //never interact twice with the same grabscript before EndInteraction is called.
        {
            bool grabbedByOther = this.IsGrabbed() && !InteractingWith(grabScript);

            if (!this.IsGrabbed() || (grabbedByOther && this.canTransfer))
            {
                if (this._grabScript && this._grabScript.pickupMethod == GrabType.Parent)
                {   //if we were patented before
                    this.transform.parent = this.originalParent;
                }

                this.grabReference = grabScript.grabReference;
                this._grabScript = grabScript;

                if (this._grabScript.pickupMethod == GrabType.Follow)
                {
                    //Quaternion.Inverse(QT) * (vT - vO);
                    this.grabOffset = Quaternion.Inverse(this.grabReference.transform.rotation) * (this.grabReference.transform.position - this.transform.position);

                    //Quaternion.Inverse(QT) * (Qo);
                    this.grabRotation = Quaternion.Inverse(this.grabReference.transform.rotation) * this.transform.rotation;
                }
                else if (this._grabScript.pickupMethod == GrabType.Parent)
                {
                    this.originalParent = this.gameObject.transform.parent;
                    this.transform.parent = this.grabReference.transform;
                }

                if (this.physicsBody)
                {
                    if (!grabbedByOther)
                    {
                        this.wasKinematic = this.physicsBody.isKinematic;
                        this.usedGravity = this.physicsBody.useGravity;
                    }

                    this.physicsBody.useGravity = false;
                    this.physicsBody.isKinematic = true;
                    this.physicsBody.velocity = new Vector3(0, 0, 0);
                    this.physicsBody.angularVelocity = new Vector3(0, 0, 0);
                }
                OnPickedUp();
            }
        }
    }

    public override void UpdateInteraction()
    {
        if (this.grabReference != null)
        {
            if (this._grabScript.pickupMethod == GrabType.Follow)
            {
                this.transform.rotation = this.grabReference.transform.rotation * this.grabRotation;
                this.transform.position = this.grabReference.transform.position - (this.grabReference.transform.rotation * grabOffset);
            }
        }
    }

    public void EndInteraction()
    {
        this.EndInteraction(this._grabScript);
    }

    public override void EndInteraction(SenseGlove_GrabScript grabScript)
    {
        if (InteractingWith(grabScript)) //only do the proper endInteraction if the EndInteraction comes from the script currently holding it.
        {
            if (this.originalParent != null)
            {
                this.transform.parent = this.originalParent;
            }
            if (grabScript != null)
            {
                //if we're not being held by this same grabscript a.k.a. we've been passed on to another one...

                if (this.physicsBody != null)
                {
                    this.physicsBody.useGravity = this.usedGravity;
                    this.physicsBody.isKinematic = this.wasKinematic;
                    this.physicsBody.velocity = grabScript.GetVelocity();
                    //this.physicsBody.angularVelocity = ???
                }
            }
            OnReleased();
            this.grabReference = null;
            this._grabScript = null;
            this.originalParent = null;
        }
    }

    public override void ResetObject()
    {
        if (this.originalParent)
        {
            this.transform.parent = originalParent;
            this.originalParent = null;
        }
        
        this.transform.position = this.originalPos;
        this.transform.rotation = this.originalRot;
        if (this.physicsBody)
        {
            this.physicsBody.velocity = Vector3.zero;
            this.physicsBody.angularVelocity = Vector3.zero;
            this.physicsBody.isKinematic = this.wasKinematic;
            this.physicsBody.useGravity = this.usedGravity;
        }
    }
 
    //----------------------------------------------------------------------------------------------------------------------------------
    // Utility Methods


    /// <summary>
    /// Check if this Interactable is currently being held by a SenseGlove GrabScript.
    /// </summary>
    /// <returns></returns>
    public bool IsGrabbed()
    {
        return this.grabReference != null;
    }

    /// <summary>
    /// Returns the rigidbody properties that are assigned to the physicsbody when this object is released.
    /// </summary>
    /// <returns></returns>
    public bool[] GetRBProps()
    {
        return new bool[2] { this.usedGravity, this.wasKinematic };
    }

    /// <summary>
    /// Edit the rigidbody properties that are assigned to the physicsbody when this object is released.
    /// </summary>
    /// <param name="gravity"></param>
    /// <param name="kinematic"></param>
    public void SetRBProps(bool gravity, bool kinematic)
    {
        this.usedGravity = gravity;
        this.wasKinematic = kinematic;
    }
       
    /// <summary>
    /// Set the original parent of this object, before it was picked up by the GrabScript.
    /// </summary>
    /// <param name="newParent"></param>
    public void SetOriginalParent(Transform newParent)
    {
        this.originalParent = newParent;
    }

    /// <summary>
    ///  Get the original parent of this object, before it was picked up by the GrabScript.
    /// </summary>
    /// <returns></returns>
    public Transform GetOriginalParent()
    {
        return this.originalParent;
    }

    //-------------------------------------------------------------------------------------------------------------------------


}
