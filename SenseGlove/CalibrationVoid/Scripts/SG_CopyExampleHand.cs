using SG.Util;
using UnityEngine;

/*
 * Because we're lazy; instead of defining another animator for the left hand, we simply copy the rotations and adjust the (local) position(s) of the right hand
 * Within the calibration Void instructions.
 * 
 * Note: Don't use this on any other type of hand model or in any other context. It will likely not work.
 * author: max@senseglove.com
 */


namespace SG.Calibration
{
    public class SG_CopyExampleHand : MonoBehaviour
    {
        public Transform originalHandBones;
        public Transform myHandBones;

        private void CopyChildren(Transform source, Transform target)
        {
            for (int i = 0; i < source.childCount; i++)
            {
                Transform sourceChild = source.GetChild(i);
                Transform targetChild = target.GetChild(i);
                targetChild.localRotation = sourceChild.localRotation;
                CopyChildren(sourceChild, targetChild); //recursively
            }
        }

        private void LateUpdate()
        {
            // We Mirror the hand bones
            Vector3 mirroredPosition = originalHandBones.localPosition;
            mirroredPosition.z *= -1;

            Quaternion mirroredRotation = SG_Conversions.MirrorZ(originalHandBones.localRotation);

            myHandBones.localRotation = mirroredRotation;
            myHandBones.localPosition = mirroredPosition;

            CopyChildren(originalHandBones, myHandBones);
        }

    }

}
