using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> A special kind of HandPoser3D that updates itself </summary>
	public class SG_LinkedHandPoser3D : SG_HandPoser3D
	{
		/// <summary> GameObject from which the IHandPoseProvider is extracted </summary>
		[Header("Hand Pose Input")]
		public GameObject handPoseSource;

		/// <summary> The actual Hand Pose Source </summary>
		public IHandPoseProvider handPoser;

		public Color wireframeColor = Color.white;

		protected virtual void Start()
		{
			if (this.handPoser == null && this.handPoseSource != null)
			{
				this.handPoser = this.handPoseSource.GetComponent<IHandPoseProvider>();
			}
		}


		protected virtual void LateUpdate()
        {
			if (this.handPoser != null)
            {
				SG_HandPose pose;
				if (handPoser.GetHandPose(out pose))
				{
					this.UpdateHandPoser(pose);
				}
			}
        }


		protected virtual void OnEnable()
        {
			this.LinesEnabled = true;
        }

		protected virtual void OnDisable()
        {
			this.LinesEnabled = false;
        }


#if UNITY_EDITOR
		protected override void OnValidate()
        {
            base.OnValidate();
			this.LinesColor = this.wireframeColor;
        }
#endif
	}

}