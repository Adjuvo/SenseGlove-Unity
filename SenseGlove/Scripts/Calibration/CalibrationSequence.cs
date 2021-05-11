using SGCore.Kinematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SGCore.Calibration
{
    public enum CalibrationType
    {
        Quick,
        //GuidedSteps
    }

    /// <summary> A data point for Calibration, stored in a separate class so I can add / tweak as much as I want. </summary>
    public class CalDataPoint
    {
        /// <summary> Actual calibration values. </summary>
        public SGCore.Kinematics.Vect3D[] calibrationValues;

        /// <summary> The Calibration stage this belongs to. </summary>
        public int stage;

        /// <summary> Create a new data point </summary>
        /// <param name="currSate"></param>
        /// <param name="calVals"></param>
        public CalDataPoint(int currSate, SGCore.Kinematics.Vect3D[] calVals)
        {
            calibrationValues = calVals;
            stage = currSate;
        }

        /// <summary> Log Calibration values for storing on disk </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public string ToLogData(string delim = "\t")
        {
            string res = stage + delim + delim;
            for (int f = 0; f < calibrationValues.Length; f++)
            {
                res += calibrationValues[f].x + delim + calibrationValues[f].y + delim + calibrationValues[f].z + delim + delim;
            }
            return res;
        }
    }


    /// <summary> A sequence which collects data points untill specific criteria have been met. It must be 'fed' an Update command from your program in order to work. </summary>
    public class HapticGlove_CalibrationSequence
    {
        /// <summary> Glove linked to this calibration sequence. Will attempt to gram data from this glove evey time Update() is called. </summary>
        protected SGCore.HapticGlove iGlove;

        /// <summary> Calibration points colledted during this calibration process. One is added every thme the "Update()" function is called.
        /// Used to create a profile after the calibration finishes. </summary>
        public List<CalDataPoint> calibrationPoints = new List<CalDataPoint>();

        /// <summary> The maximum buffer size for calibration points. Once calibrationPoints exceeds this value, the  </summary>
        public const int maxDataPoints = 60000;

        /// <summary> How much time has elapsed during this calibration sequence. Useful for logging or for automated functions. </summary>
        public float elapsedTime = 0;

        /// <summary> Used to determine if the user has manually ended the sequence, to prevent us from adding any more calibration points. </summary>
        public bool ManualCompleted
        {
            get; set;
        }

        /// <summary> Kinematic information to generate a calibration preview pose. </summary>
        public SGCore.Kinematics.BasicHandModel HandModel
        {
            get; set;
        }

        /// <summary> If a sequence consists of multiple stages, this integer will show you which one is currently active. </summary>
        public virtual int CurrentStageInt
        {
            get { return -1; }
        }


        /// <summary> Basic constructor for subclasses to inherit from </summary>
        protected HapticGlove_CalibrationSequence() { }

        public HapticGlove_CalibrationSequence(SGCore.HapticGlove gloveToCalibrate)
        {
            iGlove = gloveToCalibrate;
            Reset();
        }



        /// <summary> The Glove linked to this Calibration Sequence. Can be changed or re-assigned. </summary>
        public SGCore.HapticGlove LinkedGlove
        {
            get { return iGlove; }
            set { iGlove = value; }
        }


        /// <summary> Returns the amount of calibration points collected so far </summary>
        public int DataPointCount
        {
            get { return this.calibrationPoints.Count; }
        }

        /// <summary> Whether or not this sequence was completed as per it's own rules </summary>
        /// <returns></returns>
        public virtual bool AutoCompleted
        {
            get { return false; }
        }


        /// <summary> Returns true if this sequence is marked as 'complete' and can begin compiling a profile. </summary>
        public virtual bool Completed
        {
            get { return this.AutoCompleted || this.ManualCompleted; }
        }

        /// <summary> Returns the current HandPose; either the one we should be making, or what it would look like at the moment.  </summary>
        /// <param name="currentPose"></param>
        /// <returns></returns>
        public bool GetHandPose(out SGCore.HandPose currentPose)
        {
            if (this.LinkedGlove != null)
            {
                return GetHandPose(LinkedGlove.IsRight(), out currentPose);
            }
            currentPose = null;
            return false;
        }


        /// <summary> Returns the current HandPose; either the one we should be making, or what it would look like at the moment.  </summary>
        /// <param name="currentPose"></param>
        /// <returns></returns>
        public virtual bool GetHandPose(bool rightHand, out SGCore.HandPose currentPose)
        {
            currentPose = null;
            return false;
        }


        /// <summary> Retrieve instructions on what to do during the current step. </summary>
        /// <returns></returns>
        public virtual string GetCurrentInstuction(string nextStepKey = "")
        {
            if (nextStepKey.Length > 0) { return "Press [" + nextStepKey + "] for the next step."; }
            return "";
        }



        /// <summary> Manual confirmation of whatever step we're supposed to making at the moment. </summary>
        public virtual void ConfirmCurrentStep() { }

        /// <summary> Safely adds calibration data. Automatically done within 'Update' </summary>
        /// <param name="calibrationData"></param>
        public virtual void AddDataPoint(Vect3D[] calibrationData)
        {
            if (calibrationPoints.Count > maxDataPoints - 1) { calibrationPoints.RemoveAt(0); }
            calibrationPoints.Add(new CalDataPoint(CurrentStageInt, calibrationData));
        }

        /// <summary> Update this calibration sequence with new data. We use deltaTime to check for things like stability. </summary>
        /// <param name="deltaTime_s"></param>
        public virtual void Update(float deltaTime_s)
        {
            if (!Completed)
            {
                elapsedTime += deltaTime_s;
                Vect3D[] calData;
                if (iGlove != null && iGlove.GetCalibrationValues(out calData))
                {
                    this.AddDataPoint(calData);
                }
            }
        }

        /// <summary> Resets this calibration sequence's data, but not it's LinkedGlove. </summary>
        public virtual void Reset()
        {
            this.calibrationPoints.Clear();
            elapsedTime = 0;
            ManualCompleted = false;
        }




        /// <summary> Compile a min/max range from the datapoints collected by this sequence. Returns true if it's actually possible. </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public virtual bool CompileRange(out SensorRange range)
        {
            range = new SensorRange();
            return false;
        }


        /// <summary> Compile a profile from the datapoints collected by this sequence. Returns true if it's actually possible.  </summary>
        /// <param name="forDevice"></param>
        /// <param name="rightHand"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public virtual bool CompileProfile(SGCore.DeviceType forDevice, bool rightHand, out SGCore.HandProfile profile)
        {
            profile = SGCore.HandProfile.Default(rightHand);
            return false;
        }

        /// <summary> Converts a calibration range taken from a Calibration sequence, and converts it into a profile. Use this if you're not actually using the sequence. </summary>
        /// <param name="range"></param>
        /// <param name="forDevice"></param>
        /// <param name="rightHand"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static bool CompileProfile(SensorRange range, SGCore.DeviceType forDevice, bool rightHand, out SGCore.HandProfile profile)
        {
            profile = SGCore.HandProfile.Default(rightHand); //create a default profile with the correct starting rotations and joint limits.

            if (forDevice == DeviceType.SENSEGLOVE)
            {
                SGCore.SG.SenseGlove.CalibrateInterpolation(range.MinValues, range.MaxValues, rightHand, ref profile.senseGloveProfile);
                return true;
            }
            else if (forDevice == DeviceType.NOVA)
            {
                SGCore.Nova.NovaGlove.CalibrateInterpolation(range.MinValues, range.MaxValues, ref profile.novaProfile);
                return true;
            }
            return false;
        }

    }



    /// <summary> A quick version that just requires you to open / close your hand for a few seconds, and compiles a profile out of that. </summary>
    public class HapticGlove_QuickCalibration : HapticGlove_CalibrationSequence
    {
        /// <summary> time, in seconds, after which this sequence stops gathering data. Based on the DeltaTime variable passed in the Update() function. </summary>
        public float autoEndAfter;

        /// <summary> Default time in seconds, to end this sequence. 15 seconds is very quick for some people. </summary>
        public const float autoEndTime = 10.0f;
        
        /// <summary> When compiling the final profile, we use a Weighted Moving Average filter with this period to filer out some outliers. </summary>
        public int smoothingSamples = 5;

        /// <summary> Time after you've first started moving. </summary>
        protected float movedTime;

        /// <summary> The min/max values recorded during this calibration sequence. </summary>
        public SensorRange sensorRange;
        /// <summary> A temporary profile to apply the sensorRange to. Used to generate a 'preview' HandPose. </summary>
        public SGCore.HandProfile tempProfile;
    
        /// <summary> Create a new instance of a QuickCalibration for Haptic Gloves. </summary>
        /// <param name="gloveToCalibrate"></param>
        /// <param name="endAfter_s"></param>
        public HapticGlove_QuickCalibration(SGCore.HapticGlove gloveToCalibrate, float endAfter_s = autoEndTime)
        {
            iGlove = gloveToCalibrate;
            autoEndAfter = endAfter_s;
            Reset();
        }

        /// <summary> This sequence autocmpletes after its autoEndTime has elapsed. </summary>
        public override bool AutoCompleted
        {
            get { return movedTime >= autoEndAfter && CanAnimate; }
        }

        /// <summary> Determines if the user has moved enough so that we can animate. </summary>
        public bool CanAnimate
        {
            get; set;
        }

        /// <summary> After we confirm the current step, we're basically done. </summary>
        public override void ConfirmCurrentStep()
        {
            this.ManualCompleted = true;
        }


        /// <summary> Resets datapoints, min/max ranges and profile. </summary>
        public override void Reset()
        {
            sensorRange = SensorRange.ForCalibration();
            tempProfile = iGlove != null ? SGCore.HandProfile.Default(iGlove.IsRight()) : null;
            CanAnimate = false;
            movedTime = 0;
            base.Reset();
        }

        /// <summary> Retrieve instructions on what to do during the current step.  </summary>
        /// <param name="nextStepKey"></param>
        /// <returns></returns>
        public override string GetCurrentInstuction(string nextStepKey = "")
        {
            if (Completed) { return "Calibration Completed!"; }
            else
            {
                if (nextStepKey.Length > 0)
                {
                    return "Move your fingers until you are satisfied with your calibration. Then press [" + nextStepKey + "] to finish";
                }
                return "Move your fingers until you are satisfied with your calibration.";
            }
        }

        public override void Update(float deltaTime_s)
        {
            base.Update(deltaTime_s);
            if (this.CanAnimate) //If we can animate, we've moved a minmum amount.
            {
                this.movedTime += Time.deltaTime;
            }
        }


        /// <summary> Add a new datapoint to this sequence's collection. Updates the range and profile used to generate a preview. </summary>
        /// <param name="calibrationData"></param>
        public override void AddDataPoint(Vect3D[] calibrationData)
        {
            base.AddDataPoint(calibrationData);
            this.sensorRange.CheckForExtremes(calibrationData); //recreate the temp rpfile?

            //can we animate?
            if (!CanAnimate)
            {
                //Debug.Log("CanAnimate?? " + this.sensorRange.RangeString());
                //CanAnimate = true;
                this.CanAnimate = iGlove != null && HapticGlove_CalCheck.MovedMinimum(this.sensorRange.Range, iGlove.GetDeviceType());
            }

            //Updates the temp profile
            if (tempProfile == null && iGlove != null) { tempProfile = SGCore.HandProfile.Default(iGlove.IsRight()); }
            if (this.iGlove != null && tempProfile != null)
            {
                SGCore.DeviceType type = iGlove.GetDeviceType();
                if (type == DeviceType.NOVA)
                {
                    SGCore.Nova.NovaGlove.CalibrateInterpolation(sensorRange.MinValues, sensorRange.MaxValues, ref tempProfile.novaProfile);
                }
                else if (type == DeviceType.SENSEGLOVE)
                {
                    SGCore.SG.SenseGlove.CalibrateInterpolation(sensorRange.MinValues, sensorRange.MaxValues, iGlove.IsRight(), ref tempProfile.senseGloveProfile);
                }
            }
        }


        /// <summary> Returns a 'preview' of what a HandPose with this sequence's current settings would look like.  </summary>
        /// <param name="rightHand"></param>
        /// <param name="currentPose"></param>
        /// <returns></returns>
        public override bool GetHandPose(bool rightHand, out HandPose currentPose)
        {
            if (iGlove != null && CanAnimate)
            {
                if (this.HandModel == null)
                {
                    this.HandModel = SGCore.Kinematics.BasicHandModel.Default(iGlove.IsRight());
                }
                return iGlove.GetHandPose(HandModel, tempProfile, out currentPose);
            }
            return base.GetHandPose(rightHand, out currentPose);
        }


        /// <summary> Compile a sensor range from the data points collected while the sequence was running. Smoothed by a Moving Average Filter. </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool CompileRange(out SensorRange range)
        {
            if (calibrationPoints.Count > 1)
            {
                Vect3D[] minValues = new Vect3D[5];
                Vect3D[] maxValues = new Vect3D[5];
                for (int f=0; f<minValues.Length; f++)
                {
                    minValues[f] = new Vect3D(calibrationPoints[0].calibrationValues[f]);
                    maxValues[f] = new Vect3D(calibrationPoints[0].calibrationValues[f]);
                }
                for (int i=1; i<calibrationPoints.Count; i++)
                {
                    for (int f = 0; f < minValues.Length; f++)
                    {
                        minValues[f] = new Vect3D
                        (
                            System.Math.Min(minValues[f].x, calibrationPoints[i].calibrationValues[f].x),
                            System.Math.Min(minValues[f].y, calibrationPoints[i].calibrationValues[f].y),
                            System.Math.Min(minValues[f].z, calibrationPoints[i].calibrationValues[f].z)
                        );
                        maxValues[f] = new Vect3D
                        (
                            System.Math.Max(maxValues[f].x, calibrationPoints[i].calibrationValues[f].x),
                            System.Math.Max(maxValues[f].y, calibrationPoints[i].calibrationValues[f].y),
                            System.Math.Max(maxValues[f].z, calibrationPoints[i].calibrationValues[f].z)
                        );
                    }
                }

                //at this point, we have the minimum and maximum values ever.
                range = new SensorRange(minValues, maxValues);
                return true;
            }
            return base.CompileRange(out range);
        }

    }





    /// <summary> A guided calibration which expects the user to make three poses with their hand. </summary>
    public class Nova_GuidedCalibrationSequence : HapticGlove_CalibrationSequence
    {

        /// <summary> Calibration Stage made for the Nova  </summary>
        public enum CalStage
        {
            Idle = 0,
            HandsFlat,
            ThumbsUp,
            Fist,
            Done
        }



        public CalStage CalibrationStage
        {
            get; protected set;
        }


        protected SensorRange tempProfile;



        public Nova_GuidedCalibrationSequence(SGCore.HapticGlove gloveToCalibrate)
        {
            this.iGlove = gloveToCalibrate;
            this.Reset();
        }


        public override int CurrentStageInt
        {
            get
            {
                return (int)this.CalibrationStage;
            }
        }


        public override string GetCurrentInstuction(string nextStepKey = "")
        {
            return CalibrationStage.ToString();
        }


        public static bool GetInstructions(CalStage currStage, out string instr)
        {
            //switch (currStage)
            //{
            //    case CalStage.HandsFlat:
            //        //calibrationPose = SGCore.HandPose.FlatHand(rightHand);
            //        break;
            //    case CalStage.ThumbsUp:
            //       // calibrationPose = SGCore.HandPose.ThumbsUp(rightHand);
            //        break;
            //    case CalStage.Fist:
            //        //calibrationPose = SGCore.HandPose.Fist(rightHand);
            //        break;
            //    default:
            //        //calibrationPose = SGCore.HandPose.DefaultIdle(rightHand);
            //        break;
            //}
            instr = currStage.ToString();
            return instr.Length > 0;
        }



        public override bool GetHandPose(bool rightHand, out HandPose currentPose)
        {
            return GetCalibrationPose(this.CalibrationStage, rightHand, out currentPose);
        }


        public static bool GetCalibrationPose(CalStage currStage, bool rightHand, out SGCore.HandPose calibrationPose)
        {
            switch (currStage)
            {
                case CalStage.HandsFlat:
                    calibrationPose = SGCore.HandPose.FlatHand(rightHand);
                    break;
                case CalStage.ThumbsUp:
                    calibrationPose = SGCore.HandPose.ThumbsUp(rightHand);
                    break;
                case CalStage.Fist:
                    calibrationPose = SGCore.HandPose.Fist(rightHand);
                    break;
                default:
                    calibrationPose = SGCore.HandPose.DefaultIdle(rightHand);
                    break;
            }
            return false;
        }


        public override void Reset()
        {
            base.Reset();
            this.CalibrationStage = CalStage.Idle;
        }


        public override void ConfirmCurrentStep()
        {
            CalibrationStage += 1;
        }

        public override bool Completed
        {
            get
            {
                return CalibrationStage >= CalStage.Done;
            }
        }


    }


}