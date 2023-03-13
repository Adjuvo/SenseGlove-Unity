using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Examples
{
    /// <summary> Demonstrates a simple popup shown in the Demo Menu. </summary>
    public class SGEx_PopupSystem : MonoBehaviour
    {
        public string popupText = "";

        public Sprite popupImage = null;

        public float popupLifeTime_s = 1.0f;


        protected float timer_popup = 0;
        protected float timeBetweenPopups = 1.0f;


        public void SendMessage()
        {
            SG.Util.SG_NotificationSystem.SendNotification(this, new Util.SG_SimpleNotification("example", popupText, popupLifeTime_s, popupImage));
            timer_popup = 0.0f;
        }


        private void OnEnable()
        {
            timer_popup = popupLifeTime_s + timeBetweenPopups + 1.0f;
        }

        private void Update()
        {
            timer_popup += Time.deltaTime;
            if (timer_popup >= timeBetweenPopups + popupLifeTime_s)
            {
                SendMessage();
            }
        }

    }
}
