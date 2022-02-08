using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Add this to a SG_Grabable to snap the hand at a fixed location (and pose?), either on Hover or OnGrab. </summary>
    /// <remarks> It is placed in a separate behaviour so I can extend off it's GenerateGrabArgs / GetSnapPoint functionality. (For example, allowing you to assign a whole list of SnapPoints and choosing the closest. </remarks>
    [DisallowMultipleComponent]
    public class SG_SnapOptions : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Where to snap the left virtual hand to. </summary>
        public Transform leftHandSnapPoint;
        /// <summary> Where to snap the right virtual hand to. </summary>
        public Transform rightHandSnapPoint;

        /// <summary> If true, we match the grabReference position of the hand to the position of the snapPoint. </summary>
        public bool matchPosition = true;
        /// <summary> If true, we match the grabReference of the hand to the rotation of the snapPoint. </summary>
        public bool matchRotation = true;

        //--------------------------------------------------------------------------------------------------------------------------------------------
        // SnapPoint Retrieval 

        /// <summary> Retrieve the snapPoint transform for a specific hand. </summary>
        public virtual Transform GetSnapPoint(SG_GrabScript grabScript) //passing grabscript so I can add a different snapPoint based on orientation, for example.
        {
            return grabScript.IsRight ? rightHandSnapPoint : leftHandSnapPoint;
        }

        /// <summary> Safely retrieve the Snap Point, returns false if it's not assigned. </summary>
        /// <param name="rightHand"></param>
        /// <param name="snapPoint"></param>
        /// <returns></returns>
        public virtual bool TryGetSnapPoint(SG_GrabScript grabScript, out Transform snapPoint)
        {
            snapPoint = this.GetSnapPoint(grabScript);
            return snapPoint != null;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        // GrabArgument Generation.

        /// <summary> Attempt to generate snapPoint arguments. Returns false if we failed for whatever reason (e.g. no snap points were assigned). </summary>
        /// <param name="objectTransf"></param>
        /// <param name="grabScript"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool GenerateGrabArgs(Transform objectTransf, SG_GrabScript grabScript, out GrabArguments args)
        {
            args = null;
            //Snapping = we pretend that the hand grab refrence was at the snappoint upon grabbing instead.
            Transform snapPoint;
            if ((this.matchPosition || this.matchRotation) && this.TryGetSnapPoint(grabScript, out snapPoint)) //we actually want to match something and we have a valid snapPoint transform.
            {
                //Debug.Log("Snapping " + grabScript.TrackedHand.name + " to " + this.name);

                //a snap point has been assigned.
                Vector3 posOffset; Quaternion rotOffset;

                Vector3 snapOffsetPos; Quaternion snapOffsetRot;
                SG.Util.SG_Util.CalculateOffsets(objectTransf, snapPoint, out snapOffsetPos, out snapOffsetRot); //offset between myTransf and the snap Point

                if (this.matchPosition && this.matchRotation) //a very minor optimization. Don't calculate the base offsets if you're matching.
                {
                    posOffset = snapOffsetPos;
                    rotOffset = snapOffsetRot;
                }
                else
                {
                    SG.Util.SG_Util.CalculateOffsets(objectTransf, grabScript.virtualGrabRefrence, out posOffset, out rotOffset); //calculating this in case 
                    if (this.matchRotation) { rotOffset = snapOffsetRot; }
                    if (this.matchPosition) { posOffset = snapOffsetPos; }
                }
                args = new GrabArguments(grabScript, posOffset, rotOffset, objectTransf.position, objectTransf.rotation);
            }
            return args != null;
        }


    }
}