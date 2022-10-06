using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Examples
{
	/// <summary> Send a SG_Wavefrom created in the Inspector to a HapticGlove. </summary>
	public class SGEx_TestWaveform : MonoBehaviour
	{
		/// <summary> The glove to send the virbtation to </summary>
		public SG_HapticGlove glove;
		/// <summary> The WaveForm to send to the glove(s). </summary>
		public SG_Waveform waveForm;

		/// <summary> Optional instruction elements </summary>
		public UnityEngine.UI.Text instructions;

		/// <summary> Key to send and effect to the glove. </summary>
		public KeyCode sendKey = KeyCode.Space;


		void Start()
        {
			if (instructions != null)
            {
				instructions.text = "Press " + sendKey.ToString() + " to send the chosen waveform.";
            }
        }

		// Update is called once per frame
		void Update()
		{
			if (glove != null && glove.IsConnected())
			{
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
				if (Input.GetKeyDown(sendKey))
				{
					glove.SendCmd(waveForm);
				}
#endif
			}
		}
	}

}