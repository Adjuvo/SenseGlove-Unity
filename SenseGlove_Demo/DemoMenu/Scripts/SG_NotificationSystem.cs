#define SG_NOTIFICATIONS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{
	/// <summary> Adds dynamic arguments to the static SimpleNotification Class. </summary>
	public class NotificationArgs : System.EventArgs
    {
		/// <summary> The sender of the notification </summary>
        public object Sender { get; set; }

		/// <summary> Notification Contents </summary>
		public SG_SimpleNotification Notification { get; set; }

		/// <summary> System time where this notification was created </summary>
		public System.DateTime timeStamp { get; set; }

		public NotificationArgs(object _sender, SG_SimpleNotification _popup)
        {
			this.Sender = _sender;
			this.Notification = _popup;
			timeStamp = System.DateTime.Now;
        }
    }

	
    /// <summary> A system to which we can publish little notifying popups.  </summary>
    public class SG_NotificationSystem
	{


		//------------------------------------------------------------------------------------------------------------------------------------
		// Events

		public delegate void NotificationReceivedEventHandler(object sender, NotificationArgs e);

		public static event NotificationReceivedEventHandler NotificationRecieved;

		private static void OnNotificationGet(object sender, SG_SimpleNotification notification)
		{
			NotificationArgs e = new NotificationArgs(sender, notification);
			if (NotificationRecieved != null)
			{
				NotificationRecieved.Invoke(sender, e);
			}
		}

		public static event NotificationReceivedEventHandler NotificationClear;

		private static void OnNotificationClear(object sender, SG_SimpleNotification notification)
		{
			NotificationArgs e = new NotificationArgs(sender, notification);
			if (NotificationClear != null)
			{
				NotificationClear.Invoke(sender, e);
			}
		}




		public delegate void ClearAllEventHandler(object sender, bool immedeate);

		public static event ClearAllEventHandler ClearAllNotifications;

		private static void OnAllNotificationClear(object sender, bool immedeate)
		{
			if (ClearAllNotifications != null)
			{
				ClearAllNotifications.Invoke(sender, immedeate);
			}
		}



		public delegate void BoolChangedEventHandler(object sender, bool newValue);

		public static event BoolChangedEventHandler NotificationEnableChanged;

		private static void OnNotificationEnableChanged(object sender, bool newValue)
		{
			if (NotificationEnableChanged != null)
			{
				NotificationEnableChanged.Invoke(sender, newValue);
			}
		}


		//------------------------------------------------------------------------------------------------------------------------------------
		// Notification Calls

		/// <summary> Determines whether or not notifications can be sent through this ssytem. </summary>
		protected static bool notifies = true;

		/// <summary> Enable / disable notification settings. </summary>
		public static void SetNotificationsEnabled(object sender, bool newValue)
		{
			bool currValue = notifies;
			if (currValue && !newValue) //turning it off
            {
				ClearNotifications(null, false);
			}
			notifies = newValue;  //acyually changed value.
			if (currValue != newValue) //it has definitely changed
            {
				OnNotificationEnableChanged(sender, newValue);
			}
		}

		/// <summary> Whether or not Notifications have been enabled / disabled. </summary>
		/// <returns></returns>
		public static bool GetNotificationsEnabled()
        {
			return notifies;
        }




		/// <summary> Sends a new notification to the SG Notification System. If there are any subscribers, they will show them. </summary>
		/// <param name="sender"></param>
		/// <param name="notification"></param>
		public static void SendNotification(object sender, SG_SimpleNotification notification)
        {
			if (notifies)
			{
				OnNotificationGet(sender, notification);
			}
        }

		/// <summary> Clears a specific notification. If there are any subscribers, they will be cleared form them. </summary>
		/// <param name="sender"></param>
		/// <param name="notification"></param>
		public static void ClearNotification(object sender, string notificationID)
		{
			OnNotificationClear(sender, new SG_SimpleNotification(notificationID, "N\\A", 0.01f)); //just create an empty instance since it will be cleared anyway.
		}

		/// <summary> Clears a specific notification. If there are any subscribers, they will be cleared form them. </summary>
		/// <param name="sender"></param>
		/// <param name="notification"></param>
		public static void ClearNotification(object sender, SG_SimpleNotification notification)
		{
			OnNotificationClear(sender, notification);
		}

		/// <summary> Clears all notifications on the system. If there are any subscribers, they will clear the visual(s). </summary>
		/// <param name="sender"></param>
		public static void ClearNotifications(object sender, bool collapseImmedeately)
		{
			OnAllNotificationClear(sender, collapseImmedeately);
		}



	}
}