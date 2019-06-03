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

    /// <summary> Determines special conditions that must be fulfilled to release this object. </summary>
    [Tooltip("Determines special conditions that must be fulfilled to release this object. ")]
    public ReleaseMethod releaseMethod = ReleaseMethod.Default;

    /// <summary> Force then EndInteraction if the handModel ever passes more than this distance (in m) from the original grab location. </summary>
    /// <remarks> Mostly relevant for drawers and levers, or other controls that move along a specific path. </remarks>
    protected float releaseDistance = 0.10f;

    /// <summary> A reference to the GrabScript that is currently interacting with this SenseGlove. </summary>
    protected SenseGlove_GrabScript _grabScript;

    /// <summary> The original (absolute) position of this GameObject, stored on Awake() </summary>
    protected Vector3 originalPos;

    /// <summary>  The original (absolute) rotation of this GameObject, stored on Awake() </summary>
    protected Quaternion originalRot;

    /// <summary> The original distance between grabrefrence and my pickupRefrence. </summary>
    protected float originalDist;


    /// <summary> The list of touchScripts that are currently touching this object. </summary>
    protected List<SenseGlove_GrabScript> touchedScripts = new List<SenseGlove_GrabScript>();

    /// <summary> The number of colliders of a given grabscript that are touching this Interactable. </summary>
    protected List<int> touchedColliders = new List<int>();

    //---------------------------------------------------------------------------------------------------------------------------------
    // Public Interaction Methods

    /// <summary> Sets the object to be interactable (or not). </summary>
    /// <remarks> May be overridden by sub-classes. </remarks>
    /// <param name="canInteract"></param>
    public virtual void SetInteractable(bool canInteract)
    {
        this.isInteractable = canInteract;
        if (!this.isInteractable)
            this.EndInteraction();
    }

    /// <summary> Check if this object can be interacted with at this moment. </summary>
    /// <remarks> May be overridden by sub-classes. </remarks>
    /// <returns></returns>
    public virtual bool CanInteract()
    {
        return this.isInteractable;
    }

    /// <summary> Check if this object is still within acceptable distance of the grabscript. </summary>
    public virtual bool WithinBounds()
    {
        if (this._grabScript != null)
        {
            float currDist = (this._grabScript.grabReference.transform.position - this.transform.position).magnitude;
            return Mathf.Abs(currDist - this.originalDist) >= this.releaseDistance;
        }
        return false;
    }


    /// <summary> Check if this interactable allows a grabScript to end an interaction. </summary>
    /// <returns></returns>
    public virtual bool EndInteractAllowed()
    {
        if (this.releaseMethod != ReleaseMethod.FunctionCall)
            return (!this.CanInteract() || !this.WithinBounds() || this.releaseMethod == ReleaseMethod.MustOpenHand);
        else if (!this.CanInteract())
            return true;
        return false;
    //    return this.releaseMethod != ReleaseMethod.FunctionCall 
    //        && (!this.CanInteract() || (this.releaseMethod == ReleaseMethod.Default) || !this.WithinBounds());
    }



    /// <summary> Begin the interaction between this object and a GrabScript. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    public void BeginInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (grabScript != null)
        {
            if (this.isInteractable || fromExternal) //interactions only possible through these parameters.
            {
                bool begun = this.InteractionBegin(grabScript, fromExternal);
                if (begun)
                {
                    this.originalDist = (grabScript.grabReference.transform.position - this.transform.position).magnitude;
                    this.OnInteractBegin(grabScript, fromExternal);
                }
            }
        }
        else
            SenseGlove_Debugger.LogError("ERROR: You are attempting to start an interaction with " + this.name + " with grabscript set to NULL");
    }

    /// <summary> (Manually) ends all interaction with this object's GrabScript(s) </summary>
    public virtual void EndInteraction()
    {
        this.EndInteraction(this._grabScript, true);
    }

    /// <summary> (Manually) End the interaction with this GrabScript </summary>
    /// <param name="fromExternal"></param>
    /// <param name="grabScript"></param>
    public void EndInteraction(SenseGlove_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.IsInteracting())
        {
            bool ended = this.InteractionEnd(grabScript, fromExternal);
            if (ended)
            {
                this.OnInteractEnd(grabScript, fromExternal);
                this.originalDist = 0;
            }
        }
    }

    /// <summary> Called by the grabscript after it has updated. Ensures that the FollowObject always updates last. </summary>
    public virtual void UpdateInteraction() { }

    //---------------------------------------------------------------------------------------------------------------------------------
    // Abstract Interaction Methods

    /// <summary> Called when the Interaction begins on this Interactable. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    /// <returns> True if a succesfull connection has been established. </returns>
    protected abstract bool InteractionBegin(SenseGlove_GrabScript grabScript, bool fromExternal);

    
    /// <summary> Called when the Interaction ends on this Interactable. </summary>
    /// <param name="grabScript"></param>
    /// <param name="fromExternal"></param>
    /// <returns>True if the interaction has been ended.</returns>
    protected abstract bool InteractionEnd(SenseGlove_GrabScript grabScript, bool fromExternal);

    //---------------------------------------------------------------------------------------------------------------------------------
    // Touch Methods

    /// <summary> Called by SenseGlove_Touch when it touches an interactable. Informs this Interactable that it is being touched. </summary>
    /// <param name="touchScript"></param>
    public void TouchedBy(SenseGlove_Touch touchScript)
    {
        if (touchScript != null && touchScript.GrabScript != null)
        {
            int GI = GetTouchIndex(touchScript.GrabScript);
            if (GI > -1)
                this.touchedColliders[GI] = this.touchedColliders[GI] + 1; //++ does not work reliably...
            else
            {
                this.touchedScripts.Add(touchScript.GrabScript);
                this.touchedColliders.Add(1);

                if (this.touchedColliders.Count == 1) // Only for the first new script.
                    this.OnTouched();
            }
        }
    }


    /// <summary> Called by SenseGlove_Touch when it touches an interactable. Informs this Interactable that it is no longer being touched </summary>
    /// <param name="touchScript"></param>
    public void UnTouchedBy(SenseGlove_Touch touchScript)
    {
        if (touchScript != null && touchScript.GrabScript != null)
        {
            int GI = GetTouchIndex(touchScript.GrabScript);
            if (GI > -1)
            {
                this.touchedColliders[GI] = this.touchedColliders[GI] - 1; //++ does not work reliably...
                if (this.touchedColliders[GI] < 1) //less than one remaining
                {
                    this.touchedScripts.RemoveAt(GI);
                    this.touchedColliders.RemoveAt(GI);
                    if (this.touchedScripts.Count < 1) //reduced the amount of touchedScripts to 0
                        this.OnUnTouched();
                }
            }
        }
    }


    /// <summary> Get the index  </summary>
    /// <param name="grabScript"></param>
    /// <returns></returns>
    protected int GetTouchIndex(SenseGlove_GrabScript grabScript)
    {
        for (int i = 0; i < this.touchedScripts.Count; i++)
        {
            if (GameObject.ReferenceEquals(this.touchedScripts[i], grabScript))
                return i;
        }
        return -1;
    }

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
    public virtual SenseGlove_GrabScript GrabScript
    {
        get { return this._grabScript; }
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


    //-------------------------------------------------------------------------------------------------------
    // Events

     
    //Begin Interaction - fires when the object starts an interaction.

    public delegate void InteractBeginEventHandler(object source, SG_InteractArgs args);

    /// <summary> Fires after this interactable begins an interaction with a specific Grabscript. </summary>
    public event InteractBeginEventHandler InteractionBegun;

    protected virtual void OnInteractBegin(SenseGlove_GrabScript grabScript, bool fromExternal)
    {
        if (InteractionBegun != null)
        {
            InteractionBegun(this, new SG_InteractArgs(grabScript, fromExternal));
        }
    }


    // End Interaction

    public delegate void InteractEndEventHandler(object source, SG_InteractArgs args);

    /// <summary> Fires after this interactable ends an interaction with a specific GrabScript. </summary>
    public event InteractEndEventHandler InteractionEnded;

    protected virtual void OnInteractEnd(SenseGlove_GrabScript grabScript, bool fromExternal)
    {
        if (InteractionEnded != null)
        {
            InteractionEnded(this, new SG_InteractArgs(grabScript, fromExternal));
        }
    }

    
    public delegate void ResetEventHandler(object source, System.EventArgs args);
    
    /// <summary> Fires when this Object is reset to its original position. </summary>
    public event ResetEventHandler ObjectReset;

    protected void OnObjectReset()
    {
        if (ObjectReset != null)
        {
            ObjectReset(this, null);
        }
    }

    // Touched

    public delegate void TouchedEventHandler(object source, System.EventArgs args);

    /// <summary> Fires when this Interactable is first touched by a Sense Glove_Touch collider. </summary>
    public event TouchedEventHandler Touched;

    protected virtual void OnTouched()
    {
        if (Touched != null)
        {
            Touched(this, null);
        }
    }

    /// <summary> Fires when all colliders have stopped touching this Interactable. </summary>
    public event TouchedEventHandler UnTouched;

    protected virtual void OnUnTouched()
    {
        if (UnTouched != null)
        {
            UnTouched(this, null);
        }
    }


}


/// <summary> Parameter that determines how this object ends its interaction. </summary>
public enum ReleaseMethod
{
    /// <summary> The Interactable behaves as determined by the GrabScript that interacts with it. </summary>
    Default = 0,

    /// <summary> The Interactable may only be released if the Hand is sufficiently "open". </summary>
    /// <remarks> Used to improve interaction of objects that move along specified paths. </remarks>
    MustOpenHand,

    /// <summary> The interactable is only released when the EndInteraction or ResetObject functions are called. </summary>
    FunctionCall
}

/// <summary> Contains event arguments </summary>
public class SG_InteractArgs : System.EventArgs
{
    public SenseGlove_GrabScript GrabScript { get; private set; }

    public bool Forced { get; private set; }

    public SG_InteractArgs(SenseGlove_GrabScript script, bool fromExternal)
    {
        GrabScript = script;
        Forced = fromExternal;
    }

}
