using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An object that can be picked up and dropped by the SenseGlove. </summary>
public class SenseGlove_Grabable : SenseGlove_Interactable
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Attributes

    /// <summary> The rigidBody to which velocity, gravity and kinematic options are applied. </summary>
    [Tooltip("The rigidBody to which velocity, gravity and kinematic options are applied. The script automatically connects to the Rigidbody attached to this GameObject.")]
    public Rigidbody physicsBody;
    
    /// <summary> The original position of the SenseGlove_Grabable, used to reset its position. </summary>
    private Vector3 originalPos = Vector3.zero;
    /// <summary> The original rotation of the SenseGlove_Grabable, used to reset its rotation. </summary>
    private Quaternion originalRot = Quaternion.identity;

    /// <summary> The gameObject used as a reference for the Grabable's transform updates. </summary>
    private GameObject grabReference;
    /// <summary> The xyz offset of this Grabable's transform to the grabReference, on the moment it was picked up. </summary>
    private Vector3 grabOffset = Vector3.zero;
    /// <summary> The quaternion offset of this Grabable's transform to the grabReference, on the moment it was picked up.  </summary>
    private Quaternion grabRotation = Quaternion.identity;

    /// <summary> Whether this grabable's physicsBody was kinematic before it was picked up. </summary>
    private bool wasKinematic;
    /// <summary> Whether this grabable's physicsBody was used gravity before it was picked up. </summary>
    private bool usedGravity;


    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void PickedUpEventHandler(object source, EventArgs args);
    /// <summary> Fires when this Grabable is picked up. </summary>
    public event PickedUpEventHandler PickedUp;

    protected void OnPickedUp()
    {
        if (PickedUp != null)
        {
            PickedUp(this, null);
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

    protected override void HandleStart()
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

    protected override void HandleUpdate()
    {
        if (!this.isInteractable && this.grabReference != null) { this.EndInteraction(null); } //end the interaction if the object is no longer interactable with.
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Class methods

    public override void BeginInteraction(SenseGlove_PhysGrab grabScript)
    {
        if (this.isInteractable)
        {
            this.grabReference = grabScript.grabReference;

            //Quaternion.Inverse(QT) * (vT - vO);
            this.grabOffset = Quaternion.Inverse(this.grabReference.transform.rotation) * (this.grabReference.transform.position - this.transform.position);

            //Quaternion.Inverse(QT) * (Qo);
            this.grabRotation = Quaternion.Inverse(this.grabReference.transform.rotation) * this.transform.rotation;

            if (this.physicsBody)
            {
                this.wasKinematic = this.physicsBody.isKinematic;
                this.usedGravity = this.physicsBody.useGravity;
                
                this.physicsBody.useGravity = false;
                this.physicsBody.isKinematic = true;
                this.physicsBody.velocity = new Vector3(0, 0, 0);
                this.physicsBody.angularVelocity = new Vector3(0, 0, 0);
            }
            OnPickedUp();
        }
    }

    public override void FollowInteraction()
    {
        if (this.grabReference != null)
        {
            this.transform.rotation = this.grabReference.transform.rotation * this.grabRotation;
            this.transform.position = this.grabReference.transform.position - (this.grabReference.transform.rotation * grabOffset);
        }
    }

    public override void EndInteraction(SenseGlove_PhysGrab grabScript)
    {
        if (grabScript != null)
        {
            if (this.physicsBody != null)
            {
                this.physicsBody.useGravity = this.usedGravity;
                this.physicsBody.isKinematic = this.wasKinematic;
                this.physicsBody.velocity = grabScript.GetVelocity();
                //this.physicsBody.angularVelocity = ???
            }
        }
        OnPickedUp();
        this.grabReference = null;
    }

    public override void ResetObject()
    {
        this.transform.position = this.originalPos;
        this.transform.rotation = this.originalRot;
        if (this.physicsBody)
        {
            this.physicsBody.velocity = Vector3.zero;
            this.physicsBody.angularVelocity = Vector3.zero;
            this.physicsBody.isKinematic = this.wasKinematic;   //TDO : Change this?
            this.physicsBody.useGravity = this.usedGravity;     //TODO : Change this?
        }
    }
    
}
