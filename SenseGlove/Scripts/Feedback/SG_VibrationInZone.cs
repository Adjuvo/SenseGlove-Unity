using UnityEngine;


namespace SG
{
    /// <summary> Sends haptic commands to all hands insize an SG_HandDetector Script </summary>
    public class SG_VibrationInZone : MonoBehaviour
    {
        public SG_CustomWaveform waveformToSend;

        public SG_HandDetector detectionZone;


        public void OnHandDetected(SG.SG_TrackedHand hand)
        {
            if (waveformToSend == null)
                return;
            hand.SendCustomWaveform(waveformToSend, waveformToSend.intendedMotor);
        }

        private void OnEnable()
        {
            if (detectionZone != null)
                detectionZone.HandDetected.AddListener(OnHandDetected);
        }

        private void OnDisable()
        {
            if (detectionZone != null)
                detectionZone.HandDetected.RemoveListener(OnHandDetected);
        }

    }
}
