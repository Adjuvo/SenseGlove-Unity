using System.Collections.Generic;
using UnityEngine;

/// <summary> A SenseGlove_HandModel where the finger joints can be assigned via the inspector. </summary>
public class SenseGlove_VirtualHand : SenseGlove_HandModel
{
    /// <summary> The joint transforms of the thumb. </summary>
    [Header("Finger Joints")]
    [Tooltip("The joint transforms of the thumb.")]
    public List<Transform> thumbJoints = new List<Transform>(3);

    /// <summary> The joint transforms of the index finger. </summary>
    [Tooltip(" The joint transforms of the index finger. ")]
    public List<Transform> indexFingerJoints = new List<Transform>(3);

    /// <summary>The joint transforms of the middle finger. </summary>
    [Tooltip("The joint transforms of the middle finger.")]
    public List<Transform> middleFingerJoints = new List<Transform>(3);

    /// <summary> The joint transforms of the ring finger. </summary>
    [Tooltip("The joint transforms of the ring finger.")]
    public List<Transform> ringFingerJoints = new List<Transform>(3);

    /// <summary> The joint transforms of the pinky. </summary>
    [Tooltip("The joint transforms of the pinky.")]
    public List<Transform> pinkyJoints = new List<Transform>(3);

    /// <summary> Take the joints assigned via the inspector and add them to the list of fingerJoints. </summary>
    protected override void CollectFingerJoints()
    {
        this.fingerJoints.Clear();
        this.fingerJoints.Add(this.thumbJoints);
        this.fingerJoints.Add(this.indexFingerJoints);
        this.fingerJoints.Add(this.middleFingerJoints);
        this.fingerJoints.Add(this.ringFingerJoints);
        this.fingerJoints.Add(this.pinkyJoints);
    }

}
