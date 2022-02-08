using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> Utility Class to move objects from a variable start- to end location using an AnimationCurve. </summary>
	public class SG_SmoothMovement : MonoBehaviour
	{
		/// <summary> The time it takes to complete this movement. </summary>
		public float movementTime = 1;

		/// <summary> The movement controller for this script. X is [0...movementTime], Y is [start..end positions]. Should start at 0, 0 and end at 1, 1. </summary>
		public AnimationCurve movementProfile = AnimationCurve.EaseInOut(0, 0, 1, 1);

		/// <summary> Take a time in seconds, and map it to the animationCurve 0 ... 1 </summary>
		/// <param name="timeInSec"></param>
		/// <returns></returns>
		public virtual float NormalizeTime(float timeInSec)
        {
			return this.movementTime != 0 ? Mathf.Clamp01( timeInSec / movementTime ) : 1;
        }

		/// <summary> Converts a time in seconds to 0..1, then evaluate it on the movementProfile. </summary>
		/// <param name="timeInSec"></param>
		/// <returns></returns>
		public virtual float ToAnimationTime(float timeInSec)
        {
			float animX = NormalizeTime(timeInSec); //animation profile x value
			return this.movementProfile.Evaluate(animX);
        }


		/// <summary> Calculate the location of obj at elapsedTime, using its start- and end location as input. </summary>
		/// <param name="objStartPos"></param>
		/// <param name="objStartRot"></param>
		/// <param name="objTarget"></param>
		/// <param name="elapsedTime"></param>
		/// <param name="objPosition"></param>
		/// <param name="objRotation"></param>
		public void CalculateLocation(Vector3 objStartPos, Quaternion objStartRot, Transform objTarget, float elapsedTime, out Vector3 objPosition, out Quaternion objRotation)
        {
			CalculateLocation(objStartPos, objStartRot, objTarget.position, objTarget.rotation, elapsedTime, out objPosition, out objRotation);
        }


		/// <summary> Calculate the location of obj at elapsedTime,  using its start- and end location as input.  </summary>
		/// <param name="obj"></param>
		/// <param name="target"></param>
		/// <param name="elapsedTime"></param>
		/// <param name="objLocation"></param>
		/// <param name="objRotation"></param>
		/// <param name="objStartPos"></param>
		/// <param name="objStartRot"></param>
		/// <returns></returns>
		public virtual void CalculateLocation(Vector3 objStartPos, Quaternion objStartRot, Vector3 objEndPos, Quaternion objEndRot, float elapsedTime, out Vector3 objPosition, out Quaternion objRotation)
        {
			float tAnim = ToAnimationTime(elapsedTime);
			Util.SG_Util.LerpLocation(tAnim, objStartPos, objStartRot, objEndPos, objEndRot, out objPosition, out objRotation);
        }



		/// <summary> Calculates a new location and applies it </summary>
		/// <param name="objToMove"></param>
		/// <param name="objStartPos"></param>
		/// <param name="objStartRot"></param>
		/// <param name="objEndPos"></param>
		/// <param name="objEndRot"></param>
		/// <param name="elapsedTime"></param>
		public virtual void UpdateLocation(Transform objToMove, Vector3 objStartPos, Quaternion objStartRot, Vector3 objEndPos, Quaternion objEndRot, float elapsedTime)
        {
			float tAnim = ToAnimationTime(elapsedTime);
			Vector3 currPos; Quaternion currRot;
			CalculateLocation(objStartPos, objStartRot, objEndPos, objEndRot, elapsedTime, out currPos, out currRot);
			objToMove.rotation = currRot;
			objToMove.position = currPos;
		}


		/// <summary> Calculate a new location and apply it. </summary>
		/// <param name="objToMove"></param>
		/// <param name="objTarget"></param>
		/// <param name="objStartPos"></param>
		/// <param name="objStartRot"></param>
		/// <param name="elapsedTime"></param>
		public virtual void UpdateLocation(Transform objToMove, Transform objTarget, Vector3 objStartPos, Quaternion objStartRot, float elapsedTime)
		{
			UpdateLocation(objToMove, objStartPos, objStartRot, objTarget.position, objTarget.rotation, elapsedTime);
		}

	}

}