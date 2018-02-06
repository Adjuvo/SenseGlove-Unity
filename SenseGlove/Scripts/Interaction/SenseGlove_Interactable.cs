using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an object that a SenseGlove Grabscript can interact with.
/// </summary>
public abstract class SenseGlove_Interactable : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Common attributes

    /// <summary> Indicates if this object can be interacted with at this moment. </summary>
    [Tooltip("Indicates if this object can be interacted with at this moment.")]
    public bool isInteractable = true;

    /// <summary> This object can be picked up between a thumb and finger collider. </summary>
    public bool fingerThumb = true;

    /// <summary> This object can be picked up between the palm collider and a finger (including the thumb). </summary>
    public bool fingerPalm = false;

    /// <summary> A reference to the GrabScript that is currently interacting with this SenseGlove. </summary>
    protected SenseGlove_GrabScript _grabScript;

    /// <summary> The original (absolute) position of this GameObject, stored on Awake() </summary>
    protected Vector3 originalPos;

    /// <summary>  The original (absolute) rotation of this GameObject, stored on Awake() </summary>
    protected Quaternion originalRot;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary> Begin the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    public abstract void BeginInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false);

    /// <summary> Called by the grabscript after it has updated. Ensures that the FollowObject always updates last. </summary>
    public abstract void UpdateInteraction();

    /// <summary> (Manually) End the interaction with this GrabScript </summary>
    /// <param name="fromExternal"></param>
    /// <param name="grabScript"></param>
    public abstract void EndInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary>
    /// Reset this object to its original state.
    /// </summary>
    public virtual void ResetObject()
    {
        this.transform.position = this.originalPos;
        this.transform.rotation = this.originalRot;
    }

    /// <summary> Save the current "state" of the interactable, to which it will return when ResetObject is called. </summary>
    public virtual void SaveTransform()
    {
        this.originalPos = this.transform.position;
        this.originalRot = this.transform.rotation;
    }

    /// <summary> Access the grabscript that is currently interacting with this object. </summary>
    /// <returns></returns>
    public SenseGlove_GrabScript GrabScript()
    {
        return this._grabScript;
    }

    /// <summary>
    /// Check if this Interactable is (already) interacting with a specified grabscript.
    /// </summary>
    /// <param name="grabScript"></param>
    /// <returns></returns>
    public bool InteractingWith(SenseGlove_GrabScript grabScript)
    {
        if (grabScript != null && this._grabScript != null)
        {
            return GameObject.ReferenceEquals(grabScript, this._grabScript);
        }
        return false;
    }

    public virtual bool IsInteracting()
    {
        return this._grabScript != null;
    }

}


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

