using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>  A knob that can be twisted along its axis. Used in intricate button panels. </summary>
public class SenseGlove_Dial : SenseGlove_Interactable
{
    /// <summary> Grab reference of the grabscript that is currently interacting with this Dial. </summary>
    protected Transform _grabReference;

    /// <summary> The point / object around which the object pivots. </summary>
    [SerializeField]
    protected Transform hingePoint;

    /// <summary> The (local) axis of the hingePoint around which this dial pivots. </summary>
    [SerializeField]
    protected MovementAxis hingeAxis = MovementAxis.X;

    /// <summary> Base rotation at startup, which is considered 0 </summary>
    protected Quaternion qBase = Quaternion.identity;

    /// <summary> local hinge vector, updated when changing the hingeAxis. </summary>
    protected Vector3 hinge = new Vector3(1, 0, 0);

    /// <summary> The last assigned angle; used for quick access.</summary>
    [SerializeField]
    protected float currAngle = 0;

    /// <summary> Whether the dial is limited in a ny direction or not. </summary>
    public bool useLimits = false;

    /// <summary> The minimum angle of the dial, when using limits </summary>
    public float minAngle = -180;

    /// <summary> The maximum angle of the dial, when using limits </summary>
    public float maxAngle = 180;

    /// <summary>  Offset between the grabreference and the hingepoint when the object was touched. </summary>
    protected Quaternion rotOffset = Quaternion.identity;
    /// <summary> The position of the dial when it was first touched. </summary>
    protected float anglOffset = 0;

    /// <summary> Index [x=0. y=1.z=2] by which to access the proper (local) euler angle.  </summary>
    protected int angleIndex = 0;



    /// <summary> Start an interaction between this dial and a sense glove. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionBegin(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (!InteractingWith(grabScript)) //never interact twice with the same grabscript before EndInteraction is called.
        {
            this._grabScript = grabScript;
            this._grabReference = grabScript.grabReference.transform;

            this.rotOffset = Quaternion.Inverse(this._grabReference.transform.rotation) * this.hingePoint.rotation;
            this.anglOffset = this.GetAngle();
            return true;
        }
        return false;
    }

    /// <summary> End an interaction between this dial and a Sense Glove. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    protected override bool InteractionEnd(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        this._grabScript = null;
        this._grabReference = null;
        return true;
    }

    /// <summary> Update the dial while it is held by the glove. </summary>
    public override void UpdateInteraction()
    {
        //called when the grabreference position has been updated  
        this.UpdateAngle();
    }


    /// <summary> Contained in a separate method for child classes. </summary>
    protected virtual void UpdateAngle()
    {
        Quaternion qDesiredAbs = this._grabReference.transform.rotation * this.rotOffset; //check the desired 3D angle
        Quaternion qRotOriginal = this.hingePoint.transform.rotation; //save current rotation

        this.hingePoint.rotation = qDesiredAbs; //perform rotation to calculate local angles
        float angle = SenseGlove_Util.NormalizeAngle(this.hingePoint.localEulerAngles[angleIndex]) + this.anglOffset; //retrieve local angle
        this.hingePoint.rotation = qRotOriginal; //return back now that we have the angle.

        this.SetAngle(angle); //Apply the new rotation
    }


    /// <summary> Retrieve the latest angle of the dial </summary>
    /// <returns></returns>
    public float GetAngle()
    {
        return this.currAngle;
    }

    
    /// <summary> Set the angle of the dial manually. Returns the angle that was set. </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public float SetAngle(float angle)
    {
        angle = ValidateAngle(angle);
        Quaternion qAxis = Quaternion.AngleAxis(angle, hinge);
        this.hingePoint.localRotation = this.qBase * qAxis;
        this.currAngle = angle;
        return this.currAngle;
    }

    /// <summary> Validate the dial angle before applying it. </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public float ValidateAngle(float angle)
    {
        angle = SenseGlove_Util.NormalizeAngle(angle);
        if (this.useLimits)
        {
            if (angle > this.maxAngle)
                angle = this.maxAngle;
            else if (angle < this.minAngle)
                angle = this.minAngle;
        }
        return angle;
    }





    /// <summary> Retrieve a Vector3 representation of this dial's local rotation axis. </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static Vector3 GetAxis(MovementAxis axis)
    {
        switch (axis)
        {
            case MovementAxis.X: return new Vector3(1, 0, 0);
            case MovementAxis.Y: return new Vector3(0, 1, 0);
            case MovementAxis.Z: return new Vector3(0, 0, 1);
        }
        return new Vector3(1, 0, 0);
    }


    /// <summary> Retrieve the index (x, y or z) of the movementAxis. </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static int AngleIndex(MovementAxis axis)
    {
        if (axis == MovementAxis.Y)
            return 1;
        if (axis == MovementAxis.Z)
            return 2;
        return 0;
    }



    //--------------------------------------------------------------------------------------------------------
    // Monobehaviour



    // Use this for initialization
    protected virtual void Start ()
    {
	    if (this.hingePoint == null)
        {
            this.hingePoint = this.transform;
        }
        this.qBase = this.hingePoint.localRotation;
        this.hinge = GetAxis(this.hingeAxis);
        this.currAngle = 0;
        this.angleIndex = SenseGlove_Dial.AngleIndex(this.hingeAxis);
    }

    // Update is called once per frame
    protected virtual void Update ()
    {
        //force end interaction of the grabscript is removed from the scene?
        if (this.IsInteracting() && !this._grabScript.gameObject.activeInHierarchy)
        {
            this.EndInteraction(null, true);
        }
    }
}
