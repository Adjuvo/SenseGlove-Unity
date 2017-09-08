using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Wishlist
 * ----------------------------
 * Hold Multiple Objects
 * Hold an object between fingertip and MCP joint
 * 
 */

/// <summary>
/// Physics-Based Grabbing using Colliders, without Parenting.
/// </summary>
[RequireComponent(typeof(SenseGlove_Object))]
public class SenseGlove_PhysGrab : MonoBehaviour
{

    /// <summary> A SenseGlove_Object for gloveData related shenanigans. </summary>
    [Tooltip("A SenseGlove_Object for gloveData related shenanigans. Automatically assigned.")]
    public SenseGlove_Object trackedGlove;

    /// <summary> When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement / parent / fixedJoint. </summary>
    [Header("Settings")]
    [Tooltip("When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement.")]
    public GameObject grabReference;

    /// <summary> Determines if the grabscript interacts with objects between the thumb and at least one finger. </summary>
    [Tooltip("Determine if the grabscript interacts with objects between the thumb and at least one finger.")]
    public bool thumbFingerCollision = true;

    /// <summary> Determines if the grabscript interacts with objects between the hand palm and at least one finger. </summary>
    [Tooltip("Determine if the grabscript interacts with objects between the hand palm and at least one finger.")]
    public bool fingerPalmCollision = true;

    /// <summary> Determines if this script can send force feedback back to its SenseGlove. </summary>
    [Tooltip("Determines if this script can send force feedback back to its SenseGlove. ")]
    public ForceFeedbackType ForceFeedback = ForceFeedbackType.None;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private variables.

    /// <summary> The colliders for each finger, which will be checked from distal to proximal. Those at index 0 are of the thumb. </summary>
    private SenseGlove_Touch[][] fingerColliders;

    /// <summary> The collider of the handPalm </summary>
    private SenseGlove_Touch handPalm;

    /// <summary> The GameObject that is being grabbed in between the colliders. </summary>
    private GameObject objectToGrab;

    /// <summary> Becomes true after the colliders have been succesfully assigned. </summary>
    private bool setupFinished = false;

    /// <summary> If this flag was false during the previous frame and we are picking up and object, then the GrabObject Function is called. </summary>
    private bool canPickup = false;

    /// <summary> Determines if we are currently holding an object, and if our GrabbedObject should follow us. </summary>
    private bool holdingObject = false;

    /// <summary> If paused, the GrabScript will no longer raise events or grab objects untill the pauseTime has elapsed. </summary>
    private bool paused = false;

    /// <summary> The time [s] that needs to elapse before the GrabScript can pick up another object. </summary>
    private float pauseTime = 1.0f;

    /// <summary> The amount of time that has elpased since the Manual Release function was called. </summary>
    private float elapsedTime = 0;

    /// <summary> Timers to keep track of how long the finger has been open. </summary>
    private float[] openTimers = new float[5];

    /// <summary> The time the fingers must be kept open for them to register as such. </summary>
    private static float openTime = 0.2f;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Adv. Interaction Variables

    public GameObject[] heldObjects = new GameObject[5];

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Force Feedback

    /// <summary> Distance (in mm) between two colliders when the grab-interaction started. Used to determine the force level </summary>
    private float[] grabDistances = new float[5] { -1, -1, -1, -1, -1 };
    
    /// <summary> The last sent motor levels of the SenseGlove. </summary>
    private int[] motorLevels = new int[] { 0, 0, 0, 0, 0 };

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Velocity / Dynamics

    /// <summary> The grabReference's position during the last frame Update() </summary>
    private Vector3 lastPosition;
    /// <summary> The xyz velocity of the grabreference, in m/s </summary>
    private Vector3 velocity = Vector3.zero;

    /// <summary> Update the dynamics (velocity, angular velocity) of the grabreference. </summary>
    private void UpdateDynamics()
    {
        if (this.grabReference != null)
        {
            Vector3 currentPos = this.grabReference.transform.position;
            this.velocity = (currentPos - lastPosition) / Time.deltaTime;
            this.lastPosition = currentPos;
        }
    }


    //----------------------------------------------------------------------------------------------------------------------------------------
    // Setup / Activation / Cleanup methods. Called by outside script(s)


    /// <summary> Setup with one collider per finger. </summary>
    /// <param name="tipColliders"> Colliders : [thimbTip, indexTip, middleTip, ringTip, pinkyTip] </param>
    public void SetupColliders(Collider[] tipColliders, Collider palmCollider = null)
    {
        if (tipColliders != null)
        {
            Collider[][] colliders = new Collider[tipColliders.Length][];
            for (int f = 0; f < colliders.Length; f++)
            {
                colliders[f] = new Collider[] { tipColliders[f] };
            }
            SetupColliders(colliders, palmCollider);
        }
    }

    /// <summary> Setup with multiple colliders for each finger. </summary>
    /// <param name="colliders">Colliders [ [thumbColliders], [indexColliders], ..., [pinkyColliders] ]</param>
    public void SetupColliders(Collider[][] colliders, Collider palmCollider = null)
    {
        bool palm = false;
        if (palmCollider != null)
        {
            palmCollider.isTrigger = true;
            this.handPalm = palmCollider.gameObject.AddComponent<SenseGlove_Touch>();
            this.handPalm.SetSourceScript(this);
            this.handPalm.touch = palmCollider;
            palm = this.handPalm != null;
        }

        if (colliders != null)
        {
            this.CleanUpColliders(setupFinished); //dont clean up if we have set up before.

            bool oneThumb = false;
            bool oneFinger = false;

            this.fingerColliders = new SenseGlove_Touch[5][];

            for (int f = 0; f < colliders.Length && f < this.fingerColliders.Length; f++)
            {
                this.fingerColliders[f] = new SenseGlove_Touch[colliders[f].Length];
                for (int i = 0; i < colliders[f].Length; i++)
                {
                    if (colliders[f][i] != null)
                    {
                        if (f == 0) { oneThumb = true; } //we have at least one thumb collider.
                        else { oneFinger = true; } //we have at least one finger
                        colliders[f][i].isTrigger = true;
                        SenseGlove_Touch touchScript = colliders[f][i].gameObject.AddComponent<SenseGlove_Touch>();
                        touchScript.touch = colliders[f][i];
                        touchScript.SetSourceScript(this);
                        this.fingerColliders[f][i] = touchScript;
                    }
                }
            }
            this.setupFinished = (oneThumb || palm) && oneFinger; //ensure there is at leas one finger.
            if (!this.setupFinished)
            {
                SenseGlove_Debugger.Log("Setup failed; we require at least one finger and one thumb- or palm collider!");
            }
        }
    }

    /// <summary> Setup with one collider per finger, with predefined SenseGlove Scripts (mostly for internal use). </summary>
    /// <param name="tipColliders"> Colliders : [thimbTip, indexTip, middleTip, ringTip, pinkyTip] </param>
    public void SetupColliders(SenseGlove_Touch[] tipColliders, SenseGlove_Touch palmCollider = null)
    {
        if (tipColliders != null)
        {
            SenseGlove_Touch[][] colliders = new SenseGlove_Touch[tipColliders.Length][];
            for (int f = 0; f < colliders.Length; f++)
            {
                colliders[f] = new SenseGlove_Touch[] { tipColliders[f] };
            }
            SetupColliders(colliders, palmCollider);
        }
    }

    /// <summary> Setup with multiple colliders for each finger, with predefined SenseGlove Scripts (mostly for internal use). </summary>
    /// <param name="colliders">Colliders [ [thumbColliders], [indexColliders], ..., [pinkyColliders] ]</param>
    public void SetupColliders(SenseGlove_Touch[][] colliders, SenseGlove_Touch palmCollider = null)
    {
        bool palm = false;
        if (palmCollider != null)
        {
            palmCollider.touch.isTrigger = true;
            palmCollider.SetSourceScript(this);
            this.handPalm = palmCollider;
            palm = true;
        }

        if (colliders != null)
        {
            this.CleanUpColliders(setupFinished);

            bool oneThumb = false;
            bool oneFinger = false;

            this.fingerColliders = new SenseGlove_Touch[5][];

            for (int f = 0; f < colliders.Length && f < this.fingerColliders.Length; f++)
            {
                this.fingerColliders[f] = new SenseGlove_Touch[colliders[f].Length];
                for (int i = 0; i < colliders[f].Length; i++)
                {
                    if (colliders[f][i] != null)
                    {
                        if (f == 0 && !oneThumb) { oneThumb = true; } //we have at least one thumb collider.
                        else { oneFinger = true; } //we have at least one finger
                        colliders[f][i].touch.isTrigger = true;
                        colliders[f][i].SetSourceScript(this);
                        this.fingerColliders[f][i] = colliders[f][i]; //TODO : Convert input to Collider[][] and add the SenseGlove.Collider here.
                    }
                }
            }

            this.setupFinished = (oneThumb || palm) && oneFinger; //ensure there is at leas one finger.
            if (!this.setupFinished)
            {
                SenseGlove_Debugger.Log("Setup failed; we require at least one finger and one thumb- or palm collider!");
            }
        }
    }

    /// <summary>
    /// Cleanup and remove the old finger Colliders and SenseGlove_Touch Scripts.
    /// </summary>
    /// <param name="removeColliders">Also destroy the colliders attached to the SenseGlove_Util scripts.</param>
    void CleanUpColliders(bool removeColliders = true)
    {
        if (this.fingerColliders != null)
        {
            for (int f = 0; f < this.fingerColliders.Length; f++)
            {
                for (int i = 0; i < this.fingerColliders[f].Length; i++)
                {
                    Collider C = fingerColliders[f][i].touch;
                    Destroy(fingerColliders[f][i]); //destroy the script itself, not the GameObject!
                    if (removeColliders) { Destroy(C); }
                }
            }
        }
        //Dont remove the hand palm-there is only one of those...
        //if (this.handPalm != null)
        //{
        //    Collider C = this.handPalm.touch;
        //    Destroy(this.handPalm);
        //    if (removeColliders) { Destroy(C); }
        //}
        this.setupFinished = false;
    }


    //---------------------------------------------------------------------------------------------------------------------------------------------
    // MonoDevelop

    void Awake()
    {
        if (this.trackedGlove == null)
        {
            this.trackedGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }
        if (this.grabReference != null)
        {
            this.lastPosition = grabReference.transform.position;
        }
    }
    
    void Start()
    {
        if (this.trackedGlove == null)
        {
            this.trackedGlove = this.GetComponent<SenseGlove_Object>();
        }
    }

    // Called once per frame.
    void Update()
    {
        this.UpdateDynamics();

        if (this.paused)
        {
            if (this.elapsedTime < this.pauseTime) { this.elapsedTime += Time.deltaTime; }
            else { this.paused = false; }
        }

        if (setupFinished)
        {
            GameObject grabAble = this.CheckGrabObject();
            if (grabAble != null && !this.paused)
            {
                //if we can grab the object and not 
                if (!canPickup && !holdingObject) //if we could not pickup the object before and are not holding anythign else, pick it up now!
                {
                    //SenseGlove_Debugger.Log("Grabbing an object");
                    GrabObject(grabAble.GetComponent<SenseGlove_Interactable>());
                }
                canPickup = true;
            }
            else
            {
                if (canPickup) //if we could pickup an object before...
                {
                    //SenseGlove_Debugger.Log("Releasing an object");
                    ReleaseObject();
                    holdingObject = false;
                }
                canPickup = false;
                this.objectToGrab = null;
            }
        }

    }

    // Also called once per frame, but only after all Update() functions have been processed.
    void LateUpdate()
    {
        if (this.objectToGrab != null)
        {
            this.objectToGrab.GetComponent<SenseGlove_Interactable>().FollowInteraction();
        }
        
    }

    //--------------------------------------------------------------------------------------------------------------------------------------
    // Grab / Touch Logic


    /// <summary> Check if we can grab an object. If we can, update the ObjectToGrab and return true. </summary>
    /// <returns> True if we can grab an object, false if we cannot. </returns>
    private GameObject CheckGrabObject()
    {
        if (this.setupFinished) //setupFinished means we have at least one finger and a thumb.
        {
            //check if the fingers are not open.
            bool[] fingerOpen = this.CheckFingersOpen();

            //check what the thumb is holding, starting from the tip.
            GameObject thumbTouches = null;
            GameObject palmTouches = this.handPalm != null ? this.handPalm.TouchObject() : null;

            GameObject tempObject;

            if (!fingerOpen[0]) //no point in searhcing if the thumb should not be grabbing anything.
            {
                for (int i = fingerColliders[0].Length; i-- > 0;)
                {
                    tempObject = fingerColliders[0][i].TouchObject();
                    if (tempObject != null)
                    {
                        thumbTouches = tempObject;
                        break; // we've found out what the thumb is holding. Break out of the for loop
                    }
                }
            }

            if (thumbTouches != null || palmTouches != null)
            {
                if (handPalm.IsTouching(thumbTouches) && !fingerOpen[0] && fingerPalmCollision)
                {
                    return thumbTouches; //The hanpalm touches the same object as the thumb.
                }

                //check if any of the other colliders are touching the same thing
                for (int f = 1; f < fingerColliders.Length; f++)
                {
                    if (!fingerOpen[f]) //no use checking if the finger should be open anyway.
                    {
                        for (int i = fingerColliders[f].Length; i-- > 0;)
                        {
                            if (thumbFingerCollision && fingerColliders[f][i].IsTouching(thumbTouches))
                            {
                                return thumbTouches;
                            }
                            else if (fingerPalmCollision && fingerColliders[f][i].IsTouching(palmTouches))
                            {
                                return palmTouches;
                            }
                        }
                    }
                }
            }

        }
        return null;
    }

    public void CheckGrabObjects()
    {
        if (this.setupFinished)
        {
            bool[] fingersOpen = this.CheckFingersOpen();
            GameObject tempObject = null;

            //check the hand palm
            GameObject palmTouches = this.handPalm != null ? this.handPalm.TouchObject() : null; 

            //check the thumb
            GameObject thumbTouches = null;
            if (!fingersOpen[0])
            {
                for (int i = fingerColliders[0].Length; i-- > 0;)
                {
                    tempObject = fingerColliders[0][i].TouchObject();
                    if (tempObject != null)
                    {
                        thumbTouches = tempObject;
                        break; // we've found out what the thumb is holding. Break out of the for loop
                    }
                }
            }

            if (thumbTouches != null || palmTouches != null)
            {
                for (int f=1; f<this.fingerColliders.Length; f++)
                {
                    if (!fingersOpen[f]) //no use checking if the finger should be open anyway.
                    {
                        //GameObject fingerTouches = null;

                        //for (int i = fingerColliders[f].Length; i-- > 0;)
                        //{
                        //    if (thumbFingerCollision && fingerColliders[f][i].IsTouching(thumbTouches))
                        //    {
                        //        fingerTouches = thumbTouches;
                                
                        //        break;
                        //    }
                        //    else if (fingerPalmCollision && fingerColliders[f][i].IsTouching(palmTouches))
                        //    {
                        //        //touching something new
                        //        fingerTouches = palmTouches;
                        //        break;
                        //    }
                        //}



                    }


                }
                

            }


        }
    }


    /// <summary>
    /// Check if the fingers are in the 'open' position (MCP-PIP-DIP joint angles +/- 0 deg) for a few ms,
    /// which would indicate that someone wants to let go of an object.
    /// </summary>
    /// <returns></returns>
    private bool[] CheckFingersOpen()
    {
        bool[] res = new bool[5];
        if (this.trackedGlove != null && trackedGlove.GloveReady())
        {
            Vector3[][] angles = this.trackedGlove.GloveData().handAngles;
            float minAngle = -15;
            res[0] = false; //ToDo - Check the thumb.
            res[res.Length - 1] = false; //prevent pinky interference.
            for (int f=1; f<res.Length - 1; f++)
            {
                if (angles[f][0].z > minAngle && angles[f][1].z > minAngle && angles[f][2].z > minAngle) //the fingers are open, but for how long?
                {
                    if (openTimers[f] < openTime) { openTimers[f] += Time.deltaTime; }
                    else { res[f] = true; }
                }
                else
                {
                    openTimers[f] = 0;
                }
            }
        }
        return res;
    }

    /// <summary>
    /// Check if this SenseGlove_PhysGrab touches anything, and send force feedback commands.
    /// </summary>
    public void CheckForceFeedback()
    {
        if (this.trackedGlove != null && this.fingerColliders != null)
        {
            if (this.ForceFeedback == ForceFeedbackType.Simple)
            {
                this.motorLevels = new int[5] { 0, 0, 0, 0, 0 };
                for (int f = 0; f < this.fingerColliders.Length; f++)
                {
                    if (this.fingerColliders[f].Length > 0)
                    {
                        this.motorLevels[f] = this.fingerColliders[f][this.fingerColliders[f].Length - 1].TouchObject() != null ? 255 : 0;
                    }
                }
                this.trackedGlove.SimpleBrakeCmd(this.motorLevels);
            }
            else if (this.ForceFeedback == ForceFeedbackType.MaterialBased)
            {
                this.motorLevels = new int[5] { 0, 0, 0, 0, 0 };
                for (int f = 0; f<this.grabDistances.Length; f++)
                {

                }
                this.trackedGlove.SimpleBrakeCmd(this.motorLevels);
            }
        }
    }


    /// <summary> Calculate the relative starting orientation and position of the Object to be grabbed, then  </summary>
    public void GrabObject(SenseGlove_Interactable obj)
    {
        if (obj != null)
        {
            obj.BeginInteraction(this);
            this.objectToGrab = obj.gameObject;
            holdingObject = true;
        }
    }

    /// <summary> Release a GameObject and, if it has a RigidBody, throw it! </summary>
    private void ReleaseObject()
    {
        if (this.holdingObject && this.objectToGrab != null)
        {
            this.objectToGrab.GetComponent<SenseGlove_Interactable>().EndInteraction(this);
        }
    }

    /// <summary>
    /// Throw a rigidbody, using the (angular) velocity of the grabReference.
    /// </summary>
    /// <param name="RB"></param>
    [Obsolete("Will be removed due to the new GrabScript Handling")]
    void TossObject(Rigidbody RB)
    {
        if (RB != null)
        {
            RB.useGravity = true;
            RB.isKinematic = false;
            RB.velocity = this.velocity;
            RB.angularVelocity = new Vector3(0, 0, 0);
        }
    }

    /// <summary>
    /// Check if this script can interact with SenseGlove_Interactable objects.
    /// </summary>
    /// <returns></returns>
    public bool CanPickup()
    {
        return this.thumbFingerCollision || this.fingerPalmCollision;
    }

    /// <summary>
    /// Manually force the SenseGlove_PhysGrab to drop whatever it is holding.
    /// </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public void ManualRelease(float timeToReactivate = 1.0f)
    {
        this.paused = true;
        this.elapsedTime = 0;
        this.pauseTime = timeToReactivate;

        ReleaseObject();
        holdingObject = false;
        this.objectToGrab = null;
    }



    //--------------------------------------------------------------------------------------------------------------------------------------
    // Get Functions for other scipts.


    /// <summary>
    /// Retrieve the Velocity of this GameObject
    /// </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return this.velocity;
    }

    /// <summary> Retrieve the hand palm of this SenseGlove_touch </summary>
    /// <returns></returns>
    public SenseGlove_Touch GetPalm()
    {
        return this.handPalm;
    }


}

public enum ForceFeedbackType
{
    None = 0,
    Simple,
    MaterialBased
}