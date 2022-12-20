using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Maps finger flexion to a value between 0...1, and uses this for Haptics </summary>
public class SG_TriggerLogic : MonoBehaviour 
{
	/// <summary> Grabable linked to this Script </summary>
	public SG.SG_Grabable grabable;

	/// <summary> The trigger logic will respond to the flexion of this finger, which is given as value between 0..1. </summary>
	public SGCore.Finger respondsTo = SGCore.Finger.Index;

	[Range(0, 1)] public float startFlexion = 0.2f; //when finger flexion is above this value, trigger pressure will ramp up from 0% 
	[Range(0, 1)] public float endFlexion = 0.8f; //when finger flexion is at this value, the trigger pressure it at 100%

	/// <summary> The last calculated pressure </summary>
	private float latestPressure = 0.0f;

	/// <summary> The Trigger Pressure as calculated by this script.. </summary>
	public float TriggerPressure
    {
		get { return grabable.IsGrabbed() ? latestPressure : 0.0f; } //For outside scripts; When you're not being grabbed, return 0.
    }


	void Start()
    {

    }

	// Update is called once per frame
	void Update () 
	{
		if (grabable.IsGrabbed()) //indicated there is at least one script grabbing onto grabable
        {
			SG.SG_TrackedHand firstHand = grabable.ScriptsGrabbingMe()[0].TrackedHand; //grab the first hand

			//update the latest pressure
			float[] flexions;
			if (firstHand.GetNormalizedFlexion(out flexions)) //attempt to get the latest normalized finger flexions: values between 0...1
            {
				float currFlex = flexions[(int)this.respondsTo]; //we're using an enumerator to index the array (0..4, where 0 = thumb and 4 = pinky).
				if (startFlexion == endFlexion)  //if these are equal, mapping would result into a div/0.
				{ 
					latestPressure = currFlex >= startFlexion ? 1.0f : 0.0f;
				}
				else
                {
					latestPressure = SG.Util.SG_Util.Map(currFlex, startFlexion, endFlexion, 0.0f, 1.0f, true); //free function that comes with unity plugin; map a value from one range to another.
                }
            }

			//Update Haptics
			int amplitude = Mathf.RoundToInt(100.0f * latestPressure); //calculate intensity
			if (amplitude > 0)
			{
				SGCore.Haptics.SG_TimedBuzzCmd vibration = new SGCore.Haptics.SG_TimedBuzzCmd(this.respondsTo, amplitude, 0.02f); //send the command
				grabable.SendCmd(vibration);
			}

        }
	}
}
