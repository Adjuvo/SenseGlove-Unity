using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Tasks
{
    /// <summary> An example to control a task-system with button presses </summary>
    public class SG_Ex_TaskSystem : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The Task logic script. This one should be assigned(!) </summary>
        [SerializeField] private SG_TaskMaster taskMaster;

        /// <summary> You can skip a specific task with the appropriate key. This shows you how to skip it. </summary>
        [SerializeField] private SG_Task specificTaskToSkip;

#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
        /// <summary> Pressing this key skips the current task, whichever one that is. </summary>
        [SerializeField] private KeyCode skipTaskKey = KeyCode.Return;
        /// <summary> This key skips a specific task, but only if that is the one currently active. </summary>
        [SerializeField] private KeyCode skipSpecificKey = KeyCode.Backspace;
        /// <summary> This key prints the TaskMaster's report to the console. </summary>
        [SerializeField] private KeyCode printReportKey = KeyCode.R;
#endif

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Start is called before the first frame update
        void Start()
        {
            if (this.taskMaster == null)
                this.taskMaster = GameObject.FindObjectOfType<SG_TaskMaster>();

            if (this.taskMaster == null)
                this.enabled = false; //don't need me if there is no TaskMaster assigned...
        }

        // Update is called once per frame
        void Update()
        {
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
            if (Input.GetKeyDown(skipTaskKey))
            {
                taskMaster.SkipCurrentTask();
            }
            if (Input.GetKeyDown(printReportKey))
            {
                taskMaster.PrintReport();
            }
            if (specificTaskToSkip != null && Input.GetKeyDown(skipSpecificKey))
            {
                specificTaskToSkip.SkipTask();
            }
#endif
        }
    }
}
