using UnityEngine;

namespace SG
{
	/// <summary> A utility class where one can create a vibration waveform based on a Unity AnimationCurve. </summary>
	public class SG_Waveform : MonoBehaviour
	{
		/// <summary> The waveform over time. On horizontal axis; 0s to duration_s, vertical axis; 0 = off, 1 = magnitude. </summary>
		public AnimationCurve waveForm = AnimationCurve.Constant(0, 1, 1);

		/// <summary> Time in seconds the whole waveform takes; corresponds to 1 on the horizontal axis. </summary>
		public float duration_s = 0.2f;

		/// <summary> Maximum magnitude; corresponds to 1 on the vertical axis. </summary>
		[Range(0, 100)]
		public int magnitude = 100;

		/// <summary> Which of the fingers (thumb = 0, pinky = 4) to send this waveform to by default. </summary>
		public bool[] fingers = new bool[5] { false, true, false, false, false };

		/// <summary> Whether or not to send this effect to the wrist as opposed to the fingers. </summary>
		public bool wrist = false;
	}
}