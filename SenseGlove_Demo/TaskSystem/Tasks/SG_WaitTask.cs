using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A more specific task that waits for a specified time to elapse, then auto-completes.
 * On the Start / Complete events, you can trigger events with the same timing to activate animations, particle effects, etc.
 * 
 * @author Max Lammers
 */

namespace SG.Tasks
{
    /// <summary> This task simply waits until the alloted time has passed, then auto-completes itself. Use Get01Value() to see how far along you are!</summary>
    /// <remarks> Use the TaskStarted and TaskCompleted events to trigger your instructions, animation, etc. </remarks>
    public class SG_WaitTask : SG_Task, IOutputs01Value
    {
        //---------------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> The time you'll have to wait for. </summary>
        public float waitTime = 5.0f;

        /// <summary> Coroutine to call the CompleteTask event </summary>
        protected Coroutine waitRoutine = null;


        //---------------------------------------------------------------------------------------------------------
        // WaitTask Functions

        /// <summary> Coroutine to complete this task in waitFor seconds. </summary>
        /// <param name="waitFor"></param>
        /// <returns></returns>
        protected virtual IEnumerator StartTimedTask(float waitFor)
        {
            yield return new WaitForSeconds(waitFor);
            this.CompleteTask();
        }

        //---------------------------------------------------------------------------------------------------------
        // SG_Task Overrides

        /// <summary> This is an automated task, it requires no user interaction. </summary>
        /// <returns></returns>
        public override bool RequiresUserInteraction()
        {
            return false;
        }


        /// <summary> Starts the timer for this task. It will auto-complete when waitTime elapses. </summary>
        public override void StartTask()
        {
            base.StartTask();
            this.waitRoutine = StartCoroutine(StartTimedTask(this.waitTime));
        }

        
        /// <summary> Called when this task completes itself. Ensure the routine is cleaned up. </summary>
        public override void CompleteTask()
        {
            base.CompleteTask();
            this.waitRoutine = null;
        }

        /// <summary> Skip this task. Ensure the Routine no longer calls the CompleteTask function. </summary>
        public override void SkipTask()
        {
            if (this.waitRoutine != null)
            {
                StopCoroutine(this.waitRoutine);
            }
            this.waitRoutine = null;
            base.SkipTask();
        }


        //---------------------------------------------------------------------------------------------------------
        // IOutputs01Value Implementation

        /// <summary> How far along we are in the task's completion. </summary>
        /// <returns></returns>
        public virtual float Get01Value()
        {
            if (this.State == TaskState.NotStarted)
            {
                return 0.0f;
            }
            else if (this.State == TaskState.Active)
            {
                if (waitTime > 0)
                {
                    return Mathf.Clamp01(this.GetTaskTime() / waitTime); //GetTaskTime returns the currently active time (if this one is active), otherwise it's null.
                }
                return this.GetTaskTime() > 0.0 ? 1.0f : 0.0f;
            }
            return 1.0f; //we're finished.
        }
    }
}