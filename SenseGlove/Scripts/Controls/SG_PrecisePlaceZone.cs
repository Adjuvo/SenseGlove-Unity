using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{ 
	/// <summary> A SnapDropZone which only activates if the object's Transform matches that of a refrence point. </summary>
	public class SG_PrecisePlaceZone : SG_SnapDropZone
	{
        //-----------------------------------------------------------------------------------------------
        // Precision Place Parameters

        /// <summary> Additional Arguments to keep track of where positioning fails. </summary>
        /// <remarks> Contained within this script because it will rarely be used outside of it. </remarks>
        public class PreciseDropArgs : SnapZoneArgs
        {
            /// <summary> If true, the grabable is at the correct position and rotation. </summary>
            public bool MatchesTarget { get; protected set; }

            /// <summary> Tells us if position is within acceptable range </summary>
            protected bool posOK = false;
            /// <summary> Tells us if rotations are within acceptable range. </summary>
            protected bool xRotOK = false, yRotOK = false, zRotOK = false;

            /// <summary> Convert a boolean into a 'quick' notation for debugging purposes. </summary>
            /// <param name="isOK"></param>
            /// <returns></returns>
            public static string OKString(bool isOK)
            {
                return isOK ? "T" : "F";
            }

            /// <summary> When one or more parameter does not match, report all of them. If all parameters match, reports the detection state (x/xs). When Snapping, report snapping stage. </summary>
            /// <param name="collidersInside"></param>
            /// <param name="zoneScript"></param>
            /// <returns></returns>
            public override string Print(int collidersInside, SG_DropZone zoneScript)
            {
                if (!eventFired && !MatchesTarget) //show a more precise placement logic.
                {
                    //one of em's not ok.
                    return "Pos: " + OKString(posOK) + ", Rot: [" + OKString(xRotOK) + ", " + OKString(yRotOK) + ", " + OKString(zRotOK) + "]";
                }
                return base.Print(collidersInside, zoneScript);
            }

            /// <summary> Empty constructor for overrides </summary>
            protected PreciseDropArgs() { }

            /// <summary> Create a new instance of PreciseDropArgs </summary>
            /// <param name="detectedScript"></param>
            public PreciseDropArgs(SG_Grabable detectedScript)
            {
                grabable = detectedScript;
                wasEnabled = detectedScript.enabled;
                lastParent = detectedScript.MyTransform.parent;
            }

            /// <summary> Check if the grabable's base transform matches that of the precisionZone. Updates the MatchesTarget property. /summary>
            /// <param name="target"></param>
            public void CheckTarget(SG_PrecisePlaceZone precisionZone)
            {
                Transform obj = this.grabable.MyTransform;
                Transform refr = precisionZone.placementRefrence;

                // check positions
                if (precisionZone.matchPosition)
                {
                    float distance = (obj.position - refr.position).magnitude;
                    posOK = !precisionZone.matchPosition || distance <= precisionZone.maxDistanceAllowed;
                }
                else { posOK = true; }
                
                // Check rotation(s)
                if (precisionZone.matchRotation)
                {
                    Quaternion diff = Quaternion.Inverse(refr.rotation) * obj.rotation;
                    Vector3 eulers = SG.Util.SG_Util.NormalizeAngles(diff.eulerAngles);
                    xRotOK = eulers.x <= precisionZone.xRotAllowed && eulers.x >= -precisionZone.xRotAllowed;
                    yRotOK = eulers.y <= precisionZone.yRotAllowed && eulers.y >= -precisionZone.yRotAllowed;
                    zRotOK = eulers.z <= precisionZone.zRotAllowed && eulers.z >= -precisionZone.zRotAllowed;
                }
                else
                {
                    xRotOK = true;
                    yRotOK = true;
                    zRotOK = true;
                }
                MatchesTarget = posOK && xRotOK && yRotOK && zRotOK;
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The transform which the object's Base Transform should match. If not assigned, this zone's SnapPoint is used instead. </summary>
        [Header("Place Zone Properties")]
        public Transform placementRefrence;

        /// <summary> If true, the target object(s) must match this object's position </summary>
		public bool matchPosition = true;
        /// <summary> The maximum (world space) distance that the object should be from my refrence to allow it to snap.  </summary>
        public float maxDistanceAllowed = 1.0f;

        /// <summary> If true, the target much match this placementRefrence's rotation </summary>
		public bool matchRotation = true;

        /// <summary> The maximum X rotation of the object relative to my reference (in both directions!) for it to snap. Set to 180 degrees to ignore this rule. </summary>
        [Range(0, 180)] public float xRotAllowed = 180;

        /// <summary> The maximum Y rotation of the object relative to my reference (in both directions!) for it to snap. Set to 180 degrees to ignore this rule. </summary>
        [Range(0, 180)] public float yRotAllowed = 180;

        /// <summary> The maximum Z rotation of the object relative to my reference (in both directions!) for it to snap. Set to 180 degrees to ignore this rule. </summary>
        [Range(0, 180)] public float zRotAllowed = 180;


        /// <summary> Fires when the object's location matches that of the placementRefrence. </summary>
        public DropZoneEvent PlacementMatches = new DropZoneEvent();
        /// <summary> Fires when the object's location no longer matches that of the placementRefrence. </summary>
        public DropZoneEvent PlacementUnMatches = new DropZoneEvent();

        //-------------------------------------------------------------------------------------------------------------------------
        // SnapZone Overrides

        /// <summary> Generate PrecisionPlaceArgs instead of DropZoneArgs </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        protected override DropZoneArgs GenerateZoneArguments(SG_Grabable script)
        {
            return new PreciseDropArgs(script);
        }

        /// <summary> Returns true if we can start firing the Detected Event if the timer has passed. This is where we check for positioning. </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override bool CanFireEvent(DropZoneArgs args)
        {
            bool firstCheck = base.CanFireEvent(args);
            if (firstCheck) //if even the first one doesn't work, might as well skip it. Also make sure we don't check after snapping.
            {
                PreciseDropArgs precisionArgs = (PreciseDropArgs)args;
                return precisionArgs.MatchesTarget;
            }
            return firstCheck;
        }

        /// <summary> Fires whenever theres > 0 objects in the zone </summary>
        /// <param name="args"></param>
        /// <param name="dT"></param>
        protected override void CheckObject(DropZoneArgs args, float dT)
        {
            if (!args.eventFired)
            {
                PreciseDropArgs precisionArgs = (PreciseDropArgs)args;
                bool wasMatching = precisionArgs.MatchesTarget;
                precisionArgs.CheckTarget(this); //updates MatchesTarget

                if (wasMatching && !precisionArgs.MatchesTarget)
                {
                    OnPlacementUnMatch(precisionArgs);
                }
                else if (precisionArgs.MatchesTarget && !wasMatching)
                {
                    OnPlacementMatch(precisionArgs);
                }
            }
            base.CheckObject(args, dT);
        }


        /// <summary> Fires when an object Matches the correct position and orientation </summary>
        /// <param name="args"></param>
        protected virtual void OnPlacementMatch(PreciseDropArgs args)
        {
            Debug.Log("Placememnt Matches. Yay!");
            this.PlacementMatches.Invoke(args.grabable);
        }

        /// <summary> Matches when an object no longer matches  </summary>
        /// <param name="args"></param>
        protected virtual void OnPlacementUnMatch(PreciseDropArgs args)
        {
            Debug.Log("Placememnt No longer Matches. Boo!");
            this.PlacementUnMatches.Invoke(args.grabable);
        }

        /// <summary> Returns true if this script resets the timer when you no longer match the placement parameters. </summary>
        /// <returns></returns>
        public override bool ResetsTimer()
        {
			return true; //Make this into a bool?
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected override void Awake()
        {
            base.Awake();
            if (this.snapPoint == null) // we do need one for this zone!
            {
                this.snapPoint = this.transform;
            }
            if (this.placementRefrence == null) // ensure this one is set as well
            {
                this.placementRefrence = this.snapPoint;
            }
        }


    }
}
