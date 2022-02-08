namespace SG
{
	/// <summary> Interface for a Device that can accept SenseGlove Haptic Commands. </summary>
	public interface IHandFeedbackDevice
	{
        /// <summary> Device Name, for debug purposes. </summary>
        /// <returns></returns>
        string Name();

        /// <summary> Returns true if this device is connected and can accept Hapitc Commands. </summary>
        /// <returns></returns>
        bool IsConnected();

        /// <summary> Ceases all vibrotactile feedback only </summary>
        void StopAllVibrations();

        /// <summary> Ceases all haptics on this glove. </summary>
        void StopHaptics();

        // FFB

        /// <summary> Send a new Force-Feedback command for this frame. </summary>
        /// <param name="ffb"></param>
        void SendCmd(SGCore.Haptics.SG_FFBCmd ffb);


        // Vibrotactile


        /// <summary> Send a command to the finger vibrotactile actuators, if any </summary>
        /// <param name="fingerCmd"></param>
        void SendCmd(SGCore.Haptics.SG_TimedBuzzCmd fingerCmd);


        /// <summary> Send a command to the Wrist vibrotactile actuators. </summary>
        /// <param name="wristCmd"></param>
        void SendCmd(SGCore.Haptics.TimedThumpCmd wristCmd);

        /// <summary> Send a Waveform command to this device </summary>
        /// <param name="waveform"></param>
        void SendCmd(ThumperWaveForm waveform);

        /// <summary> Send a Wavefrom command from the inspector to this device. </summary>
        /// <param name="waveform"></param>
        void SendCmd(SG_Waveform waveform);

        /// <summary> Send a Haptic Pulse to mimic an impact at a specific velocity </summary>
        /// <param name="location"></param>
        /// <param name="normalizedVibration"></param>
        void SendImpactVibration(SG_HandSection location, float normalizedVibration);



    }
}
