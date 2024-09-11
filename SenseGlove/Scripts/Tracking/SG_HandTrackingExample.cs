using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    public class SG_HandTrackingExample : MonoBehaviour
    {
        
        /// <summary> Will be cloned to form individual joints </summary>
        public GameObject jointObject;


        private Transform[] leftHandJoints = new Transform[0];
        private Transform[] rightHandJoints = new Transform[0];



        private void CreateJoints()
        {
            if (jointObject == null)
                return;

            leftHandJoints = new Transform[SG_HandPose.JointCount];
            rightHandJoints = new Transform[SG_HandPose.JointCount];
            string[] jointNames = SG_HandPose.JointNames();

            leftHandJoints[0] = CloneInstance(jointObject, "Left Hand", this.transform);
            rightHandJoints[0] = CloneInstance(jointObject, "Right Hand", this.transform);

            for (int i=1; i<SG_HandPose.JointCount; i++)
            {
                leftHandJoints[i] = CloneInstance(jointObject, jointNames[i], leftHandJoints[0]);
                rightHandJoints[i] = CloneInstance(jointObject, jointNames[i], rightHandJoints[0]);
            }
        }

        private Transform CloneInstance(GameObject obj, string objName, Transform parent)
        {
            GameObject clone = GameObject.Instantiate(obj, parent);
            clone.name = objName;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localPosition = Vector3.zero;
            return clone.transform;
        }


        public static void UpdateJoints(SG_HandPose handPose, Transform[] joints)
        {
            Vector3[] positions; Quaternion[] rotations;
            handPose.GetJointLocations(true, out positions, out rotations);
            for (int i = 0; i < SG_HandPose.JointCount; i++)
            {
                joints[i].rotation = rotations[i];
                joints[i].position = positions[i];
            }
        }



        // Start is called before the first frame update
        void Start()
        {
            CreateJoints();
        }

        // Update is called once per frame
        void Update()
        {
            if (SG_HandTracking.GetSGHandPose(true, out SG_HandPose rightHandPose))
            {
                UpdateJoints(rightHandPose, rightHandJoints);
            }
            if (SG_HandTracking.GetSGHandPose(false, out SG_HandPose leftHandPose))
            {
                UpdateJoints(leftHandPose, leftHandJoints);
            }
        }
    }
}