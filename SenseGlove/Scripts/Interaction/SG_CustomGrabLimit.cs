using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> Meant to attach to SG_grabable components to give them more limits in terms of grabbing </summary>
public class SG_CustomGrabLimit : MonoBehaviour
{
    [Header("Thumb Limits")]
    [Range(0.0f, 1.0f)] public float minThumbFlexion = 0.2f;
    [Range(0.0f, 1.0f)] public float maxThumbFlexion = 1.0f;

    [Header("Index Finger Limits")]
    [Range(0.0f, 1.0f)] public float minIndexFlexion = 0.2f;
    [Range(0.0f, 1.0f)] public float maxIndexFlexion = 0.8f;

    [Header("Middle Finger Limits")]
    [Range(0.0f, 1.0f)] public float minMiddlelexion = 0.2f;
    [Range(0.0f, 1.0f)] public float maxMiddleFlexion = 0.8f;

    public bool WithinGrabLimits(float[] normalizedFlexions)
    {
        return InRange(normalizedFlexions[0], minThumbFlexion, maxThumbFlexion)
            && InRange(normalizedFlexions[1], minIndexFlexion, maxIndexFlexion)
            && InRange(normalizedFlexions[2], minMiddlelexion, maxMiddleFlexion);
    }

    public static bool InRange(float value, float min, float max)
    {
        return value >= min && value < max;
    }
}
