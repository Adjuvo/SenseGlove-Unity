using SGCore.Kinematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Like an SG_ManualPoser, but allows for individual finger angles (though normalzied for now) </summary>
    public class SG_PrecisionPoser : MonoBehaviour, IHandPoseProvider
    {
        public bool rightHand = true;
        public SG_HandModelInfo useHandModel;

        protected SGCore.Kinematics.BasicHandModel handKinematics = null;



        [Header("Thumb Angles")]
        [Range(0, 1)] public float thumb_cmc_abd = 0;
        [Range(0, 1)] public float thumb_cmc_flexion = 0;

        [Range(0, 1)] public float thumb_mcp_flexion = 0;
        [Range(0, 1)] public float thumb_ip_flexion = 0;


        [Header("Index Finger Angles")]
        [Range(0, 1)] public float index_mcp_abd = 0;
        [Range(0, 1)] public float index_mcp_flexion = 0;

        [Range(0, 1)] public float index_pip_flexion = 0;
        [Range(0, 1)] public float index_dip_flexion = 0;

        [Header("Middle Finger Angles")]
        [Range(0, 1)] public float middle_mcp_abd = 0;
        [Range(0, 1)] public float middle_mcp_flexion = 0;

        [Range(0, 1)] public float middle_pip_flexion = 0;
        [Range(0, 1)] public float middle_dip_flexion = 0;

        [Header("Ring Finger Angles")]
        [Range(0, 1)] public float ring_mcp_abd = 0;
        [Range(0, 1)] public float ring_mcp_flexion = 0;

        [Range(0, 1)] public float ring_pip_flexion = 0;
        [Range(0, 1)] public float ring_dip_flexion = 0;

        [Header("Pinky Finger Angles")]
        [Range(0, 1)] public float pinky_mcp_abd = 0;
        [Range(0, 1)] public float pinky_mcp_flexion = 0;

        [Range(0, 1)] public float pinky_pip_flexion = 0;
        [Range(0, 1)] public float pinky_dip_flexion = 0;


        [Header("Simulation Overrides")]
        [Range(0, 1)] public float overrideGrab = 0;
        [Range(0, 1)] public float overrideUse = 0;



        protected SG_HandPose lastPose = null;


        
        /// <summary> Returns the abdiction- and flexion sets from this poser, for input purposes. </summary>
        /// <param name="f"></param>
        /// <param name="abd"></param>
        /// <param name="flexions"></param>
        public void GetNormalizedValues(int f, out float abd, out float[] flexions)
        {
            switch (f)
            {
                case 0:
                    abd = thumb_cmc_abd;
                    flexions = new float[3] { thumb_cmc_flexion, thumb_mcp_flexion, thumb_ip_flexion };
                    break;
                case 1:
                    abd = index_mcp_abd;
                    flexions = new float[3] { index_mcp_flexion, index_pip_flexion, index_dip_flexion };
                    break;
                case 2:
                    abd = middle_mcp_abd;
                    flexions = new float[3] { middle_mcp_flexion, middle_pip_flexion, middle_dip_flexion };
                    break;
                case 3:
                    abd = ring_mcp_abd;
                    flexions = new float[3] { ring_mcp_flexion, ring_pip_flexion, ring_dip_flexion };
                    break;
                case 4:
                    abd = pinky_mcp_abd;
                    flexions = new float[3] { pinky_mcp_flexion, pinky_pip_flexion, pinky_dip_flexion };
                    break;
                default:
                    abd = 0.0f;
                    flexions = new float[3];
                    break;
            }

        }


        /// <summary> Returns the abdiction- and flexion sets from this poser, for input purposes.  </summary>
        /// <param name="abductions"></param>
        /// <param name="flexions"></param>
        public void GetNormalizedValues(out float[] abductions, out float[][] flexions)
        {
            abductions = new float[5];
            flexions = new float[5][];

            abductions[0] = thumb_cmc_abd;
            flexions[0] = new float[3] { thumb_cmc_flexion, thumb_mcp_flexion, thumb_ip_flexion };

            abductions[1] = index_mcp_abd;
            flexions[1] = new float[3] { index_mcp_flexion, index_pip_flexion, index_dip_flexion };

            abductions[2] = middle_mcp_abd;
            flexions[2] = new float[3] { middle_mcp_flexion, middle_pip_flexion, middle_dip_flexion };

            abductions[3] = ring_mcp_abd;
            flexions[3] = new float[3] { ring_mcp_flexion, ring_pip_flexion, ring_dip_flexion };

            abductions[4] = pinky_mcp_abd;
            flexions[4] = new float[3] { pinky_mcp_flexion, pinky_pip_flexion, pinky_dip_flexion };
        }




        public void UpdatePoser()
        {
            SGCore.Kinematics.BasicHandModel handModel = this.GetKinematics();
            bool right = handModel.IsRight;

            float[] abds01; float[][] flexes01;
            GetNormalizedValues(out abds01, out flexes01);

            Vect3D[][] jointAngles = new Vect3D[5][]; //input for HandAngles...
            for (int f=0; f<5; f++)
            {
                SGCore.Finger finger = (SGCore.Finger)f;
                float abdAngle = SGCore.Kinematics.Anatomy.Abduction_FromNormalized(finger, abds01[f], right);
                float f0, f1, f2;
                if (finger == SGCore.Finger.Thumb)
                {
                    f0 = SGCore.Kinematics.Anatomy.ThumbFlexion_FromNormalized(flexes01[f][0], 0);
                    f1 = SGCore.Kinematics.Anatomy.ThumbFlexion_FromNormalized(flexes01[f][1], 1);
                    f2 = SGCore.Kinematics.Anatomy.ThumbFlexion_FromNormalized(flexes01[f][2], 2);
                }
                else
                {
                    f0 = SGCore.Kinematics.Anatomy.FingerFlexion_FromNormalized(flexes01[f][0], 0);
                    f1 = SGCore.Kinematics.Anatomy.FingerFlexion_FromNormalized(flexes01[f][1], 1);
                    f2 = SGCore.Kinematics.Anatomy.FingerFlexion_FromNormalized(flexes01[f][2], 2);
                }
                Vect3D[] fingerAngles = new Vect3D[3]
                {
                    new Vect3D(0.0f, f0, abdAngle),
                    new Vect3D(0.0f, f1, 0.0f),
                    new Vect3D(0.0f, f2, 0.0f)
                };
                jointAngles[f] = fingerAngles;
            }

            SGCore.HandPose pose = SGCore.HandPose.FromHandAngles(jointAngles, rightHand, handModel);
            this.lastPose = new SG_HandPose(pose);
            this.lastPose.wristRotation = this.transform.rotation;
            this.lastPose.wristPosition = this.transform.position;
        }


        public void CopyTrackingValues(SG_PrecisionPoser other)
        {
            this.thumb_cmc_abd = other.thumb_cmc_abd;
            this.thumb_cmc_flexion = other.thumb_cmc_flexion;
            this.thumb_mcp_flexion = other.thumb_mcp_flexion;
            this.thumb_ip_flexion = other.thumb_ip_flexion;

            this.index_mcp_flexion = other.index_mcp_flexion;
            this.index_pip_flexion = other.index_pip_flexion;
            this.index_dip_flexion = other.index_dip_flexion;

            this.middle_mcp_flexion = other.middle_mcp_flexion;
            this.middle_pip_flexion = other.middle_pip_flexion;
            this.middle_dip_flexion = other.middle_dip_flexion;

            this.ring_mcp_flexion = other.ring_mcp_flexion;
            this.ring_pip_flexion = other.ring_pip_flexion;
            this.ring_dip_flexion = other.ring_dip_flexion;

            this.pinky_mcp_flexion = other.pinky_mcp_flexion;
            this.pinky_pip_flexion = other.pinky_pip_flexion;
            this.pinky_dip_flexion = other.pinky_dip_flexion;

            this.UpdatePoser();
        }






        public bool GetHandPose(out SG_HandPose handPose, bool forcedUpdate = false)
        {
            handPose = this.lastPose;
            return handPose != null;
        }

        
        public BasicHandModel GetKinematics()
        {
            if (this.useHandModel != null)
            {
                return useHandModel.HandKinematics;
            }
            if (this.handKinematics == null)
            {
                this.handKinematics = SGCore.Kinematics.BasicHandModel.Default(this.rightHand);
            }
            return this.handKinematics;
        }


        public bool GetNormalizedFlexion(out float[] flexions)
        {
            flexions = this.lastPose != null ? this.lastPose.normalizedFlexion : new float[5];
            return this.lastPose != null;
        }

        public bool IsConnected()
        {
            return this.lastPose != null;
        }

        public float OverrideGrab()
        {
            return this.overrideGrab;
        }

        public float OverrideUse()
        {
            return overrideUse;
        }

        public void SetKinematics(BasicHandModel handModel)
        {
            this.handKinematics = handModel;
        }

        public bool TracksRightHand()
        {
            return this.GetKinematics().IsRight;
        }


        public HandTrackingDevice TrackingType()
        {
            return HandTrackingDevice.Unknown;
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            value01 = -1.0f;
            return false;
        }



        // Start is called before the first frame update
        void Start()
        {
            UpdatePoser();
        }

        // Update is called once per frame
        void Update()
        {

        }

#if UNITY_EDITOR

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                this.UpdatePoser();
            }
        }

#endif

    }
}