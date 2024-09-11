/************************************************************************************
Filename    :   SG_XR_Link.cs
Content     :   A Script to link your XR Rig in a specific scene to the SenseGlove script(s).
Author      :   Max Lammers

Changes to this file may be lost when updating the SenseGlove Plugin
************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> Represents links to HMD and Hand Tracking components inside your Scene. Attach it somewhere easy to access to link your scripts. If this script does not exist, you might get some warnings when deviating from the 'normal' </summary>
public class SG_XR_SceneTrackingLinks : MonoBehaviour
{
    /// <summary> The root of your XR Rig that moves / teleports around. This is needed becasue UnityXR only gives us controller locations relative to the XR rig, not in your world space </summary>
    public Transform xrRig;

    /// <summary> The Camera / Head location, which is used to check where the user's head is at, or whether they are glancing at particular objects. </summary>
    public Transform head;

    /// <summary> Optional: The GameObject controlled by hardware that controls the left hand. </summary>
    public Transform leftHandTrackingDevice;

    /// <summary> Optional: The GameObject controlled by hardware that controls the right hand. summary>
    public Transform rightHandTrackingDevice;

    /// <summary> Link to this scene's XR Rig </summary>
    private static SG_XR_SceneTrackingLinks _currentSceneLink = null;

    private static bool givesError = true;

    private static void CheckWarning()
    {
        if (_currentSceneLink == null && Time.frameCount > 0) //not the setup, at least
        {
            if (givesError)
            {
                givesError = false;
                if (UnityEngine.XR.XRSettings.enabled)
                {
                    Debug.LogError("The current Scene does not have an SG_XR_SceneTrackingLinks script. Without it, SenseGlove cannot access various important variables. This will result in your hand tracking being off. Please add one to the scene.");
                }
            }
        }
    }


    /// <summary> Retrieve the XR Rig within the current scene </summary>
    public static Transform SceneXRRig
    {
        get 
        {
            CheckWarning();
            return _currentSceneLink != null ? _currentSceneLink.xrRig : null;
        }
    }

    public static SG_XR_SceneTrackingLinks CurrentSceneLinks
    {
        get { return _currentSceneLink; }
    }

    public static Transform GetTrackingObj(bool rightHand)
    {
        if (_currentSceneLink != null)
        {
            return rightHand ? _currentSceneLink.rightHandTrackingDevice : _currentSceneLink.leftHandTrackingDevice;
        }
        return null;
    }


    public void CollectComponents()
    {
        if (this.head == null)
        {
            if (Camera.main != null)
            {
                this.head = Camera.main.transform;
            }
        }
        if (xrRig == null)
        {
            GameObject rigObject = GameObject.Find("XRRig"); //this is the default name, usually.
            if (rigObject != null)
            {
                xrRig = rigObject.transform;
            }
        }
    }


    private void Awake()
    {
        CollectComponents();
        if (_currentSceneLink == null)
        {
            _currentSceneLink = this;
            Debug.Log("SG Tracking linked to " + this.name, this);
        }
        else
        {
            Debug.LogWarning("There seems to already be a SG_XR_SceneTrackingLinks script in this scene. This may mean that your scripts aren't properly linked.", this);
        }
    }

    private void OnDestroy()
    {
        if (_currentSceneLink != null && _currentSceneLink == this)
        {
            _currentSceneLink = null;
            givesError = true; //since we're removing our old link, we can give error(s) again
        }
    }


#if UNITY_EDITOR
    /// <summary> Fires when the script is first added to an object, and when Reset() is called. </summary>
    private void Reset()
    {
        CollectComponents();
    }
#endif
}
