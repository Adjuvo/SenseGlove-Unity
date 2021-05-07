using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG
{
	/// <summary> An extension of the HandDetector, more optimized to be used as a "button" with optional text instructions. The innerZone dissapears until the outerZone has been exited. </summary>
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
			if (innerZone != null) { innerZone.SetHighLight(active); }
			InstructionsEnabled = active;
        }


		//--------------------------------------------------------------------------------------------------------------
		// Class Logic - Before Calling events

		/// <summary> Called when the sphere is touched for the first time. </summary>
		public void CheckSphereConfirm(SG_HandDetector.GloveDetectionArgs args)
        {
			if (enableAllowed && zoneEnabled)
			{
				enableAllowed = false; //we're no longer allowed to enable our glove. 
				//Debug.Log(this.name + ": Activate!");
				this.innerZone.SetHighLight(false);
				InstructionsEnabled = instructionsStayVisible;

				//fire the event
				OnZoneActivated(args);
			}
		}

		/// <summary> Called when the hand exits the outerZone. </summary>
		public void CheckSphereReset(SG_HandDetector.GloveDetectionArgs args)
        {
			if (!enableAllowed)
			{
				enableAllowed = true;
				//Debug.Log(this.name + ": Reset!");
				//Only turn these back on if the zone is enabled.
				this.innerZone.SetHighLight(zoneEnabled);
				InstructionsEnabled = zoneEnabled;

				//fire the event
				OnZoneReset(args);
			}
		}


		//--------------------------------------------------------------------------------------------------------------
		// Event Handlers - What this script is subscribed to.

		/// <summary> Fired when entering the innermost zone. </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		private void InnerZone_GloveDetected(object source, SG_HandDetector.GloveDetectionArgs args)
		{
			CheckSphereConfirm(args);
		}

		/// <summary> Fired when the user removes the hand from the outermost zone. Allows us to use the sphere again </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		private void OuterZone_GloveRemoved(object source, SG_HandDetector.GloveDetectionArgs args)
		{
			CheckSphereReset(args);
		}



		//--------------------------------------------------------------------------------------------------------------
		// Events - What other scripts subscribe to.

		/// <summary> Event Handler for detection. </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		public delegate void ConfirmSphereEventHandler(object source, SG_HandDetector.GloveDetectionArgs args);

		/// <summary> Fires when the user puts their glove inside the zone and activated it for the first time. </summary>
		public event ConfirmSphereEventHandler Activated;

		/// <summary> Fires the user has removed their hand from the zone and it can be activated once more. </summary>
		public event ConfirmSphereEventHandler Reset;


		/// <summary> Fire the Activated Event, if we have subscribers available </summary>
		/// <param name="args"></param>
		private void OnZoneActivated(SG_HandDetector.GloveDetectionArgs args)
		{
			if (Activated != null) { Activated(this, args); }
		}

		/// <summary> Fire the Activated Event, if we have subscribers available </summary>
		/// <param name="args"></param>
		private void OnZoneReset(SG_HandDetector.GloveDetectionArgs args)
		{
			if (Reset != null) { Reset(this, args); }
		}


		//--------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		private void OnEnable()
        {
			if (innerZone != null) { innerZone.GloveDetected += InnerZone_GloveDetected; }
			if (outerZone != null) { outerZone.GloveRemoved += OuterZone_GloveRemoved; }
        }

        private void OnDisable()
		{
			if (innerZone != null) { innerZone.GloveDetected -= InnerZone_GloveDetected; }
			if (outerZone != null) { outerZone.GloveRemoved -= OuterZone_GloveRemoved; }
		}


		// Use this for initialization
		private void Start()
		{
			if (outerZone != null)
			{
				outerZone.SetHighLight(false); //we'll not be using this one
				outerZone.singleGlove = true;
			}
			if (innerZone != null) { innerZone.singleGlove = true; }
		}

	}
}