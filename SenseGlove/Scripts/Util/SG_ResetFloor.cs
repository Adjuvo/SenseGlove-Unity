using UnityEngine;

namespace SG.Util
{
    /// <summary> Attach to a Trigger Collider to automatically reset SG_Grabables to their original position when they enter the zone (and aren't being held). </summary>
    public class SG_ResetFloor : SG_DropZone
    {
        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Resets all objects with this tag. If left empty, it resets and and all SG_Grabables. </summary>
        public string resetTag = "resetable";

        /// <summary> Enables / Diables the reset functionality (since OnTriggerEnter also fires on disabled behaviours) </summary>
        public bool resetEnabled = true;

        /// <summary> Upon detecting an object, reset its location. </summary>
        /// <param name="args"></param>
        protected override void OnObjectDetected(DropZoneArgs args)
        {
            base.OnObjectDetected(args);
            if (args.grabable.tag.Contains(this.resetTag))
            {
                Debug.Log("Resetting " + args.grabable.name);
                args.grabable.ResetLocation(true);
            }
        }

    }
}
