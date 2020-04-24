using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
    public class SG_ResetFloor : MonoBehaviour
    {
        public string resetTag = "resetable";

        public bool resetEnabled = true;

        // Use this for initialization
        void Start()
        {
            this.GetComponent<BoxCollider>().isTrigger = true;
        }

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

        protected void CheckReset(Collider other)
        {
            SG_Grabable grabable = other.gameObject.GetComponent<SG_Grabable>();

            if (other.tag.Contains(this.resetTag) && grabable != null && !grabable.IsInteracting())
            {
                grabable.ResetObject();
                //Debug.Log("Reset " + other.name);
            }
        }

    }
}
