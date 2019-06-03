using UnityEngine;
using System.Collections.Generic;

/// <summary> A DropZone that snaps a Grabable to a specific SnapPoint. </summary>
public class SenseGlove_SnapZone : SenseGlove_DropZone
{

    /// <summary> The way in which a SnapZone attaches objects to itself. </summary>
    public enum SnapMethod
    {
        /// <summary> The snapzone chooses which option to use, based on the Grabable's GrabType propery </summary>
        ObjectDependent = 0,
        /// <summary> The Grabable becomes a child object of the dropzone. If it posesses a Rigidbody, it is marked as kinematic. </summary>
        Parent,
        /// <summary> The DropZone creates a PhysicsJoint connection between this dropzone and the grabable rigidBody. </summary>
        FixedJoint
    }

    //--------------------------------------------------------------------------------------------------------------
    // Snap Properties

    #region SnapProperties

    /// <summary> Contains parameters that assist in snapping/unsnapping to a SnapZone. </summary>
    /// <remarks> Placed inside a class to reduce the amount of List<> parameters. </remarks>
    protected class SnapProps
    {
        /// <summary> Determines if this object was Interactable before it snapped to this zone. </summary>
        public bool wasInteractable;
        /// <summary> Determines if the RigidBody was Kinematic before it snapped. </summary>
        public bool wasKinematic;
        /// <summary> Determines if the RigidBody used Gravity before it snapped. </summary>
        public bool usedGravity;
        /// <summary> Optional PhysicsJoint that is created if the Object is picked up using FixedJoints. </summary>
        public Joint myJoint;
        /// <summary> The old parent of the object </summary>
        public Transform oldParent = null;

        /// <summary> Lets the zone know if this object has snapped yet. False by default. </summary>
        public bool isSnapped = false;

        /// <summary> Create a new instance of SnapProps, based on a singele Grabable's properties. </summary>
        /// <param name="grabable"></param>
        public SnapProps(SenseGlove_Grabable grabable)
        {
            this.wasInteractable = grabable.CanInteract();
            this.wasKinematic = grabable.WasKinematic;
            this.usedGravity = grabable.UsedGravity;
            this.oldParent = grabable.OriginalParent;
            this.myJoint = null;
        }


        /// <summary> Restore properties back to their original state(s). </summary>
        /// <param name="grabable"></param>
        public void RestoreProperties(SenseGlove_Grabable grabable)
        {
            if (!grabable.IsInteracting())
            {
                grabable.SetInteractable(this.wasInteractable);
                grabable.pickupReference.transform.parent = this.oldParent;
                if (grabable.physicsBody != null)
                {
                    grabable.physicsBody.useGravity = this.usedGravity;
                    grabable.physicsBody.isKinematic = this.wasKinematic;
                }
            }
            this.BreakJoint();
        }

        /// <summary> Create a Physics Joint between a grabable and a snapZone. </summary>
        /// <param name="grabable"></param>
        /// <param name="snapZoneBody"></param>
        /// <param name="breakForce"></param>
        public void CreateJoint(SenseGlove_Grabable grabable, Rigidbody snapZoneBody, float breakForce)
        {
            if (this.myJoint == null)
            {
                if (grabable.physicsBody != null)
                {
                    this.myJoint = grabable.physicsBody.gameObject.AddComponent<FixedJoint>();
                    this.myJoint.connectedBody = snapZoneBody;
                    this.myJoint.enableCollision = false;
                    this.myJoint.breakForce = breakForce;
                }
            }
            else
                SenseGlove_Debugger.LogError("Multiple Physics connections to my Properties. Wrong index!");
        }

        /// <summary> Destroy the PhysicsJoint if it was created in the past. </summary>
        public void BreakJoint()
        {
            if (this.myJoint != null)
            {
                GameObject.Destroy(this.myJoint);
                this.myJoint = null;
            }
        }
        
    }

    #endregion SnapProperties

    //--------------------------------------------------------------------------------------------------------------
    // Class Properties

    /// <summary> When set to true, this SnapZone automatically disables the interaction of the SenseGlove_Grabables that enter it. </summary>
    [Header("Snap Settings")]
    [Tooltip("When set to true, this SnapZone automatically disables the interaction of the SenseGlove_Grabables that enter it.")]
    public bool disablesInteraction = false;

    /// <summary> If set to true, this SnapZone ends the interaction between the hand and the interactable. </summary>
    [Tooltip("If set to true, this SnapZone ends the interaction between the hand and the interactable when detected.")]
    public bool takesFromHand = false;

    /// <summary> The point to which the SenseGlove_Grabables will attempt to snap. </summary>
    /// <remarks> If no RigidBody is attached to this zone, we will attempt to look for one here. </remarks>
    [Tooltip("The point to which the SenseGlove_Grabables will attempt to snap. If left unassigned, it will default to this GameObject")]
    [SerializeField] protected Transform snapPoint;

    /// <summary> The way in which the SnapZone attaches objects to itself. </summary>
    [Tooltip("The way in which the SnapZone attaches objects to itself.")]
    public SnapMethod snapMethod = SnapMethod.ObjectDependent;

    /// <summary> Contains properties for before/after snapping </summary>
    protected List<SnapProps> snapProperties = new List<SnapProps>();


    //--------------------------------------------------------------------------------------------------------------
    // Methods

    // Base Class Overrides

    /// <summary> Validates RB / Collider settings on initialization. Add another check for RigidBodies. </summary>
    public override void ValidateSettings()
    {
        base.ValidateSettings(); 
        if (this.physicsBody == null) //if this RB was not set in Base
            this.physicsBody = this.snapPoint.GetComponent<Rigidbody>();
    }


    /// <summary> Fires when an object first enters the zone. Record its snap-properties. </summary>
    /// <param name="grabable"></param>
    public override void AddObject(SenseGlove_Grabable grabable)
    {
        this.snapProperties.Add(new SnapProps(grabable)); //empty
        base.AddObject(grabable); //if time==0, then CallObjectDetect is also coaaled.
    }

    /// <summary> Called when an Object is detected and its event is called. End interation if needed, then snap it </summary>
    /// <param name="detectedObject"></param>
    protected override void CallObjectDetect(SenseGlove_Grabable detectedObject)
    {
        if (this.disablesInteraction)
            detectedObject.SetInteractable(false);

        if (this.takesFromHand)
            detectedObject.EndInteraction();

        if (!detectedObject.IsInteracting())
            this.AttachObject(detectedObject);
        else //we still are interacting, meaning we did not disable nor take from the hand.
            detectedObject.InteractionEnded += Grabable_InteractionEnded; //not subscribed to untill this object is released.

        base.CallObjectDetect(detectedObject);
    }

    /// <summary> Fires when an object is removed from the zone. Unsubscribe from method(s). </summary>
    /// <param name="index"></param>
    protected override void RemoveObject(int index)
    {
        this.ReleaseObject(index); //released the object
        this.snapProperties.RemoveAt(index);

        base.RemoveObject(index); //removes the object from a list.
    }



    // Class Methods


    /// <summary> Returns true if this particular object has been detected and snapped within the SnapZone. </summary>
    /// <param name="grabable"></param>
    /// <returns></returns>
    public bool IsSnapped(SenseGlove_Grabable grabable)
    {
        int index = ListIndex(grabable, this.objectsInside);
        if (index > -1)
            return this.snapProperties[index].isSnapped;
        return false;
    }


    /// <summary> Snaps an object to this Zone's snapPoint, based on the Grabable's grabType. </summary>
    /// <param name="grabable"></param>
    protected void AttachObject(SenseGlove_Grabable grabable)
    {
        grabable.SnapMeTo(this.snapPoint);
        grabable.InteractionBegun += Grabable_InteractionBegun;
        grabable.ObjectReset += Grabable_ObjectReset;

        int index = ListIndex(grabable, this.objectsInside);

        if (this.snapMethod == SnapMethod.FixedJoint 
            || (this.snapMethod == SnapMethod.ObjectDependent && grabable.pickupMethod == GrabType.FixedJoint))
        {
            if (grabable.physicsBody != null)
            {
                grabable.physicsBody.useGravity = true;
                if (index > -1)
                {
                    this.snapProperties[index].CreateJoint(grabable, this.physicsBody, SenseGlove_Grabable.defaultBreakForce);
                    this.snapProperties[index].isSnapped = true;
                }
            }
            else
                SenseGlove_Debugger.LogWarning(grabable.name + " does not have a RigidBody to attach to " + this.name + " via PhysicsJoint.");
        }
        else //any other way we snap it using the parent method.
        {
            grabable.pickupReference.parent = this.snapPoint;
            if (grabable.physicsBody != null)
            {
                grabable.physicsBody.useGravity = false;
                grabable.physicsBody.isKinematic = true;
            }

            if (index > -1)
                this.snapProperties[index].isSnapped = true;
        }
    }

    /// <summary> Release a specific object from the zone. </summary>
    /// <param name="grabable"></param>
    public void ReleaseObject(SenseGlove_Grabable grabable)
    {
        this.ReleaseObject(ListIndex(grabable, this.objectsInside));
    }

    /// <summary> Released an obejct from physics, but not from detection </summary>
    /// <param name="index"></param>
    protected void ReleaseObject(int index)
    {
        if (index > -1 && index <this.objectsInside.Count)
        {
            this.objectsInside[index].InteractionEnded -= Grabable_InteractionEnded;
            this.objectsInside[index].InteractionBegun -= Grabable_InteractionBegun;
            this.objectsInside[index].ObjectReset -= Grabable_ObjectReset;

            this.snapProperties[index].RestoreProperties(this.objectsInside[index]); //apply properties to the object back to where it was before.
        }
    }

    // Event Handlers

    /// <summary> Fires when an object is picked up from the Sense Glove. Disconnect it from this SnapZone. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Grabable_InteractionBegun(object source, SG_InteractArgs args)
    {
        SenseGlove_Grabable grabable = (SenseGlove_Grabable)source;
        grabable.InteractionBegun -= Grabable_InteractionBegun; //unsibscribe regardless.
        this.RemoveObject(grabable);
    }

    /// <summary> Fires when one of my ObjectsToGet is released. </summary>
    /// <remarks> Should only be subscribed to when  </remarks>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Grabable_InteractionEnded(object source, SG_InteractArgs args)
    {
        SenseGlove_Grabable grabable = (SenseGlove_Grabable)source;
        grabable.InteractionEnded -= Grabable_InteractionEnded; //unsubscribe from the method.
        this.AttachObject(grabable);
    }


    /// <summary> Fires when an object is reset. Disconnect it from this SnapZone.</summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void Grabable_ObjectReset(object source, System.EventArgs args)
    {
        Debug.Log("An object was reset!");

        SenseGlove_Grabable grabable = (SenseGlove_Grabable)source;
        int index = ListIndex(grabable, this.objectsInside);
        if (index > -1)
            this.snapProperties[index].BreakJoint();
        this.RemoveObject(grabable);
    }

    //--------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    // Used for initialization.
    protected virtual void Start()
    {
        if (this.snapPoint == null)
            this.snapPoint = this.transform;
    }

    ////Called when I'm destroyed..
    //private void OnDestroy()
    //{
    //    //this.ClearObjects();
    //}

}
