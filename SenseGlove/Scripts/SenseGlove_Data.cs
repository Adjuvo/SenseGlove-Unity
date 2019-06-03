using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> Unity wrapper for the GloveData, which contains all a developer will need. </summary>
public class SenseGlove_Data
{
    //-------------------------------------------------------------------------------------------------------------------
    // General Variables

    /// <summary> Determines if the glove-specific data has been loaded yet. </summary>
    public bool dataLoaded = false;
    /// <summary> Check whether or not this is a left-handed or right-handed glove.  </summary>
    public GloveSide gloveSide;

    /// <summary> The unique ID of this SenseGlove. </summary>
    public string deviceID;
    /// <summary> The hardware version of this SenseGlove. </summary>
    public string gloveVersion;
    /// <summary> The version of the firmware that runs on this SenseGlove's Microcontroller </summary>
    public float firmwareVersion;

    //-------------------------------------------------------------------------------------------------------------------
    // Sensor Variables

    /// <summary> The angles between glove segments, as calculated by the firmware. Sorted by finger, from proximal to distal. </summary>
    public float[][] gloveValues;

    /// <summary> Teh number of sensors on this Sense Glove.  </summary>
    public int numberOfSensors;

    /// <summary> The raw x y z w values of the IMU within the SenseGlove. </summary>
    public float[] imuValues;

    /// <summary> 
    /// The IMU Calibration values for System, Gyro-, Accelero- and Magnetometer. 
    /// These vary from -1 (N/A) and from 0 (not calibrated) to 3 (fully calibrated) 
    /// </summary>
    public int[] imuCalibration;

    /// <summary> The amount of sensor packets the senseglove is sending to your system. </summary>
    public int packetsPerSecond = 0;


    //-------------------------------------------------------------------------------------------------------------------
    // Hand Variables

    /// <summary> The position in mm of the common origin of the Hand and Glove, relative to the wrist. </summary>
    public Vector3 commonOriginPos;

    /// <summary> The orientation of the common origin of the Hand and Glove, relative to the wrist. </summary>
    public Quaternion commonOriginRot;


    /// <summary> The euler angles between glove sections relative to its previous section, sorted by finger, from proximal to distal. </summary>
    public Vector3[][] gloveAngles;

    /// <summary> The lengths of each glove section, sorted by finger, from proximal to distal.  </summary>
    public Vector3[][] gloveLengths;

    /// <summary> 
    /// The positions of the glove joints and thimble in mm, relative to the common origin. 
    /// Sorted by finger, from proximal to distal.  
    /// </summary>
    public Vector3[][] glovePositions;

    /// <summary> 
    /// The orientation of the glove joints and thimble, relative to the common origin. 
    /// Sorted by finger, from proximal to distal.  
    /// </summary>
    public Quaternion[][] gloveRotations;


    /// <summary> 
    /// The euler angles [pronation/supination, abduction/adduction, flexion/extension] between finger joints relative to the previous bone, 
    /// Sorted by finger, from proximal to distal. 
    /// </summary>
    public Vector3[][] handAngles;

    /// <summary> The lengths, in mm, of the finger phalanges. Sorted by finger, from proximal to distal. </summary>
    public Vector3[][] handLengths;

    /// <summary> 
    /// The positions of the hand joints fingertips, in mm, relative to the common origin. 
    /// Sorted by finger, from proximal to distal.  
    /// </summary>
    public Vector3[][] handPositions;

    /// <summary> 
    /// The orientation of the hand joints and fingertips, relative to the common origin. 
    /// Sorted by finger, from proximal to distal.  
    /// </summary>
    public Quaternion[][] handRotations;


    //-------------------------------------------------------------------------------------------------------------------
    // Wrist Variables

    /// <summary> The absolute IMU orientation of the wrist. </summary>
    public Quaternion absoluteWrist;

    /// <summary> The wrist orientation relative to the foreArm. </summary>
    public Quaternion relativeWrist;

    /// <summary> The absolute wrist angles, corrected with foreArm calibration. </summary>
    public Quaternion absoluteCalibratedWrist;

    //-------------------------------------------------------------------------------------------------------------------
    // Calibration Variables

    /// <summary> The current step of the calibration algorithm. </summary>
    public int calibrationStep = -1;

    /// <summary> The total number of steps of the calibration algorithm. </summary>
    public int totalCalibrationSteps = 0;


    //-------------------------------------------------------------------------------------------------------------------
    // Constructor

    /// <summary> Create an instance of SenseGlove_Data with default values. </summary>
    private SenseGlove_Data()
    {
        Vector3[] zero = new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero };
        Vector3[][] multiZero = new Vector3[5][] { zero, zero, zero, zero, zero };

        Quaternion[] ident = new Quaternion[] { Quaternion.identity, Quaternion.identity, Quaternion.identity };
        Quaternion[][] multiIdent = new Quaternion[5][] { ident, ident, ident, ident, ident };

        this.absoluteCalibratedWrist = Quaternion.identity;
        this.absoluteWrist = Quaternion.identity;
        this.calibrationStep = -1;
        this.commonOriginPos = Vector3.zero;
        this.commonOriginRot = Quaternion.identity;
        this.dataLoaded = false;
        this.deviceID = "N\\A";
        this.firmwareVersion = -1.0f;
        this.gloveAngles = multiZero;
        this.gloveLengths = multiZero;
        this.glovePositions = multiZero;
        this.gloveRotations = multiIdent;
        this.gloveSide = GloveSide.Unknown;
        this.gloveValues = new float[0][] { };
        this.gloveVersion = "N\\A";
        this.handAngles = multiZero;
        this.handLengths = multiZero;
        this.handPositions = multiZero;
        this.handRotations = multiIdent;
        this.imuCalibration = new int[4] { -1, -1, -1, -1 };
        this.imuValues = new float[] { 0, 0, 0, 1 };
        this.numberOfSensors = 0;
        this.packetsPerSecond = 0;
        this.relativeWrist = Quaternion.identity;
        this.totalCalibrationSteps = 0;
    }

    /// <summary> Retrieve an unloaded set of data, which indictates that this glove has not been loaded yet. </summary>
    /// <remarks> Allows access to the empty Constructor without exposing it. </remarks>
    public static SenseGlove_Data Empty { get { return new SenseGlove_Data(); }  }


    /// <summary> Extract right-handed coordinate system data from the SenseGlove DLL and convert it into Unity values. </summary>
    /// <param name="data"></param>
    /// <param name="packets"></param>
    /// <param name="totalCSteps"></param>
    /// <param name="currCStep"></param>
    public SenseGlove_Data(SenseGloveCs.GloveData data)
    {
        if (data != null)
        {
            this.gloveSide = SenseGlove_Data.GetSide(data.kinematics.isRight);

            this.deviceID = data.deviceID;
            this.firmwareVersion = data.firmwareVersion;
            this.gloveVersion = data.deviceVersion;
            
            this.packetsPerSecond = data.samplesPerSec;

            this.commonOriginPos = SenseGlove_Util.ToUnityPosition(data.kinematics.gloveRelPos);
            this.commonOriginRot = SenseGlove_Util.ToUnityQuaternion(data.kinematics.gloveRelRot);

            this.UpdateVariables(data);
        }
    }

    /// <summary> Updates all variables that can change during the simulation.</summary>
    /// <param name="data"></param>
    public void UpdateVariables(SenseGloveCs.GloveData data)
    {
        this.dataLoaded = data.dataLoaded;
        this.deviceID = data.deviceID;
        this.gloveValues = data.gloveValues;
        this.imuValues = data.imuValues;
        this.imuCalibration = data.imuCalibration;

        this.numberOfSensors = data.numberOfSensors;

        this.packetsPerSecond = data.samplesPerSec;

        this.calibrationStep = data.currentCalStep;
        this.totalCalibrationSteps = data.totalCalSteps;

        this.absoluteCalibratedWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.QcalibratedAbs);
        this.absoluteWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.QwristAbs);
        this.relativeWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.Qrelative);

        SenseGlove_Data.GetChainVariables(ref data.kinematics.gloveLinks, ref this.glovePositions,
                ref this.gloveAngles, ref this.gloveRotations, ref this.gloveLengths);

        SenseGlove_Data.GetChainVariables(ref data.kinematics.fingers, ref this.handPositions,
            ref this.handAngles, ref this.handRotations, ref this.handLengths);
    }


    //-------------------------------------------------------------------------------------------------------------------
    // Accessors

    /// <summary> Retrieve the total glove angles, used for gesture recognition (for each finger; pronation, abduction, flexion). </summary>
    /// <returns></returns>
    public Vector3[] TotalGloveAngles()
    {
        Vector3[] res = new Vector3[5];
        for (int f=0; f<5; f++)
        {
            res[f] = Vector3.zero;
            if (this.gloveAngles.Length > f)
            {
                for (int i=0; i<this.gloveAngles[f].Length - 1; i++) //-1 so we skip the "fingertip" position, that will always be 0, 0, 0.
                {
                    res[f] += this.gloveAngles[f][i];
                }
            }
        }
        return res;
    }



    /// <summary> Retrieve the finger lengths of this GloveData </summary>
    /// <returns></returns>
    public float[][] GetFingerLengths()
    {
        float[][] res = new float[5][];
        for (int f = 0; f < 5; f++)
        {
            res[f] = new float[3];
            for (int j = 0; j < 3; j++)
            {
                res[f][j] = this.handLengths[f][j][0]; //all x lengths
            }
        }
        return res;
    }

    /// <summary> Retrieve the joint positions </summary>
    /// <returns></returns>
    public Vector3[] GetJointPositions()
    {
        Vector3[] res = new Vector3[5];
        for (int f = 0; f < 5; f++)
        {
            res[f] = this.handPositions[f][0];
        }
        return res;
    }


    //-------------------------------------------------------------------------------------------------------------------
    // Bulk Conversion Methods

    /// <summary> Retrieve the Glove Side of this Sense Glove. </summary>
    /// <param name="isRight"></param>
    /// <returns></returns>
    protected static GloveSide GetSide(bool isRight)
    {
        return isRight ? GloveSide.RightHand : GloveSide.LeftHand;
    }

    /// <summary>
    /// Fill a number of arrays with data from a single kinematic chain.
    /// </summary>
    /// <param name="chains"></param>
    /// <param name="positions"></param>
    /// <param name="angles"></param>
    /// <param name="rotations"></param>
    /// <param name="lengths"></param>
    protected static void GetChainVariables(ref SenseGloveCs.Kinematics.JointChain[] chains, ref Vector3[][] positions, ref Vector3[][] angles, ref Quaternion[][] rotations, ref Vector3[][] lengths)
    {
        int N = chains.Length;

        angles = new Vector3[N][];
        lengths = new Vector3[N][];
        positions = new Vector3[N][];
        rotations = new Quaternion[N][];

        for (int f = 0; f < N; f++)
        {
            SenseGlove_Data.GetLinkVariables(ref chains[f], ref positions[f], ref angles[f], ref rotations[f], ref lengths[f]);
        }
    }

    /// <summary> Fill the appropriate unity Quaternion and Vector3 arrays based on a single joing chain (finger or glove semgent) </summary>
    /// <param name="chain"></param>
    /// <param name="positions"></param>
    /// <param name="angles"></param>
    /// <param name="rotations"></param>
    /// <param name="lengths"></param>
    protected static void GetLinkVariables(ref SenseGloveCs.Kinematics.JointChain chain, ref Vector3[] positions, ref Vector3[] angles, ref Quaternion[] rotations, ref Vector3[] lengths)
    {
        int n = chain.joints.Length;
        positions = new Vector3[n];
        angles = new Vector3[n];
        rotations = new Quaternion[n];
        lengths = new Vector3[n - 1];

        for (int j=0; j<n; j++)
        {
            positions[j] = SenseGlove_Util.ToUnityPosition(chain.joints[j].position);
            angles[j] = SenseGlove_Util.ToUnityEuler(chain.joints[j].relativeAngle);
            rotations[j] = SenseGlove_Util.ToUnityQuaternion(chain.joints[j].rotation);
            if (j < n - 1)
            {
                lengths[j] = SenseGlove_Util.ToUnityPosition(chain.lengths[j]);
            }
        }

    }
    
    
}

/// <summary> Whether this glove is left- or right handed. </summary>
public enum GloveSide
{
    /// <summary> No data about this glove is available yet. </summary>
    Unknown = 0,
    /// <summary> This is a right hand. </summary>
    RightHand,
    /// <summary> This is a left hand. </summary>
    LeftHand
}

