using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	public enum PopupType
	{
		/// <summary> Simple message with no additional cues </summary>
		Default,
		/// <summary> Something requires your attention, but it could wait. Dissapears after a few seconds </summary>
		Warning,
		/// <summary> Something requires your attention right now. Does not dissapear at all. </summary>
		Critical
	}



	/// <summary> Represents a popup notification that can be played though the SG_PopupSystem. </summary>
	public class SG_SimpleNotification
	{
		/// <summary> ID for this notification. </summary>
		public string NotificationID { get; protected set; }

		/// <summary> The Message to display to the user </summary>
		public string Message { get; set; }

		/// <summary> Optional sprite to display alongside the image </summary>
		public Sprite Image { get; set; }

		/// <summary> A value <= 0 means the popup will not be cleared until you do so manually. </summary>
		public float LifeTime { get; set; }

		/// <summary> Create a new Simple notification to send around the SG_SimpleNotificationSystem </summary>
		/// <param name="_ID"></param>
		/// <param name="_message"></param>
		/// <param name="_lifetime"></param>
		/// <param name="img"></param>
		public SG_SimpleNotification(string _ID, string _message, float _lifetime, Sprite img = null)
        {
			if (_ID == null || _ID.Length == 0)
            {
				throw new System.ArgumentException("_ID cannot be NULL or empty!");
            }
			this.NotificationID = _ID;
			this.Message = _message;
			this.Image = img;
			this.LifeTime = _lifetime;
		}

    }
}