using UnityEngine;

namespace SG
{
	/// <summary> Create a vibration pattern to send to specific fingers, or the wrist. </summary>
	[CreateAssetMenu(fileName = "Waveform", menuName = "SenseGlove/Legacy Waveform", order = 1)]
	[System.Obsolete("This class is deprecated and will be removed soon. Use an SG_CustomWaveform instead.", false)]
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
		[Range(0.0f, 1.0f)] public float amplitude = 1.0f;

		/// <summary> The intended location for the effect. </summary>
		public VibrationLocation intendedLocation = VibrationLocation.WholeHand;

	}
}