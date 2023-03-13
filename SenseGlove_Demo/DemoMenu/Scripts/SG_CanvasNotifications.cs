using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{ 

	/// <summary> A system to visualize SenseGlove notifications on a Unity UI Canvas. </summary>
	public class SG_CanvasNotifications : MonoBehaviour
	{

		/// <summary> Where popup messages will appear </summary>
		public RectTransform messageContainer;

        /// <summary> Used as a base </summary>
        public SG_PopupCanvas templateMessage;

        public float unfoldTime = 0.25f;
        public float collapseTime = 0.25f;

        /// <summary> List of all popups active on the canvas at this time. </summary>
        protected List<SG_PopupCanvas> activeNotifications = new List<SG_PopupCanvas>();


        public void CleanupActive()
        {
            for (int i=0; i<activeNotifications.Count;)
            {
                if (activeNotifications[i] == null) //it's cleaned itself up.
                {
                    this.activeNotifications.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public int GetNotificationIndex(SG_SimpleNotification notification)
        {
            for (int i=0; i<this.activeNotifications.Count; i++)
            {
                if (this.activeNotifications[i].notification.NotificationID.Equals(notification.NotificationID))
                {
                    return i;
                }
            }
            return -1;
        }



        private void SG_NotificationSystem_NotificationRecieved(object sender, NotificationArgs e)
        {
            CleanupActive();
            //Debug.Log("Attempt to show " + e.Notification.notificationID);
            int index = GetNotificationIndex(e.Notification);
            if (index < 0) //we are not showing it yet
            {
                SG_PopupCanvas visual = this.SpawnNewCanvasObj(e.Notification);
                if (visual != null)
                {
                    this.activeNotifications.Add(visual);
                    visual.StartShowing(this.unfoldTime, this.collapseTime);
                }
            }
            //else
            //{
            //    Debug.Log("Already showing " + e.Notification.notificationID);
            //}
        }
        
        private void SG_NotificationSystem_NotificationClear(object sender, NotificationArgs e)
        {
            CleanupActive();
            //Debug.Log("Attempt to clear " + e.Notification.notificationID);
            int index = GetNotificationIndex(e.Notification);
            if (index > -1) //we are not showing it yet
            {
                this.activeNotifications[index].StartHiding(this.collapseTime);
            }
            //else
            //{
            //    Debug.Log("Not showing " + e.Notification.notificationID);
            //}
        }


        private void SG_NotificationSystem_ClearAllNotifications(object sender, bool immedeate)
        {

            float hideTime = immedeate ? 0.0f : this.collapseTime;
            //Debug.Log("Attempt to clear all notifications - " + (immedeate).ToString() + " => " + hideTime.ToString());
            for (int i=0; i<this.activeNotifications.Count; i++)
            {
                if (activeNotifications[i] != null) //becuase we don;t check for nulls untill after telling the remaining ones to collapse.
                {
                    activeNotifications[i].StartHiding(hideTime);
                }
            }
            CleanupActive();
        }


        protected SG_PopupCanvas SpawnNewCanvasObj(SG_SimpleNotification notification)
        {
            if (this.templateMessage == null)
            {
                Debug.LogError(this.name + " requires a template object!");
                return null;
            }
            SG_PopupCanvas res = GameObject.Instantiate(this.templateMessage, this.templateMessage.transform.parent, false);
            res.LinkTo(notification);
            res.gameObject.name = notification.NotificationID;
            res.gameObject.SetActive(true);
            return res;
        }




        protected void OnEnable()
        {
            CleanupActive();
            SG_NotificationSystem.NotificationRecieved += SG_NotificationSystem_NotificationRecieved;
            SG_NotificationSystem.NotificationClear += SG_NotificationSystem_NotificationClear;
            SG_NotificationSystem.ClearAllNotifications += SG_NotificationSystem_ClearAllNotifications;
        }

        

        protected void OnDisable()
        {
            SG_NotificationSystem.NotificationRecieved -= SG_NotificationSystem_NotificationRecieved;
            SG_NotificationSystem.NotificationClear -= SG_NotificationSystem_NotificationClear;
            SG_NotificationSystem.ClearAllNotifications -= SG_NotificationSystem_ClearAllNotifications;
            CleanupActive();
        }



		// Use this for initialization
		void Start()
		{
            if (this.templateMessage != null)
            {
                this.templateMessage.gameObject.SetActive(false);
            }
		}

	}
}