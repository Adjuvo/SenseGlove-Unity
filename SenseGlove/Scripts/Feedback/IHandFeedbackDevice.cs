namespace SG
{
    /// <summary> All available glove vibration locations. </summary>
    public enum VibrationLocation
    { 
        Unknown,
        Thumb_Tip,
        Index_Tip,
        Middle_Tip,
        Ring_Tip,
        Pinky_Tip,
        Palm_IndexSide,
        Palm_PinkySide,
        /// <summary> Play this Haptic Effect on the whole hand. For General Hand Feedback. </summary>
        WholeHand
    }


    /// <summary> A Legacy SG Haptic Command, reserved for Nova 1.0 and DK 1.0 </summary>
    public class LegacyCommand
    {
        /// <summary> The Duration of the effect </summary>
        public float Duration
        {
            get; protected set;
        }

        /// <summary> How much time has elapsed since the effect has started. </summary>
        public float ElapsedTime
        {
            get; protected set;
        }

        /// <summary> The (current) amplitude of the effect. </summary>
        public float Amplitude
        {
            get; protected set;
        }

        public bool HasElapsed()
        {
            return ElapsedTime >= Duration;
        }

        public float NormalizedTime()
        {
            return Duration > 0 ? ElapsedTime / Duration : 1.0f;
        }

        public LegacyCommand()
        {
            
        }

        public LegacyCommand(float amplitude, float duration, float startTime)
        {
            Amplitude = amplitude;
            Duration = duration;
            ElapsedTime = startTime;
        }

        public virtual void UpdateEffect(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }
    }

    public class LegacyWaveform : LegacyCommand
    {
        protected float baseAmplitude;

        public UnityEngine.AnimationCurve Waveform
        {
            get; protected set;
        }

        public LegacyWaveform(float amplitude, float duration, UnityEngine.AnimationCurve wfCurve, float startTime)
        {
            baseAmplitude = amplitude;
            Duration = duration;
            Waveform = wfCurve;

            ElapsedTime = startTime;
        }

        public override void UpdateEffect(float deltaTime)
        {
            base.UpdateEffect(deltaTime);
            float elapsed = NormalizedTime();
            float ampl = Waveform.Evaluate(elapsed);
            this.Amplitude = ampl * baseAmplitude;
            //UnityEngine.Debug.LogError(UnityEngine.Time.time + ": Updated Effect: " + ElapsedTime.ToString("0.00") + " / " + Duration.ToString("0.00") 
            //    + " -> " + elapsed.ToString("0.00") + " -> " + ampl.ToString("0.00") + " * " + this.baseAmplitude.ToString("0.00") + " - > " + Amplitude.ToString("0.00"));
        }
    }


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

        /// <summary> Tell the device to queue up a Force-Feedback command to this particular finger </summary>
        /// <param name="finger"></param>
        /// <param name="value01"></param>
        void QueueFFBCmd(SGCore.Finger finger, float value01);

        /// <summary> Tell the device to queue up Force-Feedback to several fingers. Levels 0...1 from Thumb = 0 to Pinky = 4. </summary>
        /// <param name="values01"></param>
        void QueueFFBCmd(float[] values01);



        /// <summary> Return true if this device supports automatic locking of the fingers at a set flexion. </summary>
        bool FlexionLockSupported();

        /// <summary> Tell this device to lock specific fingers at set flexions, or to stop doing so (by setting fingers to false). Arrays should be size 5. </summary>
        /// <param name="fingers"></param>
        /// <param name="fingerFlexions"></param>
        void SetFlexionLocks(bool[] fingers, float[] fingerFlexions);


        // Active Contact Feedback

        /// <summary> Set the amount of squeeze feedback desired on the wrist. Where 0 is no squeeze, and 1 is full squeeze force. </summary>
        /// <param name="squeezeLevel01"></param>
        void QueueWristSqueeze(float squeezeLevel01);

        /// <summary> Stops any active squeeze effects on the wrist. </summary>
        void StopWristSqueeze();


        // Vibrotactile

        /// <summary> Returns true if this device has a Vibration Motor at the selected location. </summary>
        /// <param name="atLocation"></param>
        /// <returns></returns>
        bool HasVibrationMotor(VibrationLocation atLocation);

        /// <summary> Send a timed command to a particular location on the hand. If the device supports said location, it will fire a haptic effect </summary>
        /// <param name="location"></param>
        /// <param name="amplitude"></param>
        /// <param name="duration"></param>
        /// <param name="frequency"></param>
        void SendVibrationCmd(VibrationLocation location, float amplitude, float duration, float frequency);


        ///// <summary> Send a command to the finger vibrotactile actuators, if any </summary>
        ///// <param name="fingerCmd"></param>
        //void SendCmd(SGCore.Haptics.SG_TimedBuzzCmd fingerCmd);


        ///// <summary> Send a command to the Wrist vibrotactile actuators. </summary>
        ///// <param name="wristCmd"></param>
        //void SendCmd(SGCore.Haptics.TimedThumpCmd wristCmd);

        ///// <summary> Send a Waveform command to this device </summary>
        ///// <param name="waveform"></param>
        //void SendCmd(ThumperWaveForm waveform);

        /// <summary> Send a Wavefrom command from the inspector to this device. </summary>
        /// <param name="waveform"></param>
#pragma warning disable CS0618 // Type or member is obsolete
        void SendLegacyWaveform(SG_Waveform waveform);
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary> Send a waveform command, but allow for overrides. </summary>
        /// <param name="waveform"></param>
        /// <param name="amplitude"></param>
        /// <param name="duration"></param>
        /// <param name="location"></param>
#pragma warning disable CS0618 // Type or member is obsolete
        void SendLegacyWaveform(SG_Waveform waveform, float amplitude, float duration, VibrationLocation location);
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary> Send a Haptic Pulse to mimic an impact at a specific velocity </summary>
        /// <param name="location"></param>
        /// <param name="normalizedVibration"></param>
        void SendImpactVibration(SG_HandSection location, float normalizedVibration);


        /// <summary> Sends a custom waveform to the device, if it is a Nova Glove. </summary>
        /// <param name="customWaveform"></param>
        /// <param name="location"></param>
        void SendCustomWaveform(SG_CustomWaveform customWaveform, VibrationLocation location);


    }
}
