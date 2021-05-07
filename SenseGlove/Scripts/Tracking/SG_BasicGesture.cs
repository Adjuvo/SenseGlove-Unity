using UnityEngine;

namespace SG
{
    /// <summary> Defines a static gesture based on simple flexion and on abduction/adduction. Is attached to a GestureLayer, which activates it's GestureMade / UnMade booleans. </summary>
    public class SG_BasicGesture : MonoBehaviour
    {
        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The minimum required thumb flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Header("Thumb")]
        [Range(0, 1)] public float thumbFlexMin = 0;
        /// <summary> The maximum required thumb flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Range(0, 1)] public float thumbFlexMax = 0.3f;

        /// <summary> The minimum required index finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Header("Index Finger")]
        [Range(0, 1)] public float indexFlexMin = 0.5f;
        /// <summary> The maximum required index finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Range(0, 1)] public float indexFlexMax = 1.00f;

        /// <summary> The minimum required middle finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Header("Middle Finger")]
        [Range(0, 1)] public float middleFlexMin = 0.5f;
        /// <summary> The maximum required middle finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Range(0, 1)] public float middleFlexMax = 1.00f;

        /// <summary> The minimum required ringe finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Header("Ring Finger")]
        [Range(0, 1)] public float ringFlexMin = 0.5f;
        /// <summary> The maximum required ringe finger flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Range(0, 1)] public float ringFlexMax = 1.00f;

        /// <summary> The minimum required pinky flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Header("Pinky")]
        [Range(0, 1)] public float pinkyFlexMin = 0.5f;
        /// <summary> The maximum required pinky flexion of the thumb needed to make this gesture (0=fully exyended, 1=full flexion) </summary>
        [Range(0, 1)] public float pinkyFlexMax = 1.00f;

        /// <summary> How many steps to divide a 0 .. 1 range for a string representation. </summary>
        private static int stringSteps = 10;


        /// <summary> Returns true if this gesture is currently being made. Input.GetKey() equivalent. </summary>
        public bool IsGesturing
        {
            get; private set;
        }


        /// <summary> Returns true during the frame the gesture was first made. Input.GetKeyDown() equivalent.  </summary>
        public bool GestureMade
        {
            get; protected set;
        }

        /// <summary> Returns true during the frame the gesture was first stopped. Input.GetKeyUp() equivalent.  </summary>
        public bool GestureStopped
        {
            get; protected set;
        }


        //----------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Utility function te reiteve the minimum flexion fo a finger </summary>
        /// <param name="finger"></param>
        /// <returns></returns>
        public float GetMinFlexion(SGCore.Finger finger)
        {
            switch (finger)
            {
                case SGCore.Finger.Thumb:
                    return thumbFlexMin;
                case SGCore.Finger.Index:
                    return indexFlexMin;
                case SGCore.Finger.Middle:
                    return middleFlexMin;
                case SGCore.Finger.Ring:
                    return ringFlexMin;
                case SGCore.Finger.Pinky:
                    return pinkyFlexMin;
            }
            return 0;
        }

        /// <summary> Utility function te reiteve the maximum flexion fo a finger </summary>
        /// <param name="finger"></param>
        /// <returns></returns>
        public float GetMaxFlexion(SGCore.Finger finger)
        {
            switch (finger)
            {
                case SGCore.Finger.Thumb:
                    return thumbFlexMax;
                case SGCore.Finger.Index:
                    return indexFlexMax;
                case SGCore.Finger.Middle:
                    return middleFlexMax;
                case SGCore.Finger.Ring:
                    return ringFlexMax;
                case SGCore.Finger.Pinky:
                    return pinkyFlexMax;
            }
            return 1;
        }


        /// <summary> Update this gesture, based on it's current flexions, which are normalized into values between 0 .. 1. </summary>
        /// <param name="flexions01"></param>
        public virtual void UpdateGesture(float[] flexions01)
        {
            if (flexions01.Length > 4)
            {
                bool[] fingerCheck = new bool[5];
                fingerCheck[0] = flexions01[0] >= thumbFlexMin && flexions01[0] <= thumbFlexMax;
                fingerCheck[1] = flexions01[1] >= indexFlexMin && flexions01[1] <= indexFlexMax;
                fingerCheck[2] = flexions01[2] >= middleFlexMin && flexions01[2] <= middleFlexMax;
                fingerCheck[3] = flexions01[3] >= ringFlexMin && flexions01[3] <= ringFlexMax;
                fingerCheck[4] = flexions01[4] >= pinkyFlexMin && flexions01[4] <= pinkyFlexMax;

                //evaluate if the full gesture is being made
                bool currentlyGesturing = true;
                for (int f = 0; f < fingerCheck.Length; f++)
                {
                    if (!fingerCheck[f]) { currentlyGesturing = false; break; }
                }
                GestureMade = currentlyGesturing && !IsGesturing;
                GestureStopped = IsGesturing && !currentlyGesturing;
                IsGesturing = currentlyGesturing;
            }
            else
            {
                Debug.LogWarning(this.name + " gesture received insufficient values (" + flexions01.Length + "/5). It will not be updated.");
            }
        }


        //----------------------------------------------------------------------------------------------------------------------------------------
        // Visual "Range String" indications

        /// <summary> Convert a value between 0 .. 1 to a position in a string with a length of steps. </summary>
        /// <param name="val01"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        protected static int ToSteps(float val01, int steps)
        {
            return Mathf.Clamp((int)Mathf.Round(val01 * steps), 0, steps);
        }

        /// <summary> Convert a value between a min / max, into a string </summary>
        /// <param name="currVal"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="stringSteps"></param>
        /// <returns></returns>
        protected static string ToBlockString(float currVal, float minVal, float maxVal, int stringSteps)
        {
            float min = Mathf.Min(minVal, maxVal);
            float max = Mathf.Max(minVal, maxVal);

            int minPos = ToSteps(min, stringSteps);
            int maxPos = ToSteps(max, stringSteps);

            int flexPos = ToSteps(currVal, stringSteps);

            int rangeSpacing = flexPos > -1 ? 2 : 1;
            //check there is enough space
            if (Mathf.Abs(maxPos - minPos) < rangeSpacing) //they are less than two apart, so we should fix that
            {
                if (minPos == 0) { maxPos = rangeSpacing; }
                else if (maxPos == stringSteps) { minPos = stringSteps - rangeSpacing; }
                else { minPos = maxPos - rangeSpacing; }
            }
            //check flex pos
            if (flexPos == minPos)
            {
                flexPos = currVal < minPos ? minPos - 1 : minPos + 1;
            }
            else if (flexPos == maxPos)
            {
                flexPos = currVal > maxPos ? maxPos + 1 : maxPos - 1;
            }
            //generate the string based on these parameters
            string res = "0 ";
            for (int i = 0; i < stringSteps + 1; i++)
            {
                if (i == flexPos) { res += "|"; }
                else if (i == minPos) { res += "["; }
                else if (i == maxPos) { res += "]"; }
                else { res += "-"; }
            }
            return res + " 1";
        }


        /// <summary> Convert a fingle finger's flexion range and current flexion into a string representation. </summary>
        /// <param name="flexion"></param>
        /// <param name="finger"></param>
        /// <returns></returns>
        public string ToRangeString(float flexion, SGCore.Finger finger)
        {
            float min = GetMinFlexion(finger);
            float max = GetMaxFlexion(finger);
            return ToBlockString(flexion, min, max, stringSteps);
        }

        /// <summary> Convert a range of flexions into a scrting representation, showing the ranges and current flexion for each finger in a string "-----[--|]-"  </summary>
        /// <param name="flexions"></param>
        /// <returns></returns>
        public string ToRangeString(float[] flexions)
        {
            string res = "";
            for (int f = 0; f < 5; f++)
            {
                res += ToRangeString(flexions[f], (SGCore.Finger)f);
                if (f < 4) { res += "\r\n"; }
            }
            return res;
        }



    }

}