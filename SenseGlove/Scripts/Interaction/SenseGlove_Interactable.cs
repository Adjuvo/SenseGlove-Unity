using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Represents an object that a SenseGlove Grabscript can interact with. Extended by most of the Interaction scripts. </summary>
public abstract class SenseGlove_Interactable : MonoBehaviour
{
    //----------------------------------------------------------------------------------------------------------------------------
    // Properties

    /// <summary> Indicates if this object can be interacted with at this moment. </summary>
    [Tooltip("Indicates if this object can be interacted with at this moment.")]
    protected bool isInteractable = true;

    /// <summary> This object can be picked up between a thumb and finger collider. </summary>
    [Tooltip("This object can be picked up between a thumb and finger collider.")]
    public bool fingerThumb = true;

    /// <summary> This object can be picked up between the palm collider and a finger (including the thumb). </summary>
    [Tooltip("This object can be picked up between the palm collider and a finger (including the thumb).")]
    public bool fingerPalm = false;

    /// <summary> Determines special conditions under which this Interactable can end an interaction. </summary>
    [Tooltip("Determines special conditions under which this Interactable is released. ")]
    public ReleaseMethod releaseMethod = ReleaseMethod.Default;

    /// <summary> A reference to the GrabScript that is currently interacting with this SenseGlove. </summary>
    protected SenseGlove_GrabScript _grabScript;

    /// <summary> The original (absolute) position of this GameObject, stored on Awake() </summary>
    protected Vector3 originalPos;

    /// <summary>  The original (absolute) rotation of this GameObject, stored on Awake() </summary>
    protected Quaternion originalRot;

    //---------------------------------------------------------------------------------------------------------------------------------
    // Public Interaction Methods

    /// <summary> Sets the object to be interactable (or not). </summary>
    /// <remarks> May be overridden by sub-classes. </remarks>
    /// <param name="canInteract"></param>
    public virtual void SetInteractable(bool canInteract)
    {
        this.isInteractable = canInteract;
    }

    /// <summary> Check if this object can be interacted with at this moment. </summary>
    /// <remarks> May be overridden by sub-classes. </remarks>
    /// <returns></returns>
    public virtual bool CanInteract()
    {
        return this.isInteractable;
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    // Abstract Interaction Methods

    /// <summary> Begin the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    public abstract void BeginInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false);

    /// <summary> Called by the grabscript after it has updated. Ensures that the FollowObject always updates last. </summary>
    public abstract void UpdateInteraction();



    /// <summary> (Manually) ends all interaction with this object's GrabScript(s), if any exists.. </summary>
    public virtual void EndInteraction()
    {
        if (this.IsInteracting())
        {
            this.EndInteraction(this._grabScript, true);
        }
    }

    /// <summary> (Manually) End the interaction with this GrabScript </summary>
    /// <param name="fromExternal"></param>
    /// <param name="grabScript"></param>
    public abstract void EndInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false);
    
    //---------------------------------------------------------------------------------------------------------------------------------
    // Utility Methods
    
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
    public virtual SenseGlove_GrabScript GrabScript()
    {
        return this._grabScript;
    }

    /// <summary>
    /// Check if this Interactable is (already) interacting with a specified grabscript.
    /// </summary>
    /// <param name="grabScript"></param>
    /// <returns></returns>
    public virtual bool InteractingWith(SenseGlove_GrabScript grabScript)
    {
        if (grabScript != null && this._grabScript != null)
        {
            return GameObject.ReferenceEquals(grabScript, this._grabScript);
        }
        return false;
    }

    /// <summary> Check if this object is being interacted with. </summary>
    /// <returns></returns>
    public virtual bool IsInteracting()
    {
        return this._grabScript != null;
    }
    
    /// <summary> Whether or not this object wants interaction to end. </summary>
    /// <returns></returns>
    public virtual bool CanEndInteraction()
    {
        return true;
    }


    //-------------------------------------------------------------------------------------------------------
    // Events - W.I.P.



    // Touched - W.I.P.

    //public delegate void TouchedEventHandler(object source, SG_InteractArgs args);

    //public event TouchedEventHandler OnTouched;

    //protected virtual void InteractTouched()
    //{
    //    if (OnTouched != null)
    //    {
    //        OnTouched(this, new SG_InteractArgs());
    //    }
    //}


    //Begin Interaction - fires when the object starts an interaction.

    //public delegate void InteractBeginEventHandler(object source, SG_InteractArgs args);

    ///// <summary>  </summary>
    //public event InteractBeginEventHandler InteractionBegun;

    //protected virtual void OnInteractBegin(/* Parameters for InteractArgs here. */)
    //{
    //    if (InteractionBegun != null)
    //    {
    //        InteractionBegun(this, new SG_InteractArgs());
    //    }
    //}


    // End Interaction

    //public delegate void InteractEndEventHandler(object source, SG_InteractArgs args);

    //public event InteractEndEventHandler InteractionEnded;

    //protected virtual void OnInteractEnd(/* Parameters for InteractArgs here. */)
    //{
    //    if (InteractionEnded != null)
    //    {
    //        InteractionEnded(this, new SG_InteractArgs());
    //    }
    //}



}


/*
/// <summary> Parameter that determines how this object begins its interaction. </summary>
public enum GrabMethod
{
    /// <summary> The Interactable behaves as determined by the GrabScript that interacts with it. </summary>
    Default = 0,

    /// <summary> The Interactable may only be released if the Hand is sufficiently "open". </summary>
    /// <remarks> Used to improve interaction of objects that move along specified paths. </remarks>
    MustOpenHand
}
*/

/// <summary> Parameter that determines how this object ends its interaction. </summary>
public enum ReleaseMethod
{
    /// <summary> The Interactable behaves as determined by the GrabScript that interacts with it. </summary>
    Default = 0,

    /// <summary> The Interactable may only be released if the Hand is sufficiently "open". </summary>
    /// <remarks> Used to improve interaction of objects that move along specified paths. </remarks>
    MustOpenHand
}

/// <summary> Contains event arguments </summary>
public class SG_InteractArgs : System.EventArgs
{

}
