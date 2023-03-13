using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SG.Demo
{
	/// <summary> A script that handles logic / events from a Dev menu. This menu has a set of "Standard" functions, and can be extended to handle things for your own functions. </summary>
	public class SG_CollapsiblePanel : MonoBehaviour
	{
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Utility Enums

		/// <summary> Used to indicate the starting state of a collapsiblePanel. This is easier to distinguish from the collapseOnStart boolean. </summary>
		public enum StartingState
		{
			StartsExpanded,
			StartsCollapsed
		}

		/// <summary> 2D Movement direction for Collapsible Panel </summary>
		public enum PanelMoveDirection
		{
			X = 0,
			Y,
			NegativeX,
			NegativeY
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> If not assigned, this.GetComponent<RectTrasnform>() is called.</RectTrasnform> </summary>
		public RectTransform panelTransform;

		/// <summary> The direction that the panel will be "Expanding" to when closed. If it's expanded, the panel will move in a negative direction. </summary>
		[Header("Animation Parameters")]
		public PanelMoveDirection expandDirection = PanelMoveDirection.X;

		/// <summary> The state of the menu on startup. This variable is used to determine which direction is "Expanded" </summary>
		public StartingState startingState = StartingState.StartsExpanded;

		/// <summary> The time (in seconds) it will take for the menu to go from fully opened to fully closed (and vice versa). </summary>
		public float menuOpenTime = 0.5f;

		/// <summary> How we go from starting position (y=0) to end position (y=1) </summary>
		public AnimationCurve movementProfile = AnimationCurve.Linear(0, 0, 1, 1);

		/// <summary> If true, this menu will collapse on start </summary>
		[Header("Control Parameters")]
		public bool collapseOnStart = true;

		/// <summary> The button to open / close the menu. </summary>
		public Button toggleButton;

		/// <summary> Debus / HotKey for toggling this menu. </summary>
		public KeyCode toggleHotKey = KeyCode.Tab;

		/// <summary> Whether or not the menu is collapsed or collapsing </summary>
		private bool collapsed = true;
		/// <summary> Indictaes whether or not the menu should keep moving. </summary>
		private bool moving = false;

		private bool init = true;

		/// <summary> Calculated based on menu with and menuOpenTime </summary>
		private float travelDistance = 0;


		private Vector2 startingLocation = Vector2.zero;
		private Vector2 collapsedPosition = Vector2.zero;
		private Vector2 expandedPosition = Vector2.zero;

		private float elapsedTime = 0;

		public UnityEvent OnStateChanged = new UnityEvent();

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Accessors


		public bool FullyCollapsed
		{
			get { return collapsed && !moving; }
		}

		public bool FullyExpanded
		{
			get { return !collapsed && !moving; }
		}

		/// <summary> Returns true if the menu is collapsed or in the process of collapsing. If you want something to happen when collapsing animation finishes, use FullyCollapsed / FullyExpanded instead. </summary>
		public bool CollapsedState
        {
			get { return collapsed; }
        }

		public float NormalizedTime
		{
			get
			{
				return this.menuOpenTime > 0 ? Mathf.Clamp01(this.elapsedTime / menuOpenTime) : 0;
			}
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Animation Functions

		/// <summary> Check if a movement axis is negative or not. </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static bool IsNegative(PanelMoveDirection dir)
		{
			return dir == PanelMoveDirection.NegativeX || dir == PanelMoveDirection.NegativeY;
		}

		/// <summary> Get a Vector representation of a movement direction, postentially ensuring it's always a positive number. </summary>
		/// <param name="dir"></param>
		/// <param name="absolute"></param>
		/// <returns></returns>
		public static Vector2 GetAxis(PanelMoveDirection dir, bool absolute = false)
		{
			switch (dir)
			{
				case PanelMoveDirection.X:
					return new Vector2(1, 0);
				case PanelMoveDirection.NegativeX:
					return new Vector2(absolute ? 1 : -1, 0);
				case PanelMoveDirection.Y:
					return new Vector2(0, 1);
				case PanelMoveDirection.NegativeY:
					return new Vector2(0, absolute ? 1 : -1);
			}
			return new Vector2(0, 0);
		}

		/// <summary> Called on Awake(), calculates targets etc. Call this funtion to "zero" this panel </summary>
		public void CollectParameters(bool resetStartLoc = false)
		{
			if (this.panelTransform == null)
			{
				this.panelTransform = this.GetComponent<RectTransform>();
			}

			//Vector2 startLoc = this.startingLocation;
			if (init || resetStartLoc)
			{
				init = false;
				this.startingLocation = panelTransform.anchoredPosition;
				//Debug.Log("Updated Starting Location to " + this.startingLocation.ToString());
			}

			//(Re)Collect Panel parameters
			if (expandDirection == PanelMoveDirection.X || expandDirection == PanelMoveDirection.NegativeX)
			{ //X
				this.travelDistance = panelTransform.rect.width;
			}
			else if (expandDirection == PanelMoveDirection.Y || expandDirection == PanelMoveDirection.NegativeY)
			{ //Y
				this.travelDistance = panelTransform.rect.height;
			}
			else
			{ //Z
				this.travelDistance = 0;
			}

			Vector2 dExpand = GetAxis(this.expandDirection) * travelDistance; //the distance we need to move to go to the "Expanded" state

			this.collapsed = this.startingState == StartingState.StartsCollapsed;
			if (collapsed)
			{
				collapsedPosition = startingLocation;
				expandedPosition = startingLocation + dExpand;
			}
			else
			{
				expandedPosition = startingLocation;
				collapsedPosition = startingLocation - dExpand;
			}
			//Debug.Log(this.name + " needs to expand " + travelDistance + "px towards " + expandDirection + " and is currently " + (collapsed ? "collapsed" : "expanded")
			//	+ ". Collapsed Position = " + collapsedPosition.ToString() + ", Expanded Position = " + expandedPosition.ToString());
		}


		/// <summary> Collapse the Dev menu (if it isn't already) </summary>
		/// <param name="instant"></param>
		public void CollapseMenu(bool instant = false)
		{
			if (FullyCollapsed || this.moving) //Already collapsed
				return;

			bool snap = instant || menuOpenTime <= 0;

			moving = !snap;
			collapsed = true;
			this.elapsedTime = 0;
			if (snap)
			{
				this.panelTransform.anchoredPosition = collapsedPosition;
			}
			//else we will update in the next Update() loop
			OnStateChanged.Invoke();
		}

		/// <summary>  </summary>
		/// <param name="instant"></param>
		public void ExpandMenu(bool instant = false)
		{
			if (FullyExpanded || this.moving) //Already expanded
				return;

			bool snap = instant || menuOpenTime <= 0;
			moving = !snap;
			collapsed = false;
			this.elapsedTime = 0;
			if (snap)
			{
				this.panelTransform.anchoredPosition = expandedPosition;
			}
			//else we will update in the next Update() loop
			OnStateChanged.Invoke();
		}

		/// <summary> If collapsed / collapsing, then Expand. If Expanded, then collapse. </summary>
		/// <param name="instant"></param>
		public void ToggleMenu(bool instant = false)
		{
			if (this.collapsed) { this.ExpandMenu(instant); }
			else { this.CollapseMenu(instant); }
		}

		/// <summary> Toggle this menu based on standard parameters </summary>
		public void ToggleMenu()
		{
			this.ToggleMenu(false);
		}


		/// <summary> Updates the Panel Position and checks if the animation has finished. </summary>
		/// <param name="dT"></param>
		private void UpdateMovement(float dT)
		{
			if (this.moving)
			{
				this.elapsedTime += dT;
				Vector2 endPosition = this.collapsed ? this.collapsedPosition : this.expandedPosition;
				if (elapsedTime >= this.menuOpenTime)
				{
					this.panelTransform.anchoredPosition = endPosition;
					this.moving = false;
				}
				else
				{
					Vector2 startPosition = this.collapsed ? this.expandedPosition : this.collapsedPosition;
					float t = this.movementProfile.Evaluate(this.NormalizedTime);
					this.panelTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
				}
			}
		}



		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour


		// Use this for initialization
		protected virtual void Start()
		{
			CollectParameters();
			if (this.collapseOnStart)
			{
				this.CollapseMenu(true);
			}
		}

		// Update is called once per frame
		protected virtual void Update()
		{
			UpdateMovement(Time.deltaTime);
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
			if (Input.GetKeyDown(toggleHotKey))
			{
				this.ToggleMenu(false);
			}
#endif
		}

		protected virtual void OnEnable()
		{
			if (this.toggleButton != null)
			{
				this.toggleButton.onClick.AddListener(ToggleMenu);
			}
		}

		protected virtual void OnDisable()
		{
			if (this.toggleButton != null)
			{
				this.toggleButton.onClick.RemoveListener(ToggleMenu);
			}
		}
	}
}