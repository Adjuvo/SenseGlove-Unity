using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SenseGlove_GrabScript : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Grabscript Options

    /// <summary> A SenseGlove_Object for gloveData related shenanigans. </summary>
    [Tooltip("A SenseGlove_Object for gloveData related shenanigans. Automatically assigned.")]
    public SenseGlove_Object senseGlove;

    public SenseGlove_HandModel handModel;

    /// <summary> When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement / parent / fixedJoint. </summary>
    [Header("Settings")]
    [Tooltip("When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement.")]
    public GameObject grabReference;

    /// <summary> The way that the object(s) will be picked up by this GrabScript. </summary>
    [Tooltip("The way that the object(s) will be picked up by this GrabScript.")]
    public GrabType pickupMethod = GrabType.Follow;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Interaction Variables

    /// <summary> Becomes true after the colliders have been succesfully assigned. </summary>
    protected bool setupFinished = false;

    /// <summary> The grabReference's position during the last frame Update() </summary>
    protected Vector3 lastPosition;
    /// <summary> The xyz velocity of the grabreference, in m/s </summary>
    protected Vector3 velocity = Vector3.zero;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Dynamics Methods


    /// <summary> Update the dynamics (velocity, angular velocity) of the grabreference. </summary>
    protected void UpdateDynamics()
    {
        if (this.grabReference != null)
        {
            Vector3 currentPos = this.grabReference.transform.position;
            this.velocity = (currentPos - lastPosition) / Time.deltaTime;
            this.lastPosition = currentPos;
        }
    }

    /// <summary> Retrieve the Velocity of this GameObject </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return this.velocity;
    }

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

    // Internal

    public abstract bool Setup();

    // External

    /// <summary> Manually force the SenseGlove_PhysGrab to drop whatever it is holding. </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public abstract void ManualRelease(float timeToReactivate = 1.0f);

    /// <summary> Returns true if this grabscript;s settings allow it to interact with other objects. </summary>
    /// <returns></returns>
    public abstract bool CanInteract();

    /// <summary> Return a list of GameObjects that this script is able to pick up. </summary>
    /// <returns></returns>
    public abstract GameObject[] CanPickup();


    /// <summary> Retrieve the hand palm touchscript of this grabScript. </summary>
    /// <returns></returns>
    public abstract SenseGlove_Touch GetPalm();

}



//-----------------------------------------------------------------------------------------------------------------------------------------
// Enumerators


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

/// <summary> The way that the Force-Feedback is calculated. </summary>
public enum ForceFeedbackType
{
    /// <summary> No Force feedback is calculated for this SenseGlove. </summary>
    None = 0,
    /// <summary> On/Off style force feedback using the Material's 'passive force'  </summary>
    Simple,
    /// <summary> Force feedback is calculated based on how far the fingers have collided within the object. </summary>
    MaterialBased
}
