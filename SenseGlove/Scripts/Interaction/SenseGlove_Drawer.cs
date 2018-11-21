using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A SenseGlove_Interactable that moves along one (local) axis. </summary>
public class SenseGlove_Drawer : SenseGlove_Interactable
{
    //---------------------------------------------------------------------------------------------------------------------------------------
    //  Properties

    #region Properties

    /// <summary> The movement axis along which the SenseGlove_Drawer slides. </summary>
    [Header("Drawer Options")]
    [Tooltip("The movement axis along which the SenseGlove_Drawer slides. Change this GameObject's rotation to match the desired direction.")]
    public MovementAxis moveDirection = MovementAxis.X;

    /// <summary> The handles connected to this drawer. </summary>
    [Tooltip("Optional handles connected to this drawer.")]
    public List<SenseGlove_GrabZone> handles = new List<SenseGlove_GrabZone>();

    // <summary>  Whether or not to snap this drawer onto its ends. </summary>
    //public bool snapToEnd = false;

    // <summary> Whether or not this drawer should return to original position. </summary>
    //public bool returnToPosition = true;

    /// <summary> The minimum distance that this drawer can move from its starting position. </summary>
    [Tooltip("The minimum distance that this drawer can move from its starting position in the positive moveDirection.")]
    public float minDrawerDist = 0;

    /// <summary> The maximum distance that this drawer can move from its starting position. </summary>
    [Tooltip("The maximum distance that this drawer can mode from its starting position in the positive moveDirection.")]
    public float maxDrawerDist = 1;

    //public GameObject drawerContensGroup;

    //public bool hideDrawerContents; //

    /// <summary> Used to ensure the open and closed events are not fired every time. </summary>
    private bool openEventFired = false, closeEventFired = true; //start in closed position.

    /// <summary> The Grabreference of the SenseGlove_Grabscript that is attached to this drawer. </summary>
    private GameObject grabReference;

    /// <summary> The offset between the grabReference at the time this drawer's interaction began. </summary>
    private Vector3 grabOffset = Vector3.zero;
    //private Quaternion grabRotation = Quaternion.identity;

    /// <summary> The movement axis of this drawer. Will always be normalized (size is 1) </summary>
    private Vector3 moveAxis;

    /// <summary> Automatically recalculates the MoveAxis when one changes the moveDirection via the public property. </summary>
    private MovementAxis actualMoveDirection = MovementAxis.X;

    #endregion Properties

    //---------------------------------------------------------------------------------------------------------------------------------------
    //  Monobehaviour

    #region Monobehaviour

    protected virtual void Awake()
    {
        this.isInteractable = false;
        this.actualMoveDirection = this.moveDirection;
        this.SetMoveAxis(this.actualMoveDirection);
        for (int i=0; i<this.handles.Count; i++)
        {
            this.handles[i].ConnectTo(this);
        }
    }

    protected virtual void Start()
    {
        this.originalPos = this.transform.position;
        this.originalRot = this.transform.rotation;
    }

    protected virtual void Update()
    {
        if (this.actualMoveDirection != this.moveDirection)
        {
            this.SetMoveAxis(this.moveDirection);
        }

        //the drawer is no longer being held.
        if (this.grabReference == null)
        {
            //return to original position.
        }

    }

    #endregion Monobehaviour

    //---------------------------------------------------------------------------------------------------------------------------------------
    //  Class Methods

    #region ClassMethods

    /// <summary> Called when a new SenseGlove_Grabscript engages in an interaction with this Drawer </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionBegin(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        //SenseGlove_Debugger.Log("Handle.BeginInteraction"); 
        if (!InteractingWith(grabScript)) //never interact twice with the same grabscript before EndInteraction is called.
        {
            this.grabReference = grabScript.handModel.foreArmTransfrom.gameObject;
            this._grabScript = grabScript;
   
            //Quaternion.Inverse(QT) * (vT - vO);
            this.grabOffset = Quaternion.Inverse(this.grabReference.transform.rotation) * (this.grabReference.transform.position - this.transform.position);

            //Quaternion.Inverse(QT) * (Qo);
            //this.grabRotation = Quaternion.Inverse(this.grabReference.transform.rotation) * this.transform.rotation;

            /*
            if (this.physicsBody)
            {
                this.wasKinematic = this.physicsBody.isKinematic;
                this.usedGravity = this.physicsBody.useGravity;

                this.physicsBody.useGravity = false;
                this.physicsBody.isKinematic = true;
                this.physicsBody.velocity = new Vector3(0, 0, 0);
                this.physicsBody.angularVelocity = new Vector3(0, 0, 0);
            }
            */
            return true;
        }
        return false;
    }

    /// <summary> Called when a SenseGlove_Grabscript ends the interaction with this drawer. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionEnd(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        //SenseGlove_Debugger.Log("Handle.EndInteraction");
        if (InteractingWith(grabScript)) //only do the proper endInteraction if the EndInteraction comes from the script currently holding it.
        {
            if (grabScript != null)
            {
                //if we're not being held by this same grabscript a.k.a. we've been passed on to another one...
                /*
                if (this.physicsBody != null)
                {
                    this.physicsBody.useGravity = this.usedGravity;
                    this.physicsBody.isKinematic = this.wasKinematic;
                    this.physicsBody.velocity = grabScript.GetVelocity();
                    //this.physicsBody.angularVelocity = ???
                }
                */
            };
            this.grabReference = null;
            this._grabScript = null;
            return true;
        }
        return false;
    }

    /// <summary> Called when the grabreference of the SenseGlove_Grabscript has been updated during the LateUpdate function. </summary>
    public override void UpdateInteraction()
    {
        bool maxDistReached = false;
        bool minDistReached = false;

        //SenseGlove_Debugger.Log("Handle.UpdateInteraction");
        if (this.grabReference != null)
        {
           // SenseGlove_Debugger.Log("GrabReference != null");
            
            //Quaternion newRotation = this.grabReference.transform.rotation * this.grabRotation;
            Vector3 newPosition = this.grabReference.transform.position - (this.grabReference.transform.rotation * grabOffset);

            Vector3 OP = newPosition - this.originalPos;

            //project the new position on the desired axis and calculate its length.
            float cos = Vector3.Dot(OP, this.moveAxis) / (OP.magnitude);
            float dist = OP.magnitude * cos;

            //SenseGlove_Debugger.Log(SenseGlove_Util.ToString(OP) + " . " + SenseGlove_Util.ToString(this.moveAxis) + " = " + Vector3.Dot(OP, this.moveAxis));
            //SenseGlove_Debugger.Log(SenseGlove_Util.ToString(OP) + " proj on " + SenseGlove_Util.ToString(OP) + " = " + dist);

            //limit it within the drawer's contstraints
            if (!float.IsNaN(dist))
            {

                if (dist >= this.maxDrawerDist)
                {
                    dist = maxDrawerDist;
                    maxDistReached = true;
                }
                else if (dist <= this.minDrawerDist)
                {
                    dist = minDrawerDist;
                    minDistReached = true;
                }

                //scale the (normalized) moveAxis by this same value
                this.transform.position = new Vector3(
                    (this.moveAxis.x * dist) + this.originalPos.x,
                    (this.moveAxis.y * dist) + this.originalPos.y,
                    (this.moveAxis.z * dist) + this.originalPos.z
                );

            }   
        }

        if (maxDistReached && !this.openEventFired)
        {
            this.openEventFired = true;
            this.closeEventFired = false;
            this.OnDrawerOpened();
        }
        else if (minDistReached && !this.closeEventFired)
        {
            this.closeEventFired = true;
            this.openEventFired = false;
            this.OnDrawerClosed();
        }
    }

    /// <summary> Retrieve the current movement axis [0, 0, 1]. </summary>
    /// <returns></returns>
    public Vector3 MoveAxis()
    {
        switch (this.actualMoveDirection)
        {
            case MovementAxis.X: return new Vector3(1, 0, 0);
            case MovementAxis.Y: return new Vector3(0, 1, 0);
            case MovementAxis.Z: return new Vector3(0, 0, 1);
        }
        return new Vector3(1, 0, 0); //will probably never get here unless one messes with the DrawerAxis variable(s)
    }


    /// <summary> Force this drawer to its open (maxDist) position. </summary>
    public void ForceOpen(bool raiseEvent = false)
    {
        this.transform.position = this.originalPos + new Vector3(
            this.moveAxis.x * this.maxDrawerDist,
            this.moveAxis.y * this.maxDrawerDist,
            this.moveAxis.z * this.maxDrawerDist
            );
        if (!this.openEventFired)
        {
            this.openEventFired = true;
            this.OnDrawerOpened();
        }
        this.closeEventFired = false;
    }

    /// <summary> Force this drawer to its original closed (minDist) position </summary>
    public void ForceClosed(bool raiseEvent = false)
    {
        this.transform.position = this.originalPos;
        if (!this.closeEventFired)
        {
            this.closeEventFired = true;
            this.OnDrawerClosed();
        }
        this.openEventFired = false;
    }

    /// <summary>  Set the moveDirection of this drawer. This method is cleaner than doing it via the public property </summary>
    /// <param name="newAxis"></param>
    public void SetMoveAxis(MovementAxis newAxis)
    {
        this.moveDirection = newAxis;
        this.actualMoveDirection = newAxis;
        this.moveAxis = (this.originalRot * this.MoveAxis()).normalized;
    }

    /// <summary> Wheck if this drawer is currently open </summary>
    /// <returns></returns>
    public bool IsOpen()
    {
        return (this.transform.position - this.originalPos).magnitude >= this.maxDrawerDist;
    }

    /// <summary> Check if this drawer is currently closed. </summary>
    /// <returns></returns>
    public bool IsClosed()
    {
        return (this.transform.position - this.originalPos).magnitude <= 0;
    }

    /// <summary> Save this drawer's current position when the ResetObject is called. </summary>
    public override void SaveTransform()
    {
        this.originalPos = this.transform.position;
    }

    /// <summary> Reset the drawer (and its contents?) To their original position. </summary>
    public override void ResetObject()
    {
        this.transform.position = this.originalPos;
    }


    #endregion ClassMethods

    //---------------------------------------------------------------------------------------------------------------------------------------
    //  Events

    #region Events

    //DrawerClosed
    public delegate void DrawerClosedEventHandler(object source, EventArgs args);
    /// <summary> Fires the Drawer returns to its initial position. </summary>
    public event DrawerClosedEventHandler DrawerClosed;

    protected void OnDrawerClosed()
    {
        if (DrawerClosed != null)
        {
            DrawerClosed(this, null);
        }
    }

    //DrawerExtended
    public delegate void DrawerOpenedEventHandler(object source, EventArgs args);
    /// <summary> Fires when the drawer reached its maximum extension. </summary>
    public event DrawerOpenedEventHandler DrawerOpened;

    protected void OnDrawerOpened()
    {
        if (DrawerOpened != null)
        {
            DrawerOpened(this, null);
        }
    }

    #endregion Events;


}



/// <summary> The axis along which the drawer is moved. </summary>
public enum MovementAxis
{
    X = 0,
    Y = 1,
    Z = 2
}