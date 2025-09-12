using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Demo
{

    /// <summary> This script fires events when its buttonPressed variable is set to true. It does not handle button inputs, and can therefore be linked to any event. </summary>
    /// <remarks> Alternatively, you can extend off this script and toggle the buttonPressed variable instead. </remarks>
    public class SG_DoubleControlButton : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> When this variable is set to true, a timer will begin running to fire events </summary>
        protected bool buttonPressed = false;

        /// <summary> If the button is released before this time, it's considered a "short press". Otherwise, it's a "Long Press" </summary>
        public float longPressTime = 1.0f;

        /// <summary> Fires when the buttonPressed is true for a short period of time (< longPressTime). Fires as soon as the button is released. </summary>
        public SG.Util.SGEvent ButtonShortPressed = new SG.Util.SGEvent();
        /// <summary> Fires when the buttonPressed is true for a longer period of time (>= longPressTime). Fires as soon as the time is passed.  </summary>
        public SG.Util.SGEvent ButtonLongPressed = new SG.Util.SGEvent();

        /// <summary> Utility variable for previous frame, used to fire events on change. </summary>
        protected bool wasPressed = false;
        /// <summary> The time for which the buttonPressed has been True. </summary>
        protected float pressTime = 0;
        /// <summary> Whether or not we've fired a "Long Press" </summary>
        protected bool firedLong = false;


        //------------------------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Normalized Press progress, can be used to animate some form of progress bar. </summary>
        public float PressProgress
        {
            get { return longPressTime != 0 ? pressTime / longPressTime : 0; }
        }

        /// <summary> Direclty set hte buttonPressed variable. Function to call from external script that call this script </summary>
        /// <param name="pressed"></param>
        public void SetButtonPressed(bool pressed)
        {
            buttonPressed = pressed;
        }

        /// <summary> Toggle the buttonPressed State.  </summary>
        public void ToggleButtonPressed()
        {
            buttonPressed = !buttonPressed;
        }


        /// <summary> Fire a Long Pressed event. Ensures we no longer fire ShortPressed. </summary>
        protected virtual void FireLongPress()
        {
            //Debug.Log("Long!");
            firedLong = true;
            ButtonLongPressed.Invoke();
        }

        /// <summary> Fire the Short Press event. </summary>
        protected virtual void FireShortPress()
        {
            //Debug.Log("Short!");
            ButtonShortPressed.Invoke();
        }



        //------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Update is called once per frame
        protected virtual void Update()
        {

            if (!buttonPressed && wasPressed) //the button is released
            {
                if (!firedLong)
                {
                    FireShortPress();
                }
                firedLong = false; //either way, reset the long press.
                pressTime = 0;
            }
            if (buttonPressed && !wasPressed)
            {
                //the button is pressed for the first time
            }

            if (buttonPressed && !firedLong)
            {
                pressTime += Time.deltaTime;
                if (pressTime >= longPressTime)
                {
                    FireLongPress();
                }
            }
            wasPressed = buttonPressed;
        }

    }
}