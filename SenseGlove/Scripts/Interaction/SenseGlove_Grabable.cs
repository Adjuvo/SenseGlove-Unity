using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An object that can be picked up and dropped by the SenseGlove. </summary>
public class SenseGlove_Grabable : SenseGlove_Interactable
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Attributes

    /// <summary> The way that the object(s) will be picked up by this GrabScript. </summary>
    [Header("Grabable Options")]
    [Tooltip("The way that the object(s) will be picked up by this GrabScript.")]
    public GrabType pickupMethod = GrabType.Parent;

    /// <summary> Whether or not this object can be picked up from the Sense Glove by another Grabscript. </summary>
    [Tooltip("Whether or not this object can be picked up from the Sense Glove by another Grabscript.")]
    public bool canTransfer = true;

    /// <summary> The transform that is grabbed as part of this object. Useful when dealing with a grabable that is a child of another grabable. </summary>
    public Transform pickupReference;

    /// <summary> The gameObject used as a reference for the Grabable's transform updates. </summary>
    private GameObject grabReference;

    //Folllow GrabType Variables
    
    /// <summary> The xyz offset of this Grabable's transform to the grabReference, on the moment it was picked up. </summary>
    private Vector3 grabOffset = Vector3.zero;
    /// <summary> The quaternion offset of this Grabable's transform to the grabReference, on the moment it was picked up.  </summary>
    private Quaternion grabRotation = Quaternion.identity;

    //Parent GrabType Variables

    private Transform originalParent;

    //PhysicsJoint GrabType Variables

    private Joint connection;

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

    void Awake()
    {
        this.CheckPickupRef();
        this.SaveTransform();
    }

    public void CheckPickupRef()
    {
        if (this.pickupReference == null)
        {
            this.pickupReference = this.transform;
        }
    }

    void Start()
    {
        if (!this.physicsBody) { this.physicsBody = this.pickupReference.GetComponent<Rigidbody>(); }

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

    public override void BeginInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.isInteractable) //never interact twice with the same grabscript before EndInteraction is called.
        {
          //  Debug.Log("Begin Interaction");

            bool alreadyBeingHeld = this.InteractingWith(grabScript);
            
            if (!alreadyBeingHeld)
            {
                //always record the properties of the object in case we switch pickupTypes mid-Play for some reason.
                this.originalParent = this.pickupReference.parent;
                if (this.physicsBody)
                {
                    this.wasKinematic = this.physicsBody.isKinematic;
                    this.usedGravity = this.physicsBody.useGravity;
                }
            }

            //if the object was actually grabbed.
            if (!alreadyBeingHeld || (alreadyBeingHeld && this.canTransfer))
            {
                this.grabReference = grabScript.grabReference;
                this._grabScript = grabScript;
                
                //Apply proper pickup 
                if (this.pickupMethod == GrabType.Parent)
                {
                    this.pickupReference.parent = grabScript.grabReference.transform;
                }
                else if (this.pickupMethod == GrabType.Follow)
                {
                    //Quaternion.Inverse(QT) * (vT - vO);
                    this.grabOffset = Quaternion.Inverse(this.grabReference.transform.rotation) * (this.grabReference.transform.position - this.pickupReference.position);

                    //Quaternion.Inverse(QT) * (Qo);
                    this.grabRotation = Quaternion.Inverse(this.grabReference.transform.rotation) * this.pickupReference.rotation;
                }
                else if (this.pickupMethod != GrabType.FixedJoint)
                {
                    if (this.physicsBody)
                    {
                        grabScript.ConnectJoint(this.physicsBody);
                    }
                    else
                    {
                        SenseGlove_Debugger.Log("Using a FixedJoint connection required a Rigidbody.");
                    }
                }
                

                //apply physicsBody settings.
                if (this.physicsBody)
                {
                    this.physicsBody.velocity = new Vector3(0, 0, 0);
                    this.physicsBody.angularVelocity = new Vector3(0, 0, 0);
                    if (this.pickupMethod != GrabType.FixedJoint)
                    {
                        this.physicsBody.useGravity = false;
                        this.physicsBody.isKinematic = true;
                    }
                }
                this.OnPickedUp();
            }
           
        }
    }

    public override void UpdateInteraction()
    {
        if (this.grabReference != null)
        {
            if (this.pickupMethod == GrabType.Follow)
            {
                this.pickupReference.rotation = this.grabReference.transform.rotation * this.grabRotation;
                this.pickupReference.position = this.grabReference.transform.position - (this.grabReference.transform.rotation * grabOffset);
            }
        }
    }



    public void EndInteraction()
    {
        this.EndInteraction(this._grabScript);
    }



    public override void EndInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
      //  Debug.Log("End Interaction");

        if (this.InteractingWith(grabScript))
        {

            if (this.IsInteracting())
            {   //break every possible instance that could connect this interactable to the grabscript.
                this._grabScript.BreakJoint();
                this.pickupReference.parent = this.originalParent;
                if (this.physicsBody != null)
                {
                    this.physicsBody.useGravity = this.usedGravity;
                    this.physicsBody.isKinematic = this.wasKinematic;
                    if (grabScript != null)
                    {
                        this.physicsBody.velocity = grabScript.GetVelocity();
                        this.physicsBody.angularVelocity = grabScript.GetAngularVelocity();
                    }
                }
            }

            if (this.physicsBody)
            {
                this.physicsBody.velocity = grabScript.GetVelocity();
            }

            this.OnReleased();

            this._grabScript = null;
            this.grabReference = null;
        }
    }

    public override void ResetObject()
    {
        this.CheckPickupRef();
        if (this.originalParent)
        {
            this.pickupReference.parent = originalParent;
            this.originalParent = null;
        }
        
        this.pickupReference.position = this.originalPos;
        this.pickupReference.rotation = this.originalRot;

        if (this.physicsBody)
        {
            this.physicsBody.velocity = Vector3.zero;
            this.physicsBody.angularVelocity = Vector3.zero;
            this.physicsBody.isKinematic = this.wasKinematic;
            this.physicsBody.useGravity = this.usedGravity;
        }
    }


    public override void SaveTransform()
    {
        this.CheckPickupRef();
        this.originalPos = this.pickupReference.transform.position;
        this.originalRot = this.pickupReference.transform.rotation;
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
