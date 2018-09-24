using UnityEngine;

namespace SenseGlove_Examples
{
    public class ResetPosition : MonoBehaviour
    {

        private static KeyCode resetKey = KeyCode.Return;

        private Vector3 startPos = Vector3.zero;
        private Quaternion startRot = Quaternion.identity;

        // Use this for initialization
        void Awake()
        {
            this.startPos = this.gameObject.transform.position;
            this.startRot = this.gameObject.transform.rotation;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(resetKey))
            {
                SenseGlove_Debugger.Log("Reset!");
                this.Reset();
            }
        }

        public void Reset()
        {
            this.transform.position = startPos;
            this.transform.rotation = startRot;
            Rigidbody RB = this.GetComponent<Rigidbody>();
            if (RB != null)
            {
                RB.velocity = Vector3.zero;
                RB.angularVelocity = Vector3.zero;
                RB.isKinematic = false;
                RB.useGravity = true;
            }
        }

    }

}