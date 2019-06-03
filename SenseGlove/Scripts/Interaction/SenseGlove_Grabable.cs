using UnityEngine;

/// <summary> An object that can be picked up and dropped by the SenseGlove. </summary>
public class SenseGlove_Grabable : SenseGlove_Interactable
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    /// <summary> The way that this object is be picked up by a GrabScript. </summary>
    [Header("Grabable Options")]
    [Tooltip("The way that this object is be picked up by a GrabScript.")]
    public GrabType pickupMethod = GrabType.Parent;

    /// <summary> The way this object connects itself to the grabscript. </summary>
    [Tooltip("The way this object connects itself to the grabscript")]
    public AttachType attachMethod = AttachType.Default;

    /// <summary> If this object has an attachType of SnapToAnchor, this transform is used as a refrence. </summary>
    [Tooltip("If this object has an attachType of SnapToAnchor, this transform is used as a refrence.")]
    public Transform snapReference;

    /// <summary> Whether or not this object can be picked up by another Grabscript while it is being held. </summary>
    [Tooltip("Whether or not this object can be picked up from the Sense Glove by another Grabscript.")]
    public bool canTransfer = true;

    /// <summary> The transform that is grabbed instead of this object. Useful when dealing with a grabable that is a child of another grabable. </summary>
    [Tooltip("The transform that is grabbed instead of this object. Useful when dealing with a grabable that is a child of another grabable.")]
    public Transform pickupReference;

    /// <summary> The gameObject used as a reference for the Grabable's transform updates. </summary>
    protected GameObject grabReference;

    //Folllow GrabType Variables
    
    /// <summary> The xyz offset of this Grabable's transform to the grabReference, on the moment it was picked up. </summary>
    protected Vector3 grabOffset = Vector3.zero;
    /// <summary> The quaternion offset of this Grabable's transform to the grabReference, on the moment it was picked up.  </summary>
    protected Quaternion grabRotation = Quaternion.identity;

    //Parent GrabType Variables

    protected Transform originalParent;

    //PhysicsJoint GrabType Variables

    protected Joint connection;

    //Object RigidBody Variables

    /// <summary> The rigidBody to which velocity, gravity and kinematic options are applied. </summary>
    [Tooltip("The rigidBody to which velocity, gravity and kinematic options are applied. The script automatically connects to the Rigidbody attached to this GameObject.")]
    public Rigidbody physicsBody;

    /// <summary> Whether this grabable's physicsBody was kinematic before it was picked up. </summary>
    protected bool wasKinematic;
    /// <summary> Whether this grabable's physicsBody was used gravity before it was picked up. </summary>
    protected bool usedGravity;

    public const float defaultBreakForce = 4000;

    #endregion Properties
    
    //--------------------------------------------------------------------------------------------------------------------------
    // Class methods

    #region ClassMethods

    /// <summary> Called when a SenseGlove_Grabscript initiates an interaction with this grabable. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionBegin(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.isInteractable) //never interact twice with the same grabscript before EndInteraction is called.
        {
           // SenseGlove_Debugger.Log("Begin Interaction with " + grabScript.name);

            bool alreadyBeingHeld = this.IsInteracting();

            //if the object was actually grabbed.
            if (!alreadyBeingHeld || (alreadyBeingHeld && this.canTransfer))
            {
                //todo release?

                this.grabReference = grabScript.grabReference;
                this._grabScript = grabScript;

                this.originalDist = (grabScript.grabReference.transform.position - this.pickupReference.transform.position).magnitude;

                if (this.attachMethod != AttachType.Default && this.snapReference != null)
                {
                    if (this.attachMethod != AttachType.Default && this.snapReference != null)
                    {
                        if (this.attachMethod == AttachType.SnapToAnchor)
                        {
                            this.SnapMeTo(grabScript.grabAnchor.transform);
                        }
                        //other attachmethods.
                    }
                }


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
                else if (this.pickupMethod == GrabType.FixedJoint)
                {
                    this.ConnectJoint(grabScript.grabAnchor);
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
                return true;
            }
           
        }

        return false;
    }

    /// <summary> Called when this object is being held and the GrabReference is updated. </summary>
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
    
    /// <summary> Called when a SenseGlove_Grabscript no longer wishes to interact with this grabable. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionEnd(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        //SenseGlove_Debugger.Log("End Interaction, fromExternal = " + fromExternal);

        if (this.InteractingWith(grabScript) || fromExternal)
        {
            if (this.IsInteracting())
            {   //break every possible instance that could connect this interactable to the grabscript.
                if (this.pickupReference != null)
                    this.pickupReference.parent = this.originalParent;

                this.BreakJoint();

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
            
            this._grabScript = null;
            this.grabReference = null;

            return true;
        }
        return false;
    }

    /// <summary> Moves this Grbabale such that its snapRefrence matches the rotation and position of the originToMatch. </summary>
    /// <param name="originToMatch"></param>
    public void SnapMeTo(Transform originToMatch)
    {
        Quaternion Qto = originToMatch.rotation;
        Quaternion Qmain = this.pickupReference.transform.rotation;
        Quaternion Qsub = this.snapReference.transform.rotation;

        Quaternion QMS = Quaternion.Inverse(Qmain) * Qsub;
        this.pickupReference.rotation = (Qto) * Quaternion.Inverse(QMS);

        //calculate diff between my snapanchor and the glove's grabAnchor. 
        Vector3 dPos = this.snapReference.position - originToMatch.transform.position;
        this.pickupReference.transform.position = this.pickupReference.transform.position - dPos;
    }

    /// <summary> Save this object's position and orientation, in case the ResetObject function is called. </summary>
    public override void SaveTransform()
    {
        this.CheckPickupRef();
        this.originalParent = this.pickupReference.parent;
        this.originalPos = this.pickupReference.position;
        this.originalRot = this.pickupReference.rotation;
    }

    /// <summary> Reset this object back to its original position. Removes all connections between this and grabscripts. </summary>
    public override void ResetObject()
    {
        this.OnObjectReset();
        this.CheckPickupRef();

        this.BreakJoint();

        this.pickupReference.parent = originalParent;

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

    /// <summary>
    /// Check if this Interactable is currently being held by a SenseGlove GrabScript.
    /// </summary>
    /// <returns></returns>
    public bool IsGrabbed()
    {
        return this.grabReference != null;
    }


    #endregion ClassMethods




    //----------------------------------------------------------------------------------------------------------------------------------
    // Utility Methods

    #region Utility

    /// <summary> The original parent of this Grabable, before any GrabScripts picked it up. </summary>
    public Transform OriginalParent
    {
        get { return this.originalParent; }
        set { this.originalParent = value; }
    }

    /// <summary> Whether this Grabable used gravity before it was picked up </summary>
    public bool UsedGravity
    {
        get { return this.usedGravity; }
        set { this.usedGravity = value; }
    }

    /// <summary> Whether this Grabable was marked as Kinematic before it was picked up </summary>
    public bool WasKinematic
    {
        get { return this.wasKinematic; }
        set { this.wasKinematic = value; }
    }
    

    /// <summary> Set the Velocities of this script to 0. Stops the grabable from rotating / flying away. </summary>
    public void ZeroVelocity()
    {
        if (this.physicsBody)
        {
            this.physicsBody.velocity = Vector3.zero;
            this.physicsBody.angularVelocity = Vector3.zero;
        }
    }

    /// <summary> Connect this Grabable's rigidBody to another using a FixedJoint </summary>
    /// <param name="other"></param>
    /// <returns>True, if the connection was sucesfully made.</returns>
    public bool ConnectJoint(Rigidbody other, float breakForce = SenseGlove_Grabable.defaultBreakForce)
    {
        if (other != null)
        {
            if (this.physicsBody)
            {
                this.connection = this.physicsBody.gameObject.AddComponent<FixedJoint>();
                this.connection.connectedBody = other;
                this.connection.enableCollision = false;
                this.connection.breakForce = breakForce;
                return true;
            }
            else
            {
                SenseGlove_Debugger.Log("Using a FixedJoint connection requires a Rigidbody.");
            }
        }
        else
        {
            SenseGlove_Debugger.Log("No rigidbody to connect to " + other.name);
        }
        return false;
    }

    /// <summary> Remove a fixedJoint connection between this object and another. </summary>
    public void BreakJoint()
    {
        if (this.connection != null)
        {
            GameObject.Destroy(this.connection);
            this.connection = null;
        }
    }



    /// <summary> Enable/Disable rigidbody collision of this Grabable. </summary>
    /// <param name="active"></param>
    public void SetCollision(bool active)
    {
        if (this.physicsBody)
        {
            this.physicsBody.detectCollisions = active;
        }
    }

    /// <summary> Check the PickupReference of this Grabable </summary>
    public virtual void CheckPickupRef()
    {
        if (this.pickupReference == null)
        {
            this.pickupReference = this.transform;
        }
        if (this.snapReference == null)
        {
            this.snapReference = this.transform;
        }
    }

    /// <summary> Store the RigidBody parameters of this Grabable </summary>
    public virtual void SaveRBParameters()
    {
        if (!this.physicsBody) { this.physicsBody = this.pickupReference.GetComponent<Rigidbody>(); }

        //Verify the kinematic variables
        if (this.physicsBody)
        {
            this.wasKinematic = this.physicsBody.isKinematic;
            this.usedGravity = this.physicsBody.useGravity;
        }
    }

    #endregion Utility


    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    protected virtual void Awake()
    {
        this.CheckPickupRef();
        this.SaveRBParameters();
        this.SaveTransform();
    }

    protected virtual void Update()
    {
        if (!this.isInteractable && this.grabReference != null) { this.EndInteraction(null); } //end the interaction if the object is no longer interactable with.
    }

    #endregion Monobehaviour


}



/// <summary> The way in which this Grabscript picks up SenseGlove_Interactable objects. </summary>
public enum GrabType
{
    /// <summary> The grabbed object's transform follows that of the GrabReference through world coordinates. Does not interfere with VRTK scripts. </summary>
    Follow = 0,
    /// <summary> A FixedJoint is created between the grabbed object and the GrabReference, which stops it from passing through rigidbodies. </summary>
    FixedJoint,
    /// <summary> The object becomes a child of the Grabreference. Its original parent is restored upon release. </summary>
    Parent
}

/// <summary> The way that this SenseGlove_Grabable attaches to a GrabScript that tries to pick it up. </summary>
public enum AttachType
{
    /// <summary> Default. The object keeps its current position. </summary>
    Default = 0,
    /// <summary> The object snaps to the Grabscript in a predefined position and orientation; useful for tools etc. </summary>
    SnapToAnchor,
    // /// <summary> (BETA) Same as SnapToAnchor; but the object reaches its desired destination with a smooth animation. </summary>
    // FlowToAnchor
}
