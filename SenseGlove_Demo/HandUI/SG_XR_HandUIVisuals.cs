//#define DEBUG_HANDUI_VISUALS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * This controls what is shwon on a SenseGlove Hand UI by default (SG_XR_HAndUI).
 */

namespace SG.XR
{
    public class SG_XR_HandUIVisuals : MonoBehaviour
    {
        //set all byt Y to zero(?)

        public SG_XR_HandUI mainUILogic = null;


        /// <summary> Battery Icons </summary>
        public Image batteryIcon01, batteryIcon02, batteryIcon03, batteryIcon04;
        protected Image[] allBatteryDots = new Image[0];

        public Color EmptyBatteryColor = Color.black;
        public Color LowBatteryColor = Color.red;
        public Color NormalBatteryColor = Color.green;

        public const float lowBattThreshold = 0.20f;
        public const float midBatteryBound = 0.50f;
        public const float fullBatteryThreshold = 0.80f;

#if DEBUG_HANDUI_VISUALS
        [Range(-1.0f, 1.0f)] public float debugLvl = -1.0f;
#endif

        protected Coroutine batteryUpdater = null;

        /// <summary> Returns true if this HandUI is linked to a right hand. </summary>
        public bool LinksToRightHand
        {
            get { return mainUILogic != null ? mainUILogic.LinkedToRightHand : true; }
        }

        public bool TryGetBatteryLevel(out float battLvl01)
        {
            if (this.mainUILogic != null && mainUILogic.linkedHand != null) 
            {
                return mainUILogic.linkedHand.TryGetBatteryLevel(out battLvl01);
            }
            battLvl01 = -1.0f;
            return false;
        }

        public void UpdateBatteryIcons() //TODO: Only call this Xevery X seconds. 
        {
#if DEBUG_HANDUI_VISUALS
            UpdateBatteryDots(this.debugLvl);
#else
            //if getting the battery level fails, we no longer care..
            if (this.TryGetBatteryLevel(out float battLvl01))
            {
                UpdateBatteryDots(battLvl01);
            }
            else //hide the icons?
            {
                UpdateBatteryDots(-1.0f);
            }
#endif
        }


        public void UpdateBatteryDots(float battLvl) //set it to a particular level
        {
            Color fillColor = this.NormalBatteryColor;
            int fillUpTo = -1; //no dots are filled.
            if (battLvl >= 0.0f) // this is a valid Battery Level
            {
                if (battLvl <= lowBattThreshold) //but it's too low!
                {
                    fillColor = this.LowBatteryColor;
                    fillUpTo = 0; //index 0 is the only one filled.
                }
                else if (battLvl <= midBatteryBound)
                {
                    fillUpTo = 1; //2 dots
                }
                else if (battLvl <= fullBatteryThreshold)
                {
                    fillUpTo = 2;
                }
                else
                {
                    fillUpTo = 3; //it's not smaller than any of these values. So full battery
                }
            }
            for (int i = 0; i < this.allBatteryDots.Length; i++)
            {
                this.allBatteryDots[i].color = i <= fillUpTo ? fillColor : this.EmptyBatteryColor;
            }
        }

        public void Awake()
        {
            this.allBatteryDots = new Image[4];
            allBatteryDots[0] = this.batteryIcon01;
            allBatteryDots[1] = this.batteryIcon02;
            allBatteryDots[2] = this.batteryIcon03;
            allBatteryDots[3] = this.batteryIcon04;
        }


        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }



        public void Start()
        {
            
        }


        public void Update()
        {
            UpdateBatteryIcons();
        }


    }
}