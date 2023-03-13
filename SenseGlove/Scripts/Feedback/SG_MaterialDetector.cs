using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG
{
    /// <summary> A collider that detects SG_Material scripts. </summary>
    public class SG_MaterialDetector : SG_SimpleTracking
    {
        [System.Serializable]
        public class MaterialColliderEvent : UnityEvent<Collider> { }


        /// <summary> Flexible detector list that will handle most of the detection work. We're only interested in the material scripts it provides. </summary>
        public SG_ScriptDetector<SG_Material> materialsTouched = new SG_ScriptDetector<SG_Material>();


        /// <summary> Optional Component to display all objects touched by this collider. </summary>
        public TextMesh debugTxt;

        /// <summary> Fires when a new collider attached to an SG_Material is detected </summary>
        public MaterialColliderEvent ColliderDetected = new MaterialColliderEvent();
        /// <summary> Fires when an existing collider is removed from this zone. </summary>
        public MaterialColliderEvent ColliderRemoved = new MaterialColliderEvent();



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
                ContentText = materialsTouched.PrintContents();
            }
        }

        /// <summary> Returns the number of objects currently hovered over by this collider. </summary>
        /// <returns></returns>
        public int HoveredCount()
        {
            return this.materialsTouched.DetectedCount();
        }

        public bool IsTouching()
        {
            return this.HoveredCount() > 0;
        }

        public bool IsTouching(SG_Material obj)
        {
            return this.materialsTouched.IsDetected(obj);
        }

        /// <summary> Returns a list of all touched objects </summary>
        /// <returns></returns>
        public SG_Material[] GetTouchedObjects()
        {
            return materialsTouched.GetDetectedScripts();
        }

        public SG_Material GetClosestObject(Transform sortClosestTo)
        {
            SG_Material[] touched = materialsTouched.GetDetectedScripts();
            return SG.Util.SG_Util.GetClosestComponent(sortClosestTo, touched);
        }

        public int ClosestObjectIndex(Transform sortClosestTo)
        {
            SG_Material[] touched = materialsTouched.GetDetectedScripts();
            return SG.Util.SG_Util.ClosestObjIndex(sortClosestTo, touched);
        }

        public SG_Material[] GetTouchedObjects(Transform sortClosestTo)
        {
            SG_Material[] touched = materialsTouched.GetDetectedScripts();
            return SG.Util.SG_Util.SortByProximity(sortClosestTo.position, touched);
        }

        public List<Collider> GetColliders()
        {
            return this.materialsTouched.GetColliders();
        }


        /// <summary> Retrurns a list of colliders of all non-broken materials </summary>
        /// <returns></returns>
        public List<Collider> GetUnbrokenColliders()
        {
            List<Collider> res = new List<Collider>();
            for (int i=0; i<this.materialsTouched.detectedScripts.Count; i++)
            {
                if ( materialsTouched.detectedScripts[i].script != null && !((SG_Material)materialsTouched.detectedScripts[i].script).IsBroken() ) //it can't be a broken material (a.k.a. the object is disabled. The collider itself should also be active/ enabled.
                {
                    //To be fair, our ScriptDetectors do validate on update. So it should never give me broken colliders or scripts that are null? 
                    List<Collider> cols = materialsTouched.detectedScripts[i].GetColliders();
                    for (int j=0; j<cols.Count; j++)
                    {
                        res.Add(cols[j]);
                    }
                }
            }
            return res;
        }


        public bool GetConnectedMaterial(Collider col, out SG_Material materialScript)
        {
            MonoBehaviour script;
            if ( this.materialsTouched.GetAssociatedScript(col, out script))
            {
                materialScript = (SG_Material)script;
                return true;
            }
            materialScript = null;
            return false;
        }

        /// <summary> Returns a list with more details of the materials touched by this script. Do not modify this unless you know what you're doing(!) </summary>
        /// <returns></returns>
        public List<DetectArguments> GetTouchDetails(bool copyArray = false)
        {
            if (copyArray)
            {
                return Util.SG_Util.ArrayCopy(this.materialsTouched.detectedScripts);
            }
            return materialsTouched.detectedScripts;
        }


        /// <summary> Returns the materials that are detected by both this and the other HoverCollider. </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SG_Material[] GetMatchingObjects(SG_MaterialDetector other)
        {
            if (this.HoveredCount() == 0 || other.HoveredCount() == 0) //doesn' make sense to check for overlap if one or both aren' touching anything
            {
                return new SG_Material[0];
            }
            List<SG_Material> res = new List<SG_Material>();
            List<DetectArguments> otherArgs = other.GetTouchDetails(true); //get a copy of one of the scripts so that I can remove elements wihtout affecting the original
            for (int i = 0; i < this.materialsTouched.detectedScripts.Count; i++)
            {
                //go through all of the other scripts. if I find the matching one, I'll not need to consider it later.
                for (int j = 0; j < otherArgs.Count; j++)
                {
                    if (materialsTouched.detectedScripts[i].script == otherArgs[j].script) //these two are referenceing the same script(!)
                    {
                        res.Add((SG_Material)otherArgs[j].script); //cast should always be valid, since this script is configured to only detect these materials.
                        otherArgs.RemoveAt(j); //should shrink this array until none is left.
                        break;
                    }
                }
                if (otherArgs.Count == 0) { break; } //stop going through my scripts if I've already found all of the other's
            }
            return res.ToArray();
        }


        public virtual List<Collider> GetDetectionColliders()
        {
            Collider[] myColliders = this.gameObject.GetComponents<Collider>();
            List<Collider> res = new List<Collider>(myColliders.Length);
            for (int i=0; i<myColliders.Length; i++)
            {
                res.Add(myColliders[i]);
            }
            return res;
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
            int deletedElements = materialsTouched.ValidateDetectedObjects(this);
            if (deletedElements > 0)
            {
                ColliderRemoved.Invoke(null);
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
            int addCode = materialsTouched.TryAddList(other, this);
            //int addCode = materialsTouched.TryAddList(other, this, out touchedObj);
            if (addCode > 0) //only update if there is a change (code > 0)
            {
                //Debug.Log(this.name + " collided with " + touchedObj.name + ", code " + addCode + ". New Count: " + materialsTouched.DetectedCount + "\n" + materialsTouched.PrintContents());
                OnHover();
                this.ColliderDetected.Invoke(other);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            int removeCode = materialsTouched.TryRemoveList(other);
            //int removeCode = materialsTouched.TryRemoveList(other, out unTouchedobj);
            if (removeCode > 0) //only update if there is a change (code > 0)
            {
                //.Log(this.name + "Uncollided with " + unTouchedobj.name + ", code " + removeCode + ". Remaining: " + materialsTouched.DetectedCount + "\n" + materialsTouched.PrintContents());
                OnUnHover();
                this.ColliderRemoved.Invoke(other);
            }
        }

    }
}
