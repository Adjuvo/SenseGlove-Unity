using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> A collider that detects SG_Interactable scripts. </summary>
	public class SG_HoverCollider : SG_SimpleTracking
	{
		/// <summary> Flexible detector list that will handle most of the detection work. We're only interested in the Interactable scripts it provides. </summary>
		public SG_ScriptDetector<SG_Interactable> interactablesTouched = new SG_ScriptDetector<SG_Interactable>();


		/// <summary> Optional Component to display all objects touched by this collider. </summary>
		public TextMesh debugTxt;

		/// <summary> Debug Text to show all detected colliders. </summary>
		public string ContentText
        {
			get { return debugTxt != null ? debugTxt.text : ""; }
			set { if (debugTxt != null) { debugTxt.text = value; } }
        }

        /// <summary> Report the touched objects and their colliders to the ContentText </summary>
		public void UpdateDebugger()
        {
			if (debugTxt != null)
			{
				ContentText = interactablesTouched.PrintContents();
			}
		}

		/// <summary> Returns the number of objects currently hovered over by this collider. </summary>
		/// <returns></returns>
		public int HoveredCount()
        {
			return this.interactablesTouched.DetectedCount();
        }

        public bool IsTouching()
        {
            return this.HoveredCount() > 0;
        }

        public bool IsTouching(SG_Interactable obj)
        {
            return this.interactablesTouched.IsDetected(obj);
        }

		/// <summary> Returns a list of all touched objects </summary>
		/// <returns></returns>
		public SG_Interactable[] GetTouchedObjects()
        {
			return interactablesTouched.GetDetectedScripts();
        }

        public SG_Interactable GetClosestObject(Transform sortClosestTo)
        {
            SG_Interactable[] touched = interactablesTouched.GetDetectedScripts();
            return SG.Util.SG_Util.GetClosestComponent(sortClosestTo, touched);
        }

        public int ClosestObjectIndex(Transform sortClosestTo)
        {
            SG_Interactable[] touched = interactablesTouched.GetDetectedScripts();
            return SG.Util.SG_Util.ClosestObjIndex(sortClosestTo, touched);
        }

        public SG_Interactable[] GetTouchedObjects(Transform sortClosestTo)
        {
            SG_Interactable[] touched = interactablesTouched.GetDetectedScripts();
            return SG.Util.SG_Util.SortByProximity(sortClosestTo.position, touched);
        }

        /// <summary> Returns a list with more details of the interactables touched by this script. Do not modify this unless you know what you're doing(!) </summary>
        /// <returns></returns>
        public List<DetectArguments> GetTouchDetails(bool copyArray = false)
        {
            if (copyArray)
            {
                return Util.SG_Util.ArrayCopy(this.interactablesTouched.detectedScripts);
            }
			return interactablesTouched.detectedScripts;
        }


        /// <summary> Returns the Interactables that are detected by both this and the other HoverCollider. </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SG_Interactable[] GetMatchingObjects(SG_HoverCollider other)
        {
            if (this.HoveredCount() == 0 || other.HoveredCount() == 0) //doesn' make sense to check for overlap if one or both aren' touching anything
            {
                return new SG_Interactable[0];
            }
            List<SG_Interactable> res = new List<SG_Interactable>();
            List<DetectArguments> otherArgs = other.GetTouchDetails(true); //get a copy of one of the scripts so that I can remove elements wihtout affecting the original
            for (int i = 0; i < this.interactablesTouched.detectedScripts.Count; i++)
            {
                //go through all of the other scripts. if I find the matching one, I'll not need to consider it later.
                for (int j = 0; j < otherArgs.Count; j++)
                {
                    if (interactablesTouched.detectedScripts[i].script == otherArgs[j].script) //these two are referenceing the same script(!)
                    {
                        res.Add((SG_Interactable)otherArgs[j].script); //cast should always be valid, since this script is configured to only detect these Interactables.
                        otherArgs.RemoveAt(j); //should shrink this array until none is left.
                        break;
                    }
                }
                if (otherArgs.Count == 0) { break; } //stop going through my scripts if I've already found all of the other's
            }
            return res.ToArray();
        }



		// Use this for initialization
		protected virtual void Start()
		{
			UpdateDebugger();
			//ToDo: Validate Collider / Rigidbody
		}

		// Update is called once per frame
		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			//ToDo: Call a wellness check on existsing objects?
			int deletedElements = interactablesTouched.ValidateDetectedObjects(this);
			if (deletedElements > 0)
            {
				UpdateDebugger();
			}
		}


		/// <summary> Behaviour to run when this object hovers over an object </summary>
		protected virtual void OnHover()
        {
			UpdateDebugger();
		}

		/// <summary> Behaviour to run after we finished hovering over and object </summary>
		protected virtual void OnUnHover()
        {
			UpdateDebugger();
		}


		protected virtual void OnTriggerEnter(Collider other)
        {
			int addCode = interactablesTouched.TryAddList(other, this);
			//int addCode = interactablesTouched.TryAddList(other, this, out touchedObj);
			if (addCode > 0) //only update if there is a change (code > 0)
			{
				//Debug.Log(this.name + " collided with " + touchedObj.name + ", code " + addCode + ". New Count: " + interactablesTouched.DetectedCount + "\n" + interactablesTouched.PrintContents());
				OnHover();
			}
        }

		protected virtual void OnTriggerExit(Collider other)
        {
			int removeCode = interactablesTouched.TryRemoveList(other);
			//int removeCode = interactablesTouched.TryRemoveList(other, out unTouchedobj);
			if (removeCode > 0) //only update if there is a change (code > 0)
			{
				//.Log(this.name + "Uncollided with " + unTouchedobj.name + ", code " + removeCode + ". Remaining: " + interactablesTouched.DetectedCount + "\n" + interactablesTouched.PrintContents());
				OnUnHover();
			}
		}

	}
}
