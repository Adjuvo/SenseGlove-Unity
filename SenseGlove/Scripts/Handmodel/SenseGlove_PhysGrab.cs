using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> Physics-Based Grabbing using colliders rather than gestures, with options to make this method more intuitive. </summary>
[RequireComponent(typeof(SenseGlove_Object))]
public class SenseGlove_PhysGrab : SenseGlove_GrabScript
{
    //-----------------------------------------------------------------------------------------------------------------------------------
    // Public Variables

    #region Properties

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Public Variables

    /// <summary> If set to true, we check the intention of the user to determine if we can interact with objects. </summary>
    [Tooltip("If set to true, we check the intention of the user to determine if we can interact with objects.")]
    public bool checkIntention = false;

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

    /// <summary> The collider of the hand palm. </summary>
    public Collider palmCollider;

    //Debug Settings

    /// <summary> Method to debug the SenseGlove_Touch scripts that are connected to this GrabScript </summary>
    [Header("Debug")]
    [Tooltip("Method to debug the SenseGlove_Touch scripts that are connected to this GrabScript")]
    public PickupDebug debugMode = PickupDebug.Off;

    /// <summary> The color of the debug colliders </summary>
    [Tooltip("The color of the debug colliders.")]
    public Color debugColliderColor = Color.green;

    /// <summary> Whether of not this finger has the intention of grabbing an object </summary>
    [Tooltip("Whether of not the fingers have the intention of grabbing an object ")]
    public bool[] wantsGrab = new bool[5] { true, true, true, true, true };


    //-----------------------------------------------------------------------------------------------------------------------------------
    // Private Variables

    /// <summary> The actual touchScripts used for grab logic. </summary>
    private SenseGlove_Touch[][] touchScripts = null;

    /// <summary> The collider of the handPalm </summary>
    private SenseGlove_Touch handPalm;

    /// <summary> If paused, the GrabScript will no longer raise events or grab objects untill the pauseTime has elapsed. </summary>
    private bool paused = false;

    /// <summary> The time [s] that needs to elapse before the GrabScript can pick up another object. </summary>
    private float pauseTime = 1.0f;

    /// <summary> The amount of time that has elpased since the Manual Release function was called. </summary>
    private float elapsedTime = 0;

    /// <summary> The object(s) that are being held by this script. </summary>
    private List<SenseGlove_Interactable> heldObjects = new List<SenseGlove_Interactable>(2);

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    //Load Resources before Start() function is called
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

    //Runs once after Awake
    void Start()
    {
        if (this.senseGlove == null)
        {
            this.senseGlove = this.GetComponent<SenseGlove_Object>();
        }
    }

    //Runs once every frame
    void Update()
    {
        this.UpdateDynamics();
        
        if (this.setupFinished)
        {
            if (this.paused)
            {
                if (this.elapsedTime < this.pauseTime) { this.elapsedTime += Time.deltaTime; }
                else { this.paused = false; }
            }
            else
            {
                this.wantsGrab = this.CheckGestures();
                this.CheckGrab();
            }
        }
    }

    //runs after all trasforms have been updated
    void LateUpdate()
    {
        for (int f = 0; f < heldObjects.Count; f++)
        {
            //follow interactions on all follow interaction
            if (this.heldObjects[f] != null)
            {
                this.heldObjects[f].GetComponent<SenseGlove_Interactable>().UpdateInteraction();
            }
        }
    }

    #endregion Monobehaviour


    //-----------------------------------------------------------------------------------------------------------------------------------
    // Abstract Class Methods

    #region ClassMethods

    /// <summary> Check whether or not this grab script is interacting with any objects. </summary>
    /// <returns></returns>
    public override bool CanInteract()
    {
        return this.heldObjects.Count > 0;
    }

    /// <summary> Returns true if this grabscript is currently holding an object </summary>
    public override bool IsGrabbing()
    {
        return this.heldObjects.Count > 0;
    }

    /// <summary> Check if one of the physGrab colliders is touching something. </summary>
    /// <returns></returns>
    public override bool IsTouching()
    {
        if (this.setupFinished)
        {
            if (this.handPalm != null && this.handPalm.IsTouching()) { return true; }
            for (int f=0; f<this.touchScripts.Length; f++)
            {
                for (int i=0; i<this.touchScripts[f].Length; i++)
                {
                    if (touchScripts[f][i].IsTouching()) { return true; }
                }
            }
        }
        return false;
    }


    /// <summary>
    /// Return a list of objects that this pickup script is currently interacting with.
    /// </summary>
    /// <returns></returns>
    public override GameObject[] CanPickup()
    {
        GameObject[] objects = new GameObject[this.heldObjects.Count];
        for (int i = 0; i < this.heldObjects.Count; i++)
        {
            objects[i] = this.heldObjects[i].gameObject;
        }
        return objects;
    }

    /// <summary> Retrieve the Palm collider of this PhysGrab Script. </summary>
    /// <returns></returns>
    public override SenseGlove_Touch GetPalm()
    {
        return this.handPalm;
    }

    public override void ManualRelease(float timeToReactivate = 1)
    {
        this.paused = true;
        this.elapsedTime = 0;
        this.pauseTime = timeToReactivate;

        //release all objects
        for (int i = 0; i < this.heldObjects.Count; i++)
        {
            this.heldObjects[i].EndInteraction(this);
        }
        this.heldObjects.Clear();
    }

    #endregion ClassMethods

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Setup

    #region Setup

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
                for (int i = 0; i < touchColliders[f].Count; i++)
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
            script.CreateDebugObject(this.debugColliderColor);
            script.SetDebugLevel(this.debugMode);

            //also add a rigidbody
            Rigidbody RB = C.gameObject.GetComponent<Rigidbody>();
            if (RB == null)
            {
                RB = C.gameObject.AddComponent<Rigidbody>();
            }
            RB.useGravity = false;
            RB.isKinematic = true;

            return script;
        }
        return null;
    }


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

    #endregion Setup


    //-----------------------------------------------------------------------------------------------------------------------------------
    // Grab Logic

    #region GrabLogic


    /// <summary> Check if this grabscript should start or end an interaction. </summary>
    public void CheckGrab()
    {
        //find all objects that this script can currently grab
        List<SenseGlove_Interactable> grabables = this.GetGrabObjects();

        //End interaction with objects that are in the heldObjects but not in the grabables.
        for (int i = 0; i < this.heldObjects.Count;)
        {
            if (!this.IsInside(heldObjects[i], grabables))
            {
                this.heldObjects[i].EndInteraction(this);
                this.heldObjects.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        //Start interacting with objects that are in grabables and not yet in heldObjects
        for (int i = 0; i < grabables.Count; i++)
        {
            if (!this.AlreadyHeld(grabables[i]))
            {
                grabables[i].BeginInteraction(this);
                this.heldObjects.Add(grabables[i]);
            }
        }
    }

    /// <summary> Retrieve a list of all objects that CAN be grabbed </summary>
    /// <returns></returns>
    public List<SenseGlove_Interactable> GetGrabObjects()
    {
        List<SenseGlove_Interactable> grabables = new List<SenseGlove_Interactable>();

        SenseGlove_Touch thumbTouches = null;
        if (this.wantsGrab[0]) { thumbTouches = GetTouch(0); } //only if the thumb is used for grabbing

        //The thumb and hand palm are touching the same object
        if (thumbTouches != null && this.handPalm != null && thumbTouches.IsTouching(handPalm.TouchObject()))
        {
            if (thumbTouches.TouchedScript().fingerPalm) { grabables.Add(thumbTouches.TouchedScript()); }
        }

        for (int f = 1; f < this.touchScripts.Length; f++)
        {
            if (this.wantsGrab[f]) //if the user wants to grab something with this finger
            {
                SenseGlove_Touch fingerTouches = this.GetTouch(f);
                if (fingerTouches != null) //we've found a touchScript and it is touching an Interactable :D
                {
                    //check thumb-finger
                    if (fingerTouches.TouchedScript().fingerThumb && thumbTouches != null && fingerTouches.IsTouching(thumbTouches))
                    {
                        grabables.Add(fingerTouches.TouchedScript());
                    }
                    //check finger-palm
                    else if (fingerTouches.TouchedScript().fingerPalm && handPalm.TouchObject() != null && fingerTouches.IsTouching(handPalm))
                    {
                        grabables.Add(fingerTouches.TouchedScript());
                    }
                    //TODO: check finger-finger
                    //TODO: check fingertip-MCP
                }
            }
        }
        return grabables;
    }

    /// <summary> Get the first SenseGlove_Touch script of a finger that is touching an object, otherwise return null </summary>
    /// <param name="finger"></param>
    /// <returns></returns>
    public SenseGlove_Touch GetTouch(int finger)
    {
        for (int i = touchScripts[finger].Length; i-- > 0;) //checked from fingertip to MCP joint
        {
            if (touchScripts[finger][i].TouchObject() != null) { return this.touchScripts[finger][i]; }
        }
        return null;
    }


    private readonly int maxDIPFlexion = -70;
    private readonly int maxDIPExtension = 0;

    /// <summary> Check whether or not the user is intending to pick up any of the Interactables. </summary>
    public bool[] CheckGestures()
    {
        bool[] res = new bool[5] { true, true, true, true, true };

        if (this.senseGlove != null && this.senseGlove.GloveReady())
        {
            if (this.checkIntention)
            {
                //in Unity, the HandAngles are positive (extension) and negative (flexion), both in degrees.
                Vector3[][] handAngles = this.senseGlove.GloveData().handAngles;

                //thumb
                res[0] = handAngles[0][2].z >= -70 && handAngles[0][2].z <= 8;
            
                //fingers
                for (int f = 1; f < 5; f++)
                {
                    res[f] = handAngles[f][2].z >= maxDIPFlexion && handAngles[f][2].z <= maxDIPExtension;
                }
            }
        }
        else
        {
            res = new bool[5] { false, false, false, false, false };
        }
        return res;
    }

    /// <summary> Check if an interactable is already being held by this GrabScript </summary>
    /// <param name="interactable"></param>
    /// <returns></returns>
    public bool AlreadyHeld(SenseGlove_Interactable interactable)
    {
        return IsInside(interactable, this.heldObjects);
    }

    private bool IsInside(SenseGlove_Interactable obj, List<SenseGlove_Interactable> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (GameObject.ReferenceEquals(list[i], obj))
            {
                return true;
            }
        }
        return false;
    }

    #endregion GrabLogic

    
}

/// <summary> Debugging options for Pickup colliders. </summary>
public enum PickupDebug
{
    Off = 0,
    AlwaysOn,
    ToggleOnTouch
}