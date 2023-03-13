using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{
	/// <summary> A script to detect whether or a transform is visible to the camera. </summary>
	public class SG_CameraFaceDetection : MonoBehaviour
	{
		///// <summary> The normal of the screen or face you're checking against the camera. </summary>
		//public SG.Util.MoveAxis facePlaneNormal = SG.Util.MoveAxis.Y;

		///// <summary> Camera head transform </summary>
		//public Transform hmdTransform;
		///// <summary> Which axis of the camera is considered "forward" a.k.a. moving along this axis of the hmdTransform gets you further away. </summary>
		//public SG.Util.MoveAxis hmdForward = MoveAxis.Z;


		///// <summary> Returns the angle between the hmdTransform and the facePlaneNormal. Is 0 when directly faced, and up to 180 degrees when  </summary>
		///// <returns></returns>
		//public virtual float CalculateFaceAngle()
  //      {
		//	if (this.hmdTransform != null)
		//	{
		//		return FaceAngle(this.transform, this.facePlaneNormal, this.hmdTransform, this.hmdForward);
		//	}
		//	return 0.0f;
		//}

		///// <summary> Calculates the angle (in degrees) between a face and a camera. </summary>
		///// <param name="faceTransform"></param>
		///// <param name="faceNormal"></param>
		///// <param name="hmdTransform"></param>
		///// <param name="hmdFwd"></param>
		///// <returns></returns>
		//public static float FaceAngle(Transform faceTransform, MoveAxis faceNormal, Transform hmdTransform, MoveAxis hmdFwd)
  //      {
		//	Vector3 myNormal = faceTransform.rotation * SG_Util.GetAxis(faceNormal); //3d Representation of my "up" vector.
		//	Vector3 camInward = hmdTransform.rotation * (SG_Util.GetAxis(hmdFwd) * -1); //We need to invert the camera's "fwd" axis; angle should be 0 if you're directly above it.
		//	return Vector3.Angle(myNormal, camInward);
  //      }


		//// Use this for initialization
		//protected virtual void Start()
		//{
		//	if (hmdTransform == null && Camera.main != null)
  //          {
		//		hmdTransform = Camera.main.transform;
  //          }
		//}

	}
}
