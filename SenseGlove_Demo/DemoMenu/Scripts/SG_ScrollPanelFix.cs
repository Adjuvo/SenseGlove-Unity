using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Util
{
	/// <summary> A fix for the terribly complex Unity Scroll list system. Useful when you don't know the size of your content. </summary>
	public class SG_ScrollPanelFix : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The base panel that will contain all your content. </summary>
		public RectTransform baseElement;
		/// <summary> The ScrollRect that will visualize your content and determine that the ScrollBar needs to be hidden </summary>
		public ScrollRect scrollRect;
		/// <summary> Used to properly fit the content within the menu. </summary>
		public GridLayoutGroup contentFitter;

		/// <summary> The width of the base element. </summary>
		protected float mainWidth = 0;
		/// <summary> The width of the scrollbar. </summary>
		protected float scrollBarWidth = 0;

		/// <summary> Scrollbar's tranforms. Useful if I ever need it again </summary>
		protected RectTransform scrollbarTransf;
		/// <summary> The viewport must be edged to the left depending on whether or not the scrollbar is visible </summary>
		protected RectTransform viewPortTransf;

		/// <summary> The maximum frames this script will check for the scollBar before disabling itself. </summary>
		protected const int maxScrollChecks = 5;
		/// <summary> Whether or not the scrollbar was enabled the last time we checked. Minor optimization. </summary>
		protected bool wasEnabled = false;

		protected bool currentlyChecking = false;
		protected Coroutine checkRoutine;

		//--------------------------------------------------------------------------------------------------------------
		// Functions

		/// <summary> Collect all the required data, and apply the proper parameters to it. </summary>
		protected void CollectData()
		{
			mainWidth = baseElement.rect.width;

			if (scrollRect.verticalScrollbar != null)
			{
				scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; //it should not interfere or resize like the chump it is.
				scrollbarTransf = scrollRect.verticalScrollbar.GetComponent<RectTransform>();
				if (scrollbarTransf != null)
				{
					scrollBarWidth = scrollbarTransf.rect.width * scrollbarTransf.localScale.x;
				}
			}

			if (scrollRect.viewport != null)
			{
				viewPortTransf = scrollRect.viewport.GetComponent<RectTransform>(); //must be set to 0 OR width
			}

			if (contentFitter != null) //vert 1
			{
				contentFitter.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				contentFitter.constraintCount = 1;
			}

		}

		/// <summary> Recalculate the Grid sizes and viewport location based on the current scrollbar location. </summary>
		/// <param name="scrollbarEnabled"></param>
		/// <param name="forceUpdate"></param>
		protected void RecalculateLayout(bool scrollbarEnabled, bool forceUpdate = false)
		{
			if (wasEnabled != scrollbarEnabled || forceUpdate)
			{
				wasEnabled = scrollbarEnabled;
				float contentWidth = scrollbarEnabled ? (mainWidth - scrollBarWidth) : mainWidth;
				//Debug.Log("Check " + this.scrollChecks + ": scrollbar " + scrollbarEnabled + ", set content to size " + contentWidth);


				if (contentFitter != null)
				{
					Vector3 size = contentFitter.cellSize;
					size.x = contentWidth;
					contentFitter.cellSize = size;
					//Debug.Log("CellSize set to " + size.ToString());
				}

				if (viewPortTransf != null)
				{
					float left = scrollbarEnabled ? scrollBarWidth : 0;
					viewPortTransf.offsetMin = new Vector2(left, viewPortTransf.offsetMin.y);
					viewPortTransf.offsetMax = new Vector2(0, viewPortTransf.offsetMax.y);
				}

			}
		}

		/// <summary> Reset and check the scroll lists again. </summary>
		public void CheckAgain()
		{
			if (this.checkRoutine != null)
            {
				StopCoroutine(this.checkRoutine);
            }
			if (this.enabled && this.gameObject.activeInHierarchy)
			{
				this.checkRoutine = StartCoroutine(CheckScrollBar());
			}
		}

		/// <summary> Fix the elements such that the list content fits the main element + scrollbar. </summary>
		public void FixElements()
		{
			this.CollectData();
			this.RecalculateLayout(this.scrollRect.verticalScrollbar.isActiveAndEnabled, true);
		}


		public IEnumerator CheckScrollBar()
        {
			currentlyChecking = true;
			for (int i=0; i<maxScrollChecks; i++)
            {
				RecalculateLayout(this.scrollRect.verticalScrollbar.isActiveAndEnabled);
				yield return null; //wait until the next frame
			}
			currentlyChecking = false;
		}

		//--------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		private void Awake()
		{
			CollectData();
		}

		private void OnEnable() //when re-enabled?
		{
			CheckAgain();
		}

		protected virtual void OnRectTransformDimensionsChange() //fires every time this rectTransform changes.
		{
			CheckAgain();
		}

		
	}
}