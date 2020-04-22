using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A simplified SenseGlove_GrabScript that grabs all objects within it's 'hover collider' when a grab gestire is made. </summary>
public class SG_GestureGrabScript : SG_GrabScript
{
    /// <summary> A collider in the hand palm that checks for  SenseGlove_Interactable objects near the hand. </summary>
    public SG_HoverCollider hoverCollider;

    /// <summary> Angles during the last update, used to check for grab/release events. </summary>
    protected float[] lastAngles = new float[5];
    /// <summary> Whether each finger can be considered to be 'grasping' or ;grabbing' </summary>
    protected bool[] grabbing = new bool[5];

    /// <summary> Total flexion must fall below these values to consider 'grabbing'. Sorted thumb to pinky </summary>
    protected static float[] baseGrabAngles = new float[5] { -60, -45, -45, -45, -90 };
    /// <summary> Total flexion must fall below these values to consider 'releasing'. Sorted thumb to pinky </summary>
    protected static float[] baseReleaseAngles = new float[5] { -20, -20, -20, -20, -45 }; 

    /// <summary> Whether a grab action was desired during the last frame. </summary>
    protected bool wantedGrab = false;

    /// <summary> Returns true if this GrabScript is all set up to go. </summary>
    /// <returns></returns>
    public override bool CanInteract()
    {
        return this.HardwareReady && hoverCollider != null;
    }


    /// <summary> Returns tru if our HoverCollider is hovering above a valid object </summary>
    /// <returns></returns>
    public override bool IsTouching()
    {
        return hoverCollider.IsTouching();
    }



    /// <summary> Setup this GrabScript's components. </summary>
    /// <returns></returns>
    public override bool Setup()
    {
        setupFinished = true;
        return true;
    }


    /// <summary> Update this GrabScript's behaviour. </summary>
    public override void UpdateGrabScript()
    {
        SG_SenseGloveHardware hardware;
        if (this.GetHardware(out hardware))
        {
            float[] currAngles = hardware.GloveData.GetFlexions();
            for (int f = 0; f < currAngles.Length; f++)
            {
                if (grabbing[f] && currAngles[f] > baseReleaseAngles[f])
                {
                    grabbing[f] = false;
                }
                else if (!grabbing[f] && currAngles[f] < baseGrabAngles[f])
                {
                    grabbing[f] = true;
                }
            }
            lastAngles = currAngles;
            bool wantsGrab = grabbing[0] && grabbing[1];

            //if grabbing; grab everything that is inside the collider (and hasn't been grabbed yet)
            if (wantsGrab && !wantedGrab)
            {
                SG_Interactable[] grabables = hoverCollider.TouchedObjects;
                for (int i = 0; i < grabables.Length; i++)
                {
                    this.TryGrabObject(grabables[i]);
                }
            }
            else if (!wantsGrab && wantedGrab) //if releasing; release everything in my held objects
            {
                for (int i = 0; i < this.heldObjects.Count;)
                {
                    i = ReleaseObjectAt(i);
                }
            }
            wantedGrab = wantsGrab;
        }
    }
}
