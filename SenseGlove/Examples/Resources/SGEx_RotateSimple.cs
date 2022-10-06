using SG.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Examples
{
    /// <summary> A script to rotate an object around a specified axis </summary>
    public class SGEx_RotateSimple : MonoBehaviour
    {

        public SG.Util.MoveAxis moveAround = SG.Util.MoveAxis.Y;
        public float rotationSpeed = 10f;
        public bool resetOnEnable = false;

        public Quaternion OriginalRotation { get; protected set; }

        public void ResetRotation()
        {
            this.transform.rotation = OriginalRotation;
        }

        void Awake()
        {
            OriginalRotation = this.transform.rotation;
        }

        void OnEnable()
        {
            if (resetOnEnable) { ResetRotation(); }
        }

        // Update is called once per frame
        void Update()
        {
            float dAngle = Time.deltaTime * rotationSpeed;
            this.transform.rotation = this.transform.rotation * Quaternion.AngleAxis(dAngle, SG.Util.SG_Util.GetAxis(this.moveAround));
        }
    }
}