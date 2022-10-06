using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> A single finger for a SenseGlove DK1 </summary>
	public class SG_DK1Finger : MonoBehaviour
	{
		public SGCore.Finger linkedTo = SGCore.Finger.Thumb;

		public Transform[] joints = new Transform[0];


		public void UpdateFinger(SGCore.SG.SG_GlovePose glovePose, Transform gloveOrigin)
        {
			UpdateFinger(glovePose, gloveOrigin.position, gloveOrigin.rotation);
        }


		public void UpdateFinger(SGCore.SG.SG_GlovePose glovePose, Vector3 gloveOriginPos, Quaternion gloveOriginRot)
		{
			int f = (int)this.linkedTo;
			if (glovePose.JointPositions.Length > f && glovePose.JointRotations.Length > f)
			{
				for (int j = 0; j < glovePose.JointPositions[f].Length && j < glovePose.JointRotations[f].Length && j < this.joints.Length; j++)
				{
					if (this.joints[j] != null)
                    {
						this.joints[j].rotation = gloveOriginRot * SG.Util.SG_Conversions.ToUnityQuaternion(glovePose.JointRotations[f][j]);
						this.joints[j].position = gloveOriginPos + (gloveOriginRot * SG.Util.SG_Conversions.ToUnityPosition(glovePose.JointPositions[f][j]));
                    }
				}
			}
		}
	}
}