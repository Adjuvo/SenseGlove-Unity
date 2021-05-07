using UnityEngine;

namespace SG
{
    /// <summary> A layer which evaluates a set of static gestures made by the user, based on the output of their HapticGlove. </summary>
    public class SG_GestureLayer : SG_HandComponent
    {
        /// <summary> The list of gestures that this GrabLayer will attmept to detect.  </summary>
        public SG_BasicGesture[] gestures = new SG_BasicGesture[0];

        /// <summary> The last flexions used to evaluate the gestures, for debugging, and convienient access </summary>
        public float[] lastFlexions = new float[5];

        void Update()
        {
            if (HardwareReady) //inherited from handComponent; the device is assigned hardware and it has been connected.
            {
                if (TrackedHand.GetNormalizedFlexions(out lastFlexions))
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