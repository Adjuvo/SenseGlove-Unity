using UnityEngine;

namespace SG
{
    /// <summary> A layer which evaluates a set of static gestures made by the user, based on the output of their HapticGlove. </summary>
    public class SG_GestureLayer : SG_HandComponent
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The list of gestures that this GrabLayer will attmept to detect.  </summary>
        public SG_BasicGesture[] gestures = new SG_BasicGesture[0];

        /// <summary> The last flexions used to evaluate the gestures, for debugging, and convienient access </summary>
        public float[] lastFlexions = new float[5];

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Update()
        {
            if (gestures.Length > 0 && TrackedHand != null && TrackedHand.IsConnected())
            {
                if (TrackedHand.GetNormalizedFlexion(out lastFlexions))
                {
                    foreach (SG_BasicGesture gesture in gestures)
                    {
                        gesture.UpdateGesture(lastFlexions);
                    }
                }
            }
        }



    }

}