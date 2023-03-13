using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> ScriptableObject version of a SimpleNotification. Allows us to define one as an asset. </summary>
	[CreateAssetMenu(fileName = "Notification", menuName = "SenseGlove/Popup Notification")]
	public class SG_SimpleScriptableNotification : ScriptableObject
	{
		///// <summary> ID for this notification. </summary>
		//public string notificationID;

		/// <summary> The Message to display to the user </summary>
		public string message = "Insert Message here";

		/// <summary> A value <= 0 means the popup will not be cleared until you do so manually. </summary>
		public float lifeTime = 2.0f;

		/// <summary> Optional sprite to display alongside the image </summary>
		public Sprite image;


		public SG_SimpleNotification Notification
        {
			get
            {
				string msgID = this.name;
				return new SG_SimpleNotification(msgID, this.message, this.lifeTime, this.image);
            }
        }

		/// <summary> Send this Notification to the SG_Notification System </summary>
		public void Send()
        {
			SG.Util.SG_NotificationSystem.SendNotification(this, this.Notification);
        }

		/// <summary> Clears this Notification in the SG_Notification System </summary>
		public void Clear()
        {
			SG.Util.SG_NotificationSystem.ClearNotification(this, this.Notification);
		}

	}
}