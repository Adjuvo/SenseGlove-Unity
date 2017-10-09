using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Drawer : SenseGlove_Interactable
{
    /// <summary> The movement axis along which the SenseGlove_Drawer slides. </summary>
    [Tooltip("The movement axis along which the SenseGlove_Drawer slides. Change this GameObject's rotation to match your desired direction.")]
    public DrawerAxis moveDirection = DrawerAxis.X;

    /// <summary> The handles connected to this drawer. </summary>
    [Tooltip("The handles connected to this drawer. Ensures that its grab events are recieved.")]
    public List<SenseGlove_GrabZone> handles = new List<SenseGlove_GrabZone>();

    // <summary>  Whether or not to snap this drawer onto its ends. </summary>
    //public bool snapToEnd = false;

    // <summary> Whether or not this drawer should return to original position. </summary>
    //public bool returnToPosition = true;

    /// <summary> The minimum distance that this drawer can move from its starting position. </summary>
    [Tooltip("The minimum distance that this drawer can move from its starting position.")]
    public float minDrawerDist = 0;

    /// <summary> The maximum distance that this drawer can move from its starting position. </summary>
    [Tooltip("The maximum distance that this drawer can mode from its starting position.")]
    public float maxDrawerDist = 1;

    //public GameObject drawerContensGroup;

    //public bool hideDrawerContents; //

    /// <summary> Used to ensure the open and closed events are not fired every time. </summary>
    private bool openEventFired = false, closeEventFired = true; //start in closed position.

    private GameObject grabReference;

    private Vector3 grabOffset = Vector3.zero;
    //private Quaternion grabRotation = Quaternion.identity;

    private Vector3 originalPos = Vector3.zero;
    private Quaternion originalRot = Quaternion.identity;

    /// <summary> The movement axis of this drawer, corrected by distance. Will always be normalized (size is 1) </summary>
    private Vector3 moveAxis;

    private DrawerAxis actualMoveDirection = DrawerAxis.X;

    void Awake()
    {
        this.isInteractable = false;
        this.actualMoveDirection = this.moveDirection;
        this.SetMoveAxis(this.actualMoveDirection);
        for (int i=0; i<this.handles.Count; i++)
        {
            this.handles[i].ConnectTo(this);
        }
    }

    void Start()
    {
        this.originalPos = this.transform.position;
        this.originalRot = this.transform.rotation;
    }

    void Update()
    {
        if (this.actualMoveDirection != this.moveDirection)
        {
            this.SetMoveAxis(this.moveDirection);
        }

        //the drawer is no longer being held.
        if (this.grabReference == null)
        {

        }

    }

    public override void BeginInteraction(SenseGlove_GrabScript grabScript)
    {
        //Debug.Log("Handle.BeginInteraction"); 
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

        }
    }

    public override void EndInteraction(SenseGlove_GrabScript grabScript)
    {
        //Debug.Log("Handle.EndInteraction");
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
        }
    }

    public override void ResetObject()
    {
        //Debug.Log("Handle.ResetObject");
    }

    public override void UpdateInteraction()
    {
        bool maxDistReached = false;
        bool minDistReached = false;

        //Debug.Log("Handle.UpdateInteraction");
        if (this.grabReference != null)
        {
           // Debug.Log("GrabReference != null");
            
            //Quaternion newRotation = this.grabReference.transform.rotation * this.grabRotation;
            Vector3 newPosition = this.grabReference.transform.position - (this.grabReference.transform.rotation * grabOffset);

            Vector3 OP = newPosition - this.originalPos;

            //project the new position on the desired axis and calculate its length.
            float cos = Vector3.Dot(OP, this.moveAxis) / (OP.magnitude);
            float dist = OP.magnitude * cos;

            //Debug.Log(SenseGlove_Util.ToString(OP) + " . " + SenseGlove_Util.ToString(this.moveAxis) + " = " + Vector3.Dot(OP, this.moveAxis));
            //Debug.Log(SenseGlove_Util.ToString(OP) + " proj on " + SenseGlove_Util.ToString(OP) + " = " + dist);

            //limit it within the drawer's contstraints
            
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
                (this.moveAxis.x*dist)+this.originalPos.x, 
                (this.moveAxis.y*dist)+this.originalPos.y, 
                (this.moveAxis.z*dist)+this.originalPos.z
            );
            
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

    /// <summary>
    /// Retrieve the current movement axis.
    /// </summary>
    /// <returns></returns>
    public Vector3 MoveAxis()
    {
        switch (this.moveDirection)
        {
            case DrawerAxis.X: return new Vector3(1, 0, 0);
            case DrawerAxis.Y: return new Vector3(0, 1, 0);
            case DrawerAxis.Z: return new Vector3(0, 0, 1);
        }
        return new Vector3(1, 0, 0); //will probably never get here unless one messes with the DrawerAxis variable(s)
    }


    //events


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


    /// <summary>
    /// Force this drawer to its open position
    /// </summary>
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

    /// <summary>
    /// Force this drawer to its original closed position
    /// </summary>
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

    /// <summary>
    /// Set the axis of this drawer; the cleanest way of doing so.
    /// </summary>
    /// <param name="newAxis"></param>
    public void SetMoveAxis(DrawerAxis newAxis)
    {
        this.actualMoveDirection = newAxis;
        this.moveAxis = (this.originalRot * this.MoveAxis()).normalized;
    }

    /// <summary> Wheck if this drawer is currently open </summary>
    /// <returns></returns>
    public bool IsOpen()
    {
        return (this.transform.position - this.originalPos).magnitude >= this.maxDrawerDist;
    }

    /// <summary>
    /// Check if this drawer is currently closed.
    /// </summary>
    /// <returns></returns>
    public bool IsClosed()
    {
        return (this.transform.position - this.originalPos).magnitude <= 0;
    }

}


/// <summary> The axis around which the drawer is moved. </summary>
public enum DrawerAxis
{
    X = 0,
    Y = 1,
    Z = 2
}