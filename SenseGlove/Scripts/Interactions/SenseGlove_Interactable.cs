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

    /// <summary> Indicates that this Interactable can activate force feedback. </summary>
    [Tooltip("Indicated if this objects activates force feedback on the Sense Glove.")]
    public bool forceFeedback = false;

    /// <summary> A reference to the GrabScript that is currently interacting with this SenseGlove. </summary>
    protected SenseGlove_GrabScript _grabScript;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary> Begin the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    public abstract void BeginInteraction(SenseGlove_GrabScript grabScript);

    /// <summary> Called by the grabscript after it has updated. Ensures that the FollowObject always updates last. </summary>
    public abstract void UpdateInteraction();

    /// <summary> (Manually) End the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    public abstract void EndInteraction(SenseGlove_GrabScript grabScript);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary>
    /// Reset this object to its original state.
    /// </summary>
    public abstract void ResetObject();

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

}
