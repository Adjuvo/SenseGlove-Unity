using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Util
{
	/// <summary> Shows the status of a HapticGlove. </summary>
	public class SG_GloveStatusIcon : MonoBehaviour
	{
		[Header("Connection Settings")]
		public bool forRightHand = true;

		[Header("UI Elements")]
		public GameObject disconnectedIcon;
		public GameObject connectedIcon;
		public Image[] batteryLvlPips = new Image[0]; //contains the battery pips, which we won't display if the glove has no battery


		protected const float checkTime = 1.0f;
		protected const float lowBatteryThreshold = 0.20f;

		protected Coroutine updateRoutine = null;

		protected bool Updating { get; private set; }

		protected bool firstConnection = true;

		/// <summary> Skip the battery updating if the device does not have a battery. </summary>
		protected bool updateBatteries = false;

		protected float lastBattLvl = -1.0f;

		/// <summary> Normal Display </summary>
		public Color batteryColor = Color.green;
		public Color lowBatteryColor = Color.red;
		public Color chargeBatteryColor = Color.blue;
		public Color emptyPipColor = Color.gray;


		/// <summary> Whether or not the battery icons are enabled. </summary>
		public bool BatteryPipsEnabled
        {
			get { return batteryLvlPips.Length > 0 ? batteryLvlPips[0].enabled : false; }
			set 
			{ 
				for (int i=0; i<batteryLvlPips.Length; i++)
                {
					batteryLvlPips[i].enabled = (value);
                }
			}
        }


		public void SetStateIcon(bool connected)
        {
			if (disconnectedIcon != null) { disconnectedIcon.SetActive(!connected); }
			if (connectedIcon != null) { connectedIcon.SetActive(connected); }
        }

		


		public void UpdateIcons()
        {
			SGCore.HapticGlove firstGlove;
			if (SGCore.HapticGlove.GetGlove(forRightHand, out firstGlove))
            {
				SetStateIcon(true);
				if (firstConnection)
                {
					firstConnection = false;
					updateBatteries = firstGlove.HasBattery();
					BatteryPipsEnabled = updateBatteries; //if we don't have a battery, those pips won't show up again.
				}
				if (updateBatteries)
                {
					float battLvl;
					if (firstGlove.GetBatteryLevel(out battLvl))
                    {
						UpdateBatteryIcons(battLvl, firstGlove.IsCharging());
                    }
                }
			}
			else //there is no glove connected with said handed-ness.
            {
				SetStateIcon(false);
				if (BatteryPipsEnabled) { BatteryPipsEnabled = false; }
			}
        }


		/// <summary> Set the appropriate battery pips based on the battery level. (For example, if you've got 4 pips; 0-25% = 1 pip, 25-50% = 2 pips, 50-75% = 3 pips, 75-100% = 4 pips </summary>
		/// <param name="battLevel01"></param>
		public void UpdateBatteryIcons(float battLevel01, bool charging)
        {
			if (batteryLvlPips.Length > 0)
			{
				Color pipColor = batteryColor;
				if (charging) { pipColor = chargeBatteryColor; }
				else if (battLevel01 <= lowBatteryThreshold) { pipColor = lowBatteryColor; }

				int showUpTo = 0; //show pips up to this one.
				float chargeStep = 1.0f / (float) batteryLvlPips.Length; //the amount of charge that each pip represents
				float upperLvl = chargeStep; //0.25, 0.5, 0.75, 1.0...
				for (int i=0; i< batteryLvlPips.Length; i++)
                {
					if (battLevel01 <= upperLvl) //we're lower than the current pip can represent. SO the next one must be disabled.
                    {
						showUpTo = i;
						break;
                    }
					upperLvl += chargeStep; //increase, it's more efficient than (i+1)*step
                }

				for (int i=0; i<batteryLvlPips.Length; i++)
                {
					batteryLvlPips[i].color = i <= showUpTo ? pipColor : emptyPipColor;
                }
			}
        }



		IEnumerator UpdatePeriodically(float time)
		{
			while (Updating)
			{
				//Debug.Log(Time.timeSinceLevelLoad + "Method is executed");
				UpdateIcons();
				yield return new WaitForSecondsRealtime(time);
			}
		}

		public void StartUpdating()
		{
			if (updateRoutine == null)
            {
				Updating = true;
				updateRoutine = StartCoroutine(UpdatePeriodically(checkTime));
            }
		}

		public void StopUpdating()
        {
			if (updateRoutine != null)
            {
				Updating = false;
				StopCoroutine(updateRoutine);
            }
        }

		


		// Use this for initialization
		void Start()
		{
			StartUpdating();
		}
	}
}
