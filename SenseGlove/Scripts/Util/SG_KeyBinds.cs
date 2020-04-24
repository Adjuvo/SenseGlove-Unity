using UnityEngine;

namespace SG
{
    /// <summary> A Keybinds component that can be attached to a TrackedHand so we may access certain functions through buttons or hotkeys. </summary>
    public class SG_KeyBinds : MonoBehaviour
    {
        public SG_TrackedHand senseGloveHand;

        public KeyCode calibrateWristKey = KeyCode.P;
        public KeyCode releaseObjectKey = KeyCode.E;


        public void LinkScripts()
        {
            if (senseGloveHand == null) { senseGloveHand = this.gameObject.GetComponent<SG_TrackedHand>(); }
        }

        public void TryCallibrateWrist()
        {
            if (this.senseGloveHand != null && senseGloveHand.handAnimation != null) { senseGloveHand.handAnimation.CalibrateWrist(); }
        }

        public void TryManualRelease() //for buttons
        {
            if (this.senseGloveHand != null && senseGloveHand.grabScript != null) { senseGloveHand.grabScript.ManualRelease(); }
        }



        // Use this for initialization
        protected void Start()
        {
            LinkScripts();
        }

        // Update is called once per frame
        protected void Update()
        {
            if (Input.GetKeyDown(calibrateWristKey))
            {
                TryCallibrateWrist();
            }
            if (Input.GetKeyDown(releaseObjectKey))
            {
                TryManualRelease();
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            LinkScripts();
        }
#endif

    }
}