using UnityEngine;

namespace SG.Util
{
    /// <summary> Attach to a Trigger Collider to automatically reset SG_Grabables to their original position when they enter the zone (and aren't being held). </summary>
    public class SG_ResetFloor : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Resets all objects with this tag. If left empty, it resets and and all SG_Grabables. </summary>
        public string resetTag = "resetable";

        /// <summary> Enables / Diables the reset functionality (since OnTriggerEnter also fires on disabled behaviours) </summary>
        public bool resetEnabled = true;


        //--------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Check if you can reset a collider. </summary>
        /// <param name="other"></param>
        protected void CheckReset(Collider other)
        {
            SG_Grabable grabable = other.gameObject.GetComponent<SG_Grabable>();
            if (grabable == null && other.attachedRigidbody != null) { grabable = other.attachedRigidbody.GetComponent<SG_Grabable>(); }
            if (grabable != null && !grabable.IsInteracting() && other.tag.Contains(this.resetTag) )
            {
                grabable.ResetObject();
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        private void OnTriggerEnter(Collider other)
        {
            if (resetEnabled)
                CheckReset(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (resetEnabled)
                CheckReset(other);
        }


    }
}
