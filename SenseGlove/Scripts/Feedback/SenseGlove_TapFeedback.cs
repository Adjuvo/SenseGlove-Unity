using UnityEngine;

/// <summary> A WIP Script that allows one to feel 2nd order feedback: Vibration when a tool that is being held hits something else. </summary>
[RequireComponent(typeof(Rigidbody))]
public class SenseGlove_TapFeedback : MonoBehaviour
{
    /// <summary> The object which to test for 2nd Order feedback. </summary>
    [Tooltip("The object which to test for 2nd Order feedback.")]
    public SenseGlove_Interactable linkedObject;

    /// <summary> The Magnitude (0..100%) of vibration feedback on a hit. </summary>
    [Tooltip("The Magnitude (0..100%) of vibration feedback on a hit.")]
    [Range(0, 100)]
    public int buzzMagnitude = 80;

    /// <summary> The time in ms to pulse the Buzz Motors for on a hit. </summary>
    [Tooltip("The time in ms to pulse the Buzz Motors for on a hit.")]
    [Range(1, 1500)]
    public int buzzTime = 50;

    /// <summary> Send a Buzz Motor Command to the hand holding the linkedObject, if it is being held. </summary>
    /// <param name="mangitude"></param>
    /// <param name="duration"></param>
    protected virtual void SendBuzzCmd(int mangitude, int duration)
    {
        if (this.linkedObject != null && linkedObject.GrabScript != null && linkedObject.GrabScript.senseGlove != null)
        {
            this.linkedObject.GrabScript.senseGlove.SendBuzzCmd(new bool[5] { true, true, true, true, true }, mangitude, duration);
        }
    }

    /// <summary> Check if a collider is part of any SenseGlove_Handmodel. </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    protected virtual bool PartOfHand(Collider other)
    {
        SenseGlove_Touch touchScript = other.GetComponent<SenseGlove_Touch>();
        SenseGlove_Feedback feedbackScript = other.GetComponent<SenseGlove_Feedback>();
        return (touchScript != null || feedbackScript != null);
    }

    //Fires during initalization
    private void Start()
    {
        if (this.linkedObject == null)
            this.linkedObject = this.GetComponent<SenseGlove_Interactable>();
    }

    //Fires when my rigidbody collides with another collider that is (also) marked as trigger.
    private void OnTriggerEnter(Collider other)
    {
        if (!PartOfHand(other))
            this.SendBuzzCmd(this.buzzMagnitude, this.buzzTime);
    }

}
