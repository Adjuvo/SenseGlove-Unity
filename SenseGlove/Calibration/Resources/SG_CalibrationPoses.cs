using UnityEngine;
using SenseGloveCs.Kinematics;

namespace SG.Calibration
{ 
    /// <summary> Configurable Calibration poses for SenseGlove solvers. Tweak at thyne own risk. </summary>
    public class CalibrationPose
    {
        /// <summary> Useful indices for interpolation </summary>
        protected static readonly int x0 = 0, x1 = 1, none = -1, y0 = 0, y1 = 1;
        /// <summary> Useful indices for movements </summary>
        protected static readonly int abd = 2, flex = 1, tw = 0;


        /// <summary> indicates this pose is meant to calibrate the x0 (0) or x1 (1) value for this finger, or no value at all (-1) </summary>
        int[][] xAffect;

        /// <summary> Which value to use for calibration (flexion, abduction, twist, none) </summary>
        int[][] calbrUsing; //which value (XYZ) to use


        /// <summary> indicates this pose is meant to calibrate the y0 (0) or y1 (1) value for this finger, or no value at all (-1) </summary>
        int[][] yAffect;
        /// <summary> //the y value to set, in case this motion sets the output (y) component. </summary>
        float[][] yValue; 


        /// <summary> Create a new pose that does not affect output (y) components </summary>
        /// <param name="affects"></param>
        /// <param name="valueIndices"></param>
        public CalibrationPose(int[][] affects, int[][] valueIndices)
        {
            xAffect = affects;
            calbrUsing = valueIndices;
            yAffect = new int[0][];
            yValue = new float[0][];
        }

        /// <summary> Create a new pose that affects output (y) components  </summary>
        /// <param name="affects"></param>
        /// <param name="valueIndices"></param>
        /// <param name="yAffects"></param>
        /// <param name="yValues"></param>
        public CalibrationPose(int[][] affects, int[][] valueIndices, int[][] yAffects, float[][] yValues)
        {
            xAffect = affects;
            calbrUsing = valueIndices;
            yAffect = yAffects;
            yValue = yValues;
        }


        /// <summary> Utility function that creates an array of integers of the appropriate size, with default values -1 (none) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        protected static int[][] SetupArray(ref InterpolationSet_IMU interpolator)
        {
            int[][] res = new int[interpolator.myAngles.Length][];
            for (int f = 0; f < interpolator.myAngles.Length; f++)
            {
                res[f] = new int[interpolator.myAngles[f].Length];
                for (int j = 0; j < res[f].Length; j++)
                    res[f][j] = none;
            }
            return res;
        }

        /// <summary> Utility function that creates an array of floats of the appropriate size, with default values 0 </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        protected static float[][] SetupFloatArray(ref InterpolationSet_IMU interpolator)
        {
            float[][] res = new float[interpolator.myAngles.Length][];
            for (int f = 0; f < interpolator.myAngles.Length; f++) { res[f] = new float[interpolator.myAngles[f].Length]; }
            return res;
        }




        /// <summary> Calibrate all parameters of an InterpolationSet, based on this pose's parameters and a set of input values. </summary>
        /// <param name="calibrationValues"></param>
        /// <param name="interpolator"></param>
        public void CalibrateParameters(Vector3[] calibrationValues, ref InterpolationSet_IMU interpolator)
        {
            for (int f = 0; f < interpolator.myAngles.Length; f++)
            {
                for (int j = 0; j < interpolator.myAngles[f].Length; j++)
                {
                    if (calbrUsing[f][j] != none)
                    {
                        float value = calibrationValues[f][calbrUsing[f][j]];
                        if (xAffect[f][j] == x0 && interpolator.myAngles[f][j].x1 != value)
                        {
                            interpolator.myAngles[f][j].x0 = value;
                        }
                        else if (xAffect[f][j] == x1 && interpolator.myAngles[f][j].x0 != value) //chekcing for nullrefs
                        {
                            interpolator.myAngles[f][j].x1 = value;
                        }

                        if (yAffect.Length > f && yAffect[f].Length > j && yAffect[f][j] != none) //activating y
                        {
                            if (yValue.Length > f && yValue[f].Length > j)
                            {
                                if (yAffect[f][j] == y0)
                                {
                                    interpolator.myAngles[f][j].x0 = yValue[f][j];
                                }
                                else if (yAffect[f][j] == y1)
                                {
                                    interpolator.myAngles[f][j].x1 = yValue[f][j];
                                }
                            }
                        }
                    }
                }
            }
        }



        /// <summary> Generates a calibration pose that corresponds to all fingers flexed (finger flexion calibration). </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetFist(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            //affects x1 of flexion / extension, using y (flexion) as input
            for (int f = 1; f < affects.Length; f++)
            {
                affects[f][(int)IntAngles_IMU_Finger.MCP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.PIP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.PIP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.DIP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.DIP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to all fingers extended (finger extension calibration). </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetOpenHand(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);

            for (int f = 1; f < affects.Length; f++)
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Finger.MCP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.PIP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.PIP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.DIP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.DIP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to a thumb up (thumb extended calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetThumbsUp(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Thumb.CMC_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.MCP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.IP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.IP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to a flexed thumb (thumb flexed calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetThumbFlexed(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Thumb.CMC_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.MCP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.IP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.IP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to a thumb moved outwards (thumb abduction calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetThumbAbd(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x1 of abduction / adduction
                affects[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = abd; //abduction
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to a thumb flat against the hand palm (thumb adduction calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetThumbNoAbd(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x0 of abduction / adduction
                affects[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = abd; //abduction
            }
            return new CalibrationPose(affects, calibrateW/*, yAffect, yValues*/);
        }




        /// <summary> Generates a calibration pose that corresponds to a fully opened hand (finger extension, thumb adduction calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetFullOpen(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Thumb.CMC_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.MCP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.IP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.IP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = x0;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = abd; //abduction
            }
            //for the rest of the fingers
            for (f = 1; f < affects.Length; f++)
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Finger.MCP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.PIP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.PIP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.DIP_FE] = x0;
                calibrateW[f][(int)IntAngles_IMU_Finger.DIP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

        /// <summary> Generates a calibration pose that corresponds to a fully gclosed hand (finger flexion, thumb abduction calibration) </summary>
        /// <param name="interpolator"></param>
        /// <returns></returns>
        public static CalibrationPose GetFullFist(ref InterpolationSet_IMU interpolator)
        {
            int[][] affects = SetupArray(ref interpolator);
            int[][] calibrateW = SetupArray(ref interpolator);
            int f = 0; //thumb
            {
                //affects x0 of flexion / extension
                affects[f][(int)IntAngles_IMU_Thumb.CMC_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.MCP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Thumb.IP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.IP_FE] = flex;

                //affects x1 of abduction / adduction
                affects[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = x1;
                calibrateW[f][(int)IntAngles_IMU_Thumb.CMC_Abd] = abd; //abduction
            }
            //for the rest of the fingers
            for (f = 1; f < affects.Length; f++)
            {
                affects[f][(int)IntAngles_IMU_Finger.MCP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.MCP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.PIP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.PIP_FE] = flex;

                affects[f][(int)IntAngles_IMU_Finger.DIP_FE] = x1;
                calibrateW[f][(int)IntAngles_IMU_Finger.DIP_FE] = flex;
            }
            return new CalibrationPose(affects, calibrateW);
        }

    }

}