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

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Monobehaviour

    // Awake runs before all objects have been setup.
    void Awake() { this.HandleAwake(); }

    // Use this for initialization
    void Start () { this.HandleStart(); }

	// Update is called once per frame
	void Update () { this.HandleUpdate(); }

    // FixedUpdate is called every time the physics are calculated, which can be adjusted in the Project Settings > Time.
    void FixedUpdate() { this.HandleFixedUpdate(); }

    // LateUpdate is also called once per frame, but only after all Update() functions have been processed.
    void LateUpdate() { this.HandleLateUpdate(); }

    // Called when the application shuts down.
    void OnApplicationQuit() { this.HandleQuit(); }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Monobehaviour wrappers (in order of execution). Virtual because not all are required.

    /// <summary> Awake runs before all objects have been setup. </summary>
    protected virtual void HandleAwake() { }

    /// <summary> Use this for initialization. </summary>
    protected virtual void HandleStart() { }

    /// <summary> Update is called once per frame </summary>
    protected virtual void HandleUpdate() { }

    /// <summary> FixedUpdate is called every time the physics are calculated, which can be adjusted in the Project Settings > Time. </summary>
    protected virtual void HandleFixedUpdate() { }

    /// <summary> LateUpdate is also called once per frame, but only after all Update() functions have been processed. </summary>
    protected virtual void HandleLateUpdate() { }

    /// <summary> Called when the application shuts down. </summary>
    protected virtual void HandleQuit() { }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary> Begin the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    public abstract void BeginInteraction(SenseGlove_PhysGrab grabScript);

    /// <summary> Called by the grabscript after it has updated. Ensures that the FollowObject always updates last. </summary>
    public abstract void FollowInteraction();

    /// <summary> (Manually) End the interaction with this GrabScript </summary>
    /// <param name="grabScript"></param>
    public abstract void EndInteraction(SenseGlove_PhysGrab grabScript);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Grabscript Methods

    /// <summary>
    /// Reset this object to its original state.
    /// </summary>
    public abstract void ResetObject();

}
