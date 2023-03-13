using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Demo
{
	/*
	- Recenter the user (at the correct height)

	- Restart the simulation

	- Quit the simulation

	- Toggle SG Logo (For Screen Recordings)

	- Toggle On-Screen battry indication

	- Swap Hands (Vive only)

	- "Fair Mode" - which enables / disables certain functions (for the user).

	We should be able to add more functions to this menu, that are more relevant to the particular demo.
	 */

	/// <summary> Houses the buttons / UI elements linked to the Demo / Settings. Can be extended to include speficic Demo controls. </summary>
	public class SG_DemoMenu : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------------------
		// Variables

		[SerializeField] protected DevMenuSettings menuSettings;

		public SG_CollapsiblePanel menuMovement;
		public SG.SG_User user;
		public SG.XR.SG_XR_RoomSetup roomCalibration;

		[Header("Menu Title")]
		[SerializeField] protected Text appInfoText; //protected because one should interface through AppInfoText

		[Header("Common Functions - Buttons")]
		public Button resetSimulationButton;
		public Button exitButton;
		public Button recenterBtn;

		[Header("Fair Mode")]
		public SG_SimpleToggleBtn fairModeBtn;

		[Header("Swapping Hands")]
		public SG_SimpleToggleBtn swapHandsBtn;

		[Header("Toggle Notifications")]
		public SG_SimpleToggleBtn toggleNotificationsBtn;

		[Header("Glove Status Icons")]
		public SG_SimpleToggleBtn toggleGloveStatusButton;
		public GameObject gloveStatusUI;

		[Header("Watermark")]
		public SG_SimpleToggleBtn toggleWatermarkBtn;
		public GameObject watermarkUI;

		public const string statusUIKey = "showGloveStatus";
		public const string watermarkKey = "showWatermark";
		public const string fairModeKey = "fairMode";

		/// <summary> Whether or not this menu is collapsed throughout the demo scene(s). </summary>
		private static bool menuCollapsed = true; //starting value.

		//--------------------------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Contains Title & AppInfo </summary>
		public string AppInfoText
		{
			get { return appInfoText != null ? appInfoText.text : ""; }
			set { if (appInfoText != null) { appInfoText.text = value; } }
		}

		public bool ShowGloveStatusUI
		{
			get { return this.gloveStatusUI != null ? this.gloveStatusUI.activeSelf : false; }
			set { if (this.gloveStatusUI != null) { this.gloveStatusUI.SetActive(value); } }
		}

		public bool ShowWaterMark
		{
			get { return this.watermarkUI != null ? this.watermarkUI.activeSelf : false; }
			set { if (this.watermarkUI != null) { this.watermarkUI.SetActive(value); } }
		}

		//--------------------------------------------------------------------------------------------------------------------------------------
		// Functions


		public static void ApplyStyles(Button btn, Color btnColor, Color txtColor)
		{
			ColorBlock block = btn.colors;

			block.normalColor = btnColor;
			block.highlightedColor = new Color(btnColor.r * 0.961f, btnColor.g * 0.961f, btnColor.b * 0.961f, 1.0f);
			block.disabledColor = new Color(btnColor.r * 0.784f, btnColor.g * 0.784f, btnColor.b * 0.784f, 0.502f);
			block.pressedColor = new Color(btnColor.r * 0.784f, btnColor.g * 0.784f, btnColor.b * 0.784f, 1.0f);

			btn.colors = block;

			Text txt = btn.GetComponentInChildren<Text>();
			if (txt != null)
			{
				txt.color = txtColor;
			}
		}

		public static void ApplyStyles(SG_SimpleToggleBtn btn, Color btnColor, Color txtColor)
		{
			ColorBlock block = btn.linkedButton.colors;
			block.normalColor = btnColor;
			block.highlightedColor = new Color(btnColor.r * 0.961f, btnColor.g * 0.961f, btnColor.b * 0.961f, 1.0f);
			block.disabledColor = new Color(btnColor.r * 0.784f, btnColor.g * 0.784f, btnColor.b * 0.784f, 0.502f);
			block.pressedColor = new Color(btnColor.r * 0.784f, btnColor.g * 0.784f, btnColor.b * 0.784f, 1.0f);

			btn.linkedButton.colors = block;
			btn.TextColor = txtColor;
		}

		public void ApplyStyles(SG_SimpleToggleBtn btn, bool toggleTrue)
		{
			if (btn != null && menuSettings != null)
			{
				ApplyStyles(btn,
					toggleTrue ? menuSettings.toggledBtnColor : menuSettings.defaultBtnColor,
					toggleTrue ? menuSettings.toggledTextColor : menuSettings.defaultTextColor);
			}
		}



		/// <summary> Load the latest settings from memory. </summary>
		public void UpdateTitle()
		{
			if (menuSettings != null)
			{
				if (menuSettings.autoVersionInfo || menuSettings.autoTitle)
				{
					string currTxt = menuSettings.autoTitle ? Application.productName : AppInfoText;
					if (menuSettings.autoVersionInfo)
					{
						string addendum = Application.version;
						if (addendum.Length > 0 && addendum[0] != 'v')
						{
							addendum = "v" + addendum;
						}
						if (currTxt.Length != 0 && currTxt[currTxt.Length - 1] != ' ') //ensure a space between.
						{
							addendum = " " + addendum;
						}
						currTxt += addendum;
					}
					this.AppInfoText = currTxt;
				}
			}
		}


		private void SG_AppSettings_SettingChanged(object sender, Util.SettingChangedArgs e)
		{
			if (e.MadeFrom != (object)this) //if we were not the one making the change; otherwise we've already updated the visual(s).
			{
				this.UpdateBtnVisuals(); //To keep things scalable, always update for now, no matter which key is updated.
			}
		}

		/// <summary> Call this when a value changes internally to update the UI </summary>
		public void UpdateBtnVisuals()
		{
			if (menuSettings != null)
			{
				ApplyStyles(exitButton, menuSettings.defaultBtnColor, menuSettings.defaultTextColor);
				ApplyStyles(resetSimulationButton, menuSettings.defaultBtnColor, menuSettings.defaultTextColor);
			}

			bool showSGUI = SG.Util.SG_AppSettings.GetBool(statusUIKey, true);
			ShowGloveStatusUI = showSGUI;
			ApplyStyles(this.toggleGloveStatusButton, showSGUI);

			bool showWM = SG.Util.SG_AppSettings.GetBool(watermarkKey, false);
			ShowWaterMark = showWM;
			ApplyStyles(this.toggleWatermarkBtn, showWM);

			bool fairMode = SG.Util.SG_AppSettings.GetBool(fairModeKey, true);
			ApplyStyles(this.fairModeBtn, fairMode);

			if (this.user != null)
			{
				bool swapped = SG_XR_Devices.HandsSwitched;
				ApplyStyles(this.swapHandsBtn, swapped);
			}

			bool notifications = SG.Util.SG_NotificationSystem.GetNotificationsEnabled();
			ApplyStyles(this.toggleNotificationsBtn, notifications);
		}



		public void ExitBtnPressed()
		{
			//Debug.Log("Exit");
			SG.Util.SG_SceneControl.QuitApplication();
		}

		public void ResetBtnPressed()
		{
			//Debug.Log("Reset");
			SG.Util.SG_SceneControl.ToFirstScene(); //resets fully(!)
		}

		public void RecenterPressed()
		{
			//Debug.Log("Recenter");
			if (this.roomCalibration != null)
			{
				this.roomCalibration.Recenter();
			}
		}

		public void ToggleGloveStatusUI()
		{
			//Debug.Log("Toggle Glove UI");
			//Update the value itself
			bool showingNow = !SG.Util.SG_AppSettings.GetBool(statusUIKey, true); //inverse becuase I'm toggling it.
			SG.Util.SG_AppSettings.SetBool(statusUIKey, showingNow, this);
			//Update visuals
			UpdateBtnVisuals();
		}

		public void ToggleWaterMark()
		{
			//Debug.Log("Toggle Watermark");
			//Update the value itself
			bool showingNow = !SG.Util.SG_AppSettings.GetBool(watermarkKey, false); //inverse becuase I'm toggling it.
			SG.Util.SG_AppSettings.SetBool(watermarkKey, showingNow, this);
			//Update visuals
			UpdateBtnVisuals();
		}

		public void ToggleFairMode()
		{
			//Debug.Log("Toggle Fair Mode");
			//Update the value itself
			bool fairMode = !SG.Util.SG_AppSettings.GetBool(fairModeKey, true); //inverse becuase I'm toggling it.
			SG.Util.SG_AppSettings.SetBool(fairModeKey, fairMode, this);
			UpdateBtnVisuals();
		}

		public void SwapHandTrackers()
		{
			SG_XR_Devices.SwitchHands();
		}

		protected void MenuMoved()
		{
			if (menuMovement != null)
			{
				menuCollapsed = this.menuMovement.CollapsedState; //log this so other menus know
			}
		}

		/// <summary> Fires when anyone changes the SwappedHands parameter encoded in playerprefs. </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SG_XR_Devices_HandsAreSwapped(object sender, System.EventArgs e)
		{
			UpdateBtnVisuals();
		}

		public void ToggleNotifications()
        {
			bool currVal = SG.Util.SG_NotificationSystem.GetNotificationsEnabled();
			SG.Util.SG_NotificationSystem.SetNotificationsEnabled(this, !currVal);
			//Don;t need to update visuals, as that will be called via SG_NotificationSystem_NotificationEnableChanged
		}

		/// <summary> This fires when the notification setting(s) change. I could be the sender. </summary>
		/// <param name="sender"></param>
		/// <param name="newValue"></param>
		private void SG_NotificationSystem_NotificationEnableChanged(object sender, bool newValue)
		{
			UpdateBtnVisuals();
		}

		//--------------------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		protected virtual void OnEnable()
		{
			SG.Util.SG_AppSettings.SettingChanged += SG_AppSettings_SettingChanged;
			SG.SG_XR_Devices.HandsAreSwapped += SG_XR_Devices_HandsAreSwapped;

			if (this.resetSimulationButton != null) { this.resetSimulationButton.onClick.AddListener(ResetBtnPressed); }
			if (this.exitButton != null) { this.exitButton.onClick.AddListener(ExitBtnPressed); }
			if (this.recenterBtn != null) { this.recenterBtn.onClick.AddListener(RecenterPressed); }

			if (this.toggleGloveStatusButton != null) { this.toggleGloveStatusButton.Toggled.AddListener(ToggleGloveStatusUI); }
			if (this.toggleWatermarkBtn != null) { this.toggleWatermarkBtn.Toggled.AddListener(ToggleWaterMark); }
			if (this.fairModeBtn != null) { this.fairModeBtn.Toggled.AddListener(ToggleFairMode); }
			if (this.swapHandsBtn != null) { this.swapHandsBtn.Toggled.AddListener(SwapHandTrackers); }

			if (this.toggleNotificationsBtn != null) { this.toggleNotificationsBtn.Toggled.AddListener(ToggleNotifications); }
            SG.Util.SG_NotificationSystem.NotificationEnableChanged += SG_NotificationSystem_NotificationEnableChanged;
		}

       

        protected virtual void OnDisable()
		{
			SG.Util.SG_AppSettings.SettingChanged -= SG_AppSettings_SettingChanged;
			SG.SG_XR_Devices.HandsAreSwapped -= SG_XR_Devices_HandsAreSwapped;

			if (this.resetSimulationButton != null) { this.resetSimulationButton.onClick.RemoveListener(ResetBtnPressed); }
			if (this.exitButton != null) { this.exitButton.onClick.RemoveListener(ExitBtnPressed); }
			if (this.recenterBtn != null) { this.recenterBtn.onClick.RemoveListener(RecenterPressed); }

			if (this.toggleGloveStatusButton != null) { this.toggleGloveStatusButton.Toggled.RemoveListener(ToggleGloveStatusUI); }
			if (this.toggleWatermarkBtn != null) { this.toggleWatermarkBtn.Toggled.RemoveListener(ToggleWaterMark); }
			if (this.fairModeBtn != null) { this.fairModeBtn.Toggled.RemoveListener(ToggleFairMode); }
			if (this.swapHandsBtn != null) { this.swapHandsBtn.Toggled.RemoveListener(SwapHandTrackers); }

			if (this.toggleNotificationsBtn != null) { this.toggleNotificationsBtn.Toggled.RemoveListener(ToggleNotifications); }
			SG.Util.SG_NotificationSystem.NotificationEnableChanged -= SG_NotificationSystem_NotificationEnableChanged;
		}


		// Use this for initialization
		protected virtual void Start()
		{
			if (this.user == null)
			{
				this.user = GameObject.FindObjectOfType<SG.SG_User>();
			}
			if (this.roomCalibration == null)
			{
				this.roomCalibration = GameObject.FindObjectOfType<SG.XR.SG_XR_RoomSetup>();
			}

			UpdateTitle();
			//now apply values to all of our UI elements that are toggle-able.
			UpdateBtnVisuals();

			if (menuMovement == null)
			{
				this.menuMovement = this.GetComponent<SG_CollapsiblePanel>();
			}
			if (menuMovement != null)
			{
				this.menuMovement.collapseOnStart = menuCollapsed; //if the movement's Start() function fires after this one, we ensure it does what we want it to.
				if (menuCollapsed) { this.menuMovement.CollapseMenu(true); }
				else { this.menuMovement.ExpandMenu(true); }
				this.menuMovement.OnStateChanged.AddListener(MenuMoved);
			}
		}

		protected virtual void OnDestroy()
		{
			if (menuMovement != null)
			{
				this.menuMovement.OnStateChanged.RemoveListener(MenuMoved);
			}
		}
	}
}