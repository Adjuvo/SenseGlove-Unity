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

        /// <summary> Attempt to retrieve the battery level of this device. Return false if this is a wired device. </summary>
        /// <param name="value01"></param>
        /// <returns></returns>
        bool TryGetBatteryLevel(out float value01);

        /// <summary> Ceases all vibrotactile feedback only </summary>
        void StopAllVibrations();

        /// <summary> Ceases all haptics on this glove. </summary>
        void StopHaptics();

        // FFB

        /// <summary> Send a new Force-Feedback command for this frame. </summary>
        /// <param name="ffb"></param>
        void SendCmd(SGCore.Haptics.SG_FFBCmd ffb);

        /// <summary> Return true if this device supports automatic locking of the fingers at a set flexion. </summary>
        bool FlexionLockSupported();

        /// <summary> Tell this device to lock specific fingers at set flexions, or to stop doing so (by setting fingers to false). Arrays should be size 5. </summary>
        /// <param name="fingers"></param>
        /// <param name="fingerFlexions"></param>
        void SetFlexionLocks(bool[] fingers, float[] fingerFlexions);


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


        /// <summary> Sends a custom waveform to the device, if it is a Nova Glove. </summary>
        /// <param name="customWaveform"></param>
        /// <param name="location"></param>
        void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location);


    }
}
