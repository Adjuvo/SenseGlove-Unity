using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SenseGloveUtils
{
    public class ResetFloor : MonoBehaviour
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
            SenseGlove_Grabable grabable = other.gameObject.GetComponent<SenseGlove_Grabable>();

            if (other.tag.Contains(this.resetTag) && grabable != null && !grabable.IsInteracting())
            {
                grabable.ResetObject();
                //Debug.Log("Reset " + other.name);
            }
        }

    }
}
