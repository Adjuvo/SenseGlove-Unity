using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> A layer which evaluates a set of static gestures made by the user, based on the output of their HapticGlove. </summary>
    public class SG_GestureLayer : SG_HandComponent
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The list of gestures that this GrabLayer will attmept to detect.  </summary>
        public List<SG_BasicGesture> gestures = new List<SG_BasicGesture>();

        /// <summary> The last flexions used to evaluate the gestures, for debugging, and convienient access </summary>
        public float[] lastFlexions = new float[5];

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member functions

        /// <summary> Ask this hand layer is a specific gesture is being made at any point. It will use the last evaluated flexions. </summary>
        /// <param name="gesture"></param>
        public virtual bool IsGestureMade(SG_BasicGesture gesture)
        {
            return gesture.GestureIsMade(this.lastFlexions);
        }

        public virtual bool AddGesture(SG_BasicGesture gesture)
        {
            if (gesture == null || gestures.Contains(gesture))
                return false;
            gestures.Add(gesture);
            return true;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Update()
        {
            if (gestures.Count > 0 && TrackedHand != null && TrackedHand.IsConnected())
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