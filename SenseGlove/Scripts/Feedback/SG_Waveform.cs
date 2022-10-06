using UnityEngine;

namespace SG
{
	/// <summary> Create a vibration pattern to send to specific fingers, or the wrist. </summary>
	[CreateAssetMenu(fileName = "Waveform", menuName = "SenseGlove/Waveform", order = 1)]
	public class SG_Waveform : ScriptableObject
	{
		//--------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The waveform over time. On horizontal axis; 0s to duration_s, vertical axis; 0 = off, 1 = magnitude. </summary>
		[Header("Vibration Pattern")]
		public AnimationCurve waveForm = AnimationCurve.Constant(0, 1, 1);

		/// <summary> Time in seconds the whole waveform takes; corresponds to 1 on the horizontal axis. </summary>
		public float duration_s = 0.2f;

		/// <summary> Maximum magnitude; corresponds to 1 on the vertical axis. </summary>
		[Range(0, 100)] public int magnitude = 100;

		/// <summary> Whether or not to send this vibration to the Thumb </summary>
		[Header("Send to these fingers")]
		public bool thumb = false;

		/// <summary> Whether or not to send this vibration to the index finger </summary>
		public bool index = true;

		/// <summary> Whether or not to send this vibration to the middle finger </summary>
		public bool middle = false;

		/// <summary> Whether or not to send this vibration to the ring finger </summary>
		public bool ring = false;

		/// <summary> Whether or not to send this vibration to the pinky </summary>
		public bool pinky = false;

		/// <summary> Whether or not to send this effect to the wrist as opposed to the fingers. </summary>
		public bool wrist = false;


		//--------------------------------------------------------------------------------------------
		// Utility Functions

		/// <summary> Return an array of booleans, from thumb to pinky, which can be iterated over.  </summary>
		public bool[] FingersArray
        {
			get { return new bool[5] { thumb, index, middle, ring, pinky }; }
        }

	}
}