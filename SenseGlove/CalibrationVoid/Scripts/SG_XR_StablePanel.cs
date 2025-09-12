using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.XR
{
    /// <summary> Creates an object that will follow a posint from the headset at a fixed speed, and keep its own rotation. </summary>
    public class SG_XR_StablePanel : MonoBehaviour
    {

        public Vector3 posOffset = Vector3.zero;
        public Vector3 rotOffset = Vector3.zero;

        /// <summary> Recalculated when rotOffsetChanges for efficiency's sake. </summary>
        protected Quaternion qOffset = Quaternion.identity;

        public Transform headTransform;

        /// <summary> Speen in m/s how fast this transform tries to follow the head. </summary>
        public float flySpeed = 1.0f;

        public void UpdateTransform(float dT)
        {
            if (headTransform != null)
            {
                Vector3 targetPos = headTransform.position + (headTransform.rotation * posOffset);
                Quaternion targetRotation = Quaternion.LookRotation((this.transform.position - headTransform.position).normalized, Vector3.up);

                this.transform.rotation = targetRotation * qOffset;
                this.transform.position = Vector3.Lerp(this.transform.position, targetPos, dT * flySpeed);

            }
        }

        private void Start()
        {
            qOffset = Quaternion.Euler(rotOffset);
            if (this.headTransform == null && Camera.main != null)
            {
                this.headTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            UpdateTransform(Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                qOffset = Quaternion.Euler(rotOffset);
            }
        }
#endif

    }
}