using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Physics-Based Grabbing using Colliders, without Parenting.
/// </summary>
[RequireComponent(typeof(SenseGlove_Object))]
public class SenseGlove_PhysGrab : SenseGlove_GrabScript
{

    /// <summary> The size of the pickup colliders, in mm. Used for Physics Based interactin through a SenseGlove_PhysGrab. </summary>
    //[Tooltip("The absolute radius of the pickup colliders, in m, assuming UNIFORM scaling. Used for Physics Based interaction through a SenseGlove_PhysGrab.")]
    //public float pickupColliderSize = 0.005f;

    /// <summary> Determines if the grabscript interacts with objects between the thumb and at least one finger. </summary>
    [Tooltip("Determine if the grabscript interacts with objects between the thumb and at least one finger.")]
    public bool thumbFingerCollision = true;

    /// <summary> Determines if the grabscript interacts with objects between the hand palm and at least one finger. </summary>
    [Tooltip("Determine if the grabscript interacts with objects between the hand palm and at least one finger.")]
    public bool fingerPalmCollision = false;

    /// <summary> Determines if the grabscript interacts with objects between its fingertip and its mcp joint. </summary>
    [Tooltip("Determines if the grabscript interacts with objects between its fingertip and its mcp joint.")]
    public bool fingerMCPCollision = false;

    /// <summary> The colliders that will be used for the thumb pickup logic.</summary>
    [Header("Grabscript Colliders")]
    [Tooltip("The colliders that will be used for the thumb pickup logic.")]
    public List<Collider> thumbColliders = new List<Collider>(1);

    /// <summary> The colliders that will be used for the index finger pickup logic." </summary>
    [Tooltip("The colliders that will be used for the index finger pickup logic.")]
    public List<Collider> indexColliders = new List<Collider>(1);

    /// <summary> The colliders that will be used for the middle finger pickup logic. </summary>
    [Tooltip("The colliders that will be used for the middle finger pickup logic.")]
    public List<Collider> middleColliders = new List<Collider>(1);

    /// <summary> The colliders that will be used for the ring finger pickup logic. </summary>
    [Tooltip("The colliders that will be used for the ring finger pickup logic.")]
    public List<Collider> ringColliders = new List<Collider>(1);

    /// <summary> The colliders that will be used for the little finger pickup logic. </summary>
    [Tooltip("The colliders that will be used for the little finger pickup logic.")]
    public List<Collider> pinkyColliders = new List<Collider>(1);


    /// <summary>
    /// The collider of the hand palm.
    /// </summary>
    public Collider palmCollider;

    /// <summary>
    /// The actual touchScripts used for grab logic.
    /// </summary>
    private SenseGlove_Touch[][] touchScripts = null;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private variables.

    /// <summary> The collider of the handPalm </summary>
    private SenseGlove_Touch handPalm;

    /// <summary> If paused, the GrabScript will no longer raise events or grab objects untill the pauseTime has elapsed. </summary>
    private bool paused = false;

    /// <summary> The time [s] that needs to elapse before the GrabScript can pick up another object. </summary>
    private float pauseTime = 1.0f;

    /// <summary> The amount of time that has elpased since the Manual Release function was called. </summary>
    private float elapsedTime = 0;

    /// <summary> Timers to keep track of how long the finger has been open. </summary>
    protected float[] openTimers = new float[5];

    /// <summary> The time the fingers must be kept open for them to register as such. </summary>
    protected static float openTime = 0.2f;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Adv. Interaction Variables

    /// <summary> The objects held by the five fingers </summary>
    private GameObject[] heldObjects = new GameObject[5];

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Force Feedback

    /// <summary> The distance between the thumb and the other fingers, used to check material properties. </summary>
    private float[] interactionDistances = new float[] { 0, 0, 0, 0, 0 };

    /// <summary> Distance (in mm) between two colliders when the grab-interaction started. Used to determine the force level </summary>
    private float[] grabDistances = new float[5] { -1, -1, -1, -1, -1 };

    
    /// <summary> Set the pickup colliders to the specified radius, in mm. </summary>
    /// <param name="newRadius"> The new radius, in mm, of the colliders. </param>
    /// <returns></returns>
    //public bool SetColliderRadius(float newRadius)
    //{
    //    if (this.setupFinished)
    //    {
    //        for (int f = 0; f < this.touchScripts.Length; f++)
    //        {
    //            for (int i = 0; i < this.touchScripts[f].Length; i++)
    //            {
    //                (((SphereCollider)this.touchScripts[f][i].touch)).radius = newRadius;
    //            }
    //        }
    //    }
    //    return false;
    //}
    
    //---------------------------------------------------------------------------------------------------------------------------------------------
    // MonoDevelop

    void Awake()
    {
        if (this.senseGlove == null)
        {
            this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }
        if (this.grabReference != null)
        {
            this.lastPosition = grabReference.transform.position;
        }
    }

    void Start()
    {
        if (!this.CanInteract())
        {
            SenseGlove_Debugger.Log("Warning: " + this.name + " is unable to pick up anything.");
        }

        if (this.senseGlove == null)
        {
            this.senseGlove = this.GetComponent<SenseGlove_Object>();
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
            GameObject[] grabbedObjects = this.CheckGrabObjects(); //check which objects can currenty be grabbed.

            //we know what each finger is touching. Now check the interactions
            for (int f = 0; f < this.heldObjects.Length && f < grabbedObjects.Length; f++)
            {
                if (grabbedObjects[f] != null && this.heldObjects[f] == null && !this.paused) // we can pick up a new object
                {
                    this.heldObjects[f] = grabbedObjects[f];
                    this.grabDistances[f] = this.interactionDistances[f];
                    if (!HeldByOther(f))
                    {
                       // Debug.Log("Pick Up " + grabbedObjects[f].name);
                        this.heldObjects[f].GetComponent<SenseGlove_Interactable>().BeginInteraction(this);
                    }
                }
                else if (grabbedObjects[f] == null)
                {
                    if (this.heldObjects[f] != null && !HeldByOther(f))
                    {
                       // Debug.Log("LETGO " + this.heldObjects[f].name);
                        this.heldObjects[f].GetComponent<SenseGlove_Interactable>().EndInteraction(this);
                    }
                    this.grabDistances[f] = 0;
                    this.interactionDistances[f] = 0;
                    this.heldObjects[f] = null;
                }
            }
        }

    }

    // Also called once per frame, but only after all Update() functions have been processed.
    void LateUpdate()
    {
        for (int f = 0; f < heldObjects.Length; f++)
        {
            //follow interactions on all follow interaction
            if (this.heldObjects[f] != null)
            {
                this.heldObjects[f].GetComponent<SenseGlove_Interactable>().UpdateInteraction();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------
    // Grab / Touch Logic

    /*
    /// <summary> Calculate the distances between the thumb, palm and MCP joints. </summary>
    private void CalculateDistances()
    {
        if (this.touchScripts != null && this.touchScripts[0].Length > 0)
        {
            for (int f = 1; f < this.touchScripts.Length; f++)
            {
                if (this.touchScripts[f].Length > 0)
                {
                    this.thumbDistances[f] = (this.touchScripts[0][this.touchScripts[0].Length - 1].transform.position
                        - this.touchScripts[f][this.touchScripts[f].Length - 1].transform.position).magnitude;
                }
            }
        }
    }
    */

    /// <summary> Check if the gameObject of a specific finger is not still being held by another finger. </summary>
    /// <param name="obj"></param>
    /// <param name="finger"></param>
    /// <returns></returns>
    private bool HeldByOther(int finger)
    {
        for (int f = 0; f < this.heldObjects.Length; f++)
        {
            if (f != finger && this.heldObjects[f] != null && GameObject.ReferenceEquals(this.heldObjects[finger], this.heldObjects[f]))
            {
                return true;
            }
        }
        return false;
    }


    /// <summary> Check which objects can be picked up by the Physics grab script. </summary>
    /// <returns></returns>
    public GameObject[] CheckGrabObjects()
    {
        GameObject[] grabbedObjects = new GameObject[5];

        if (this.setupFinished)
        {
            bool[] fingersOpen = this.CheckFingersOpen();
            GameObject tempObject = null;

            //check the hand palm
            GameObject palmTouches = this.handPalm != null ? this.handPalm.TouchObject() : null;

            //check the thumb
            GameObject thumbTouches = null;
            int thumbIndex = -1;
            if (!fingersOpen[0])
            {
                for (int i = touchScripts[0].Length; i-- > 0;)
                {
                    tempObject = touchScripts[0][i].TouchObject();
                    if (tempObject != null)
                    {
                        thumbTouches = tempObject;
                        thumbIndex = i;
                        break; // we've found out what the thumb is holding. Break out of the for loop
                    }
                }
            }

            if (thumbTouches != null || palmTouches != null)
            {
                for (int f = 0; f < this.touchScripts.Length; f++)
                {
                    if (!fingersOpen[f]) //no use checking if the finger should be open anyway.
                    {
                        for (int i = touchScripts[f].Length; i-- > 0;)
                        {
                            //thumb & other finger
                            if (this.thumbFingerCollision && f != 0 && touchScripts[f][i].IsTouching(thumbTouches))
                            {
                                grabbedObjects[f] = thumbTouches;
                                this.interactionDistances[f] = (touchScripts[f][i].transform.position - touchScripts[0][thumbIndex].transform.position).magnitude;
                                break;
                            }
                            // any finger and the hand palm
                            else if (this.fingerPalmCollision && touchScripts[f][i].IsTouching(palmTouches))
                            {
                                grabbedObjects[f] = palmTouches;
                                this.interactionDistances[f] = (touchScripts[f][i].transform.position - this.handPalm.transform.position).magnitude;
                                break;
                            }
                        }

                        //we have not yet found an object, we have enough colliders and we have set the variable to true.
                        if (this.fingerMCPCollision && grabbedObjects[f] == null && this.touchScripts[f].Length > 1
                            && touchScripts[f][0].IsTouching( touchScripts[f][touchScripts[f].Length - 1].TouchObject()) )
                        {
                            grabbedObjects[f] = touchScripts[f][0].TouchObject();
                            this.interactionDistances[f] = (touchScripts[f][0].transform.position - touchScripts[f][touchScripts[f].Length - 1].transform.position).magnitude;
                        }

                    }
                }

            }

        }

        return grabbedObjects;

    }


    /// <summary>
    /// Check if the fingers are in the 'open' position (MCP-PIP-DIP joint angles +/- 0 deg) for a few ms,
    /// which would indicate that someone wants to let go of an object.
    /// </summary>
    /// <returns></returns>
    private bool[] CheckFingersOpen()
    {
        bool[] res = new bool[5];
        if (this.senseGlove != null && senseGlove.GloveReady())
        {
            Vector3[][] angles = this.senseGlove.GloveData().handAngles;
            float minAngle = -15;
            res[0] = false; //ToDo - Check the thumb.
            res[res.Length - 1] = false; //prevent pinky interference.
            for (int f = 1; f < res.Length - 1; f++)
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
    /// Manually force the SenseGlove_PhysGrab to drop whatever it is holding.
    /// </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public override void ManualRelease(float timeToReactivate = 1.0f)
    {
        this.paused = true;
        this.elapsedTime = 0;
        this.pauseTime = timeToReactivate;

        //release all objects if possible.
        if (this.setupFinished)
        {
            GameObject[] grabbedObjects = new GameObject[this.heldObjects.Length];
            for (int f = 0; f < this.heldObjects.Length; f++)
            {
                if (this.heldObjects[f] != null)
                {
                    bool firedRelease = false;
                    for (int i = 0; i < f; i++) //check if we have already fired the EndInteraction (heldobjects[f] already added in the grabbedObjects list)) 
                    {
                        if (grabbedObjects[i] != null && GameObject.ReferenceEquals(grabbedObjects[i], this.heldObjects[f]))
                        {
                            firedRelease = true;
                            break;
                        }
                    }
                    if (!firedRelease)
                    {
                        //Debug.Log("FORCE LET GO OF " + this.heldObjects[f].name);
                        this.heldObjects[f].GetComponent<SenseGlove_Interactable>().EndInteraction(this);
                    }
                    grabbedObjects[f] = this.heldObjects[f];
                }
                heldObjects[f] = null;
            }
        }

    }

    //--------------------------------------------------------------------------------------------------------------------------------------
    // Get Functions for other scipts.

    /// <summary> Returns true if this grabscript;s settings allow it to interact with other objects. </summary>
    /// <returns></returns>
    public override bool CanInteract()
    {
        return this.thumbFingerCollision || this.fingerPalmCollision; 
    }

    /// <summary> Return a list of GameObjects that this script is able to pick up. </summary>
    /// <returns></returns>
    public override GameObject[] CanPickup()
    {
        List<GameObject> objects = new List<GameObject>();
        for (int f=0; f<this.heldObjects.Length; f++)
        {
            bool newObject = true;
            for (int i=0; i<f; i++)
            {
                if (GameObject.ReferenceEquals(this.heldObjects[f], this.heldObjects[i]))
                {
                    newObject = false;
                    break;
                }
            }
            if (newObject) { objects.Add(this.heldObjects[f]); }
        }
        return objects.ToArray();
    }

    /// <summary> Retrieve the hand palm touchscript of this grabScript. </summary>
    /// <returns></returns>
    public override SenseGlove_Touch GetPalm()
    {
        return this.handPalm;
    }


    //-------------------------------------------------------------------------------------------------------------------------------
    //  Setup Methods.
    
    
    /// <summary> Cleanup and remove the old finger Colliders and SenseGlove_Touch Scripts. </summary>
    /// <param name="removeColliders">Also destroy the colliders attached to the SenseGlove_Util scripts.</param>
    protected virtual void CleanUpColliders(bool removeColliders = true)
    {
        if (this.touchScripts != null)
        {
            for (int f = 0; f < this.touchScripts.Length; f++)
            {
                for (int i = 0; i < this.touchScripts[f].Length; i++)
                {
                    Collider C = touchScripts[f][i].touch;
                    Destroy(touchScripts[f][i]); //destroy the script itself, not the GameObject!
                    if (removeColliders) { Destroy(C); }
                }
            }
        }
        //the palm collider is not destroyed?

        this.setupFinished = false;
    }

    //internal

    /// <summary> Setup the colliders used for grab logic using the finger colliders assigned via the inspector. </summary>
    /// <returns></returns>
    public override bool Setup()
    {
        List<List<Collider>> colliders = new List<List<Collider>>();
        colliders.Add(this.thumbColliders);
        colliders.Add(this.indexColliders);
        colliders.Add(this.middleColliders);
        colliders.Add(this.ringColliders);
        colliders.Add(this.pinkyColliders);
        return this.Setup(colliders, this.palmCollider);
    }

    //external

    /// <summary> Setup the proper scripts and colliders via code. </summary>
    /// <param name="touchColliders"></param>
    /// <param name="palmCollider"></param>
    /// <returns></returns>
    public bool Setup(List<List<Collider>> touchColliders, Collider palmCollider)
    {
        this.handPalm = this.GetTouchScript(palmCollider);
        bool hasPalm = this.handPalm != null;

        bool hasFinger = false, hasThumb = false;
        if (touchColliders != null)
        {
            int n = touchColliders.Count > 5 ? 5 : touchColliders.Count;
            this.touchScripts = new SenseGlove_Touch[n][];
            for (int f = 0; f < this.touchScripts.Length; f++)
            {
                int colliderAmount = 0;
                //count the amount of valid colliders
                for (int i=0; i<touchColliders[f].Count; i++)
                {
                    if (touchColliders[f][i] != null) { colliderAmount++; }
                }
                
                //Add only valid colliders!
                this.touchScripts[f] = new SenseGlove_Touch[colliderAmount];
                int colliderIndex = 0;
                for (int i = 0; i < this.touchScripts[f].Length; i++)
                {
                    if (touchColliders[f][i] != null)
                    {
                        this.touchScripts[f][colliderIndex] = GetTouchScript(touchColliders[f][i]);
                        if (f == 0 && this.touchScripts[f][i] != null) { hasThumb = true; }
                        else if (this.touchScripts[f][i] != null) { hasFinger = true; }
                        colliderIndex++;
                    }
                }
            }

        }

        //TODO : Verify the correct Settings.
        this.setupFinished = (hasThumb && hasFinger) || ((hasFinger || hasThumb) && hasPalm);
        return this.setupFinished;
    }

    /// <summary>
    /// Get the SenseGlove_Touch of a specified collider. If none is present, create a new one.
    /// Then apply the desired settings. Returns null if the Collider is NULL.
    /// </summary>
    /// <param name="C"></param>
    /// <returns></returns>
    private SenseGlove_Touch GetTouchScript(Collider C)
    {
        if (C != null)
        {
            C.isTrigger = true;
            SenseGlove_Touch script = C.gameObject.GetComponent<SenseGlove_Touch>();
            if (script == null)
            {
                script = C.gameObject.AddComponent<SenseGlove_Touch>();
            }
            script.touch = C;
            script.SetSourceScript(this);
            return script;
        }
        return null;
    }

}
