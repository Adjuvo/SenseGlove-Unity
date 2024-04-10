using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Tasks
{

    /// <summary> The most basic task that can be completed by pressing a single key, calling a single function ( CompleteMe() ) or by toggling the "completeHere" checkbox in the inspector.  </summary>
    public class SG_FakeTask : SG_Task
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> If set to true, this task completes via the inspector </summary>
        [Header("Fake Task Elements")]
        [SerializeField] private bool completeHere = false;

#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
        /// <summary> Hotkey to auto-complete the task with a key press. </summary>
        public KeyCode completeHotKey = KeyCode.Space;
#endif

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic

        /// <summary>  </summary>
        public void CompleteMe()
        {
            if (this.isActiveAndEnabled) //the event will only fire when this task is currently active
            {
                this.CompleteTask();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> Called each frames </summary>
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled...
            if (completeHere) //this piece of code should only run when this task is enabled.
#else
            if (Input.GetKeyDown(completeHotKey) || completeHere) //this piece of code should only run when this task is enabled.
#endif
            {
                CompleteMe();
            }
        }
    }
}