#define DEBUG_SG_BATT //if defined, we enter debug more for this script.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> Checks the battery level of your Nova Glove (and optionally that of your two tracked devices) and reports when battery levels are critical. </summary>
	public class SG_BatteryMonitor : MonoBehaviour
	{
		/// <summary> Check every 10 seconds </summary>
		public float checkEvery = 5.0f;
		protected float timer_checkBatteries = 0.0f;

		//warn users at 20% (0.19f, 0.14f, 0.09f) and show ciritcal warning at 5%, 0.04f.
		public Sprite rightGlove_ConnectImg;
		public Sprite rightGlove_DisconnectImg;
		public Sprite leftGlove_ConnectImg;
		public Sprite leftGlove_DisconnectImg;
		public Sprite lowBatteryIcon;
		public Sprite criticalBatteryIcon;

		public const float criticalLevel = 0.04f;

		public const float warningLevel0 = 0.19f;
		public const float warningLevel1 = 0.14f;
		public const float warningLevel2 = 0.09f;


		public const string leftGloveID = "gloveMsg_L"; //the ID for the glove(s).
		public const string rightGloveID = "gloveMsg_R";

		public const string critBattID = "_crit";
		public const string warnBattID = "_warn";
		public const string disconnectID = "_disc";
		public const string connectID = "_conn";

		public const float connectTime = 2.0f;
		public const float disConnectTime = 4.0f;
		public const float lowBattTime = 4.0f;
		public const float critBatteryTime = 0.0f; //infinite


#if DEBUG_SG_BATT
        [Header("Debug")]
        [Range(0, 1)] public float leftNovaLevel = 1.0f;
        public bool leftNovaChargeState = false;
        public bool leftNovaConnectState = false;

        [Range(0, 1)] public float rightNovaLevel = 1.0f;
        public bool rightNovaChargeState = false;
        public bool rightNovaConnectState = false;
#endif

        protected float glove_lastLeftLevel = 1.0f, glove_lastRightLevel = 1.0f;
		protected bool glove_lastLeftCharge = false, glove_lastRightCharge = false;
		protected bool glove_lastLeftConnect = false, glove_lastRightConnect = false;

		protected uint leftConnections = 0, rightConnections = 0;


		protected static string GetID(bool right)
        {
			return right ? rightGloveID : leftGloveID;
        }


		public void ClearNotification(string noteID, bool immedeate)
		{
			//Debug.Log("TODO: " + (immedeate ? "C" : "Smoothly c") + "lear " + noteID);
			SG_NotificationSystem.ClearNotification(this, noteID); //creating a shite notification for this purpose.
		}

		public void SendNotification(string noteID, string message, Sprite image, float time)
		{
			//Debug.Log("TODO: Send Notification " + noteID + " : \"" + message + "\"");
			SG_NotificationSystem.SendNotification(this, new SG_SimpleNotification(noteID, message, time, image));
		}


		public void CheckNovaGlove(bool right)
		{

            // This is for debug purpopses.

#if DEBUG_SG_BATT
            bool currConnState = right ? rightNovaConnectState : leftNovaConnectState;
            bool currChargeState = right ? rightNovaChargeState : leftNovaChargeState;
            float currChargeLevel = right ? rightNovaLevel : leftNovaLevel;
#else

			bool currConnState = false;
			bool currChargeState = false;
			float currChargeLevel = 1.0f;
			SGCore.HapticGlove glove;
			if (SGCore.HapticGlove.GetGlove(right, out glove))
            {
				currConnState = glove.IsConnected();
				if (glove.HasBattery())
                {
					currChargeState = glove.IsCharging();
					glove.GetBatteryLevel(out currChargeLevel);
				}
            }
#endif

            bool lastConnState = right ? glove_lastRightConnect : glove_lastLeftConnect;
			bool lastChargeState = right ? glove_lastRightCharge : glove_lastLeftCharge;
			float lastChargeLevel = right ? glove_lastRightLevel : glove_lastLeftLevel;

			string id = GetID(right);

			if (currConnState) //we are connected now
			{
				if (lastConnState) //we were connected before this
				{
					if (currChargeState && !lastChargeState) //we are now charging, even though we were not before.
					{
						//We don;t care about these anymores
						ClearNotification(id + warnBattID, false);
						ClearNotification(id + critBattID, false);
						//Debug.Log("Charger Connected!");
					}
					else if (!currChargeState) //not charging now
					{
						if (lastChargeState) //because we just disconnected our charger
						{
							//Debug.Log("Charger Disconnected");
							if (currChargeLevel <= criticalLevel)
							{
								//Debug.Log("Glove has exited Charge Mode with critical battery");
								SendNotification(id + critBattID, (right ? "Right" : "Left") + " Glove battery is critically low!", this.criticalBatteryIcon, critBatteryTime); //then send it again!
							}
							else if (currChargeLevel <= warningLevel0)
							{
								//Debug.Log("Glove has  exited Charge Mode with low battery");
								SendNotification(id + warnBattID, (right ? "Right" : "Left") + " Glove battery is running low!", this.lowBatteryIcon, lowBattTime); //then send it again!
							}
						}
						else //we werent charging last frame either.
						{
							if (currChargeLevel <= warningLevel0) //we actually have something to report.
							{
								if (currChargeLevel <= criticalLevel) //we are below crit levels
								{
									if (lastChargeLevel > criticalLevel) //and just dropped below crititcal!
									{
										//TODO: Clear any warnings about low battery
										//TODO: Glove battery is critically low notification
										ClearNotification(id + warnBattID, true); //if there is a battery ID already active, clear it now!
										SendNotification(id + critBattID, (right ? "Right" : "Left") + " Glove battery is critically low!", this.criticalBatteryIcon, critBatteryTime); //then send it again!
										//Debug.Log("Glove battery dropped to critical level!");
									}
								}
								else //we're somewhere below warningLevel, but above the crititcal level...
								{
									if (lastChargeLevel > warningLevel0 //allready confirmed that currChargeLevel <= warningLevel0.
										|| (lastChargeLevel > warningLevel1 && currChargeLevel <= warningLevel1)
										|| (lastChargeLevel > warningLevel2 && currChargeLevel <= warningLevel2)) //first time we dropped below
									{
										//Debug.Log("Battery Level is running low!");
										//ClearNotification(id + warnBattID, true); //if there is a battery ID already active, clear it now!
										SendNotification(id + warnBattID, (right ? "Right" : "Left") + " Glove battery is running low!", this.lowBatteryIcon, lowBattTime); //then send it again!
									}
								}
							}
						}
					}
				}
				else //this is the first time connected
				{
					//Small popup to let the user know the glove has re-connected? But only if this is the second time it's connected, possibly...
					uint connVal = right ? rightConnections : leftConnections;
					if (connVal > 0)
					{
						SendNotification(id + connectID, (right ? "Right" : "Left") + " Glove Connected", (right ? this.rightGlove_ConnectImg : this.leftGlove_ConnectImg), connectTime);
					}
					if (right) { rightConnections++; }
					else { leftConnections++; }

					//Debug.Log("Glove has (re)connected");
					ClearNotification(id + disconnectID, false);
					//Evaluate basic charing levels
					if (!currChargeState)  //if we are charging, I don't care about battery levels
					{
						if (currChargeLevel <= criticalLevel) //reconnected but not on charge
						{
							//TODO: Glove battery is critically low
							SendNotification(id + critBattID, (right ? "Right" : "Left") + " Glove battery is critically low!", this.criticalBatteryIcon, critBatteryTime); //then send it again!
							//Debug.Log("Glove has reconnected with critical battery");
						}
						else if (currChargeLevel <= warningLevel0)
						{
							//TODO: Glove Battery is low
							SendNotification(id + warnBattID, (right ? "Right" : "Left") + " Glove battery is running low!", this.lowBatteryIcon, lowBattTime); //then send it again!
							//Debug.Log("Glove has reconnected with low battery");
						}
					}
				}
			}
			else if (lastConnState) //we'are no longer connected 
			{
				ClearNotification(id + warnBattID, false);
				ClearNotification(id + critBattID, false);
				SendNotification(id + disconnectID, (right ? "Right" : "Left") + " Glove Disconnected", (right ? rightGlove_DisconnectImg : leftGlove_DisconnectImg), disConnectTime);
				//Debug.Log("Glove has disconnected");
			}


			//don;t forget to store it
			if (right)
			{
				glove_lastRightLevel = currChargeLevel;
				glove_lastRightCharge = currChargeState;
				glove_lastRightConnect = currConnState;
			}
			else
			{
				glove_lastLeftLevel = currChargeLevel;
				glove_lastLeftCharge = currChargeState;
				glove_lastLeftConnect = currConnState;
			}

		}



		public void CheckDevices()
		{
			CheckNovaGlove(true);
			CheckNovaGlove(false);
		}





		void Start()
		{
			//Collect current states?
			this.timer_checkBatteries = checkEvery; //so we begin checking in Update
		}


		void Update()
		{
			timer_checkBatteries += Time.deltaTime;
			if (timer_checkBatteries >= this.checkEvery)
			{
				CheckDevices();
				timer_checkBatteries = 0.0f; //reset timee
			}
		}







	}
}