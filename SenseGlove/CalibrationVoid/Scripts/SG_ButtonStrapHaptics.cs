using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{
    /// <summary> Attaches to an SG_PhysicsButton to play palm squeeze haptics when it is pressed. </summary>
    public class SG_ButtonStrapHaptics : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Used to determine the pressure on the strap </summary>
        public SG_PhysicsButton buttonLogic;


        [Header("Feedback Triggers")]
        [Range(0.0f, 1.0f)] public float fullStrapPressure = 0.5f;
        [Range(0.0f, 1.0f)] public float releaseStrapPressure = 0.2f;

        [Header("Feedback Intensity")]
        [Range(0.0f, 1.0f)] public float enterZoneStrapLevel = 0.2f;
        [Range(0.0f, 1.0f)] public float fullyPressedStrapLevel = 1.0f;


        /// <summary> Whether or not the button has been activated </summary>
        private bool activated = false;
        /// <summary> Desires Active Strap leve;  </summary>
        private float currStrapLevel = 0.0f;
        /// <summary> Cooldown timer </summary>
        float prevTimeActQueue = 0;


        //--------------------------------------------------------------------------------------------------------------
        // Member Functions

        /// <summary> Determines Active Strap pressure based on button pressure and feedback triggers. </summary>
        private void DetermineStrapLevel()
        {
            float pressure = buttonLogic.ButtonPressure;
            if (activated)
            {
                if (pressure < releaseStrapPressure)
                {
                    activated = false;
                    currStrapLevel = enterZoneStrapLevel;
                }
            }
            else
            {
                if (pressure > fullStrapPressure)
                {
                    activated = true;
                    currStrapLevel = fullStrapPressure;
                }
            }
        }


        /// <summary> Queue's a strap level to any glove near this button. </summary>
        /// <param name="strapLevel01"></param>
        public void QueueStrapLevel(float strapLevel01)
        {
            List<SG.SG_TrackedHand> hands = buttonLogic.handDetector.FullyDetectedHands();

            if (Time.realtimeSinceStartup - prevTimeActQueue > 0.15f)
            {
                foreach (SG.SG_TrackedHand hand in hands)
                {
                    hand.QueueWristSqueeze(strapLevel01);
                }
            }
        }

        /// <summary> Fires when a new hand is detected inside the zone </summary>
        /// <param name="hand"></param>
        private void HandDetected(SG.SG_TrackedHand hand)
        {
            hand.QueueWristSqueeze(this.currStrapLevel);
        }

        /// <summary> Fires when a hand is removed from the zone. </summary>
        /// <param name="hand"></param>
        private void HandRemoved(SG.SG_TrackedHand hand)
        {
            hand.QueueWristSqueeze(0.0f);
        }


        //--------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void OnEnable()
        {
            if (buttonLogic != null && buttonLogic.handDetector != null)
            {
                buttonLogic.handDetector.HandDetected.AddListener(HandDetected);
                buttonLogic.handDetector.HandRemoved.AddListener(HandRemoved);
            }
        }

        private void OnDisable()
        {
            if (buttonLogic != null && buttonLogic.handDetector != null)
            {
                buttonLogic.handDetector.HandDetected.RemoveListener(HandDetected);
                buttonLogic.handDetector.HandRemoved.RemoveListener(HandRemoved);
            }
        }


        private void Update()
        {
            DetermineStrapLevel();
            QueueStrapLevel(currStrapLevel);
        }


    }
}