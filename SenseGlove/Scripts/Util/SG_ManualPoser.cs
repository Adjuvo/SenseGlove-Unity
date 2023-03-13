using SGCore.Kinematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> A class that serves as a source of handTracking for animation. The wrist location is set at this script's transform. </summary>
	public class SG_ManualPoser : MonoBehaviour, IHandPoseProvider
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Whther or not this pose is meant for a left or right hand. </summary>
		public bool rightHand = true;


		/// <summary> Normalized Thumb Abduction; 0 = thumb parallel to palm, 1 = thumb fully outward. </summary>
		[Header("Tracking Parameters")]
		[Range(0, 1)] public float thumbAbduction = 0;
		/// <summary> Normalized Thumb Flexion; 0 = thumb fully extended (thumbs up), 1 = thumb fully flexed. </summary>
		[Range(0, 1)] public float thumbFlexion = 0;
		/// <summary> Normalized index finger flexion; 0 = finger straight, 1 = finger fully flexed. </summary>
		[Range(0, 1)] public float indexFlexion = 0;
		/// <summary> Normalized middle finger flexion; 0 = finger straight, 1 = finger fully flexed. </summary>
		[Range(0, 1)] public float middleFlexion = 0;
		/// <summary> Normalized ring finger flexion; 0 = finger straight, 1 = finger fully flexed. </summary>
		[Range(0, 1)] public float ringFlexion = 0;
		/// <summary> Normalized pinky finger flexion; 0 = finger straight, 1 = finger fully flexed. </summary>
		[Range(0, 1)] public float pinkyFlexion = 0;
		/// <summary> Normalized finger spred; 0 = fingers forward, 1 = fingers moved outward. Does not affect thumb. </summary>
		[Range(0, 1)] public float fingerSpread = 0;

		/// <summary> The Kinematics to use for this Poser. </summary>
		protected SGCore.Kinematics.BasicHandModel handGeometry;

		/// <summary> Ensure initialization is only done once. </summary>
		protected bool init = true;

		/// <summary> Overrides Grab behaviour as though you were pressing a Grip button </summary>
		[Header("Grab Components")]
		[Range(0, 1)] public float overrideGrab = 0;
		/// <summary> Overrides Use behaviour as though you were pressing a Trigger button </summary>
		[Range(0, 1)] public float overrideUse = 0;

		[Header("Optional Components")]
		public SG_HandModelInfo useHandModel = null;

		//--------------------------------------------------------------------------------------------------------------------------
		// SG_ManualPoser functions


		/// <summary> The last pose as determined by the normalized sliders. </summary>
		public SG_HandPose LastPose
		{
			get; private set;
		}

		/// <summary> Calculate a new SG_HandPose based on the sliders. </summary>
		public void RecalculatePose()
		{
			if (handGeometry == null)
            {
				if (this.useHandModel != null)
				{
					this.handGeometry = this.useHandModel.HandKinematics;
					this.rightHand = this.useHandModel.IsRightHand;
				}
				else
				{
					this.handGeometry = SGCore.Kinematics.BasicHandModel.Default(this.TracksRightHand());
				}
            }

			float[] norms;
			this.GetNormalizedFlexion(out norms);


			this.LastPose = FromNormalized(handGeometry, norms, thumbAbduction, fingerSpread);
			this.LastPose.wristPosition = this.transform.position;
			this.LastPose.wristRotation = this.transform.rotation;
		}


		//--------------------------------------------------------------------------------------------------------------------------
		// IHandPoseProver functions


		/// <summary> Returns true if this poers is set to work for right hands. </summary>
		/// <returns></returns>
		public bool TracksRightHand()
		{
			return this.handGeometry != null ? this.handGeometry.IsRight : this.rightHand;
		}

		/// <summary>  </summary>
		/// <param name="flexions"></param>
		/// <returns></returns>
		public bool GetNormalizedFlexion(out float[] flexions)
		{
			flexions = new float[5]
			{
				thumbFlexion,
				indexFlexion,
				middleFlexion,
				ringFlexion,
				pinkyFlexion
			};
			return true;
		}

		public void SetKinematics(BasicHandModel handModel)
		{
			this.handGeometry = handModel;
		}

		public BasicHandModel GetKinematics()
		{
			return handGeometry;
		}

		/// <summary> When this script is inactive, we consider it "disabled". </summary>
		/// <returns></returns>
		public bool IsConnected()
		{
			return this.isActiveAndEnabled;
		}

		/// <summary> </summary>
		/// <returns></returns>
		public float OverrideGrab()
		{
			return overrideGrab;
		}

		/// <summary> </summary>
		/// <returns></returns>
		public float OverrideUse()
		{
			return overrideUse;
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

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// Into Anatomy

		// 01 to angles

		public static SG_HandPose FromNormalized(SGCore.Kinematics.BasicHandModel handModel, float[] flexions01, float thumbAbd01, float fingerSpread01)
        {
			Vect3D[][] handAngles = SGCore.Kinematics.Anatomy.HandAngles_FromNormalized(handModel.IsRight, flexions01, thumbAbd01, fingerSpread01);
			return new SG_HandPose(SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel));

			//Vect3D[][] handAngles = HandAngles_FromNormalized(handModel.IsRight, flexions01, thumbAbd01, fingerSpread01);
			//return new SG_HandPose(SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel));
		}

		public static Vect3D[][] HandAngles_FromNormalized(bool isRight, float[] flexions01, float thumbAbd01, float fingerSpread01)
        {
			int LR = isRight ? 1 : -1;
			Vect3D[][] handAngles = new Vect3D[5][];
			for (int f = 0; f < handAngles.Length; f++)
			{
				if (f == 0)
				{
					handAngles[f] = FromNormalized_Thumb(flexions01[f], thumbAbd01, isRight);
				}
				else
				{
					handAngles[f] = FromNormalized_Finger( (SGCore.Finger)f , flexions01[f], fingerSpread01, isRight);
				}
			}
			return handAngles;
		}


		/// <summary> Generic Entry point </summary>
		/// <param name="finger"></param>
		/// <param name="normalizedFlexion"></param>
		/// <param name="normalizedAbduction"></param>
		/// <param name="rightHand"></param>
		/// <returns></returns>
		public static Vect3D[] FromNormalized(SGCore.Finger finger, float normalizedFlexion, float normalizedAbduction, bool rightHand)
        {
			if (finger == SGCore.Finger.Thumb)
            {
				return FromNormalized_Thumb(normalizedFlexion, normalizedAbduction, rightHand);
            }
			return FromNormalized_Finger(finger, normalizedFlexion, normalizedAbduction, rightHand);
        }


		public static float[][] thumbFlexions01 = new float[3][]
		{
			new float[2] { Mathf.Deg2Rad * -35, Mathf.Deg2Rad * 10 },
			new float[2] { Mathf.Deg2Rad * 0,	Mathf.Deg2Rad * 40 },
			new float[2] { Mathf.Deg2Rad * -5,	Mathf.Deg2Rad * 90 }
		};


		public static float[][] fingerFlexions01 = new float[3][]
		{
			new float[2] { 0, Mathf.Deg2Rad * 70 },
			new float[2] { 0, Mathf.Deg2Rad * 100 },
			new float[2] { 0, Mathf.Deg2Rad * 90 }
		};


		/// <summary> Calculates only the three flexion angles from a normalized angle. </summary>
		/// <param name="finger"></param>
		/// <param name="normalizedFlexion"></param>
		/// <param name="normalizedAbduction"></param>
		/// <param name="rightHand"></param>
		/// <returns></returns>
		public static float[] Flexions_FromNormalized(SGCore.Finger finger, float normalizedFlexion)
        {
			if (finger == SGCore.Finger.Thumb)
            {
				return new float[3]
				{
					SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, thumbFlexions01[0][0], thumbFlexions01[0][1]),
					SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, thumbFlexions01[1][0], thumbFlexions01[1][1]),
					SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, thumbFlexions01[2][0], thumbFlexions01[2][1])
				};
            }
			return new float[3]
			{
				SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, fingerFlexions01[0][0], fingerFlexions01[0][1]),
				SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, fingerFlexions01[1][0], fingerFlexions01[1][1]),
				SGCore.Kinematics.Values.Map(normalizedFlexion, 0, 1, fingerFlexions01[2][0], fingerFlexions01[2][1])
			};
		}


		public static float[] abduction1_R = new float[5]
		{
			60, //use this later on
			10,
			0,
			-10,
			-20
		};

		//As thumb
		public static Vect3D[] FromNormalized_Thumb(float normalizedFlexion, float normalizedAbduction, bool rightHand)
        {
			int LR = rightHand ? 1 : -1;
			float thumbAbd1 = abduction1_R[0] * LR; //60 / -60
			float[] flexions = Flexions_FromNormalized(SGCore.Finger.Thumb, normalizedFlexion); //calculates the flexions
            return new Vect3D[3]
            {
                new Vect3D(0, flexions[0], Values.Map(normalizedAbduction, 0, 1, 0, Mathf.Deg2Rad*thumbAbd1)),
                new Vect3D(0, flexions[1], 0),
                new Vect3D(0, flexions[2], 0)
            };
        }




		//as finger
		public static Vect3D[] FromNormalized_Finger(SGCore.Finger finger, float normalizedFlexion, float fingerSpread, bool rightHand)
		{
			int LR = rightHand ? 1 : -1;
			float fingerAbd1 = abduction1_R[(int)finger] * LR;
			float[] flexions = Flexions_FromNormalized(finger, normalizedFlexion); //calculates the flexions
            return new Vect3D[3]
            {
                new Vect3D(0, flexions[0], Values.Map(fingerSpread, 0, 1, 0, Mathf.Deg2Rad * fingerAbd1)),
                new Vect3D(0, flexions[1], 0),
                new Vect3D(0, flexions[2], 0)
            };
        }


		// angles to 01

		// Flexion is Anatomy

		public static float NormalizeAbduction(SGCore.Finger finger, float totalAbduction, bool isRight)
        {
			int LR = isRight ? 1 : -1;
			float abd1 = abduction1_R[(int)finger] * LR; //the "max" abduction
			return SGCore.Kinematics.Values.Map(totalAbduction, 0, abd1, 0, 1);
		}


		public bool GetHandPose(out SG_HandPose handPose, bool forceUpdate = false)
		{
			this.RecalculatePose();
			handPose = this.LastPose;
			return true;
		}


		// Update is called once per frame
		void Update()
		{
			if (init)
			{
				init = false;
				RecalculatePose();
			}
		}

    }
}