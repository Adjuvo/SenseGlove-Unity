using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SenseGlove_Object))]
public class SenseGlove_DemoKeyBinds : MonoBehaviour
{
    public SenseGlove_Object senseGlove;

    public KeyCode smallPresetKey = KeyCode.Alpha1;
    public KeyCode mediumPresetKey = KeyCode.Alpha2;
    public KeyCode largePresetKey = KeyCode.Alpha3;

    public KeyCode calibratePositionsKey = KeyCode.Alpha0;

	// Use this for initialization
	void Start ()
    {
		if (this.senseGlove == null)
        {
            this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady())
        {
            if (Input.GetKeyDown(this.smallPresetKey))
            {
                Debug.Log("Set Small Hand Preset");
                float[][] newLengths = new float[5][]
                {
                    new float[3] { 25, 25, 20 },
                    new float[3] { 25, 20, 20 },
                    new float[3] { 30, 25, 20 },
                    new float[3] { 30, 20, 20 },
                    new float[3] { 20, 20, 20 }
                };

                this.senseGlove.SetFingerLengths(newLengths);
            }
            else if (Input.GetKeyDown(this.mediumPresetKey))
            {
                Debug.Log("Set Medium Hand Preset");
                float[][] newLengths = new float[5][]
                {
                    new float[3] { 30, 25, 20 },
                    new float[3] { 30, 25, 20 },
                    new float[3] { 35, 25, 20 },
                    new float[3] { 30, 25, 20 },
                    new float[3] { 25, 20, 20 }
                };

                this.senseGlove.SetFingerLengths(newLengths);
            }
            else if (Input.GetKeyDown(this.largePresetKey))
            {
                Debug.Log("Set Large Hand Preset");
                float[][] newLengths = new float[5][]
                {
                    new float[3] { 30, 25, 25 },
                    new float[3] { 35, 25, 25 },
                    new float[3] { 40, 35, 25 },
                    new float[3] { 35, 25, 20 },
                    new float[3] { 30, 20, 20 }
                };

                this.senseGlove.SetFingerLengths(newLengths);
            }

            if (Input.GetKeyDown(this.calibratePositionsKey))
            {
                Debug.Log("Calculating joint positions.");
                this.senseGlove.CalculateJointPositions();
            }

        }
    }
}
