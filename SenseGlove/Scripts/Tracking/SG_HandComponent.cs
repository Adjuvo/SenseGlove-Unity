using UnityEngine;

namespace SG
{
    /// <summary> Represents different sections of the hand, used to determine feedback or tracking location. </summary>
    public enum SG_HandSection
    {
        Thumb = 0,
        Index,
        Middle,
        Ring,
        Pinky,
        Wrist,
        Unknown
    }


    /// <summary> Represents a component of the SG_TrackedHand, which has access to the other layers. Most, if not all 'layers' extend from this. </summary>
    public class SG_HandComponent : MonoBehaviour
    {

        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Access the TrackedHand, through which one can send/receive SenseGlove data. </summary>
        public SG_TrackedHand TrackedHand;


        public virtual SG_HapticGlove Hardware
        {
            get { return this.TrackedHand != null ? this.TrackedHand.gloveHardware : null; }
        }

        /// <summary> Check if this glove's hardware is ready. </summary>
        public virtual bool HardwareReady
        {
            get { return this.TrackedHand != null && this.TrackedHand.gloveHardware != null && this.TrackedHand.gloveHardware.IsConnected; }
        }

        /// <summary> Check if this trackedhand is meant for a right hand. </summary>
        public virtual bool IsRight
        {
            get { return this.TrackedHand != null && this.TrackedHand.TracksRightHand; }
        }

        /// <summary> Check if this trackedhand is meant for a right or left hand. </summary>
        public virtual HandSide HandSide
        {
            get { return this.TrackedHand != null && TrackedHand.TracksRightHand ? HandSide.RightHand : HandSide.LeftHand; }
        }

        /// <summary> Access the HandModel information about this trackedhand. </summary>
        public virtual SG_HandModelInfo HandModel
        {
            get { return TrackedHand != null ? TrackedHand.handModel : null; }
        }

        /// <summary> Access the animation layer of this trackedhand. </summary>
        public virtual SG_HandAnimator HandAnimation
        {
            get { return TrackedHand != null ? TrackedHand.handAnimation : null; }
        }

        /// <summary> Access the feedback layer of this hand model. </summary>
        public virtual SG_HandFeedback HandFeedback
        {
            get { return TrackedHand != null ? TrackedHand.feedbackScript : null; }
        }

        /// <summary>  Access the grab layer of this handModel </summary>
        public virtual SG_GrabScript GrabScript
        {
            get { return TrackedHand != null ? TrackedHand.grabScript : null; }
        }

        /// <summary>  Access the grab layer of this handModel </summary>
        public virtual SG_GestureLayer Gestures
        {
            get { return TrackedHand != null ? TrackedHand.gestureLayer : null; }
        }

        public virtual SG_HandStateIndicator StatusIndicator
        {
            get { return TrackedHand != null ? TrackedHand.statusIndicator : null; }
        }

        public virtual SG_CalibrationSequence Calibration
        {
            get { return TrackedHand != null ? TrackedHand.calibration : null; }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Whether or not a debug mode is turned on/off. </summary>
        public virtual bool DebugEnabled
        {
            get { return false; }
            set { }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Check for the relevant scripts connected to this handModel. </summary>
        public virtual void CheckForScripts()
        {
            this.TrackedHand = SG.Util.SG_Util.CheckForTrackedHand(this.transform);
        }
    }
}