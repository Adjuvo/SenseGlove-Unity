﻿using SGCore.Haptics;
using UnityEngine;

/*
 * Contains some utility classes for Haptics in Unity.
 */

namespace SG
{

    //-------------------------------------------------------------------------------------------------------------------------------------------
    // Waveform Command

    /// <summary> A timed command that uses an AnimationCurve to stream a variable vibration level. </summary>
    public class SG_WaveFormCmd : SG_TimedBuzzCmd
    {
        /// <summary> AnimationCurve used to evaluate the current vibration level(s). </summary>
        protected AnimationCurve _timeline;
        /// <summary> The maximum magnitude of the signal (1 on the y-axis of the AnimationCurve). From 0 ... 100% </summary>
        protected int _amplitude;
        /// <summary> Which fingers to apply this effect to. </summary>
        protected bool[] _fingers;

        /// <summary> Update the timing of this Haptic Signal, using a deltatime in milliseconds. </summary>
        /// <param name="ms_dT"></param>
        public override void UpdateTiming(int ms_dT)
        {
            base.UpdateTiming(ms_dT);
            if (elapsedTime > 0)
            {
                float normalizedTime = elapsedTime / buzzTime;
                float eval01 = _timeline.Evaluate(normalizedTime);
                int finalLevel = Mathf.RoundToInt(eval01 * _amplitude);
                for (int f = 0; f < this.levels.Length; f++)
                {
                    this.levels[f] = _fingers[f] ? finalLevel : 0;
                }
                this.baseCmd.Levels = this.levels;
                //Debug.Log("Time " + elapsedTime + " / " + buzzTime + " > " + normalizedTime + " > " + eval01 + " > " + finalLevel);
            }
        }

        /// <summary> Create a new WaveFormCmd from an AnimationCurve. </summary>
        /// <param name="timeLine"></param>
        /// <param name="duration_s"></param>
        /// <param name="maxMagn"></param>
        /// <param name="fingers"></param>
        /// <param name="dT"></param>
        public SG_WaveFormCmd(AnimationCurve timeLine, float duration_s, int maxMagn, bool[] fingers, float dT)
        {
            _amplitude = maxMagn;
            _timeline = timeLine;
            buzzTime = Mathf.RoundToInt(duration_s * 1000);
            _fingers = new bool[5]
            {
                fingers.Length > 0 ? fingers[0] : false,
                fingers.Length > 1 ? fingers[1] : false,
                fingers.Length > 2 ? fingers[2] : false,
                fingers.Length > 3 ? fingers[3] : false,
                fingers.Length > 4 ? fingers[4] : false
            };
            this.baseCmd = new SG_BuzzCmd(this._fingers, this._amplitude);
            elapsedTime = -dT; //-dT so the first time we check it after Update(dT), it starts at 0.
        }

        /// <summary> Used for logging. </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string res = _amplitude + " for " + this.buzzTime + "ms";
            return res;
        }

        /// <summary> A deeop copy of this commmand. </summary>
        /// <returns></returns>
        public override SG_BuzzCmd Copy()
        {
            SG_WaveFormCmd res =  new SG_WaveFormCmd(this._timeline, 0, this._amplitude, this._fingers, 0);
            res.elapsedTime = this.elapsedTime;
            res.buzzTime = this.buzzTime;
            return res;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------
    // Timed Thumper Command

    /// <summary> A vibration command for the thumper. A pulse for a specific time in seconds.  </summary>
    public class TimedThumpCmd
    {
        /// <summary> Magnitude in % </summary>
        public int magnitude;

        /// <summary> duration in seconds </summary>
        public float duration;

        /// <summary> The elapsed time so far. </summary>
        public float elapsedTime;

        /// <summary> Returns true if the timed command has elapsed. </summary>
        public bool Elapsed
        {
            get { return elapsedTime >= duration; }
        }

        
        /// <summary> Internal, empty, for extended classes. </summary>
        protected TimedThumpCmd() { }

        /// <summary> Create a new Timed Thumper Command. </summary>
        /// <param name="amplitude"></param>
        /// <param name="duration_s"></param>
        public TimedThumpCmd(int amplitude, float duration_s, float startTime = 0)
        {
            magnitude = amplitude;
            duration = duration_s;
            elapsedTime = startTime;
        }

        /// <summary> Update the timing on this Thumper Command </summary>
        /// <param name="dT"></param>
        public virtual void Update(float dT)
        {
            this.elapsedTime += dT;
        }

        /// <summary> Copy the parameters of this ThumperCommand into another instance. </summary>
        /// <returns></returns>
        public virtual TimedThumpCmd Copy()
        {
            return new TimedThumpCmd(this.magnitude, this.duration, this.elapsedTime);
        }

    }


    //-------------------------------------------------------------------------------------------------------------------------------------------
    // Combination of TimedThumpCmd & SG_WaveFormCmd

    public class ThumperWaveForm : TimedThumpCmd
    {
        /// <summary> The avibration level over time. [0..1] on the x-axis is the duration_s, while [1] on the y-axis is the amplitude. </summary>
        AnimationCurve _timeline;

        /// <summary> The maximum vibration level, a.k.a. 1 on the y-axis of the animationCurve. </summary>
        public int maxAmplitude = 0;

        /// <summary> Create a new waveform. </summary>
        /// <param name="amplitude"></param>
        /// <param name="duration_s"></param>
        /// <param name="timeLine"></param>
        /// <param name="startTime"></param>
        public ThumperWaveForm(int amplitude, float duration_s, AnimationCurve timeLine, float startTime = 0)
        {
            maxAmplitude = amplitude;
            duration = duration_s;
            elapsedTime = startTime;
            _timeline = timeLine;
            this.magnitude = 0;
        }

        /// <summary> Update the timing and amplitude of this effect </summary>
        /// <param name="dT"></param>
        public override void Update(float dT)
        {
            base.Update(dT);
            float normalTime = duration != 0 ? elapsedTime / duration : 0;
            this.magnitude = Mathf.RoundToInt(_timeline.Evaluate(normalTime) * this.maxAmplitude);
        }

        /// <summary> Copy this ThumperWaveForm into a new instance. </summary>
        /// <returns></returns>
        public override TimedThumpCmd Copy()
        {
            ThumperWaveForm form = new ThumperWaveForm(this.maxAmplitude, this.duration, this._timeline, this.elapsedTime);
            form.magnitude = this.magnitude;
            return form;
        }

    }


}