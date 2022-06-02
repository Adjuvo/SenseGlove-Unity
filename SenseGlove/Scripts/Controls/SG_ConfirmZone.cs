using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG
{
	/// <summary> An extension of the HandDetector, more optimized to be used as a "button" with optional text instructions. The innerZone dissapears until the outerZone has been exited to prevent pressing it 100x per second. </summary>
	public class SG_ConfirmZone : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> If the hand is inside this zone, we're firing our event. It cannot fire again until you exit the outerZone. </summary>
		public SG_HandDetector innerZone;
		/// <summary> Additional zone, slightly larger than the innerzone, which we must exit before we can confirm again. </summary>
		public SG_HandDetector outerZone;
		/// <summary> Optional text instructions. </summary>
		public TextMesh instructions3D;

		/// <summary> If set to true, this zone fires events. </summary>
		public bool zoneEnabled = true;
		
		/// <summary> If true, instructions stay visible once you've entered the innerZone. If false, the instructions dissapear. </summary>
		public bool instructionsStayVisible = false;


		/// <summary> Controls whether or not we're allowed to activate the innerZone. Prevents re-activation if we're still inside outerZone but outside of innerZone. 
		/// OR when two gloves are inside </summary>
		private bool enableAllowed = true;

		/// <summary> Fires when a hand enters the inner zone. </summary>
		public HandDetectionEvent OnConfirm = new HandDetectionEvent();

		/// <summary> Fires when a hand exits the outer zone </summary>
		public HandDetectionEvent OnReset = new HandDetectionEvent();

		//--------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> The text displayed along this confirmZone. </summary>
		public string InstructionText
		{
			get { return instructions3D != null ? instructions3D.text : ""; }
			set { if (instructions3D != null) { instructions3D.text = value; } }
		}

		/// <summary> Enable / Disable the text instructions </summary>
		public bool InstructionsEnabled
        {
			get { return instructions3D != null ? instructions3D.gameObject.activeSelf : false; }
			set { if (instructions3D != null) { instructions3D.gameObject.SetActive(value); } }
		}

		/// <summary> Returns true if a hand is inside this confirmZone. </summary>
		public bool HandInZone
        {
			get { return !enableAllowed; }
        }

		/// <summary> Hide the zone, which prevents it from firing events. </summary>
		/// <param name="hidden"></param>
		public void SetZone(bool active)
        {
			zoneEnabled = active;
			if (innerZone != null) { innerZone.SetHighlight(active); }
			InstructionsEnabled = active;
        }


		//--------------------------------------------------------------------------------------------------------------
		// Class Logic - Before Calling events

		/// <summary> Called when the sphere is touched for the first time. </summary>
		public void CheckSphereConfirm(SG_TrackedHand args)
        {
			if (enableAllowed && zoneEnabled)
			{
				enableAllowed = false; //we're no longer allowed to enable our glove. 
				//Debug.Log(this.name + ": Activate!");
				this.innerZone.SetHighlight(false);
				InstructionsEnabled = instructionsStayVisible;

				//fire the event
				OnConfirm.Invoke(args);
			}
		}

		/// <summary> Called when the hand exits the outerZone. </summary>
		public void CheckSphereReset(SG_TrackedHand args)
        {
			if (!enableAllowed)
			{
				enableAllowed = true;
				//Debug.Log(this.name + ": Reset!");
				//Only turn these back on if the zone is enabled.
				this.innerZone.SetHighlight(zoneEnabled);
				InstructionsEnabled = zoneEnabled;

				//fire the event
				OnReset.Invoke(args);
			}
		}


		//--------------------------------------------------------------------------------------------------------------
		// Event Handlers - What this script is subscribed to.

		/// <summary> Fired when entering the innermost zone. </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		private void InnerZone_GloveDetected(SG_TrackedHand args)
		{
			CheckSphereConfirm(args);
		}

		/// <summary> Fired when the user removes the hand from the outermost zone. Allows us to use the sphere again </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		private void OuterZone_GloveRemoved(SG_TrackedHand args)
		{
			CheckSphereReset(args);
		}


		//--------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		private void OnEnable()
        {
			if (innerZone != null) { innerZone.HandDetected.AddListener(InnerZone_GloveDetected); }
			if (outerZone != null) { outerZone.HandRemoved.AddListener(OuterZone_GloveRemoved); }
        }

        private void OnDisable()
		{
			if (innerZone != null) { innerZone.HandDetected.RemoveListener(InnerZone_GloveDetected); }
			if (outerZone != null) { outerZone.HandRemoved.RemoveListener(OuterZone_GloveRemoved); }
		}


		// Use this for initialization
		private void Start()
		{
			if (outerZone != null) { outerZone.SetHighlight(false); }
			if (innerZone != null) { innerZone.SetHighlight(zoneEnabled); }
		}

	}
}