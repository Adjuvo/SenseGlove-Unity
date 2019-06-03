using System.Collections.Generic;
using UnityEngine;

/// <summary> Represents an Interactable that can rotate around a specified point and axis. Used to extend doors and levers. </summary>
public class SenseGlove_Hinge : SenseGlove_Interactable
{
    //--------------------------------------------------------------------------------------------
    // Attributes

    #region Attributes

    /// <summary> The point around which the hinge moves. </summary>
    public Transform hingePoint;

    /// <summary> The axis of the hingepoint around which the hinge moves. </summary>
    public MovementAxis hingeAxis = MovementAxis.Y;

    /// <summary> The (optional) physics-based hingejoint that controls the hinge's movement when not interacting. </summary>
    public HingeJoint joint;

    /// <summary> Set to true if you want the Sense Glove to be automatically set up. False to stop the SenseGlove from messing with your script(s). </summary>
    public bool autoSetup = true;

    /// <summary> The (optional) rigidbody of the hinge that moves it around when not interacting. </summary>
    private Rigidbody physicsBody;

    /// <summary> The minimum hinge angle, in degrees </summary>
    [Range(-180, 180)]
    public int minAngle = -180;

    /// <summary> The maximum hinge angle, in degrees </summary>
    [Range(-180, 180)]
    public int maxAngle = 180;

    /// <summary> The handles connected to this Interactable. </summary>
    public List<SenseGlove_GrabZone> handles = new List<SenseGlove_GrabZone>();

    // Private Variables

    /// <summary> The reference of the GrabScript that is holding this hinge </summary>
    private GameObject grabReference;

    /// <summary> The offset  </summary>
    //private Vector3 grabOffset;

    /// <summary> The offset angle between the grabreference and the hinge (handle) </summary>
    private float offsetAngle = 0;

    /// <summary> Whether the hinge used gravity before interaction started. </summary>
    private bool usedGravity = false;

    /// <summary> Whether the hinge was kinematic fefore any interaction started. </summary>
    private bool wasKinematic = true;


    #endregion Attributes

    //--------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    protected virtual void Awake()
    {
        if (this.autoSetup)
        {
            this.SetupHinge();
        }
    }

    // Use this for initialization
    protected virtual void Start()
    {
        for (int i = 0; i < this.handles.Count; i++)
        {
            this.handles[i].ConnectTo(this);
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //this.SetAngle(this.GethingeAngle(this.TEST.transform.position), true);
    }

    //called every physics-update
    protected virtual void FixedUpdate()
    {
        // this.CheckhingeLimits();
    }

    #endregion Monobehaviour

    //--------------------------------------------------------------------------------------------
    // Interaction Methods

    #region InteractionMethods

    /// <summary> Begin the interaction with this Interactable </summary>
    /// <param name="grabScript"></param>
    protected override bool InteractionBegin(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        //SenseGlove_Debugger.Log("Grabbing Lever");
        if (this.isInteractable || fromExternal)
        {

            if (!this.IsInteracting() && this.physicsBody != null)
            {
                //SenseGlove_Debugger.Log("Stored Kinematic variables");
                this.usedGravity = this.physicsBody.useGravity;
                this.wasKinematic = this.physicsBody.isKinematic;
            }


            this._grabScript = grabScript;
            this.grabReference = grabScript.grabReference;

            this.StopPhysicsBody();

            float currentHingeAngle = this.GetHingeAngle();
            float refAngle = this.GetHingeAngle(this.grabReference.transform.position);
            this.offsetAngle = currentHingeAngle - refAngle;

            //SenseGlove_Debugger.Log("The hinge's angle is currently " + currentHingeAngle + ", The grabreference is @" + refAngle + ". D = " + (this.offsetAngle));

            if (this.physicsBody)
            {
                this.physicsBody.useGravity = false;
                this.physicsBody.isKinematic = true;
            }
            return true;
        }
        return false;
    }

    /// <summary> Update the interaction with this Interactable. </summary>
    public override void UpdateInteraction()
    {
        //calculate the new position of the GrabReference
        if (this.grabReference != null)
        {
            float newAngle = this.GetHingeAngle(this.grabReference.transform.position) + offsetAngle;
            this.SetAngle(newAngle);
        }
    }

    /// <summary> Ends the interaction between the grabscript and this hinge </summary>
    /// <param name="grabScript"></param>
    protected override bool InteractionEnd(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.IsInteracting())
        {   //break every possible instance that could connect this interactable to the grabscript.
            if (this.physicsBody != null)
            {
                //SenseGlove_Debugger.Log("Re-Applied kinematics");
                this.physicsBody.useGravity = this.usedGravity;
                this.physicsBody.isKinematic = this.wasKinematic;
                this.StopPhysicsBody();
                if (grabScript != null)
                {
                    this.physicsBody.velocity = grabScript.GetVelocity();
                }
            }
        }
        this._grabScript = null;
        this.grabReference = null;

        return true;
    }

    #endregion InteractionMethods

    //--------------------------------------------------------------------------------------------
    // Hinge Methods

    #region HingeMethods

    /// <summary> Set this drawer and its handles to active / inactive. </summary>
    /// <param name="interactable"></param>
    public override void SetInteractable(bool interactable)
    {
        this.isInteractable = interactable;
        for (int i=0; i<this.handles.Count; i++)
        {
            this.handles[i].SetInteractable(interactable);
        }
    }

    /// <summary> Setup the hinge with the chosen options and verify them. </summary>
    public void SetupHinge()
    {
        this.isInteractable = false; //the hinge itself is not interactable(?)

        //check for unassigned bodies / hinges
        if (this.hingePoint == null)
        {
            this.hingePoint = this.transform;
        }
        if (this.physicsBody == null)
        {
            this.physicsBody = this.hingePoint.GetComponent<Rigidbody>();
            if (this.physicsBody == null)
            {
                this.physicsBody = this.gameObject.AddComponent<Rigidbody>();
            }
        }

        if (this.joint == null)
        {
            this.joint = this.hingePoint.GetComponent<HingeJoint>();
        }

        //we want a hinge
        if (this.joint != null)
        {
            this.joint.anchor = this.joint.gameObject.transform.InverseTransformPoint(this.hingePoint.position);
            this.joint.axis = this.joint.gameObject.transform.InverseTransformDirection(this.RotationAxis());
            this.joint.enableCollision = false;
        }
        else //We don't want the hinge.
        {
            this.physicsBody.useGravity = false;
            this.physicsBody.isKinematic = true;
        }

        //validate Limits
        if (this.minAngle != this.maxAngle)
        {
            int highestLimit = Mathf.Max(this.maxAngle, this.minAngle);
            int lowerLimit = Mathf.Min(this.maxAngle, this.minAngle);
            this.minAngle = lowerLimit;
            this.maxAngle = highestLimit;
        }


    }

    /// <summary> Stop the hinge body's movement before setting the angle(s) </summary>
    public void StopPhysicsBody()
    {
        if (this.physicsBody != null)
        {
            this.physicsBody.velocity = Vector3.zero;
            this.physicsBody.angularVelocity = Vector3.zero;
        }
    }

    /// <summary> Set the hinge angle to the desired value (in degrees), using its localRotation. </summary>
    /// <param name="newAngle"></param>
    public void SetAngle(float newAngle, bool freezeBody = false)
    {
        this.StopPhysicsBody();
        if (this.physicsBody && freezeBody)
        {
            this.physicsBody.isKinematic = true;
            this.physicsBody.useGravity = false;
        }

        //newAngle = SenseGloveCs.Values.Wrap( SenseGlove_hinge.NormalizeAngle( newAngle ), this.minAngle, this.maxAngle);

        Vector3 rotation = this.hingePoint.transform.localEulerAngles;
        if (this.hingeAxis == MovementAxis.X)
        {
            rotation.x = newAngle;
        }
        else if (this.hingeAxis == MovementAxis.Y)
        {
            rotation.y = newAngle;
        }
        else if (this.hingeAxis == MovementAxis.Z)
        {
            rotation.z = newAngle;
        }
        this.hingePoint.transform.localRotation = Quaternion.Euler(rotation);
    }

    /// <summary> Retrieve the local rotation angle of the hingePoint </summary>
    /// <returns></returns>
    public float GetHingeAngle()
    {
        Vector3 localAngles = this.hingePoint.localEulerAngles;
        float angle = 0;
        if (this.hingeAxis == MovementAxis.X)
        {
            angle = localAngles.x;
        }
        else if (this.hingeAxis == MovementAxis.Y)
        {
            angle = localAngles.y;
        }
        else if (this.hingeAxis == MovementAxis.Z)
        {
            angle = localAngles.z;
        }

        return SenseGlove_Util.NormalizeAngle(angle);
    }

    /// <summary> Retrieve the angle that the hinge should face to reach the chosen position. </summary>
    /// <param name="absPosition"></param>
    /// <returns></returns>
    public float GetHingeAngle(Vector3 absPosition)
    {
        float A = this.GetHingeAngle();
        float R = this.GetAngle(absPosition);
        return R + A;
    }

    /// <summary> Calculate the angle of an absolute position relative to the hinge [Internal use] </summary>
    /// <param name="absPosition"></param>
    /// <returns></returns>
    private float GetAngle(Vector3 absPosition)
    {
        //project the Object's Position on the plane that has our rotationAxis as a normal.
        Vector3 proj = this.hingePoint.InverseTransformPoint(absPosition);

        float res = 0;

        if (this.hingeAxis == MovementAxis.X)
        {
            res = Mathf.Atan2(proj.z, proj.y);
        }
        else if (this.hingeAxis == MovementAxis.Y)
        {
            res = -Mathf.Atan2(proj.z, proj.x);
        }
        else if (this.hingeAxis == MovementAxis.Z)
        {
            res = Mathf.Atan2(proj.y, proj.x);
        }

        res = SenseGloveCs.Values.Degrees(res);

        //  SenseGlove_Debugger.Log("Position " + SenseGlove_Util.ToString(absPosition) + " ==> " + res);

        res = SenseGlove_Util.NormalizeAngle(res);

        return res;
    }


    /// <summary> Check if the hinge is still within its working limits. </summary>
    private void CheckLimits()
    {
        float angle = this.GetHingeAngle();
        //SenseGlove_Debugger.Log(this.minAngle + " < " + angle + " < " + this.maxAngle);
        if (angle > this.maxAngle)
        {
            this.SetAngle(this.maxAngle);
        }
        else if (angle < this.minAngle)
        {
            this.SetAngle(this.minAngle);
        }
    }

    /// <summary> Returns the (absolute) rotation axis of this hinge. </summary>
    /// <returns></returns>
    private Vector3 RotationAxis()
    {
        Vector3 localAxis = new Vector3(0, 1, 0);
        if (this.hingeAxis == MovementAxis.X)
        {
            localAxis = new Vector3(1, 0, 0);
        }
        else if (this.hingeAxis == MovementAxis.Z)
        {
            localAxis = new Vector3(0, 0, 1);
        }
        return this.hingePoint.rotation * localAxis;
    }
    
    /// <summary> Retrieve the ratio (0 .. 1) of this Hinge Joint, from minAngle (0) to maxAngle (1). Used for events / animations. </summary>
    /// <returns></returns>
    public float HingeRatio()
    {
        return Mathf.Clamp01( SenseGloveCs.Values.Interpolate(this.GetHingeAngle(), this.minAngle, this.maxAngle, 0, 1) );
    }

    #endregion HingeMethods
    
}
