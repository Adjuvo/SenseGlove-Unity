using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An object that can be picked up and dropped by the SenseGlove. </summary>
public class SenseGlove_Grabable : SenseGlove_Interactable
{
    public Rigidbody physicsBody;

    private Vector3 originalPos = Vector3.zero;
    private Quaternion originalRot = Quaternion.identity;

    private GameObject grabReference;
    private Vector3 grabOffset = Vector3.zero;
    private Quaternion grabRotation = Quaternion.identity;

    protected override void HandleStart()
    {
        this.originalPos = this.transform.position;
        this.originalRot = this.transform.rotation;
        if (!this.physicsBody) { this.physicsBody = this.gameObject.GetComponent<Rigidbody>(); }
    }

    protected override void HandleUpdate()
    {
        if (!this.isInteractable && this.grabReference != null) { this.EndInteraction(null); } //end the interaction if the object is no longer interactable with.
    }

    public override void BeginInteraction(SenseGlove_PhysGrab grabScript)
    {
        if (this.isInteractable)
        {
            Debug.Log("PickUp");

            this.grabReference = grabScript.grabReference;

            //Quaternion.Inverse(QT) * (vT - vO);
            this.grabOffset = Quaternion.Inverse(this.grabReference.transform.rotation) * (this.grabReference.transform.position - this.transform.position);

            //Quaternion.Inverse(QT) * (Qo);
            this.grabRotation = Quaternion.Inverse(this.grabReference.transform.rotation) * this.transform.rotation;

            if (this.physicsBody)
            {
                this.physicsBody.useGravity = false;
                this.physicsBody.isKinematic = true;
                this.physicsBody.velocity = new Vector3(0, 0, 0);
                this.physicsBody.angularVelocity = new Vector3(0, 0, 0);
            }

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
                this.physicsBody.useGravity = true;
                this.physicsBody.isKinematic = false;
                this.physicsBody.velocity = grabScript.GetVelocity();
                //this.physicsBody.angularVelocity = ???
            }
        }
        Debug.Log("Put down!");
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
            this.physicsBody.isKinematic = false;   //TDO : Change this?
            this.physicsBody.useGravity = true;     //TODO : Change this?
        }
    }
    
}
