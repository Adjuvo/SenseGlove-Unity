namespace SG
{
    /// <summary> A HandAnimator that grabs its animation info from a SG_HandModelInfo script. </summary>
    public class SG_AutoHandAnimation : SG_HandAnimator
    {
        /// <summary> teh HandModelInfo that this scripts animates. </summary>
        public SG_HandModelInfo handModelInfo;

        /// <summary> Assign the joints of this script so that the SG_HandAnimator script takes over animation. </summary>
        protected override void CollectFingerJoints()
        {
            SG_Util.CheckForHandInfo(this.transform, ref this.handModelInfo);
            if (this.handModelInfo != null)
            {
                this.fingerJoints = handModelInfo.FingerJoints;
                if (this.foreArmTransfrom == null)
                {
                    this.foreArmTransfrom = handModelInfo.foreArmTransform;
                }
                if (this.wristTransfrom == null)
                {
                    this.wristTransfrom = handModelInfo.wristTransform;
                }
            }
        }

        /// <summary> Check for relevant linked scripts for this HandAnimator, specifically to the SG_HandModelInfo. </summary>
        protected override void CheckForScripts()
        {
            base.CheckForScripts();
            SG_Util.CheckForHandInfo(this.transform, ref this.handModelInfo);
            if (this.foreArmTransfrom == null)
            {
                this.foreArmTransfrom = handModelInfo.foreArmTransform;
            }
            if (this.wristTransfrom == null)
            {
                this.wristTransfrom = handModelInfo.wristTransform;
            }
        }

        /// <summary> If we have HandModelInfo, we can already collect joints </summary>
        protected override void Start()
        {
            CollectFingerJoints();
            base.Start();
        }


    }
}
