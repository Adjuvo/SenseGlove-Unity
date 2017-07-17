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
public class SenseGlove_PhysGrab : MonoBehaviour
{

    [Tooltip("Adding a SenseGlove_Object to this script is entirely optional, but increases the precision of the grab detection by checking the joint angles.")]
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
    // Velocity / Dynamics

    private Vector3 lastPosition;
    private Vector3 velocity = Vector3.zero;

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
    // Events

    public delegate void GrabObjectEventHandler(object source, GrabEventArgs args);
    public event GrabObjectEventHandler GrabbingObject;

    protected void OnGrabbingObject(GameObject obj)
    {
        if (GrabbingObject != null)
        {
            GrabbingObject(this, new GrabEventArgs() { gameObject = obj } );
        }
    }

    public delegate void ReleaseObjectEventHandler(object source, GrabEventArgs args);
    public event ReleaseObjectEventHandler ReleasingObject;

    protected void OnReleasingObject(GameObject obj)
    {
        if (ReleasingObject != null)
        {
            ReleasingObject(this, new GrabEventArgs() { gameObject = obj } );
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
        if (this.grabReference != null)
        {
            this.lastPosition = grabReference.transform.position;
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
                    OnGrabbingObject(grabAble); //raise the event;
                    GrabObject(grabAble);
                }
                canPickup = true;
            }
            else
            {
                if (canPickup) //if we could pickup an object before...
                {
                    //SenseGlove_Debugger.Log("Releasing an object");
                    OnReleasingObject(this.objectToGrab); // raise event
                    ReleaseObject(this.objectToGrab);
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
    GameObject CheckGrabObject()
    {
        //return this.thumb.IsTouching(this.index.TouchObject());
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

           // SenseGlove_Debugger.Log("Index isOpen is " + fingerOpen[1]);

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

    /// <summary>
    /// Check if the fingers are in the 'open' position (MCP-PIP-DIP joint angles +/- 0 deg),
    /// which would indicate that someone wants to let go of an object.
    /// </summary>
    /// <returns></returns>
    private bool[] CheckFingersOpen()
    {
        bool[] res = new bool[5];
        if (this.trackedGlove != null)
        {
            float[][][] angles = this.trackedGlove.GetGloveData().handModel.handAngles;
            float minAngle = Mathf.Deg2Rad * 15;
            res[0] = false; //ToDo - Check the thumb.
            for (int f=1; f<res.Length; f++)
            {
                if (angles[f][0][1] < minAngle && angles[f][1][1] < minAngle && angles[f][2][1] < minAngle) //the fingers are open, but for how long?
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

    /// <summary> Calculate the relative starting orientation and position of the Object to be grabbed, then  </summary>
    public void GrabObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.GetComponent<SenseGlove_Interactable>().BeginInteraction(this);
            this.objectToGrab = obj;
            holdingObject = true;
        }
    }

    /// <summary> Release a GameObject and, if it has a RigidBody, throw it! </summary>
    public void ReleaseObject(GameObject obj)
    {
        if (obj != null)
        {
            obj.GetComponent<SenseGlove_Interactable>().EndInteraction(this);
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
    /// 
    /// </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public void ManualRelease(float timeToReactivate = 1.0f)
    {
        this.paused = true;
        this.elapsedTime = 0;
        this.pauseTime = timeToReactivate;

        ReleaseObject(this.objectToGrab);
        holdingObject = false;
        this.objectToGrab = null;
    }

    /// <summary>
    /// Retrieve the Velocity of this GameObject
    /// </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return this.velocity;
    }

}
