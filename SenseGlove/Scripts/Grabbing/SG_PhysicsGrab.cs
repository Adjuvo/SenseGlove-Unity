using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> A simplified version of the original SenseGlove_PhysGrab script; If an object is touched by finger-thumb or by palm-finger </summary>
public class SG_PhysicsGrab : SG_GrabScript
{
    /// <summary> The Hand Model info to which to link this script's colliders. If left unassigned, one needs to assign tracking to the colliders manually. </summary>
    [Header("PhysicsGrab Components")]
    public SG_HandModelInfo handModel;

    /// <summary> The Hand Palm collider, used when grabbing objects between the palm and finger (tool/handle grips) </summary>
    public SG_HoverCollider palmTouch;
    /// <summary> Thumb collider, used to determine finger/thumb collision </summary>
    public SG_HoverCollider thumbTouch;
    /// <summary> Index collider, used to determine finger/thumb and finger/palm collision </summary>
    public SG_HoverCollider indexTouch;
    /// <summary> Index collider, used to determine finger/thumb and finger/palm collision </summary>
    public SG_HoverCollider middleTouch;

    /// <summary> Keeps track of the 'grabbing' pose of fingers </summary>
    protected bool[] wantsGrab = new bool[3];
    /// <summary> Above these flexions, the hand is considered 'open' </summary>
    protected static float[] openHandThresholds = new float[5] { -20, -20, -20, -20, -90 };
    /// <summary> below these flexions, the hand is considered 'open' </summary>
    protected static float[] closedHandThresholds = new float[5] { -360, -360, -360, -360, -360 }; //set to -360 so it won;t trigger for now

    /// <summary> The touchscript collection that is easier to iterate through. </summary>
    protected SG_HoverCollider[] touchScripts = new SG_HoverCollider[0];




    /// <summary> Show / Hide the hover colliders of this script. </summary>
    public override bool DebugEnabled
    {
        set
        {
            palmTouch.DebugEnabled = value;
            for (int f = 0; f < touchScripts.Length; f++)
            {
                touchScripts[f].DebugEnabled = value;
            }
        }
    }

    /// <summary> Retruns true if this GrabScript is ready to grab objects </summary>
    /// <returns></returns>
    public override bool CanInteract()
    {
        return this.isActiveAndEnabled;
    }

    /// <summary> Returns true if one of the fingers is touching an object </summary>
    /// <returns></returns>
    public override bool IsTouching()
    {
        return palmTouch.IsTouching() || thumbTouch.IsTouching() || middleTouch.IsTouching();
    }


    /// <summary> Called by SG_GrabScript. Assign required variables </summary>
    /// <returns></returns>
    public override bool Setup()
    {
        touchScripts = new SG_HoverCollider[3];
        touchScripts[0] = thumbTouch;
        touchScripts[1] = indexTouch;
        touchScripts[2] = middleTouch;

        setupFinished = true;
        return true;
    }


    /// <summary> Returns true if an SG_Interactable is inside a list of other SG_Interactables </summary>
    /// <param name="heldObject"></param>
    /// <param name="objectsToGrab"></param>
    /// <returns></returns>
    public static bool IsInside(SG_Interactable heldObject, List<SG_Interactable> objectsToGrab)
    {
        for (int i = 0; i < objectsToGrab.Count; i++)
        {
            if (GameObject.ReferenceEquals(objectsToGrab[i].gameObject, heldObject.gameObject))
            {
                return true;
            }
        }
        return false;
    }


    /// <summary> Setup and check for connected scripts </summary>
    public override void CheckForScripts()
    {
        base.CheckForScripts();
        SG_Util.CheckForHandInfo(this.transform, ref this.handModel);
    }


    /// <summary> Grab new objects and release objects taht are no longer touched. </summary>
    public override void UpdateGrabScript()
    {
        if (this.HardwareReady)
        {
            CheckGestures();
            List<SG_Interactable> objectsToGrab = GetGrabables();

            //End interaction with objects that are in the heldObjects but not in the grabables.
            for (int i = 0; i < this.heldObjects.Count;)
            {
                if (heldObjects[i] != null)
                {
                    if (!IsInside(heldObjects[i], objectsToGrab)
                        && ((this.CanRelease(heldObjects[i]) && heldObjects[i].EndInteractAllowed())
                            || heldObjects[i].MustBeReleased()))
                    {
                        i = ReleaseObjectAt(i); //if actually removed, is not updated
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i = ReleaseObjectAt(i);  //if actually removed, is not updated
                }
            }

            //Start interacting with objects that are in grabables and not yet in heldObjects
            for (int i = 0; i < objectsToGrab.Count; i++)
            {
                if (objectsToGrab[i].CanInteract())
                {
                    TryGrabObject(objectsToGrab[i]);
                }
            }
        }
    }


    /// <summary> Returns true if this grabscript can release an object </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected override bool CanRelease(SG_Interactable obj)
    {
        if (obj.releaseMethod == ReleaseMethod.FunctionCall) { return false; }
        else if (obj.releaseMethod == ReleaseMethod.MustOpenHand)
        {
            //Can only release if our fingers do not want to grab
            return !(wantsGrab[1] || wantsGrab[2]);
        }
        return base.CanRelease(obj);
    }


    /// <summary> Returns all grabables that both fingers are touching </summary>
    /// <param name="finger1"></param>
    /// <param name="finger2"></param>
    /// <returns></returns>
    public SG_Interactable[] GetMatching(int finger1, int finger2)
    {
        return GetMatching(finger1, touchScripts[finger2]);
    }


    /// <summary> Returns all grabables that both fingers are touchign </summary>
    /// <param name="finger1"></param>
    /// <param name="finger2"></param>
    /// <returns></returns>
    public SG_Interactable[] GetMatching(int finger1, SG_HoverCollider touch)
    {
        if (touchScripts[finger1] != null && touch != null)
        {
            return touchScripts[finger1].MatchingObjects(touch);
        }
        return new SG_Interactable[] { };
    }

    /// <summary> Updates grab gestures specific to this script. </summary>
    public void CheckGestures()
    {
        SG_SenseGloveHardware hardware;
        if (this.GetHardware(out hardware))
        {
            float[] currAngles = hardware.GloveData.GetFlexions();
            for (int f = 0; f < wantsGrab.Length && f < currAngles.Length; f++)
            {
                wantsGrab[f] = currAngles[f] < openHandThresholds[f] && currAngles[f] > closedHandThresholds[f];
            }
        }
    }

    /// <summary> Returns a list of all Grabables that this script should be touching at this moment </summary>
    /// <returns></returns>
    public List<SG_Interactable> GetGrabables()
    {
        List<SG_Interactable> grabables = new List<SG_Interactable>();
        for (int f = 1; f < this.touchScripts.Length; f++)
        {
            if (this.wantsGrab[f]) //if the user wants to grab something with this finger
            {
                //checks thumb -finger
                SG_Interactable[] thumbMatches = GetMatching(0, f);
                for (int i = 0; i < thumbMatches.Length; i++)
                {
                    if (thumbMatches[i].fingerThumb && !grabables.Contains(thumbMatches[i]))
                    {
                        grabables.Add(thumbMatches[i]);
                    }
                }
                //check thumb-palm;
                SG_Interactable[] palmMatches = GetMatching(f, palmTouch);
                for (int i = 0; i < palmMatches.Length; i++)
                {
                    if (palmMatches[i].fingerPalm && !grabables.Contains(palmMatches[i]))
                    {
                        grabables.Add(palmMatches[i]);
                    }
                }
            }
        }
        return grabables;
    }




    protected override void Awake()
    {
        base.Awake();
        SG_Util.CheckForHandInfo(this.transform, ref this.handModel);
        if (this.handModel != null)
        {
            if (palmTouch != null && !palmTouch.HasTarget) { this.palmTouch.SetTrackingTarget(handModel.wristTransform, true); }

            Transform target;
            if (thumbTouch != null && !thumbTouch.HasTarget && handModel.GetFingerTip(SG_HandSection.Thumb, out target)) { this.thumbTouch.SetTrackingTarget(target, true); }
            if (indexTouch != null && !indexTouch.HasTarget && handModel.GetFingerTip(SG_HandSection.Index, out target)) { this.indexTouch.SetTrackingTarget(target, true); }
            if (middleTouch != null && !middleTouch.HasTarget && handModel.GetFingerTip(SG_HandSection.Middle, out target)) { this.middleTouch.SetTrackingTarget(target, true); }
        }
    }


}
