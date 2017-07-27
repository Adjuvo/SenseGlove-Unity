using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> Unity wrapper for the GloveData, which contains all a developer will need. </summary>
public class SenseGlove_Data
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // General Variables

    /// <summary> Determines if the glove-specific data has been loaded yet. </summary>
    public bool dataLoaded = false;
    /// <summary> Check whether or not this is a left-handed or right-handed glove.  </summary>
    public bool isRight;

    /// <summary> The unique ID of this SenseGlove. </summary>
    public string deviceID;
    /// <summary> The hardware version of this SenseGlove. </summary>
    public string gloveVersion;
    /// <summary> The version of the firmware that runs on this SenseGlove's Microcontroller </summary>
    public string firmwareVersion;



    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Sensor Variables

    /// <summary> The angles between glove segments, as calculated by the firmware. Sorted by finger, from proximal to distal. </summary>
    public float[][] gloveValues;

    /// <summary> The raw x y z w values of the IMU within the SenseGlove. </summary>
    public float[] imuValues;

    /// <summary> The amount of sensor packets the senseglove is sending to your system. </summary>
    public int packetsPerSecond = 0;


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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



    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Wrist Variables

    /// <summary> The absolute IMU orientation of the wrist. </summary>
    public Quaternion absoluteWrist;

    /// <summary> The wrist orientation relative to the foreArm. </summary>
    public Quaternion relativeWrist;

    /// <summary> The absolute wrist angles, corrected with foreArm calibration. </summary>
    public Quaternion absoluteCalibratedWrist;


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Constructor

    
    /// <summary>
    /// Extract right-handed coordinate system data from the SenseGlove DLL and convert it into Unity values.
    /// </summary>
    /// <param name="data"></param>
    public SenseGlove_Data(SenseGloveCs.GloveData data, int packets)
    {
        if (data != null)
        {
            this.dataLoaded = data.dataLoaded;
            this.isRight = data.isRight;

            this.deviceID = data.deviceID;
            this.firmwareVersion = data.firmwareVersion;
            this.gloveVersion = data.gloveVersion;

            this.gloveValues = data.gloveValues;
            this.imuValues = data.imuValues;
            this.packetsPerSecond = packets;

            this.absoluteCalibratedWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.QcalibratedAbs);
            this.absoluteWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.QwristAbs);
            this.relativeWrist = SenseGlove_Util.ToUnityQuaternion(data.wrist.Qrelative);

            this.commonOriginPos = SenseGlove_Util.ToUnityPosition(data.handModel.gloveRelPos);
            this.commonOriginRot = SenseGlove_Util.ToUnityQuaternion(data.handModel.gloveRelOrient);

            this.gloveAngles = SenseGlove_Data.ToEuler(data.handModel.gloveAngles);
            this.gloveLengths = SenseGlove_Data.ToVector3(data.handModel.gloveLengths);
            this.gloveRotations = SenseGlove_Data.ToQuaternion(data.handModel.gloveRotations);
            this.glovePositions = SenseGlove_Data.ToVector3(data.handModel.glovePositions);

            this.handAngles = SenseGlove_Data.ToEuler(data.handModel.handAngles);
            this.handLengths = SenseGlove_Data.ToVector3(data.handModel.handLengths);
            this.handRotations = SenseGlove_Data.ToQuaternion(data.handModel.handRotations);
            this.handPositions = SenseGlove_Data.ToVector3(data.handModel.handPositions);
        }
    }
  

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Bulk Conversion Methods


    /// <summary>
    /// Convert a collection of right-handed positions stored in a float[][][] into a Unity Vector3[][].
    /// </summary>
    /// <param name="positions"></param>
    /// <returns></returns>
    private static Vector3[][] ToVector3(float[][][] positions)
    {
        Vector3[][] res = new Vector3[positions.Length][];
        for (int f=0; f<positions.Length; f++)
        {
            res[f] = new Vector3[positions[f].Length];
            for (int i=0; i<res[f].Length; i++)
            {
                res[f][i] = SenseGlove_Util.ToUnityPosition(positions[f][i]);
            }
        }
        return res;
    }

    /// <summary>
    /// Convert a collection of right handed euler angles stored in a float[][][] into a Unity Vector3[][].
    /// </summary>
    /// <param name="eulers"></param>
    /// <returns></returns>
    private static Vector3[][] ToEuler(float[][][] eulers)
    {
        Vector3[][] res = new Vector3[eulers.Length][];
        for (int f = 0; f < eulers.Length; f++)
        {
            res[f] = new Vector3[eulers[f].Length];
            for (int i = 0; i < res[f].Length; i++)
            {
                res[f][i] = SenseGlove_Util.ToUnityEuler(eulers[f][i]);
            }
        }
        return res;
    }

    /// <summary>
    /// Convert a collection of right-handed Quaternion rotations stored in a float[][][] into a Unity Quaternion[][].
    /// </summary>
    /// <param name="rotations"></param>
    /// <returns></returns>
    private static Quaternion[][] ToQuaternion(float[][][] rotations)
    {
        Quaternion[][] res = new Quaternion[rotations.Length][];
        for (int f = 0; f < rotations.Length; f++)
        {
            res[f] = new Quaternion[rotations[f].Length];
            for (int i = 0; i < res[f].Length; i++)
            {
                res[f][i] = SenseGlove_Util.ToUnityQuaternion(rotations[f][i]);
            }
        }
        return res;
    }

   
}
