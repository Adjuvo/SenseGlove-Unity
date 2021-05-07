using SGCore.Kinematics;
using UnityEngine;

namespace SGCore.Calibration
{

    /// <summary> Startup Stage used to indicate where of not a glove must be recalibrated. </summary>
    public enum CalibrationStage
    {
        /// <summary> We've only just started up. User needs to move their hands so we know if we're still in the same sensor range. </summary>
        MoveFingers,
        /// <summary> After moving, we've determined that a calibration must happen for proper hand tracking to happen. </summary>
        CalibrationNeeded,
        /// <summary> After moving, we've determined that a calibration must happen for proper hand tracking to happen. </summary>
        Calibrating,
        /// <summary> We've determined that this needs no (more) calibration to function as intended. </summary>
        Done
    }


    /// <summary> An algorithm that checks whether or not our current user is running in the same calibration range as last time. </summary>
    public class HapticGlove_CalCheck
	{
        /// <summary> The last calibration range that we are checking against. </summary>
        public SGCore.Calibration.SensorRange lastRange;
        
        /// <summary> The currently measured sensor range. </summary>
        public SGCore.Calibration.SensorRange currentRange;

        // public SGCore.Calibration.CalibrationPoints calibrationCheck = new SGCore.Calibration.CalibrationPoints();

        //When the user is at roughtly the right thresholds.
        /// <summary> If we're 'around' the lastRange for this amount of time, we're calling it 'done'. </summary>
        public float perfectThresholdTime = 0.5f;
        /// <summary> Time that we've been around the last range for. </summary>
        public float timer_atThreshold = 0;


        //Checks for when the user's fingers are much smaller, and can therefore not reach the extremes.
        /// <summary> Whether or not we've moved a minimum amount. Something that even someone with the smallest hands can do. </summary>
        public bool movedMinimum = false;
        /// <summary> After moving the minimum amount of time but not reaching the perivous range, we'll say the hand is too small. </summary>
        public float minMoveTime = 3.0f;
        /// <summary> Time since we made the minumum amount of movement </summary>
        public float timer_MinMove = 0.0f;

        // When one or more of the sensors are suffuciently out of bounds to cause concern
        /// <summary> The amount of time the current values can be out of range for. Catches a minor case where sensor jitter causes us to be outside of the range. </summary>
        public float outOfBoundsTime = 0.1f;
        /// <summary> Time we've been out of bounds for. </summary>
        public float timer_outOfBounds = 0;

        
        /// <summary> The current stage of this algorithm </summary>
        public CalibrationStage CalibrationStage
        {
            get; private set;
        }

        /// <summary> Whether or not his algorithm has determined if calibration is required. </summary>
        public bool ReachedConclusion
        {
            get { return this.CalibrationStage != CalibrationStage.MoveFingers; }
        }


        /// <summary> The last calibration range can be null, at which point you defnitely need calibration. </summary>
        /// <param name="lastCalibrationRange"></param>
        public HapticGlove_CalCheck(SGCore.Calibration.SensorRange lastCalibrationRange)
        {
            if (lastCalibrationRange != null)
            {
                lastRange = new SensorRange(lastCalibrationRange); //deep copy the range just to be sure.
            }
            Reset();
        }

        /// <summary> Reset the calibration range, so it may be used again. This does not reset the LastRange. </summary>
        public void Reset()
        {
            currentRange = null;
            timer_atThreshold = 0;
            timer_MinMove = 0;
            timer_outOfBounds = 0;
            CalibrationStage = CalibrationStage.MoveFingers; //todo: Check for Nova or SenseGlove DK1
            movedMinimum = false;
        }

        /// <summary> Using the currently received Sensor Values, check if calibration is required. This function will have to be callen until a conclusion can be reached. </summary>
        /// <param name="currentValues"></param>
        /// <param name="deltaTime_s"></param>
        /// <param name="deviceType"></param>
        public void CheckRange(Vect3D[] currentValues, float deltaTime_s, DeviceType deviceType)
        {
            if (this.lastRange == null)
            {
                this.CalibrationStage = CalibrationStage.CalibrationNeeded;
            }
            else if (this.CalibrationStage == CalibrationStage.MoveFingers)
            {
                //Update our current range.
                if (currentRange == null)
                {
                    this.currentRange = new SensorRange();
                    this.currentRange.MinValues = currentValues;
                    this.currentRange.MaxValues = currentValues;
                }
                else
                {
                    currentRange.CheckForExtremes(currentValues); //check against range
                }

                //we're either out of bounds
                int outOfBoundsCode = HapticGlove_CalCheck.OutOfBounds(this.lastRange, currentValues, deviceType);
                bool outOfBounds = outOfBoundsCode > -1;

                if (outOfBounds)
                {
                    timer_outOfBounds += deltaTime_s;
                }

                bool smallHand = false;
                bool movedEnough = false;
                if (!outOfBounds) //we are still inside our acceptable bounds.
                {
                    //have we moved enough  to where we can say everything's ok?
                    if (HapticGlove_CalCheck.MatchesLast(currentRange.Range, lastRange.Range, deviceType))
                    {
                        timer_atThreshold += deltaTime_s;
                        if (timer_atThreshold >= perfectThresholdTime)
                        {
                            //Debug.Log("We've in our bounds and have moved roughly the same amount as last time AND we're that for " + perfectThresholdTime + "s. So we're good!");
                            movedEnough = true;
                        }

                    }

                    // Moved a minumum amount
                    if (movedMinimum) //we've already moved a bit
                    {
                        timer_MinMove += deltaTime_s;
                        if (timer_MinMove >= minMoveTime)
                        {
                            //Debug.Log("We've moved a minumum amount for " + minMoveTime + "s");
                            smallHand = true;
                        }
                    }
                    else //not yet moved enough
                    {
                        movedMinimum = HapticGlove_CalCheck.MovedMinimum(currentRange.Range, deviceType);
                        if (movedMinimum) { /*Debug.Log("Moved a mimumum amount. Timer start!");*/ }
                    }
                }

                //Check if any timers are finsihed.

                if (timer_outOfBounds >= outOfBoundsTime || smallHand)
                {
                    if (outOfBounds) { Debug.Log("We need calibration because we are out of bounds of our previous range! Code " + outOfBoundsCode); }
                    else if (smallHand) { Debug.Log("We need Calibration because we're dealing with a small hand..."); }

                    //Debug.Log("Ended at a range of " + currentRange.ToString());

                    this.CalibrationStage = CalibrationStage.CalibrationNeeded;
                }
                else if (movedEnough || Input.GetKeyDown(KeyCode.O))
                {
                    this.CalibrationStage = CalibrationStage.Done;
                }
                
            }
        }



        //------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Static Checker Functions

        /// <summary> SenseGlove: How far from the threshold one can be where we would still call it 'the same as before' </summary>
        public const float sgThreshold = SGCore.Kinematics.Values.Deg2Rad * 15;
        /// <summary> Nova: How far from the threshold one can be where we would still call it 'the same as before' </summary>
        public const float novaThreshold = 350; //250 is... what now?

        /// <summary> SenseGlove: The minumum amount of sensor flexion movement before we start testing for a smaller hand. </summary>
        public const float sgMinFlex = SGCore.Kinematics.Values.Deg2Rad * 90;
        /// <summary> Nova: The minumum amount of sensor flexion movement before we start testing for a smaller hand. </summary>
        public const float novaMinFlex = 1500;

        /// <summary> SenseGlove: The minumum amount of sensor movement on the thumb abduction before we start testing for a smaller hand. </summary>
        public const float sgMinAbd = SGCore.Kinematics.Values.Deg2Rad * 10;
        /// <summary> Nova: The minumum amount of sensor movement on the thumb abduction before we start testing for a smaller hand. </summary>
        public const float novaMinAbd = 250;


        /// <summary> Returns true if this DeviceType requires a calibration check. </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool NeedsCheck(SGCore.DeviceType device)
        {
            return device == DeviceType.NOVA || device == DeviceType.SENSEGLOVE;
        }

        /// <summary> Checks if current values are operating out of the previous range. Returns -1 if all is fine. 0...4 to indicate which finger is out of bounds. </summary>
        /// <param name="previousRange"></param>
        /// <param name="currentValues"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int OutOfBounds(SensorRange previousRange, Vect3D[] currentValues, SGCore.DeviceType type)
        {

            float flexOver = type == DeviceType.NOVA ? novaThreshold : sgThreshold;
            for (int f = 0; f < currentValues.Length; f++)
            {
                if (currentValues[f].y < previousRange.MinValues[f].y - flexOver || currentValues[f].y > previousRange.MaxValues[f].y + flexOver)
                {
                    //Debug.Log(((SGCore.Finger)f).ToString() + " Out of range by " + (currentValues[f].y - previousRange.MinValues[f].y).ToString() + " || " + (currentValues[f].y - previousRange.MaxValues[f].y).ToString());
                    return f;
                }
            }
            //float abdOver = type == DeviceType.NOVA ? novaThreshold : sgThreshold;
            //if (currentValues[0].z < previousRange.MinValues[0].z - abdOver || currentValues[0].z > previousRange.MaxValues[0].z + abdOver)
            //         {
            //	return 6; //code 6 being abduction thumb over(?)
            //         }
            return -1;
        }


        /// <summary> Returns true if the user has moved enough in both flexion and thumb abduction movement to be considered 'active' </summary>
        /// <param name="currentRange"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool MovedMinimum(Vect3D[] currentRange, SGCore.DeviceType type)
        {
            float minMovement = type == DeviceType.NOVA ? novaMinFlex : sgMinFlex;
            for (int f = 0; f < currentRange.Length; f++)
            {
                if (currentRange[f].y < minMovement)
                {
                    //Debug.Log( ((SGCore.Finger)f).ToString() + " has not moved enough");
                    return false;
                }
            }
            if (currentRange.Length > 0) //check adduction
            {
                //Debug.Log("Thumb has not adducted moved enough");
                return currentRange[0].z < minMovement;
            }//else we're not checking the thumb?
            return true;
        }



        /// <summary> Returns true if the current sensor values have moved roughly as much as last time. </summary>
        /// <param name="currentRange"></param>
        /// <param name="lastRange"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool MatchesLast(Vect3D[] currentRange, Vect3D[] lastRange, SGCore.DeviceType type)
        {
            float flexOver = (type == DeviceType.NOVA ? novaThreshold : sgThreshold) * 2.5f;
            for (int f = 0; f < currentRange.Length; f++)
            {
                if (currentRange[f].y < lastRange[f].y - flexOver)
                {
                    //Debug.Log("Not moved enough for " + ((SGCore.Finger)f).ToString() + ": " + currentRange[f].y + " vs " + (lastRange[f].y - flexOver)) ;
                    return false;
                }
            }
            //float abdOver = (type == DeviceType.NOVA ? novaThreshold : sgThreshold) * 2f;
            //return currentRange[0].z >= lastRange[0].z - abdOver;
            return true;
        }




    }
}
