using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> A script to collect and animate Glove Poses for a SenseGlove DK1  </summary>
	public class SG_DK1Model : MonoBehaviour
	{
		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The HapticGlove from which to retrieve a GlovePose. If the linked glove is not a DK1, this model hides itself. </summary>
		[Header("Tracking Components")]
		public SG_HapticGlove senseGloveDK1;

		/// <summary> Whether or not this model is right handed or not. Used to determine if we should show it or not. </summary>
		[Header("Animation Components")]
		public bool rightHandModel = true;

		/// <summary> Visusal Model for the Hub </summary>
		public GameObject hubModel;

		/// <summary> The individual exoskeleton "fingers" what we animate using the 'pose </summary>
		public SG_DK1Finger[] fingers = new SG_DK1Finger[0];

		/// <summary> This is the glove origin location. Cached for optimization purposes. </summary>
		private Transform myTransform = null;
	
		/// <summary> Evaluated when a new glove connects, determines if we should be animating </summary>
		private bool gloveAvailable = false;

		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Returns the Glove Origin Transform. The fingers move relative to this. </summary>
		public Transform GloveOrigin
        {
            get
            {
				if (myTransform == null) { myTransform = this.transform; }
				return myTransform;
            }
			set
            {
				myTransform = value;
            }
        }
		
		/// <summary> Determines if the visual models for this DK1 are enabled. </summary>
		public bool ModelEnabled
        {
			get
            {
				return hubModel.activeSelf;
            }
			set
            {
				hubModel.SetActive(value);
				for (int f=0; f<this.fingers.Length; f++)
                {
					this.fingers[f].gameObject.SetActive(value);
                }
            }
        }

		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Updating the Exoskeleton


		/// <summary> Update the starting positions of the fingers. </summary>
		/// <param name="gloveModel"></param>
		public void UpdateStartPositions(SGCore.SG.SG_GloveInfo gloveModel)
        {
			//Debug.Log("Linking " + this.name + " to " + gloveModel.ToString());
			for (int f=0; f<this.fingers.Length && f < 5; f++)
            {
				this.fingers[f].linkedTo = (SGCore.Finger)f;
            }
			Transform origin = this.GloveOrigin;
			for (int f=0; f<gloveModel.StartPositions.Length && f< gloveModel.StartRotations.Length && f < fingers.Length; f++)
            {
				fingers[f].transform.rotation = origin.rotation * SG.Util.SG_Conversions.ToUnityQuaternion(gloveModel.StartRotations[f]);
				fingers[f].transform.position = origin.position + (origin.rotation * SG.Util.SG_Conversions.ToUnityPosition(gloveModel.StartPositions[f]));
			}
        }

		/// <summary> Animate the fingers using a GlovePose. </summary>
		/// <param name="glovePose"></param>
		public void UpdateModel(SGCore.SG.SG_GlovePose glovePose)
        {
			Transform origin = this.GloveOrigin;
			for (int f = 0; f < glovePose.JointPositions.Length && f < glovePose.JointRotations.Length && f < fingers.Length; f++)
			{
				fingers[f].UpdateFinger(glovePose, origin);
			}
		}


		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Device Linking

		/// <summary> Link this script to a GloveModel. </summary>
		/// <param name="gloveModel"></param>
		public void LinkDevice(SGCore.SG.SG_GloveInfo gloveModel)
		{
			gloveAvailable = true;
			this.ModelEnabled = true;
			this.UpdateStartPositions(gloveModel);
			SGCore.SG.SG_GlovePose basePose = SGCore.SG.SG_GlovePose.IdlePose(gloveModel);
			UpdateModel(basePose);
		}


		protected void TryLinkDevice()
		{
			if (this.senseGloveDK1 != null && this.senseGloveDK1.InternalGlove != null && this.senseGloveDK1.InternalGlove is SGCore.SG.SenseGlove && senseGloveDK1.InternalGlove.IsRight() == this.rightHandModel)
			{
				SGCore.SG.SG_GloveInfo gloveModel = ((SGCore.SG.SenseGlove)this.senseGloveDK1.InternalGlove).GetGloveModel();
				this.LinkDevice(gloveModel); //LinkDevice will set ModelEnabled to t
			}
			else
            {
				gloveAvailable = false;
				this.ModelEnabled = false;
			}
		}

		

		//---------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour
		
		void OnEnable()
        {
            TryLinkDevice(); //need to check if we're already connected
            this.senseGloveDK1.DeviceConnected.AddListener(TryLinkDevice);
        }

		void OnDisable()
        {
			this.senseGloveDK1.DeviceConnected.RemoveListener(TryLinkDevice);
		}


		// Update is called once per frame
		void Update()
		{
			if (gloveAvailable)
			{
				if (this.senseGloveDK1.InternalGlove != null && this.senseGloveDK1.InternalGlove is SGCore.SG.SenseGlove)
				{
					SGCore.SG.SG_GlovePose glovePose;
					if (((SGCore.SG.SenseGlove)this.senseGloveDK1.InternalGlove).GetGlovePose(out glovePose))
					{
						this.UpdateModel(glovePose);
					}
				}
			}
        }
	}
}