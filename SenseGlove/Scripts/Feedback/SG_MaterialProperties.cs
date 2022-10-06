using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Create a vmaterial / force response that is not the default one (100 % on/off). </summary>
	[CreateAssetMenu(fileName = "Material", menuName = "SenseGlove/Material", order = 1)]
    public class SG_MaterialProperties : ScriptableObject
    {
        /// <summary> The maximum brake force [0..100%] that the material provides when the finger is at maxForceDist inside the collider. </summary>
        [Header("Force-Feedback Settings")]
        [Range(0, 1)] public float maxForce = 1.0f;

        /// <summary> The distance [in m] before the maximum force is reached. 0.02 == 2cm.</summary>
        public float maxForceDist = 0.02f; //0.01 = 1 cm

        /// <summary> The Force-Feedback response of an object. X axis [0..1] represtents the maxForceDist in relation to the y-axis, where [1] represents the maxForce. </summary>
        public AnimationCurve forceRepsonse = AnimationCurve.Constant(0, 1, 1);


        /// <summary> Calculate the appropriate level of force-feedback based on this material's displacement, as a value between 0..1. </summary>
        /// <param name="colliderDepth"></param>
        /// <returns></returns>
        public float CalculateResponseForce(float colliderDepth)
        {
            if (maxForceDist > 0)
            {
                float mappedDispl = colliderDepth / maxForceDist;
                float forceMagn = forceRepsonse.Evaluate(mappedDispl);
                return SGCore.Kinematics.Values.Clamp(forceMagn * maxForce, 0.0f, maxForce);
            }
            return colliderDepth > 0 ? this.maxForce : 0.0f;
        }



    }
}