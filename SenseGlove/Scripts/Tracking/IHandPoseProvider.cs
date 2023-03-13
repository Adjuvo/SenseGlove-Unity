namespace SG
{
    /// <summary> The Type of device that is used to generate HandPoses. Useful for display, but also for interaction parameters  </summary>
    public enum HandTrackingDevice
    { 
        /// <summary> We do not knwo what kind of hand tracking device this is... </summary>
        Unknown,
        /// <summary> This is a hand-held motion controller device. Actions are triggered through button presses. </summary>
        Controller6DoF,
        /// <summary> This is an optical (Computer-Vision) solution - Like Quest Hand Tracking - Actions are coming from gestures.  </summary>
        HandTracking,
        /// <summary> This is a Haptic Glove. Actions come from both gestures and feedback. </summary>
        HapticGlove
    }


    /// <summary> A class implementing this Interface can provide one with a SG_HandPose. We don't care how or where it comes from. </summary>
    public interface IHandPoseProvider
    {
        /// <summary> The type of hand tracking generated from this device. Used in grabbing logic. </summary>
        /// <returns></returns>
        HandTrackingDevice TrackingType();

        /// <summary> Returns true if this HandPose Source is tracking the right hand. Otherwise, it tracks the left hand. </summary>
        /// <returns></returns>
        bool TracksRightHand();

        /// <summary> Returns true if this device is connected and ready. </summary>
        /// <returns></returns>
        bool IsConnected();

        /// <summary> Attempt to retrieve the battery level of this device. Return false if this is a wired device. </summary>
        /// <param name="value01"></param>
        /// <returns></returns>
        bool TryGetBatteryLevel(out float value01);

        /// <summary> Sets the hand dimensions for the forward kinematics of the HandPoseProvider. </summary>
        /// <param name="handModel"></param>
        void SetKinematics(SGCore.Kinematics.BasicHandModel handModel);

        /// <summary> Retrieve the HandKinematics used by this HandPoseProvider </summary>
        /// <returns></returns>
        SGCore.Kinematics.BasicHandModel GetKinematics();

        /// <summary> Retrieve the latest SG_HandPose from this provider. </summary>
        /// <param name="handPose"> When returning true, this handPose will contain the latest hand pose data from this device. </param>
        /// <param name="forcedUpdate"> If true, we force a new update even through we already retrieved a pose this frame. </param>
        /// <returns> Returns true when a handPose could be created from the device. Returns false if this method fails for a multitude of reasons (device is turned off, disconnected, etc). </returns>
        bool GetHandPose(out SG_HandPose handPose, bool forcedUpdate = false);

        /// <summary> Retrieve Normalized Flexions of this provider. </summary>
        /// <param name="flexions"> An array that will be of length 5, containing flexion values normalized between 0...1. </param>
        /// <returns> True if flexions is properly retrieved by this provider. </returns>
        bool GetNormalizedFlexion(out float[] flexions);


        /// <summary> Returns true if this script wants to grab objects regardless of HandPose. Useful when you have it mapped to a button. </summary>
        /// <remarks> Is a float so we can determine between a light and hard grasp. If it's a simple button, just use 0 and 1. </remarks>
        /// <returns></returns>
        float OverrideGrab();

        /// <summary> Returns true if this script wants to "use" an object is is currently holding, like a drill. Useful when you have it mapped to a button. </summary>
        /// <remarks> Is a float so we can vary by pressure, but otherwise can be 0 or 1 if your device doesn't support it. </remarks>
        /// <returns></returns>
        float OverrideUse();



    }
}