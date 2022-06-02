using UnityEngine;

namespace SG
{
	/// <summary> Used to place a SG_HandPose in virtual space - Allowing other colliders to follow them. Essential creates a group of Transforms that you can update, access and draw lines between for debugging. </summary>
	public class SG_HandPoser3D : MonoBehaviour
	{
        //------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The location of the Wrist in 3D space. All fingers will become part of this transform. </summary>
        [Header("Hand Components")]
        public Transform wrist;

        /// <summary> Inidvidual joints of the thumb, starting at the CMC Joint, and ending a the fingertip. This Script will ensure they exist and have the proper naming scheme. </summary>   
        public Transform[] thumbJoints = new Transform[0];
        /// <summary> Inidvidual joints of the index finger, starting at the MCP Joint, and ending a the fingertip. This Script will ensure they exist and have the proper naming scheme. </summary>   
		public Transform[] indexJoints = new Transform[0];
        /// <summary> Inidvidual joints of the middle finger, starting at the MCP Joint, and ending a the fingertip. This Script will ensure they exist and have the proper naming scheme. </summary>   
        public Transform[] middleJoints = new Transform[0];
        /// <summary> Inidvidual joints of the ring finger, starting at the MCP Joint, and ending a the fingertip. This Script will ensure they exist and have the proper naming scheme. </summary>   
        public Transform[] ringJoints = new Transform[0];
        /// <summary> Inidvidual joints of the pinky finger, starting at the MCP Joint, and ending a the fingertip. This Script will ensure they exist and have the proper naming scheme. </summary>   
        public Transform[] pinkyJoints = new Transform[0];

        /// <summary> A full array of each finger joint, used to easily iterate through a HandPose. Starts out empty, but is filled with the individual finger joints. </summary>
		public Transform[][] fingerJoints = new Transform[0][];

        /// <summary> Line Renderers to draw from the wrist to each fignertip when DebugLines is set to True. </summary>
		protected LineRenderer[] handLines = new LineRenderer[0];
        /// <summary> LineRenderer to draw a line between MCP joints to create a decent-looking hand. </summary>
		protected LineRenderer mcpLine;
        /// <summary> Determines the color for each of the Line Renderers, to distinguish this HandPoser from a different one. </summary>
		protected Color handColor = Color.white;

		protected bool setup = true;

        //------------------------------------------------------------------------------------------------------------------------
        // HandPoser Creation & Validation

        /// <summary> Ensures that the fingerCurrent array is always of size 4, set to the right parent, and has the correct anatomical name. </summary>
        /// <param name="currentFingers"></param>
        /// <param name="finger"></param>
        /// <param name="setParent"></param>
        /// <returns> A 'corrected' version of currentFingers. CurrentFingers itself is not affected. </returns>
        protected static Transform[] ValidateFinger(Transform[] currentFingers, SGCore.Finger finger, Transform setParent = null)
        {
            Transform[] valids = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                if (currentFingers.Length > i && currentFingers[i] != null)
                {
                    valids[i] = currentFingers[i];
                }
                else
                {
                    GameObject newEmpty = new GameObject();
                    valids[i] = newEmpty.transform;
                }
                valids[i].name = SGCore.Kinematics.Anatomy.GetJointName(finger, i); //ensure it has a proper name.
                valids[i].parent = setParent;
            }
            return valids;
        }


        /// <summary> Setup the Transforms of this poser so they are valid for our Animation Components. </summary>
        public void SetupTransforms()
        {
			if (setup)
			{
				setup = false;
				//setup wrist object. Create it if it doesn't exist already.
				if (this.wrist == null)
				{
					GameObject wristObj = new GameObject("Wrist");
					wrist = wristObj.transform;
				}
				// Place the Wrist Object at my local position
				wrist.parent = this.transform;
				wrist.localRotation = Quaternion.identity;
				wrist.localPosition = Vector3.zero;

				Transform newParent = wrist; //chaching it here so I could change it if I wanted (also negligible performace boost).
											 //validate finger joint arrays
				thumbJoints = ValidateFinger(thumbJoints, SGCore.Finger.Thumb, newParent);
				indexJoints = ValidateFinger(indexJoints, SGCore.Finger.Index, newParent);
				middleJoints = ValidateFinger(middleJoints, SGCore.Finger.Middle, newParent);
				ringJoints = ValidateFinger(ringJoints, SGCore.Finger.Ring, newParent);
				pinkyJoints = ValidateFinger(pinkyJoints, SGCore.Finger.Pinky, newParent);
				//Link Finger joints
				fingerJoints = new Transform[5][];
				fingerJoints[(int)SGCore.Finger.Thumb] = thumbJoints;
				fingerJoints[(int)SGCore.Finger.Index] = indexJoints;
				fingerJoints[(int)SGCore.Finger.Middle] = middleJoints;
				fingerJoints[(int)SGCore.Finger.Ring] = ringJoints;
				fingerJoints[(int)SGCore.Finger.Pinky] = pinkyJoints;
			}
        }

     
        
        //------------------------------------------------------------------------------------------------------------------------
        // Updating the HandPoser
     

        /// <summary> Updates the wrist- and finger transforms to match a desired SG_HandPose. </summary>
        /// <param name="handPose"></param>
        protected void UpdateTransforms(SG_HandPose handPose)
		{
			this.wrist.rotation = handPose.wristRotation;
			this.wrist.position = handPose.wristPosition;
			for (int f = 0; f < fingerJoints.Length; f++)
			{
				for (int j = 0; j < fingerJoints[f].Length; j++)
				{
					fingerJoints[f][j].rotation = this.wrist.rotation * handPose.jointRotations[f][j];
					fingerJoints[f][j].position = this.wrist.position + (this.wrist.rotation * handPose.jointPositions[f][j]);
				}
			}
		}

        /// <summary> Updates the wrist- and finger transforms to match a desired SG_HandPose. Also updates any debug components if they are enabled. </summary>
        /// <param name="pose"></param>
        public virtual void UpdateHandPoser(SG_HandPose pose)
		{
			this.UpdateTransforms(pose);
			if (this.LinesEnabled)
			{
				this.UpdateLineRenderers();
			}
		}


		/// <summary> For now, sets all positions (not rotations) of this poser's transforms to be the same as the HandModelInfo (3D model) provided. Used during TrackedHand Setup so we can calculate the proper offsets. </summary>
		/// <param name="handModel"></param>
		public virtual void MatchJoints(SG_HandModelInfo handModel)
        {
			try
			{
				Transform[][] modelJoints = handModel.FingerJoints;
				this.wrist.rotation = handModel.wristTransform.rotation;
				this.wrist.position = handModel.wristTransform.position;
				for (int f = 0; f < fingerJoints.Length; f++)
				{
					for (int j = 0; j < fingerJoints[f].Length; j++)
					{
						this.fingerJoints[f][j].rotation = handModel.wristTransform.rotation;
						this.fingerJoints[f][j].position = modelJoints[f][j].position;
					}
				}
				if (this.LinesEnabled)
				{
					this.UpdateLineRenderers();
				}
			}
			catch (System.NullReferenceException ex)
            {
				throw new System.NullReferenceException(ex.Message);
            }
		}

        /// <summary> Checks if a HandPoser exists before updating it. Used when it is an optional component for other scripts. </summary>
        /// <param name="handPoser"></param>
        /// <param name="pose"></param>
        public static void UpdatePoser(SG_HandPoser3D handPoser, SG_HandPose pose)
        {
            if (handPoser != null && pose != null)
            {
                handPoser.UpdateHandPoser(pose);
				//Debug.Log(Time.timeSinceLevelLoad + " : " + handPoser.name + " Updated", handPoser);
            }
        }




        //------------------------------------------------------------------------------------------------------------------------
        // Transform Accessors - Atomatically select based on Anatomy.


		/// <summary> Retruns the transfrom of a hand section. Returns null is it does not exist. </summary>
		/// <param name="handSection"></param>
		/// <returns></returns>
		public Transform GetTransform(HandJoint handSection)
        {
			switch (handSection)
            {
				case HandJoint.Wrist:
					return this.wrist;

				case HandJoint.Thumb_CMC:
					return this.thumbJoints[0];
				case HandJoint.Thumb_MCP:
					return this.thumbJoints[1];
				case HandJoint.Thumb_IP:
					return this.thumbJoints[2];
				case HandJoint.Thumb_FingerTip:
					return this.thumbJoints[3];

				case HandJoint.Index_MCP:
					return this.indexJoints[0];
				case HandJoint.Index_PIP:
					return this.indexJoints[1];
				case HandJoint.Index_DIP:
					return this.indexJoints[2];
				case HandJoint.Index_FingerTip:
					return this.indexJoints[3];

				case HandJoint.Middle_MCP:
					return this.middleJoints[0];
				case HandJoint.Middle_PIP:
					return this.middleJoints[1];
				case HandJoint.Middle_DIP:
					return this.middleJoints[2];
				case HandJoint.Middle_FingerTip:
					return this.middleJoints[3];

				case HandJoint.Ring_MCP:
					return this.ringJoints[0];
				case HandJoint.Ring_PIP:
					return this.ringJoints[1];
				case HandJoint.Ring_DIP:
					return this.ringJoints[2];
				case HandJoint.Ring_FingerTip:
					return this.ringJoints[3];

				case HandJoint.Pinky_MCP:
					return this.pinkyJoints[0];
				case HandJoint.Pinky_PIP:
					return this.pinkyJoints[1];
				case HandJoint.Pinky_DIP:
					return this.pinkyJoints[2];
				case HandJoint.Pinky_FingerTip:
					return this.pinkyJoints[3];
			}
			return null;
        }
        


        /// <summary> Get the Transform of a specific finger, at a joint index, ranging from 0 (MCP/CMC) to the 3 (fingerTip) </summary>
        /// <param name="finger"></param>
        /// <param name="jointIndex03">0, 1, 2 or 3</param>
        /// <returns></returns>
        public Transform GetTransform(SGCore.Finger finger, int jointIndex03)
        {
            return GetTransform(ToHandJoint(finger, jointIndex03));
        }


		/// <summary> As opposed to using this poser as a refrence, parent an object to a specific handJoint. Recommended for hover colliders, not for physics colliders. </summary>
		/// <param name="obj"></param>
		/// <param name="toJoint"></param>
		public void ParentObject(Transform obj, HandJoint toJoint)
        {
			Transform jointTransf = GetTransform(toJoint);
			if (jointTransf != null)
            {
				obj.transform.parent = jointTransf;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------
        // Utility Function

		public static bool IsFingerTip(HandJoint joint)
        {
			return joint == HandJoint.Thumb_FingerTip || joint == HandJoint.Index_FingerTip
				|| joint == HandJoint.Middle_FingerTip || joint == HandJoint.Ring_FingerTip
				|| joint == HandJoint.Pinky_FingerTip;
        }

		public static bool IsDistalPhalange(HandJoint from, HandJoint to)
        {
			return ((IsFingerTip(from) || IsFingerTip(to)) && Mathf.Abs(to - from) == 1);
        }

        /// <summary> Scales the Y axis of a capsule collider and positions it such that its main axis (height, Y) is equal to the distance between the specified joints. </summary>
        /// <param name="collider">Collider height should be 2.0f, should be scaled to the proper x,z that is desired, and scale.x should equal scale.z! </param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <remarks> Creates relatively accurate finger bones, regardless of whan Hand Model is used, provided you set up the correct xz scales yourself. </remarks>
        public void StretchCapsule(CapsuleCollider collider, HandJoint from, HandJoint to)
        {
			Transform transform_from = this.GetTransform(from);
			Transform transform_to = this.GetTransform(to);
			if (transform_from != null && transform_to != null)
			{
				float length = (transform_to.position - transform_from.position).magnitude;
				float realRadius = collider.radius * collider.transform.lossyScale.z; //I'm using z for now, though it and z should have the same scale.

				//Debug.Log("Stretching " + collider.name + " between " + from.ToString() + " and " + to.ToString() + " L = " + length);
				Vector3 midPos = (transform_from.position + transform_to.position) / 2.0f;
				Vector3 currScale = collider.transform.localScale;
				currScale.y = (length / 2.0f) + realRadius; //2.0f is acyually the height. Could do something with this later on, but for now I'm assuming all colliders height is 2*y.

				if (IsDistalPhalange(from, to)) //lastJoint
                {
					//Debug.Log(from + " - " + to  + " is a Distal Phalange!");
					//fThis one must be smaller, since the fingertip ens at the model, not just before it.
					currScale.y = currScale.y - realRadius;
					//TODO: Shift the position (backwards)
                }

				collider.transform.position = midPos;
				collider.transform.localScale = currScale;
			}
        }


       

        //TODO: Move these to the C# API



		/// <summary> Convert a finger+jointindex into a HandJoint. Joint index must ranging from 0 (MCP/CMC) to the 3 (fingerTip)</summary>
		/// <param name="finger"></param>
		/// <param name="jointIndex03"></param>
		/// <returns></returns>
		public static HandJoint ToHandJoint(SGCore.Finger finger, int jointIndex03)
		{
			//HandJoints start a 0 with the wrist, and then are sorted by finger, CMC to TIP (4 items).
			//because wrist is 1, we need to do +1;
			int f = ( ((int)finger ) * 4) + 1; //example; middle finger, this should give us the index of MiddleFinger MCP joint.
			return (HandJoint)(f + jointIndex03); //example; from the MCP joint we then go up to 3 forward.
        }
		
        /// <summary> Safely check which finger a HandJoint belongs to. Returns false if the HandJoint specified does not belong to a finger. </summary>
        /// <param name="HandJoint"></param>
        /// <param name="finger"></param>
        /// <returns></returns>
		public static bool ToFinger(HandJoint HandJoint, out SGCore.Finger finger)
        {
			if (HandJoint == HandJoint.None || HandJoint == HandJoint.Wrist)
            {
				finger = SGCore.Finger.Thumb;
				return false;
			}
			int conv = Mathf.FloorToInt( ((int)HandJoint - 1) / 4.0f ); //-1 because wrist starts at 0 instead of the thumb.
			finger = (SGCore.Finger)conv;
			return true;
        }

        /// <summary> Convert a handJoint notation back into a finger+jointIndex, so we know where to send haptics, for instance. </summary>
        /// <param name="handJoint"></param>
        /// <param name="finger">0 (thumb) to 4 (pinky)</param>
        /// <param name="jointIndex"></param>
        /// <returns></returns>
		public static bool ToFinger(HandJoint handJoint, out int finger, out int jointIndex)
        {
            if (handJoint == HandJoint.None || handJoint == HandJoint.Wrist)
            {
                finger = 0;
                jointIndex = -1;
                return false;
            }
			finger = Mathf.FloorToInt( ((int)handJoint - 1) / 4.0f ); //which of the fingers.
			int rawHP = (int)handJoint;
			jointIndex = rawHP - (4 * finger) - 1; //-1 because of the wrist starting at 1.
			return true;
		}

        /// <summary> Convert a handJoint notation back into a finger+jointIndex, so we know where to send haptics, for instance. </summary>
        /// <param name="handJoint"></param>
        /// <param name="finger"></param>
        /// <param name="jointIndex"></param>
        /// <returns></returns>
		public static bool ToFinger(HandJoint handJoint, out SGCore.Finger finger, out int jointIndex)
		{
			int f;
			bool res = ToFinger(handJoint, out f, out jointIndex);
			finger = (SGCore.Finger)f;
			return res;
		}




        //------------------------------------------------------------------------------------------------------------------------
        // Debug Components and Functions

        /// <summary> Creates the Line Renderers we need for this HandPoser, using the correct LinesColor. </summary>
        protected void SetupLineRenderers()
        {
            handLines = new LineRenderer[5]; //no. 6 is for the mcp joint connector.
            for (int f = 0; f < handLines.Length; f++)
            {
                handLines[f] = SG.Util.SG_Util.AddDebugRenderer(this.gameObject, 5, this.handColor);
            }
            mcpLine = SG.Util.SG_Util.AddDebugRenderer(this.gameObject, 5, this.handColor);
        }

        /// <summary> Enables / Disables rendering of the handPoser - Drawing lines between the Transforms. </summary>
        public bool LinesEnabled
		{
			get { return mcpLine != null && mcpLine.enabled; }
			set
			{
				if (value && mcpLine == null) //We don't create these until the first time our program needs them.
				{
					SetupLineRenderers();
				}
				if (mcpLine != null) { mcpLine.enabled = value; }
				for (int i = 0; i < this.handLines.Length; i++)
				{
					handLines[i].enabled = value;
				}
			}
		}

		/// <summary> The colour of the Line Renderers that this HandPoser uses for visulatization. Used to distinguish it from other posers. </summary>
		public Color LinesColor
		{
			get { return this.handColor; }
			set
			{
				this.handColor = value; //will spawn the linerenderers with the correct color next time.
				if (mcpLine != null)
				{
					//Debug.Log("Setting " + this.name + " to " + value.ToString());
					mcpLine.startColor = value;
					mcpLine.endColor = value;
					mcpLine.material.color = value;
				}
				for (int i = 0; i < this.handLines.Length; i++)
				{
					//Debug.Log("Setting " + this.name + " to " + value.ToString());
					handLines[i].startColor = value;
					handLines[i].endColor = value;
					handLines[i].material.color = value;
				}
			}
		}

        /// <summary> Update the positions within the Line Renderers. </summary>
		protected void UpdateLineRenderers()
		{
			for (int f = 0; f < this.fingerJoints.Length; f++)
			{
				handLines[f].SetPosition(0, this.wrist.position);
				for (int j = 0; j < this.fingerJoints[f].Length; j++)
				{
					handLines[f].SetPosition(j + 1, this.fingerJoints[f][j].position);
				}
				int mcpIndex = f == 0 ? 1 : 0; //thumb MCP isn't at 0, that's the CMC joint.
				mcpLine.SetPosition(f, this.fingerJoints[f][mcpIndex].position);
			}
		}

        /// <summary> Optional helper function: Draws Debug.DrawLines between the Transforms as an alternative to using LineRederers. </summary>
		public void DrawDebugLines()
		{
			for (int f = 0; f < this.fingerJoints.Length; f++)
			{
				Transform lastJoint = this.wrist;
				for (int j = 0; j < this.fingerJoints[f].Length; j++)
				{
					Debug.DrawLine(lastJoint.position, this.fingerJoints[f][j].position, this.handColor);
					lastJoint = this.fingerJoints[f][j];
				}
				//Draw a line between MCP joints
				if (f > 0)
				{
					int mcpIndex = f == 1 ? 1 : 0; //thumb MCP isn't at 0, that's the CMC joint.
					Debug.DrawLine(this.fingerJoints[f - 1][mcpIndex].position, this.fingerJoints[f][0].position, this.handColor);
				}
			}
		}




		//------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		/// <summary> Fires when the HandPoser is created. </summary>
		protected virtual void Awake()
		{
			SetupTransforms();
		}

        /// <summary> Fires when the HandPoser is destroyed. I need to release the LineRenderers created through code. </summary>
        protected virtual void OnDestroy()
		{
			for (int i = 0; i < this.handLines.Length; i++)
			{
				if (handLines[i] != null) { Destroy(this.handLines[i].material); }
			}
			if (this.mcpLine != null)
			{
				Destroy(this.mcpLine.material);
			}
		}

#if UNITY_EDITOR
        /// <summary> When we change the HandColor through the Inspector, we should update the LineRenderers as well. </summary>
        protected virtual void OnValidate()
		{
            if (Application.isPlaying)
            {
                this.LinesColor = this.handColor;
            }
		}
#endif

    }
}