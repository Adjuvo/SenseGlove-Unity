using UnityEngine;


namespace SG
{
    /// <summary> A script to show the Haptic Glove status to the User. </summary>
    public class SG_HandStateIndicator : SG_HandComponent
    {
        //----------------------------------------------------------------------------------------------------
        // HandState Enumerator

        /// <summary> The available status indication of the hand. </summary>
        public enum HandState
        {
            /// <summary> The glove is operating as normal. </summary>
            Default,
            /// <summary> The device si disconnected </summary>
            Disconnected,
            /// <summary> The user should move their fingers </summary>
            CheckRanges,
            /// <summary> The user needs to calibrate their hand, but there is no algorithm running. </summary>
            CalibrationNeeded,
            /// <summary> The user is calibrating their hand. </summary>
            Calibrating
        }

        //----------------------------------------------------------------------------------------------------
        // Member variables

        /// <summary> Optional wrist test to notify your user of something important. </summary>
        [Header("HandState Components")]
        public TextMesh wristText = null;


        /// <summary> A list of Rendering components to apply materials to. </summary>
        public Renderer[] handMeshes = new Renderer[0];

        /// <summary> The default material(s) of the hand. Automatically assigned if left empty </summary>
        public Material[] mats_Default = new Material[0];
        /// <summary> The materials to show when the glove is initializing and the user has to move their fingers. </summary>
        public Material[] mats_Initializing = new Material[0];
        /// <summary>  The materials to show when the glove should be calibrated, but nothing has activated to make it do so. </summary>
        public Material[] mats_NeedCalibration = new Material[0];
        /// <summary>  The materials to show when the glove is calibrating. </summary>
        public Material[] mats_Calibrating = new Material[0];
        /// <summary> The materials to show when the glove is disconnected. </summary>
        public Material[] mats_Disconnected = new Material[0];

        /// <summary> The current HandState of this Indicator. </summary>
        public HandState CurrentState
        {
            get; private set;
        }


        //----------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Access the wrist text. </summary>
        public string WristText
        {
            get { return this.wristText != null ? this.wristText.text : ""; }
            set { if (this.wristText != null) { this.wristText.text = value; } }
        }


        //----------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Set this hand's indicator based on hand and connection stage. </summary>
        /// <param name="stage"></param>
        /// <param name="connected"></param>
        public void SetMaterials(HandState status)
        {
            switch (status)
            {
                case HandState.CheckRanges:
                    SetMaterials(mats_Initializing);
                    break;
                case HandState.Calibrating:
                    SetMaterials(mats_Calibrating);
                    break;
                case HandState.CalibrationNeeded:
                    SetMaterials(mats_NeedCalibration);
                    break;
                case HandState.Disconnected:
                    SetMaterials(mats_Disconnected);
                    break;
                default:
                    SetMaterials(mats_Default);
                    break;
            }
        }

        /// <summary> Set a collection of materials to the relevant renderers. </summary>
        /// <param name="newMaterials"></param>
        public void SetMaterials(Material[] newMaterials)
        {
            if (newMaterials != null && newMaterials.Length > 0)
            {
                for (int i = 0; i < handMeshes.Length; i++)
                {
                    handMeshes[i].materials = newMaterials;
                }
            }
        }

        //-------------------------------------------------------------------------------------
        // HandComponent overrides

        protected override void CreateComponents()
        {
            base.CreateComponents();
            if (mats_Default.Length == 0 && handMeshes.Length > 0)
            {   //Check if we have a default material
                Material[] dMats = handMeshes[0].materials;
                Material[] copyMats = new Material[dMats.Length];
                for (int i = 0; i < dMats.Length; i++) { copyMats[i] = dMats[i]; }
                mats_Default = copyMats;
            }
        }

        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand_Internal(newHand, firstLink);
            if (wristText != null)
            {
                SG_SimpleTracking link = Util.SG_Util.TryAddComponent<SG_SimpleTracking>(wristText.gameObject);
                Transform wrist = newHand.GetTransform(SG_TrackedHand.TrackingLevel.RenderPose, HandJoint.Wrist);
                link.SetTrackingTarget(wrist, true);
                link.updateTime = SG_SimpleTracking.UpdateDuring.LateUpdate;
            }
        }

    }
}