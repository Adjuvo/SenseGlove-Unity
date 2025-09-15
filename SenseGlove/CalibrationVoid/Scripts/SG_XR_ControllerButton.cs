using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;


namespace SG.XR
{
    public enum XRControllerBtn
    {
        LeftTrigger = 0,
        LeftGrip,
        LeftAnalogStick,
        LeftPrimaryBtn,
        LeftSecondaryBtn,

        RightTrigger,
        RightGrip,
        RightAnalogStick,
        RightPrimaryBtn,
        RightSecondaryBtn,
    }


    /// <summary> Allows access to a singe controller button </summary>
    public class SG_XR_ControllerButton : MonoBehaviour
    {

        [SerializeField] protected XRControllerBtn listenTo = XRControllerBtn.RightTrigger;

        public UnityEvent ButtonPressed = new UnityEvent();
        public UnityEvent ButtonReleased = new UnityEvent();

#if UNITY_2019_4_OR_NEWER

        protected bool handedNess = true;
        protected InputFeatureUsage<bool> button;
        protected bool firstUpdate = true; //the very first update

        public bool IsPressed { get; protected set; }

        public XRControllerBtn LinkedButton
        {
            get { return this.listenTo; }
            set
            {
                this.listenTo = value;
                this.UpdateParams();
            }
        }

        protected void UpdateParams()
        {
            handedNess = ToHand(this.listenTo);
            button = ToInput(this.listenTo);
        }


        public static bool ToHand(XRControllerBtn btn)
        {
            return btn >= XRControllerBtn.RightTrigger;
        }

        public static InputFeatureUsage<bool> ToInput(XRControllerBtn btn)
        {
            switch (btn)
            {
                case XRControllerBtn.LeftTrigger:
                    return CommonUsages.triggerButton;
                case XRControllerBtn.RightTrigger:
                    return CommonUsages.triggerButton;

                case XRControllerBtn.LeftGrip:
                    return CommonUsages.gripButton;
                case XRControllerBtn.RightGrip:
                    return CommonUsages.gripButton;

                case XRControllerBtn.LeftPrimaryBtn:
                    return CommonUsages.primaryButton;
                case XRControllerBtn.RightPrimaryBtn:
                    return CommonUsages.primaryButton;

                case XRControllerBtn.LeftSecondaryBtn:
                    return CommonUsages.secondaryButton;
                case XRControllerBtn.RightSecondaryBtn:
                    return CommonUsages.secondaryButton;

                case XRControllerBtn.LeftAnalogStick:
                    return CommonUsages.primary2DAxisClick;
                case XRControllerBtn.RightAnalogStick:
                    return CommonUsages.primary2DAxisClick;
            }
            return CommonUsages.triggerButton;
        }


        protected void CheckPressed()
        {
            SG_XR_Devices.SG_XR_HandReference device;
            if (SG_XR_Devices.GetHandDevice(this.handedNess, out device))
            {
                // Auto-Determine what to listen to depending on the System. TODO: Move this into SGSettings?
                if (firstUpdate && device.DeviceLinked && device.Hardware != SGCore.PosTrackingHardware.Custom) //we can acutall grab the hardware and it is linked!
                {
                    firstUpdate = false;
                    if (device.Hardware == SGCore.PosTrackingHardware.ViveFocus3WristTracker) //Special case for Vive Trackers, where we use the primary button as opposed to the trigger.
                    {
                        LinkedButton = handedNess ? XRControllerBtn.RightPrimaryBtn : XRControllerBtn.LeftPrimaryBtn;
                    }
                    //else
                    //{
                    //    LinkedButton = handedNess ? XRControllerBtn.RightTrigger : XRControllerBtn.LeftTrigger;
                    //}
                }
                bool pressed;
                if (device.XRDevice.TryGetFeatureValue(this.button, out pressed))
                {
                    if (firstUpdate)
                    {
                        firstUpdate = false;
                        this.IsPressed = pressed;
                    }
                    else
                    {
                        //Debug.Log(this.listenTo.ToString() + " - " + button.name + " Pressed is " + pressed.ToString());
                        //Do Something
                        if (this.IsPressed && !pressed)
                        {
                            //Debug.Log(this.listenTo.ToString() + " - " + button.name + " is Released");
                            if (SGCore.HandLayer.DeviceConnected(this.handedNess)) //Only if we have a glove connected....
                            {
                                ButtonReleased.Invoke();
                            }
                        }
                        else if (!this.IsPressed && pressed)
                        {
                            //Debug.Log(this.listenTo.ToString() + " - " + button.name + " is Pressed");
                            if (SGCore.HandLayer.DeviceConnected(this.handedNess)) //Only if we have a glove connected....
                            {
                                ButtonPressed.Invoke();
                            }
                        }
                        this.IsPressed = pressed;
                    }
                }
            }
        }


        protected virtual void Start()
        {
            UpdateParams();
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            CheckPressed();
        }
#endif
    }
}