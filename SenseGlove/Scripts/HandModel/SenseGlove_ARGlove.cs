using UnityEngine;

/// <summary> A type of handModel that follows only glove hardware; with feedback colliders at the tips of the Thimbles. </summary>
public class SenseGlove_ARGlove : SenseGlove_HandModel
{

    /// <summary> The five end-effectors of the Sense Glove, who can be parented to pickup- and/or feedback colliders. </summary>
    [Header("AR Glove")]
    [Tooltip("The five end-effectors of the Sense Glove, who can be parented to pickup- and/or feedback colliders.")]
    [SerializeField]
    protected Transform[] thimbles = new Transform[0];

    protected override void CollectFingerJoints()
    {
        //does nothing(?)
        if (thimbles == null)
        {
            thimbles = new Transform[0];
        }
    }


    public override void UpdateHand(SenseGlove_Data data)
    {
        if (data.dataLoaded)
        {
            for (int f = 0; f < this.thimbles.Length && f < 5; f++)
            {
                int L = data.gloveRotations[f].Length - 1;
                this.thimbles[f].localRotation = data.gloveRotations[f][L];
                this.thimbles[f].localPosition = data.glovePositions[f][L];

                Transform baseT = this.wristTransfrom.GetChild(0);

                //Debug: Draw the glove's parts(?)
                Vector3[] poses = new Vector3[data.glovePositions[f].Length]; //-1 because we already have the last position

                poses[0] = baseT.TransformPoint(data.glovePositions[f][0]);
                for (int i=1; i<poses.Length; i++)
                {
                    poses[i] = baseT.TransformPoint(data.glovePositions[f][i]);
                    Debug.DrawLine(poses[i - 1], poses[i], Color.white);
                }
            }

        }
    }



}
