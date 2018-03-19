using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A Grabscript that uses a number of the Sense Glove's data to start and end interactions. </summary>
public abstract class SenseGlove_GrabScript : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Grabscript Options

    #region Properties

    /// <summary> A SenseGlove_Object for gloveData related shenanigans. </summary>
    [Tooltip("A SenseGlove_Object for gloveData related shenanigans.")]
    public SenseGlove_Object senseGlove;

    /// <summary> The handModel connected to this GrabScript. </summary>
    [Tooltip("The handModel connected to this GrabScript")]
    public SenseGlove_HandModel handModel;

    /// <summary> When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement / parent / fixedJoint. </summary>
    [Header("Settings")]
    [Tooltip("When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement.")]
    public GameObject grabReference;

    /// <summary> A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint. </summary>
    [Tooltip("A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint.")]
    public Rigidbody grabAnchor;

    /// <summary> If set to true, the position and rotation of the grabscript must be stable before one can start / end interaction. </summary>
    public bool checkStability = false;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Interaction Variables

    /// <summary> Becomes true after the colliders have been succesfully assigned. </summary>
    protected bool setupFinished = false;

    /// <summary> The grabReference's position during the last frame Update() </summary>
    protected Vector3 lastPosition;

    /// <summary> The grabReference's rotation during the last frame Update() </summary>
    protected Quaternion lastRotation;

    /// <summary> The xyz velocity of the grabreference, in m/s </summary>
    protected Vector3 velocity = Vector3.zero;

    /// <summary> Angilar velocity in rad/sec </summary>
    protected Vector3 angularVelocity = Vector3.zero;

    /// <summary> Whether there is consistent tracking on the grabreference (with regards to the positional tracking messing up) </summary>
    protected bool isStable = true;

    /// <summary> If the glove's velocity reaches above this number, the tracking is considered unstable. </summary>
    protected float velocityThreshold = 5;

    /// <summary> If the glove's angularVelocity reaches above this number, the tracking is considered unstable.  </summary>
    protected float angleVelocityThreshold = 25;

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Dynamics Methods

    #region Dynamics

    /// <summary> Update the dynamics (velocity, angular velocity) of the grabreference. </summary>
    protected void UpdateDynamics()
    {
        if (this.grabReference != null)
        {
            Vector3 currentPos = this.grabReference.transform.position;
            this.velocity = (currentPos - lastPosition) / Time.deltaTime;

            Quaternion currentRot = this.grabReference.transform.rotation;
            this.angularVelocity = SenseGlove_Util.CalculateAngularVelocity(currentRot, this.lastRotation);

            if (this.checkStability)
            {
                this.isStable = (this.velocity.magnitude <= this.velocityThreshold || angularVelocity.magnitude <= this.angleVelocityThreshold); 
            }
            else
            {
                this.isStable = true;
            }
            this.lastPosition = currentPos;
            this.lastRotation = currentRot;
        }
    }

    /// <summary> Manually set this grabscript to be stable, but only if the grabscript itself considers it stable as well </summary>
    /// <param name="stable"></param>
    public void SetStable(bool stable)
    {
        this.isStable = stable;
    }

    /// <summary> Check whether or not the grabscript's grabreference is considered 'stable'. </summary>
    /// <returns></returns>
    public bool IsStable()
    {
        return this.isStable;
    }

    /// <summary> Retrieve the Velocity of this GameObject </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return this.velocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return this.angularVelocity;
    }

    #endregion Dynamics

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    void Awake()
    {
        if (this.senseGlove == null)
        {
            this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }
        if (this.handModel == null)
        {
            this.handModel = this.gameObject.GetComponent<SenseGlove_HandModel>();
        }
        if (this.grabReference != null)
        {
            this.lastPosition = grabReference.transform.position;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Grabscript Methods

    #region GrabMethods

    // Internal

    /// <summary> Run setup on this grabscript; creating and/or resizing the proper colliders etc. </summary>
    /// <returns></returns>
    public abstract bool Setup();

    // External

    /// <summary> Manually force the SenseGlove_PhysGrab to drop whatever it is holding. </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public abstract void ManualRelease(float timeToReactivate = 1.0f);

    /// <summary> Returns true if this grabscript can currently pickup an object </summary>
    /// <returns></returns>
    public abstract bool CanInteract();

    /// <summary> Return a list of GameObjects that this script is able to pick up. </summary>
    /// <returns></returns>
    public abstract GameObject[] CanPickup();


    /// <summary> Retrieve the hand palm touchscript of this grabScript. </summary>
    /// <returns></returns>
    public abstract SenseGlove_Touch GetPalm();

    /// <summary> Returns true if this grabscript is currently holding an object </summary>
    public abstract bool IsGrabbing();

    /// <summary>  Returns true if the grabscript is touching an object </summary>
    /// <returns></returns>
    public abstract bool IsTouching();

    #endregion GrabMethods

}


