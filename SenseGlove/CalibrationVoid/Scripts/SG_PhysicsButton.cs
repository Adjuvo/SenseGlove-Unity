using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SG;
using UnityEngine.Events;

/*
 * Controls for a simple button that can only move in the Y direction. Controlled by physics, so it can be a bit janky at times.
 * 
 * jelle@senseglove.com
 * max@senseglove.com
 * 
 * Update Max 12/12/2023: Added a Hand-detector. We play a haptic effect to any hand that is in this zone when the button is detected. Simplifies things a lot.
 */


/// <summary> A button composed of two parts, one of which moves only in the (local) Y direction. </summary>
/// <remarks>  Based on physics, so you can't only copy this script. There's a bit of setup required, too. Recommend against directly using this one in your project for now. </remarks>
public class SG_PhysicsButton : MonoBehaviour
{
    [Header("Event that triggers when the button is pressed")]
    public UnityEvent ButtonPressed = new UnityEvent();

    [Header("Button top part")]
    public GameObject buttonTop;

    [Header("Distance to activate button")]
    public float buttonActivate = 0.018F;

    [Header("Max distance that top part can move")]
    public float maxButtonMovement = 0.02F;

    [Header("Detects which hand is pressing the button")]
    public SG.SG_HandDetector handDetector;

    [Header("Haptic effect to play when pressed")]
    public SG.SG_CustomWaveform hapticEffect;

    [Header("The speed the button returns to original state")]
    public float buttonReturnSpeed = 8f;

    [Header("The table or other object this button is placed on, so it can ignore its collision")]
    public GameObject table;

    // private vars
    private float timerLength = 1F;
    private bool switched = false;
    private Vector3 localStartPos;
    private bool onOrOff = false;


    private float prevLocalButtonTopPos;
    private float buttonSpeed;

    public float ButtonPressure
    {
        get; private set;
    }

    public float ActivationPressure
    {
        get; private set;
    }

    public float ButtonSpeed
    {
        get { return buttonSpeed; } private set { }
    }

    private void Awake()
    {
        // set the start position of the top part of the button
        localStartPos = buttonTop.transform.localPosition;

        // ignore the collider that the button is placed on
        if (table != null && table.GetComponent<Collider>() != null)
            Physics.IgnoreCollision(table.GetComponent<Collider>(), buttonTop.GetComponent<Collider>());

        ActivationPressure = SG.Util.SG_Util.Map(-buttonActivate, localStartPos.y, localStartPos.y - maxButtonMovement, 0.0f, 1.0f, true);
    }

    void Update()
    {
        // if the max range is reached then fire of the event and a haptic command
        if ((localStartPos.y + buttonTop.transform.localPosition.y) < -buttonActivate)
            ButtonPushed();

        // update the button position to the start position
        UpdateButtonPosition();
    }

    // Update the button position to move slowly back to its starting position
    private void UpdateButtonPosition()
    {
        // if the button is outside the bounderies return set position
        if ((localStartPos.y + buttonTop.transform.localPosition.y) < -maxButtonMovement)
            buttonTop.transform.localPosition = new Vector3(localStartPos.x, localStartPos.y - maxButtonMovement, localStartPos.z);

        if (buttonTop.transform.localPosition.y > localStartPos.y)
            buttonTop.transform.localPosition = localStartPos;

        if (buttonTop.transform.localPosition.x != localStartPos.x || buttonTop.transform.localPosition.z != localStartPos.z)
            buttonTop.transform.localPosition = new Vector3(localStartPos.x, buttonTop.transform.localPosition.y, localStartPos.z);

        // Return button to startPosition
        buttonTop.transform.localPosition = Vector3.Lerp(buttonTop.transform.localPosition, localStartPos, Time.deltaTime * buttonReturnSpeed);

        buttonSpeed = (buttonTop.transform.localPosition.y - prevLocalButtonTopPos) / Time.deltaTime;
        prevLocalButtonTopPos = buttonTop.transform.localPosition.y;

        ButtonPressure = SG.Util.SG_Util.Map(buttonTop.transform.localPosition.y, localStartPos.y, localStartPos.y - maxButtonMovement, 0.0f, 1.0f, true );
    }

    // The button is pushed further than the distance to activate it then complete the activation
    private void ButtonPushed()
    {
        
        onOrOff = !onOrOff;

        if (switched)
        {
            SendHapticCommand();
            ButtonPressed.Invoke();
            switched = false;
        }
            
        if (this.isActiveAndEnabled)
            StartCoroutine(Timer(timerLength));
    }

    // timer to make sure the button doesn't fire multiple time in quick succession
    private IEnumerator Timer(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        switched = true;
    }

    // create a haptic effect on the glove when you pressed the button deep enough to trigger the event
    private void SendHapticCommand()
    {
        if (hapticEffect == null || this.handDetector == null)
            return;

        SG.SG_TrackedHand[] inZone = handDetector.HandsInZone();
        foreach (SG.SG_TrackedHand hand in inZone)
        {
            hand.SendCustomWaveform(hapticEffect, hapticEffect.intendedMotor);
        }
    }

    private void OnDisable()
    {
        buttonTop.transform.localPosition = localStartPos;
        switched = false;
    }


}
