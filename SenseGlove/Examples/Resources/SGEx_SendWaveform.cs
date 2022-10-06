using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Examples
{
	/// <summary> Sends a chosed waveform to one or more HapticGloves. </summary>
	public class SGEx_SendWaveform : MonoBehaviour
	{
		//---------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The Gloves to send the effect to  </summary>
		public SG_HapticGlove[] hapticGloves = new SG_HapticGlove[0];

		/// <summary> The effect to send to the glove(s). </summary>
		public SG_Waveform waveFormToSend;

		/// <summary> Optional multiple waveforms to send. </summary>
		public SG_Waveform[] allWaveForms = new SG_Waveform[0];


		[Header("UI Elements")]
		public UnityEngine.UI.Text waveFormNameText;

		public UnityEngine.UI.Button sendBtn, nextBtn, prevBtn;


		/// <summary> Hotkey to send the effect </summary>
		[Header("HotKeys")]
		public KeyCode sendWaveFormKey = KeyCode.Space;
		public KeyCode nextWaveFormKey = KeyCode.D;
		public KeyCode prevWaveFormKey = KeyCode.A;

		protected int selectedWaveForm = 0;

		//---------------------------------------------------------------------------------------------------
		// Function

		/// <summary> Send the waveFormToSend to each glove in memory. </summary>
		public void SendWaveForm()
        {
			Debug.Log("Sending " + waveFormToSend.name + " to " + hapticGloves.Length + " gloves");
            foreach (SG_HapticGlove glove in hapticGloves)
            {
				if (glove != null)
                {
					glove.SendCmd(waveFormToSend);
                }
			}
        }

		public void SelectNext()
        {
			if (this.allWaveForms.Length > 0)
            {
				selectedWaveForm++;
				if (selectedWaveForm >= allWaveForms.Length) { selectedWaveForm = 0; }
				this.waveFormToSend = allWaveForms[selectedWaveForm];
				UpdateText();
            }
        }

		public void SelectPrevious()
        {
			if (this.allWaveForms.Length > 0)
			{
				selectedWaveForm--;
				if (selectedWaveForm < 0) { selectedWaveForm = allWaveForms.Length -1; }
				this.waveFormToSend = allWaveForms[selectedWaveForm];
				UpdateText();
			}
		}

		public void UpdateText()
        {
			if (this.waveFormNameText != null)
            {
				this.waveFormNameText.text = this.waveFormToSend != null ? this.waveFormToSend.name : "";
			}
        }

		

        //---------------------------------------------------------------------------------------------------
        // Monobehaviour


        private void OnEnable()
        {
			if (this.nextBtn != null)
			{ 
				nextBtn.onClick.AddListener(SelectNext);
				SG.Util.SG_Util.SetButtonText(nextBtn, "Next Wavefrom", this.nextWaveFormKey);
			}
			if (this.prevBtn != null)
			{ 
				prevBtn.onClick.AddListener(SelectPrevious);
				SG.Util.SG_Util.SetButtonText(prevBtn, "Previous WaveForm", this.prevWaveFormKey);
			}
			if (this.sendBtn != null)
			{ 
				sendBtn.onClick.AddListener(SendWaveForm);
				SG.Util.SG_Util.SetButtonText(sendBtn, "Send Wavefrom", this.sendWaveFormKey);
			}
        }

		private void OnDisable()
		{
			if (this.nextBtn != null) { nextBtn.onClick.RemoveListener(SelectNext); }
			if (this.prevBtn != null) { prevBtn.onClick.RemoveListener(SelectPrevious); }
			if (this.sendBtn != null) { sendBtn.onClick.RemoveListener(SendWaveForm); }
		}

		// Use this for initialization
		void Start()
		{
			if (this.waveFormToSend != null)
			{
				int index = -1;
				for (int i = 0; i < this.allWaveForms.Length; i++)
				{
					if (allWaveForms[i] == this.waveFormToSend)
					{
						index = i;
						break;
					}
				}
				this.selectedWaveForm = index;
			}
			else if (this.allWaveForms.Length > 0)
			{
				this.waveFormToSend = allWaveForms[0];
				this.selectedWaveForm = 0;
			}
			//else we don;t have anything to send and no waveforms anyway.

			UpdateText();
		}

		// Update is called once per frame
		void Update()
		{
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
			if (Input.GetKeyDown(sendWaveFormKey))
            {
				SendWaveForm();
			}
			if (Input.GetKeyDown(nextWaveFormKey))
            {
				SelectNext();
            }
			else if (Input.GetKeyDown(prevWaveFormKey))
            {
				SelectPrevious();
            }
#endif
		}
	}

}