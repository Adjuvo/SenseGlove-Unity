using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> Physics-Based Grabbing using colliders rather than gestures, with options to make this method more intuitive. </summary>
public class SenseGlove_PhysGrab : SenseGlove_GrabScript
{
    //-----------------------------------------------------------------------------------------------------------------------------------
    // Public Variables

    #region Properties

    /// <summary> If set to true, we check the intention of the user to determine if we can interact with objects. </summary>
    [Tooltip("If set to true, we check the intention of the user to determine if we can interact with objects.")]
    protected CheckIntention checkIntention = CheckIntention.Static;

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
    [Tooltip("The collider of the hand palm.")]
    public Collider palmCollider;

    //Debug Settings

    /// <summary> Whether of not this finger has the intention of grabbing an object </summary>
    [Tooltip("Whether of not the fingers have the intention of grabbing an object ")]
    protected bool[] wantsGrab = new bool[5] { true, true, true, true, true };

    /// <summary> Used for Gesture Recognition </summary>
    protected float[] dipAngles = new float[5] { 0, 0, 0, 0, 0 };

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Private Variables

    /// <summary> The actual touchScripts used for grab logic. </summary>
    protected SenseGlove_Touch[][] touchScripts = null;

    /// <summary> The collider of the handPalm </summary>
    protected SenseGlove_Touch handPalm;


    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    

    #endregion Monobehaviour


    //-----------------------------------------------------------------------------------------------------------------------------------
    // Abstract Class Methods

    #region ClassMethods

    /// <summary> Check whether or not this grab script is interacting with any objects. </summary>
    /// <returns></returns>
    public override bool CanInteract()
    {
        return this.IsTouching();
    }

    /// <summary> Returns true if this grabscript is currently holding an object </summary>
    public override bool IsGrabbing()
    {
        return this.heldObjects.Count > 0;
    }

    /// <summary> Check if at least one of the physGrab colliders is touching something. Used for logic </summary>
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

    /// <summary> CHeck if this script wishes to release objects at the moment. </summary>
    /// <returns></returns>
    protected override bool CanRelease(SenseGlove_Interactable obj)
    {
        if (obj.releaseMethod == ReleaseMethod.MustOpenHand)
            return !this.wantsGrab[0] && !this.wantsGrab[1]; //only index and thumb atm.
        return base.CanRelease(obj);
    }
    
    /// <summary> Manually release (all) objects that this SenseGlove_physgrab is interacting with. </summary>
    /// <param name="timeToReactivate">(Optional) time (in seconds) before the SenseGlove_Physgrab can start picking up objects again.</param>
    public override void ManualRelease(float timeToReactivate = 1)
    {
        //Debug.Log("ManualRelease!");
        this.paused = true;
        this.elapsedTime = 0;
        this.pauseTime = timeToReactivate;

        base.ManualRelease(timeToReactivate);
    }


    /// <summary> Clear all touched objects by the touchScripts. </summary>
    public override void ClearHeldObjects()
    {
        for (int f=0; f<this.touchScripts[f].Length; f++)
        {
            for (int i=0; i<this.touchScripts[f].Length; i++)
            {
                this.touchScripts[f][i].ClearTouchedObjects();
            }
        }
    }

    /// <summary> Update this GrabScript's logic. </summary>
    public override void UpdateGrabScript()
    {
        this.wantsGrab = this.CheckGestures();
        this.CheckGrab();
    }

    #endregion ClassMethods

    //-----------------------------------------------------------------------------------------------------------------------------------
    // Setup

    #region Setup

    /// <summary> Setup the colliders used for grab logic using the finger colliders assigned via the inspector. </summary>
    /// <returns></returns>
    public override bool Setup()
    {
        if (!this.setupFinished)
        {
            List<List<Collider>> colliders = new List<List<Collider>>();
            colliders.Add(this.thumbColliders);
            colliders.Add(this.indexColliders);
            colliders.Add(this.middleColliders);
            colliders.Add(this.ringColliders);
            colliders.Add(this.pinkyColliders);
            return this.Setup(colliders, this.palmCollider);
        }
        else return this.setupFinished;
    }


    /// <summary> Setup the proper scripts and colliders via code. </summary>
    /// <param name="touchColliders"></param>
    /// <param name="palmCollider"></param>
    /// <returns></returns>
    public bool Setup(List<List<Collider>> touchColliders, Collider palmCollider)
    {
        this.handPalm = SenseGlove_GrabScript.GetTouchScript(palmCollider, this);
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
                        this.touchScripts[f][colliderIndex] = SenseGlove_GrabScript.GetTouchScript(touchColliders[f][i], this);
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


    /// <summary> Check if this SenseGlove_PhysGrab script is ready to go. </summary>
    /// <returns></returns>
    public bool SetupComplete()
    {
        return this.touchScripts != null;
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

    /// <summary> Retrieve whether each of the fingers want to grab an object </summary>
    public bool[] GrabValues
    {
        get { return this.wantsGrab; }
    }

    

    #region GrabLogic


    /// <summary> Check if this grabscript should start or end an interaction and do so. </summary>
    public void CheckGrab()
    {
        //find all objects that this script can currently grab
        List<SenseGlove_Interactable> grabables = this.GetGrabObjects();

        //End interaction with objects that are in the heldObjects but not in the grabables.
        for (int i = 0; i < this.heldObjects.Count;)
        {
            if (!this.IsInside(heldObjects[i], grabables)
                && this.CanRelease(heldObjects[i]) && heldObjects[i].EndInteractAllowed())
            {
               // Debug.Log("Ending interaction with " + heldObjects[i].name + " because we are no longer touching it.");
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
    

    /// <summary> Retrieve a list of all objects that CAN be grabbed this frame </summary>
    /// <returns></returns>
    protected List<SenseGlove_Interactable> GetGrabObjects()
    {
        List<SenseGlove_Interactable> grabables = new List<SenseGlove_Interactable>();

        SenseGlove_Touch thumbTouches = null;
        thumbTouches = GetTouch(0); //the fingers can still override a thumb.wantsGrab()

        //The thumb and hand palm are touching the same object, and the thumb wants to grab something.
        if (wantsGrab[0] && thumbTouches != null && this.handPalm != null && thumbTouches.IsTouching(handPalm.TouchObject()))
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
    protected SenseGlove_Touch GetTouch(int finger)
    {
        for (int i = touchScripts[finger].Length; i-- > 0;) //checked from fingertip to MCP joint
        {
            if (touchScripts[finger][i].TouchObject() != null) { return this.touchScripts[finger][i]; }
        }
        return null;
    }

    /// <summary> Test Variable </summary>
    protected readonly int maxDIPFlexion = -160;
    /// <summary> Test Variable </summary>
    protected readonly int maxDIPExtension = -10;

    /// <summary> Check whether or not the user is intending to pick up any of the Interactables. </summary>
    protected bool[] CheckGestures()
    {
        bool[] res = new bool[5] { true, true, true, true, true };
        if (this.checkIntention != CheckIntention.Off)
        {
            if (this.senseGlove != null && this.senseGlove.GloveReady)
            {
                //bool[] D = new bool[5] { true, true, true, true, true };

                Vector3[] sumAngles = this.senseGlove.GloveData.TotalGloveAngles();

                //collect the relative flexion angles of the fingers.
                SenseGlove_Data data = this.senseGlove.GloveData;
                this.dipAngles = new float[5];
                for (int f=0; f<5; f++)
                    this.dipAngles[f] = sumAngles[f].z;

                if (this.checkIntention >= CheckIntention.Static)
                {
                    //in Unity, the HandAngles are positive (extension) and negative (flexion), both in degrees.
                    //thumb
                    res[0] = dipAngles[0] >= -70 && dipAngles[0] <= -17.5f;
                    for (int f=1; f<4; f++)
                    {
                        res[f] = dipAngles[f] >= maxDIPFlexion && dipAngles[f] <= maxDIPExtension;
                    }
                    res[4] = dipAngles[4] >= (maxDIPFlexion + 10) && dipAngles[4] <= (maxDIPExtension - 15); //the pinky
                }
                //and/or perform a dynamic analysis
                //if (this.checkIntention >= CheckIntention.Dynamic)
                //{

                //}
                
            }
            else
            {
                res = new bool[5] { false, false, false, false, false };
            }
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

    /// <summary>Check if an interactable is within an array of other interactables.</summary>
    /// <remarks>Used multiple times</remarks>
    /// <param name="obj"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    protected bool IsInside(SenseGlove_Interactable obj, List<SenseGlove_Interactable> list)
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


/// <summary> The way in which the grabscript check for grab / release intention </summary>
public enum CheckIntention
{
    /// <summary> Intention Checking is off. </summary>
    Off = 0,
    /// <summary> No interaction takes place if the user stretches their fingers, or when they make a fist. </summary>
    Static,
    //Dynamic
}