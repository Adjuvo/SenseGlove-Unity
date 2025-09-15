using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Contains utility classes for SenseGlove calibration that relies on internal classes. 
 * Placed in a separate .cs file to keep the CalibrationVoid less cluttered.
 * 
 * Author: max@senseglove.com
 */


namespace SG.Calibration
{

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Wrist Calibration Data - for tracking


    /// <summary> Contains data relevant for wrist calibration (and stability of that algorithm) </summary>
    public class SG_WristCalibrationData
    {
    }



    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Base Calibration Data - No Glove / No Calibration Needed

    /// <summary> Contains the acutal calibration logic and variables, which are different for Nova 1 and Nova 2.
    /// Meant to be extended by other classes. This base class is used for a non-existing glove that will always calibrate. </summary>
    public class SG_GloveCalibrationData
    {
        /// <summary> If true, this calibration was created for the right hand. </summary>
        public bool RightHand { get; protected set; }

       
        /// <summary> Return true if this glove must be calibrated. If not (because it's turned off), it will be ignored during the sequence.
        /// If both gloves don't need to be calibrated, we skip to EndCalibration. </summary>
        public virtual bool CalibrationRequired() { return false; }


        /// <summary> Resets the calibration value(s) of the Glove and ensures it does not lock until we say so </summary>
        public virtual void InitializeCalibration() { }



        /// <summary> Returns true if the calibration variables gathered here are correct. If not, we must retry </summary>
        /// <returns></returns>
        public virtual bool CalibrationValid() { return true; }


        /// <summary> Locks the calibration in internally once we're finished. </summary>
        public virtual void LockInCalibration() { }



        /// <summary> Update your finger data with the latest values from the glove. </summary>
        /// <param name="dT"></param>
        public virtual void UpdateFingerData(float dT, FingerCalibrationOrder state) { }

        /// <summary> The required amount of abduction motions </summary>
        public const int RequiredAbdMotions = 4;

        /// <summary> The amount of abduction motions made during one particular step </summary>
        /// <returns></returns>
        public virtual int GetAbductionMotions() { return RequiredAbdMotions; }

        /// <summary> Is the current finger tracking compatible / stable? </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual bool IsStateStable(FingerCalibrationOrder state, bool checkMinimumMovement) { return true; }

        public virtual void StoreFingerData(FingerCalibrationOrder state) { }

        /// <summary> Reset some (if not all) data for a second attempt </summary>
        public virtual void ResetForNextAttempt() { }

        protected SG_GloveCalibrationData() { }

        /// <summary> Creates a default glove calibration sequence that is always stable and never required. 
        /// Used when a glove is not connected  or does not require calibration. </summary>
        /// <param name="_rightHand"></param>
        public SG_GloveCalibrationData(bool _rightHand)
        {
            RightHand = _rightHand;
        }

    }



    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Nova 2 Calibration Data


    /// <summary> Gathers calibration data for the Nova 2 Glove. </summary>
    public class SG_Nova2CalibrationData : SG_GloveCalibrationData
    {
        private enum Nova2SensorPosition
        {
            ThumbAbd = 0,
            ThumbFlex,
            IndexProximalFlex,
            IndexDistalFlex,
            MiddleFlex,
            RingFlex
        }

        public const float stabilityThreshold_flexion = 55; //[Raw Sensor Value]
        public const float thumbFlexionDistance = 1600; //minimum difference between thumbs up and thumb under middle finger for it to count [Raw sensor value]
        public const float fingerFlexionDistance = 1600; //minimum difference between figners in thumbs up and handsTogether
        public const float indexProxDistance = 500; //minimum difference between figners in thumbs up and handsTogether
        public const float indexDistDistance = 500; //minimum difference between figners in thumbs up and handsTogether
        public const float abductionDistance = 100; //minimum difference for an abduction value to be counted as 'valid'
        
        public const float minimumFirstMovement = 250; //minimum difference for an abduction value to be counted as 'valid'

        /// <summary> Internal Nova 2 Glove to gather calibration data from... </summary>
        private SGCore.Nova.Nova2Glove iGlove;


        //private SGCore.Nova.Nova2_SensorData thumbsUpData, thumbBelowRingData, thumbAbdData, handsTogetherData;
        //private SGCore.Nova.Nova2_SensorData lastData = null;
        private float[] lastData = new float[6];
        private MovingAverageCalculator[] movAverages;

        private int samplesThisStep = 0;

        /// <summary> This is the maximum one during all the steps </summary>
        private float maxAbdThisStep = 0;

        /// <summary> This is purely for the one motion, gets reset every time </summary>
        public const float abductionCountDistance = 100; //minimum difference for an abduction value to be counted
        
        private float minAbdThisCount = float.MaxValue;
        private float maxAbdThisCount = float.MinValue;
        private bool abdMustIncrease = true;
        private int abdCount = 0;

        private float[] veryfirstMinValues = new float[6];
        private float[] veryfirstMaxValues = new float[6];

        private float[] minSensorValues = new float[6];
        private float[] maxSensorValues = new float[6];

        public SG_Nova2CalibrationData(SGCore.Nova.Nova2Glove nova2Instance)
        {
            iGlove = nova2Instance;
            RightHand = nova2Instance.IsRight();
            movAverages = new MovingAverageCalculator[6];
            for (int i=0; i<movAverages.Length; i++)
            {
                movAverages[i] = new MovingAverageCalculator(55);
            }

            veryfirstMinValues = new float[6];
            veryfirstMaxValues = new float[6];
            for (int i = 0; i < 6; i++)
            {
                veryfirstMinValues[i] = float.MaxValue;
                veryfirstMaxValues[i] = float.MinValue;
            }
        }


        /// <summary> This glove requires calibration once created! </summary>
        /// <returns></returns>
        public override bool CalibrationRequired() { return true; }


        /// <summary> Grab some data from the Nova 2 and make sure it knows not to lock itself in. </summary>
        public override void InitializeCalibration()
        {
            SGCore.Nova.Nova2Glove.LockCalibrationInternal = false; //ensure they know we're not allowed to lock in
            iGlove.ResetCalibration();
        }


        public override void LockInCalibration()
        {
            SGCore.Util.SensorNormalization normalizer = new SGCore.Util.SensorNormalization(SGCore.Nova.Nova2Glove.GetMinimalSensorMovement());
            normalizer.NormalizeValues(minSensorValues);
            normalizer.NormalizeValues(maxSensorValues);
            normalizer.CollectNormalization = false; //locks in the normalizer
            iGlove.SetSensorNormalizer(normalizer); //and assigns it to the glove instance. Bayum.
            iGlove.EndCalibration(); //for good measure.
            SGCore.Nova.Nova2Glove.LockCalibrationInternal = true; //set this back to default so as not to disrupt anything custom later down the line.
        }

        private void ResetAbductionTracking()
        {
            minAbdThisCount = float.MaxValue;
            maxAbdThisCount = float.MinValue;
        }

        public override void ResetForNextAttempt()
        {
            ResetAbductionTracking();
            abdCount = 0;
            minAbdThisCount = float.MaxValue;
            maxAbdThisCount = float.MinValue;
            abdMustIncrease = true;
            samplesThisStep = 0;

            veryfirstMinValues = new float[6];
            veryfirstMaxValues = new float[6];
            for (int i=0; i<6; i++)
            {
                veryfirstMinValues[i] = float.MaxValue;
                veryfirstMaxValues[i] = float.MinValue;
            }
        }

        public override void UpdateFingerData(float dT, FingerCalibrationOrder state)
        {
            if (iGlove.GetSensorData(out SGCore.Nova.Nova2_SensorData newData))
            {
                samplesThisStep++;
                lastData = SGCore.Nova.Nova2Glove.ToNormalizedValues(newData);
                for (int i = 0; i < lastData.Length; i++)
                    movAverages[i].AddObservation(lastData[i]);

                if (state == 0)
                {
                    for (int i=0; i<lastData.Length; i++)
                    {
                        veryfirstMaxValues[i] = Mathf.Max(lastData[i], veryfirstMaxValues[i]);
                        veryfirstMinValues[i] = Mathf.Min(lastData[i], veryfirstMinValues[i]);
                    }
                }

                if (state == FingerCalibrationOrder.ThumbAbduction)
                {
                    //TODO: Count the amount of abductions / adductions
                    float currAbd = lastData[(int)Nova2SensorPosition.ThumbAbd];
                    maxAbdThisStep = samplesThisStep == 1 ? currAbd : Mathf.Max(currAbd, maxAbdThisStep);

                    maxAbdThisCount = Mathf.Max(maxAbdThisCount, currAbd); //log the maximum...
                    minAbdThisCount = Mathf.Min(minAbdThisCount, currAbd);

                    //Since we did a thumbs up at one point, this is stored at MinValues[Sensor.ThumbAbd]. I must move X amount away from that, then move back up.
                    if (abdMustIncrease) //thumb moves ourward
                    {
                        float landmark = abdCount == 0 ? Mathf.Min(minSensorValues[(int)Nova2SensorPosition.ThumbAbd], minAbdThisCount) : minAbdThisCount;
                        if (currAbd - landmark > abductionCountDistance)
                            abdMustIncrease = false;
                    }
                    else //then inward again!
                    {
                        if (maxAbdThisCount - currAbd > abductionCountDistance)
                        {
                            abdMustIncrease = true; //we're going back in increasing again...
                            minAbdThisCount = currAbd; //we'll need to move higher than this one again
                            maxAbdThisCount = currAbd; //basically resetting it for now...
                            abdCount++;
                        }
                    }
                    

                    
                }
            }
        }


        /// <summary> Returns true if the finger is considered 'stable' for the current state. It does not need to account for it's own timing, only that it's there. </summary>
        /// <param name="state">Passign this in case there's a requirement for moving beyond a certain value... </param>
        /// <returns></returns>
        public override bool IsStateStable(FingerCalibrationOrder state, bool checkMinimumMovement)
        {
            if (samplesThisStep == 0)
                return false;

            for (int i = 0; i < movAverages.Length; i++)
            {
                if (movAverages[i].StandardDeviation > stabilityThreshold_flexion) //todo: Threshold per finger, per sensor, or per type of movement?
                    return false;
            }

            if (checkMinimumMovement)
            {
                //TODO: For the fist state, we do need to have move a liiitle bit.
                if (state == 0)
                {
                    float[] diffs = new float[6];
                    for (int i = 0; i < 6; i++)
                    {
                        diffs[i] = veryfirstMaxValues[i] - veryfirstMinValues[i];
                    }
                    if (diffs[(int)Nova2SensorPosition.IndexProximalFlex] < minimumFirstMovement
                        || diffs[(int)Nova2SensorPosition.MiddleFlex] < minimumFirstMovement)
                        return false;
                }

                //usign an if statement here because a switch statement made things messy
                if (state == FingerCalibrationOrder.ThumbBelowRingfinger)
                {
                    //If we do a thumbs up before a thumbBelowRingFinger, we must flex (increase sensor value) sufficiently away from the ThumbsUp gesture
                    if (FingerCalibrationOrder.ThumbBelowRingfinger > FingerCalibrationOrder.Thumbsup)
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached 
                        //Debug.Log("UnderRing after Thumbs Up: Diff = " + (lastData[(int)Nova2SensorPosition.ThumbFlex] - minSensorValues[(int)Nova2SensorPosition.ThumbFlex]).ToString());
                        return lastData[(int)Nova2SensorPosition.ThumbFlex] - minSensorValues[(int)Nova2SensorPosition.ThumbFlex] >= thumbFlexionDistance;
#pragma warning restore CS0162
                    }
                }
                //If we do a thumbBelowRingFinger before a thumbsUp, we must extend (decrease sensor value) sufficiently away from the ThumbsUp gesture
                else if (state == FingerCalibrationOrder.Thumbsup)
                {
                    if (FingerCalibrationOrder.Thumbsup > FingerCalibrationOrder.HandsTogether) //we held our hands together before so we must flex our fingers far enough
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached
                        bool flexedEnough = lastData[(int)Nova2SensorPosition.IndexProximalFlex] - minSensorValues[(int)Nova2SensorPosition.IndexProximalFlex] >= indexProxDistance
                            && lastData[(int)Nova2SensorPosition.IndexDistalFlex] - minSensorValues[(int)Nova2SensorPosition.IndexDistalFlex] >= indexDistDistance
                            && lastData[(int)Nova2SensorPosition.MiddleFlex] - minSensorValues[(int)Nova2SensorPosition.MiddleFlex] >= fingerFlexionDistance
                            && lastData[(int)Nova2SensorPosition.RingFlex] - minSensorValues[(int)Nova2SensorPosition.RingFlex] >= fingerFlexionDistance;
                        if (!flexedEnough)
                            return false; //else check the next one...
#pragma warning restore CS0162
                    }
                    if (FingerCalibrationOrder.Thumbsup > FingerCalibrationOrder.ThumbBelowRingfinger) //we did a thumbBelowRing before so we must extend the thumb far enough
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached
                        //Debug.Log("Thumbs Up after UnderRing: Diff = " + (maxSensorValues[(int)Nova2SensorPosition.ThumbFlex] - lastData[(int)Nova2SensorPosition.ThumbFlex]).ToString());
                        return maxSensorValues[(int)Nova2SensorPosition.ThumbFlex] - lastData[(int)Nova2SensorPosition.ThumbFlex] >= thumbFlexionDistance;
#pragma warning restore CS0162
                    }
                }
                else if (state == FingerCalibrationOrder.HandsTogether)
                {
                    //We did a thumbs up before, which calibrates MaxValues fingers. This means our fingers need to be extended far enough away...
                    if (FingerCalibrationOrder.HandsTogether > FingerCalibrationOrder.Thumbsup)
                    {
                        //TODO: Index finger distance...?
                        return maxSensorValues[(int)Nova2SensorPosition.IndexProximalFlex] - lastData[(int)Nova2SensorPosition.IndexProximalFlex] >= indexProxDistance
                            && maxSensorValues[(int)Nova2SensorPosition.IndexDistalFlex] - lastData[(int)Nova2SensorPosition.IndexDistalFlex] >= indexDistDistance
                            && maxSensorValues[(int)Nova2SensorPosition.MiddleFlex] - lastData[(int)Nova2SensorPosition.MiddleFlex] >= fingerFlexionDistance
                            && maxSensorValues[(int)Nova2SensorPosition.RingFlex] - lastData[(int)Nova2SensorPosition.RingFlex] >= fingerFlexionDistance;
                    }
                }
            }
            return true;
        }

        /// <summary> If abduction motions are detected... </summary>
        /// <returns></returns>
        public override int GetAbductionMotions()
        {
            return this.abdCount;
        }


        /// <summary> Store the latest finger data to the appropriate data field. </summary>
        /// <param name="state"></param>
        public override void StoreFingerData(FingerCalibrationOrder state)
        {
            if (samplesThisStep == 0)
            {
                Debug.LogError("Never received any Sensor Data from this glove during this step!");
                return;
            }
            samplesThisStep = 0;
            switch (state)
            {
                case FingerCalibrationOrder.Thumbsup:
                    //Sets the thumbs up 0 (min value) and finger fully flexed 1 (max value)
                    minSensorValues[(int)Nova2SensorPosition.ThumbFlex] = lastData[(int)Nova2SensorPosition.ThumbFlex];
                    maxSensorValues[(int)Nova2SensorPosition.IndexProximalFlex] = lastData[(int)Nova2SensorPosition.IndexProximalFlex];
                    maxSensorValues[(int)Nova2SensorPosition.IndexDistalFlex] = lastData[(int)Nova2SensorPosition.IndexDistalFlex];
                    maxSensorValues[(int)Nova2SensorPosition.MiddleFlex] = lastData[(int)Nova2SensorPosition.MiddleFlex];
                    maxSensorValues[(int)Nova2SensorPosition.RingFlex] = lastData[(int)Nova2SensorPosition.RingFlex];
                    break;

                case FingerCalibrationOrder.ThumbBelowRingfinger:
                    //Sets the thumb flexion 1 (max value)
                    maxSensorValues[(int)Nova2SensorPosition.ThumbFlex] = lastData[(int)Nova2SensorPosition.ThumbFlex];
                    break;

                case FingerCalibrationOrder.ThumbAbduction:

                    //TODO: Get the max value measured during this step
                    maxSensorValues[(int)Nova2SensorPosition.ThumbAbd] = maxAbdThisStep; //lastData[(int)Nova2SensorPosition.ThumbAbd];
                    break;

                case FingerCalibrationOrder.HandsTogether:
                    //Sets the thumb abduction at 0 (min adb) and fingers straight (0)
                    minSensorValues[(int)Nova2SensorPosition.ThumbAbd] = lastData[(int)Nova2SensorPosition.ThumbAbd];
                    minSensorValues[(int)Nova2SensorPosition.IndexProximalFlex] = lastData[(int)Nova2SensorPosition.IndexProximalFlex];
                    minSensorValues[(int)Nova2SensorPosition.IndexDistalFlex] = lastData[(int)Nova2SensorPosition.IndexDistalFlex];
                    minSensorValues[(int)Nova2SensorPosition.MiddleFlex] = lastData[(int)Nova2SensorPosition.MiddleFlex];
                    minSensorValues[(int)Nova2SensorPosition.RingFlex] = lastData[(int)Nova2SensorPosition.RingFlex];
                    break;

                default:
                    break;
            }
        }

        public override bool CalibrationValid()
        {
            float[] diffs = new float[minSensorValues.Length];
            for (int i = 0; i < minSensorValues.Length; i++)
                diffs[i] = maxSensorValues[i] - minSensorValues[i];

            return diffs[(int)Nova2SensorPosition.ThumbAbd] >= abductionDistance
                && diffs[(int)Nova2SensorPosition.ThumbFlex] >= thumbFlexionDistance
                && diffs[(int)Nova2SensorPosition.IndexProximalFlex] >= indexProxDistance
                && diffs[(int)Nova2SensorPosition.IndexDistalFlex] >= indexDistDistance
                && diffs[(int)Nova2SensorPosition.MiddleFlex] >= fingerFlexionDistance
                && diffs[(int)Nova2SensorPosition.RingFlex] >= fingerFlexionDistance;
        }

    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Nova 1 Calibration Data


    /// <summary> Gathers calibration data for the Nova 2 Glove. </summary>
    public class SG_Nova1CalibrationData : SG_GloveCalibrationData
    {
        private enum Nova1SensorPosition
        {
            ThumbAbd = 0,
            ThumbFlex,
            IndexFlex,
            MiddleFlex,
            RingFlex
        }

        public const float stabilityThreshold_flexion = 55; //[Raw Sensor Value]
        public const float thumbFlexionDistance = 1600; //minimum difference between thumbs up and thumb under middle finger for it to count [Raw sensor value]
        public const float fingerFlexionDistance = 1600; //minimum difference between figners in thumbs up and handsTogether
        public const float indexProxDistance = 500; //minimum difference between figners in thumbs up and handsTogether
        public const float indexDistDistance = 500; //minimum difference between figners in thumbs up and handsTogether
        public const float abductionDistance = 200; //minimum difference for an abduction value to be counted as 'valid'

        public const float minimumFirstMovement = 250; //minimum difference for an abduction value to be counted as 'valid'

        /// <summary> Internal Nova 2 Glove to gather calibration data from... </summary>
        private SGCore.Nova.NovaGlove iGlove;


        //private SGCore.Nova.Nova2_SensorData thumbsUpData, thumbBelowRingData, thumbAbdData, handsTogetherData;
        //private SGCore.Nova.Nova2_SensorData lastData = null;
        private float[] lastData = new float[6];
        private MovingAverageCalculator[] movAverages;

        private int samplesThisStep = 0;

        /// <summary> This is the maximum one during all the steps </summary>
        private float maxAbdThisStep = 0;

        /// <summary> This is purely for the one motion, gets reset every time </summary>
        public const float abductionCountDistance = 50; //minimum difference for an abduction value to be counted

        private float minAbdThisCount = float.MaxValue;
        private float maxAbdThisCount = float.MinValue;
        private bool abdMustIncrease = true;
        private int abdCount = 0;

        private float[] veryfirstMinValues = new float[5];
        private float[] veryfirstMaxValues = new float[5];

        private float[] minSensorValues = new float[5];
        private float[] maxSensorValues = new float[5];

        public SG_Nova1CalibrationData(SGCore.Nova.NovaGlove novaInstance)
        {
            iGlove = novaInstance;
            RightHand = novaInstance.IsRight();
            movAverages = new MovingAverageCalculator[5];
            for (int i = 0; i < movAverages.Length; i++)
            {
                movAverages[i] = new MovingAverageCalculator(55);
            }

            veryfirstMinValues = new float[5];
            veryfirstMaxValues = new float[5];
            for (int i = 0; i < 6; i++)
            {
                veryfirstMinValues[i] = float.MaxValue;
                veryfirstMaxValues[i] = float.MinValue;
            }
        }


        /// <summary> This glove requires calibration once created! </summary>
        /// <returns></returns>
        public override bool CalibrationRequired() { return true; }


        /// <summary> Grab some data from the Nova 2 and make sure it knows not to lock itself in. </summary>
        public override void InitializeCalibration()
        {
            SGCore.Nova.Nova2Glove.LockCalibrationInternal = false; //ensure they know we're not allowed to lock in
            iGlove.ResetCalibration();
        }


        public override void LockInCalibration()
        {
            SGCore.Util.SensorNormalization normalizer = new SGCore.Util.SensorNormalization(SGCore.Nova.NovaGlove.GetMinimalSensorMovement());
            normalizer.NormalizeValues(minSensorValues);
            normalizer.NormalizeValues(maxSensorValues);
            normalizer.CollectNormalization = false; //locks in the normalizer
            iGlove.SetSensorNormalizer(normalizer); //and assigns it to the glove instance. Bayum.
            iGlove.EndCalibration(); //for good measure.
            SGCore.Nova.Nova2Glove.LockCalibrationInternal = true; //set this back to default so as not to disrupt anything custom later down the line.
        }

        private void ResetAbductionTracking()
        {
            minAbdThisCount = float.MaxValue;
            maxAbdThisCount = float.MinValue;
        }

        public override void ResetForNextAttempt()
        {
            ResetAbductionTracking();
            abdCount = 0;
            minAbdThisCount = float.MaxValue;
            maxAbdThisCount = float.MinValue;
            abdMustIncrease = true;
            samplesThisStep = 0;

            veryfirstMinValues = new float[5];
            veryfirstMaxValues = new float[5];
            for (int i = 0; i < 6; i++)
            {
                veryfirstMinValues[i] = float.MaxValue;
                veryfirstMaxValues[i] = float.MinValue;
            }
        }

        public override void UpdateFingerData(float dT, FingerCalibrationOrder state)
        {
            if (iGlove.GetSensorData(out SGCore.Nova.Nova_SensorData newData))
            {
                samplesThisStep++;
                lastData = SGCore.Nova.NovaGlove.ToNormalizedValues(newData);
                for (int i = 0; i < lastData.Length; i++)
                    movAverages[i].AddObservation(lastData[i]);

                if (state == 0)
                {
                    for (int i = 0; i < lastData.Length; i++)
                    {
                        veryfirstMaxValues[i] = Mathf.Max(lastData[i], veryfirstMaxValues[i]);
                        veryfirstMinValues[i] = Mathf.Min(lastData[i], veryfirstMinValues[i]);
                    }
                }

                if (state == FingerCalibrationOrder.ThumbAbduction)
                {
                    //TODO: Count the amount of abductions / adductions
                    float currAbd = lastData[(int)Nova1SensorPosition.ThumbAbd];
                    maxAbdThisStep = samplesThisStep == 1 ? currAbd : Mathf.Max(currAbd, maxAbdThisStep);

                    maxAbdThisCount = Mathf.Max(maxAbdThisCount, currAbd); //log the maximum...
                    minAbdThisCount = Mathf.Min(minAbdThisCount, currAbd);

                    //Since we did a thumbs up at one point, this is stored at MinValues[Sensor.ThumbAbd]. I must move X amount away from that, then move back up.
                    if (abdMustIncrease) //thumb moves ourward
                    {
                        float landmark = abdCount == 0 ? Mathf.Min(minSensorValues[(int)Nova1SensorPosition.ThumbAbd], minAbdThisCount) : minAbdThisCount;
                        if (currAbd - landmark > abductionCountDistance)
                            abdMustIncrease = false;
                    }
                    else //then inward again!
                    {
                        if (maxAbdThisCount - currAbd > abductionCountDistance)
                        {
                            abdMustIncrease = true; //we're going back in increasing again...
                            minAbdThisCount = currAbd; //we'll need to move higher than this one again
                            maxAbdThisCount = currAbd; //basically resetting it for now...
                            abdCount++;
                        }
                    }



                }
            }
        }


        /// <summary> Returns true if the finger is considered 'stable' for the current state. It does not need to account for it's own timing, only that it's there. </summary>
        /// <param name="state">Passign this in case there's a requirement for moving beyond a certain value... </param>
        /// <returns></returns>
        public override bool IsStateStable(FingerCalibrationOrder state, bool checkMinimumMovement)
        {
            if (samplesThisStep == 0)
                return false;

            for (int i = 0; i < movAverages.Length; i++)
            {
                if (movAverages[i].StandardDeviation > stabilityThreshold_flexion) //todo: Threshold per finger, per sensor, or per type of movement?
                    return false;
            }

            if (checkMinimumMovement)
            {
                //TODO: For the fist state, we do need to have move a liiitle bit.
                if (state == 0)
                {
                    float[] diffs = new float[6];
                    for (int i = 0; i < 6; i++)
                    {
                        diffs[i] = veryfirstMaxValues[i] - veryfirstMinValues[i];
                    }
                    if (diffs[(int)Nova1SensorPosition.IndexFlex] < minimumFirstMovement
                        || diffs[(int)Nova1SensorPosition.MiddleFlex] < minimumFirstMovement)
                        return false;
                }

                //usign an if statement here because a switch statement made things messy
                if (state == FingerCalibrationOrder.ThumbBelowRingfinger)
                {
                    //If we do a thumbs up before a thumbBelowRingFinger, we must flex (increase sensor value) sufficiently away from the ThumbsUp gesture
                    if (FingerCalibrationOrder.ThumbBelowRingfinger > FingerCalibrationOrder.Thumbsup)
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached 
                        //Debug.Log("UnderRing after Thumbs Up: Diff = " + (lastData[(int)Nova1SensorPosition.ThumbFlex] - minSensorValues[(int)Nova1SensorPosition.ThumbFlex]).ToString());
                        return lastData[(int)Nova1SensorPosition.ThumbFlex] - minSensorValues[(int)Nova1SensorPosition.ThumbFlex] >= thumbFlexionDistance;
#pragma warning restore CS0162
                    }
                }
                //If we do a thumbBelowRingFinger before a thumbsUp, we must extend (decrease sensor value) sufficiently away from the ThumbsUp gesture
                else if (state == FingerCalibrationOrder.Thumbsup)
                {
                    if (FingerCalibrationOrder.Thumbsup > FingerCalibrationOrder.HandsTogether) //we held our hands together before so we must flex our fingers far enough
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached
                        bool flexedEnough = lastData[(int)Nova1SensorPosition.IndexFlex] - minSensorValues[(int)Nova1SensorPosition.IndexFlex] >= fingerFlexionDistance
                            && lastData[(int)Nova1SensorPosition.MiddleFlex] - minSensorValues[(int)Nova1SensorPosition.MiddleFlex] >= fingerFlexionDistance
                            && lastData[(int)Nova1SensorPosition.RingFlex] - minSensorValues[(int)Nova1SensorPosition.RingFlex] >= fingerFlexionDistance;
                        if (!flexedEnough)
                            return false; //else check the next one...
#pragma warning restore CS0162
                    }
                    if (FingerCalibrationOrder.Thumbsup > FingerCalibrationOrder.ThumbBelowRingfinger) //we did a thumbBelowRing before so we must extend the thumb far enough
                    {
#pragma warning disable CS0162 // We might change the enum order to make things more accessible, so this code may or may not be reached
                        //Debug.Log("Thumbs Up after UnderRing: Diff = " + (maxSensorValues[(int)Nova1SensorPosition.ThumbFlex] - lastData[(int)Nova1SensorPosition.ThumbFlex]).ToString());
                        return maxSensorValues[(int)Nova1SensorPosition.ThumbFlex] - lastData[(int)Nova1SensorPosition.ThumbFlex] >= thumbFlexionDistance;
#pragma warning restore CS0162
                    }
                }
                else if (state == FingerCalibrationOrder.HandsTogether)
                {
                    //We did a thumbs up before, which calibrates MaxValues fingers. This means our fingers need to be extended far enough away...
                    if (FingerCalibrationOrder.HandsTogether > FingerCalibrationOrder.Thumbsup)
                    {
                        //TODO: Index finger distance...?
                        return maxSensorValues[(int)Nova1SensorPosition.IndexFlex] - lastData[(int)Nova1SensorPosition.IndexFlex] >= fingerFlexionDistance
                            && maxSensorValues[(int)Nova1SensorPosition.MiddleFlex] - lastData[(int)Nova1SensorPosition.MiddleFlex] >= fingerFlexionDistance
                            && maxSensorValues[(int)Nova1SensorPosition.RingFlex] - lastData[(int)Nova1SensorPosition.RingFlex] >= fingerFlexionDistance;
                    }
                }
            }
            return true;
        }

        /// <summary> If abduction motions are detected... </summary>
        /// <returns></returns>
        public override int GetAbductionMotions()
        {
            return this.abdCount;
        }


        /// <summary> Store the latest finger data to the appropriate data field. </summary>
        /// <param name="state"></param>
        public override void StoreFingerData(FingerCalibrationOrder state)
        {
            if (samplesThisStep == 0)
            {
                Debug.LogError("Never received any Sensor Data from this glove during this step!");
                return;
            }
            samplesThisStep = 0;
            switch (state)
            {
                case FingerCalibrationOrder.Thumbsup:
                    //Sets the thumbs up 0 (min value) and finger fully flexed 1 (max value)
                    minSensorValues[(int)Nova1SensorPosition.ThumbFlex] = lastData[(int)Nova1SensorPosition.ThumbFlex];
                    maxSensorValues[(int)Nova1SensorPosition.IndexFlex] = lastData[(int)Nova1SensorPosition.IndexFlex];
                    maxSensorValues[(int)Nova1SensorPosition.MiddleFlex] = lastData[(int)Nova1SensorPosition.MiddleFlex];
                    maxSensorValues[(int)Nova1SensorPosition.RingFlex] = lastData[(int)Nova1SensorPosition.RingFlex];
                    break;

                case FingerCalibrationOrder.ThumbBelowRingfinger:
                    //Sets the thumb flexion 1 (max value)
                    maxSensorValues[(int)Nova1SensorPosition.ThumbFlex] = lastData[(int)Nova1SensorPosition.ThumbFlex];
                    break;

                case FingerCalibrationOrder.ThumbAbduction:

                    //TODO: Get the max value measured during this step
                    maxSensorValues[(int)Nova1SensorPosition.ThumbAbd] = maxAbdThisStep; //lastData[(int)Nova1SensorPosition.ThumbAbd];
                    break;

                case FingerCalibrationOrder.HandsTogether:
                    //Sets the thumb abduction at 0 (min adb) and fingers straight (0)
                    minSensorValues[(int)Nova1SensorPosition.ThumbAbd] = lastData[(int)Nova1SensorPosition.ThumbAbd];
                    minSensorValues[(int)Nova1SensorPosition.IndexFlex] = lastData[(int)Nova1SensorPosition.IndexFlex];
                    minSensorValues[(int)Nova1SensorPosition.MiddleFlex] = lastData[(int)Nova1SensorPosition.MiddleFlex];
                    minSensorValues[(int)Nova1SensorPosition.RingFlex] = lastData[(int)Nova1SensorPosition.RingFlex];
                    break;

                default:
                    break;
            }
        }

        public override bool CalibrationValid()
        {
            float[] diffs = new float[minSensorValues.Length];
            for (int i = 0; i < minSensorValues.Length; i++)
                diffs[i] = maxSensorValues[i] - minSensorValues[i];

            return diffs[(int)Nova1SensorPosition.ThumbAbd] >= abductionDistance
                && diffs[(int)Nova1SensorPosition.ThumbFlex] >= thumbFlexionDistance
                && diffs[(int)Nova1SensorPosition.IndexFlex] >= fingerFlexionDistance
                && diffs[(int)Nova1SensorPosition.MiddleFlex] >= fingerFlexionDistance
                && diffs[(int)Nova1SensorPosition.RingFlex] >= fingerFlexionDistance;
        }

    }






    //TODO: Refactor this so it's no longer required.



    // got this to calculate rolling standard deviation.
    // for standard deviation to check if finger pose is stable.
    // from: https://stackoverflow.com/questions/29265626/calculate-a-moving-standard-deviation
    public class MovingAverageCalculator
    {
        public MovingAverageCalculator(int period)
        {
            _period = period;
            _window = new float[period];
        }

        public float Average
        {
            get { return _average; }
        }

        public float StandardDeviation
        {
            get
            {
                var variance = Variance;
                if (variance >= float.Epsilon)
                {
                    float sd = (float)System.Math.Sqrt(variance);
                    return float.IsNaN(sd) ? 0.0f : sd;
                }
                return 0.0f;
            }
        }

        public float Variance
        {
            get
            {
                var n = N;
                return n > 1 ? _variance_sum / (n - 1) : 0.0f;
            }
        }

        public bool HasFullPeriod
        {
            get { return _num_added >= _period; }
        }

        public IEnumerable<float> Observations
        {
            get { return _window.Take(N); }
        }

        public int N
        {
            get { return System.Math.Min(_num_added, _period); }
        }

        public void AddObservation(float observation)
        {
            // Window is treated as a circular buffer.
            var ndx = _num_added % _period;
            var old = _window[ndx];     // get value to remove from window
            _window[ndx] = observation; // add new observation in its place.
            _num_added++;

            // Update average and standard deviation using deltas
            var old_avg = _average;
            if (_num_added <= _period)
            {
                var delta = observation - old_avg;
                _average += delta / _num_added;
                _variance_sum += (delta * (observation - _average));
            }
            else // use delta vs removed observation.
            {
                var delta = observation - old;
                _average += delta / _period;
                _variance_sum += (delta * ((observation - _average) + (old - old_avg)));
            }
        }

        private readonly int _period;
        private readonly float[] _window;
        private int _num_added;
        private float _average;
        private float _variance_sum;
    }


}