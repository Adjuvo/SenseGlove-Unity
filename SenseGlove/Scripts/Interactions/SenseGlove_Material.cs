using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A data struct that can be assinged to a SenseGlove_Interactable </summary>
public class SenseGlove_Material : MonoBehaviour
{
    
    /// <summary> The force applied to the Glove when SimpleForceFeedback is selected. </summary>
    public int passiveForce = 255;
    

    /// <summary> Calculates the force on the finger based on material properties. </summary>
    /// <param name="displacement"></param>
    /// <returns></returns>
    public int CalculateForce(float displacement)
    {
        return this.passiveForce;
    }

    //----------------------------------------------------------------------------------------------
    // Default Material Properties - Used when no SenseGlove_Material Data is present.

    /// <summary> The passive force used when Simple ForceFeedback option is chosen but no SenseGlove_Matrial script can be found. </summary>
    public static int defaultPassiveForce = 255;

    /// <summary> The force used when the Material-Based ForceFeedback option is chosen but no SenseGlove_Matrial script can be found. </summary>
    /// /// <param name="displacement"></param>
    /// <returns></returns>
    public static int CalculateDefault(float displacement)
    {
        //TODO: Define default material properties.
        return defaultPassiveForce;
    }


}
