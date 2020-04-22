using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A script to assign information of hand joints, used by other scripts that use hand tracking. </summary>
public class SG_HandModelInfo : MonoBehaviour
{
    /// <summary> The forearm of the hand model, usually the parent of the wrist transform. </summary>
    public Transform foreArmTransform;
    /// <summary> The transform of the wrist. Should be distinct from the foreArmTransform if wrist animation is not required. </summary>
    public Transform wristTransform;

    /// <summary> The thumb joint transforms, preferably including the fingertip. </summary>
    public Transform[] thumbJoints = new Transform[0];
    /// <summary> The index joint transforms, preferably including the fingertip. </summary>
    public Transform[] indexJoints = new Transform[0];
    /// <summary> The middle joint transforms, preferably including the fingertip. </summary>
    public Transform[] middleJoints = new Transform[0];
    /// <summary> The ring joint transforms, preferably including the fingertip. </summary>
    public Transform[] ringJoints = new Transform[0];
    /// <summary> The pinky joint transforms, preferably including the fingertip. </summary>
    public Transform[] pinkyJoints = new Transform[0];

    /// <summary> Debug objects to show the user where the finger joint transforms are. </summary>
    protected GameObject[][] fingerDebug = null;
    /// <summary> Debug objects to show the user where the wrist transform is </summary>
    protected GameObject wristDebug = null;



    /// <summary> Retreive all finger joints as an array of Transforms, sorted from thumb to pinky. </summary>
    public Transform[][] FingerJoints
    {
        get
        {
            Transform[][]  res = new Transform[5][];
            res[0] = thumbJoints;
            res[1] = indexJoints;
            res[2] = middleJoints;
            res[3] = ringJoints;
            res[4] = pinkyJoints;
            return res;
        }
    }



    /// <summary> Retrieve the fingertip transform of this Hand Model. </summary>
    /// <param name="finger"></param>
    /// <param name="fingerTip"></param>
    /// <returns></returns>
    public bool GetFingerTip(SG_HandSection finger, out Transform fingerTip)
    {
        int f = (int)finger;
        if (f > -1 && f < 5)
        {
            Transform[][] joints = FingerJoints;
            fingerTip = joints[f].Length > 3 ? joints[f][3] : null;
        }
        else { fingerTip = null; }
        return fingerTip != null;
    }

    /// <summary> Create/Destroy a set of small spheres on each of the hand model transforms. </summary>
    public bool DebugEnabled
    {
        get
        {
            return fingerDebug != null;
        }
        set
        {
            if (value && fingerDebug == null) //create
            {
                Transform[][] joints = this.FingerJoints;
                fingerDebug = new GameObject[joints.Length][];
                for (int f=0; f<joints.Length; f++)
                {
                    fingerDebug[f] = new GameObject[joints[f].Length];
                    for (int j=0; j<joints[f].Length; j++)
                    {
                        fingerDebug[f][j] = SG_Util.SpawnSphere(0.01f, joints[f][j], false);
                    }
                }
                if (wristTransform != null) { wristDebug = SG_Util.SpawnSphere(0.015f, wristTransform, false); }
            }
            else if (!value && fingerDebug != null) //destroy
            {
                for (int f = 0; f < fingerDebug.Length; f++)
                {
                    for (int j = 0; j < fingerDebug[f].Length; j++)
                    {
                        GameObject.Destroy(fingerDebug[f][j]);
                    }
                }
                fingerDebug = null;
                if (wristDebug != null) { GameObject.Destroy(wristDebug); }
            }
        }
    }


}
