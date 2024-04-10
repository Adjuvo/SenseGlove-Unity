using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Represents a group of SG_Tasks that can be completed in any order.
 * Since we don't know which one to skip when you call SkipTask(), we skip all sub-tasks.
 * If you want to skip a particlar (sub)task of this one, we reommend calling the SkipTask() on that specific task.
 * 
 * @author Max Lammers
 */


namespace SG.Tasks
{
    /// <summary> Represents a Group of tasks that can be completed in any order. </summary>
    public class SG_GroupTask_AnyOrder : SG_Task
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The list of Tasks that can be completed in any order. Can be filled manually, or by this script. </summary>
        [Header("Tasks")]
        [SerializeField] protected List<SG_Task> subTasks = new List<SG_Task>();

        /// <summary> If true, this script will look for tasks in its direct children. Useful to quickly switch the order of tasks in the editor. </summary>
        public bool collectTasksFromChildren = true;

        /// <summary> The count of sub-tasks that we've registered as 'completed'. If that is >= than our tasks, we can assume they're all done! </summary>
        private int completed = 0;
        /// <summary> Prevents shenanigans here completed++ would be called when a task is already completed... </summary>
        private bool[] completionEvents = new bool[0];

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors / Useful Stuff


        /// <summary> Returns the count of all sub-tasks in this group (as well as their subtasks and so forth). This task itself does not count for one. </summary>
        /// <returns></returns>
        public override int GetTaskCount()
        {
            int res = 0;
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                res += this.subTasks[i].GetTaskCount();
            }
            return res;
        }

        /// <summary> Returns the count of all completed sub-tasks in this group (as well as their subtasks and so forth). This task itself does not count for one. </summary>
        /// <returns></returns>
        public override int GetCompletionCount()
        {
            int res = 0;
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                res += this.subTasks[i].GetCompletionCount();
            }
            return res;
        }

        /// <summary> Returns the count of all sub-tasks in this group (as well as their subtasks and so forth) that match a TaskState. This task itself does not count for one. </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public override int TaskCountByState(TaskState state)
        {
            int res = 0;
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                res += this.subTasks[i].TaskCountByState(state);
            }
            return res;
        }

        /// <summary> Returns true if any of the sub-tasks here require user interaction. If none of them do (or when this Task has no subTasks) it returns false. </summary>
        /// <returns></returns>
        public override bool RequiresUserInteraction()
        {
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                if (subTasks[i].RequiresUserInteraction())
                    return true;
            }
            return false;
        }


        /// <summary> Generates a Log Entry for this task, and adds its sub-tasks underneath. </summary>
        /// <param name="report"></param>
        /// <param name="delimiter"></param>
        /// <param name="subTaskDelim"></param>
        /// <param name="level"></param>
        public override void GenerateLogEntry(ref List<string> report, string delimiter = "\t", string subTaskDelim = "\t", int level = 0)
        {
            base.GenerateLogEntry(ref report, delimiter, subTaskDelim, level);
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                this.subTasks[i].GenerateLogEntry(ref report, delimiter, subTaskDelim, level + 1);
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Creation / Setup


        /// <summary> Link this task and its sub-tasks to the taskManager, and subscribes to the relevant events. </summary>
        /// <param name="taskManager"></param>
        public override void LinkTo(SG_TaskMaster taskManager)
        {
            base.LinkTo(taskManager); //calls Setup, which collects the Children
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                this.subTasks[i].enabled = true;
                subTasks[i].LinkTo(taskManager); //call setup on the sub-takss if they haven't done so themselves already.
                this.subTasks[i].enabled = false; //disabled after setup so they don't update and use up CPU
                int tempI = i; //need a 'deep copy' so the I parameter gets passed properly
                this.subTasks[i].TaskCompleted.AddListener(delegate { OnTaskCompleted(tempI); });
            }
        }

        /// <summary> Sets up this task's assets, collects sub-tasks and calls their Setup routine </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            //Validate: Filter myself form the list of tasks, and remove any NULL entries
            for (int i = 0; i < this.subTasks.Count;)
            {
                if (this.subTasks[i] == null)
                {
                    this.subTasks.RemoveAt(i);
                }
                else if (this.subTasks[i] == this)
                {
                    Debug.LogWarning(this.name + ": Cannot assign this script as its own subtask!");
                    this.subTasks.RemoveAt(i);
                }
                else
                { i++; }
            }

            //Collect base tasks
            if (this.subTasks.Count == 0 && !collectTasksFromChildren)
            {
                Debug.LogWarning(this.name + " has no Tasks assigned! If you'd like this script to look for some, enable the 'collectTasksFromChildren' option.");
            }
            if (collectTasksFromChildren)
            {
                SG_TaskMaster.GetTasksFromDirectChildren(this.gameObject, ref this.subTasks);
                if (subTasks.Count == 0)
                {
                    Debug.LogWarning(this.name + " has no Tasks assinged and we could not find any in its direct Children! Task logic may not work as intended.");
                }
            }

            //Finally, call Setup
            completionEvents = new bool[this.subTasks.Count];
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                subTasks[i].Setup(); //call setup on the sub-takss if they haven't done so themselves already.
            }
        }




        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic

        /// <summary> Starts this task, and all of its SubTasks at the same time if it has any. Otherwise, it auto-completes. </summary>
        public override void StartTask()
        {
            base.StartTask();

            if (this.subTasks.Count == 0)
            {
                Debug.LogWarning("No tasks assigned to " + this.name + "'s Script.");
                this.CompleteTask(); //auto-complete it we have no tasks
                return;
            }

            //reset events
            completionEvents = new bool[this.subTasks.Count];
            completed = 0;

            //Starts all tasks this frame
            for (int i=0; i<this.subTasks.Count; i++)
            {
                this.subTasks[i].enabled = true;
                this.subTasks[i].StartTask();

                //TODO: Catch the event where tasks are already completed?
            }

        }


        /// <summary> Fires when one of my sub-tasks is completed. If it hasn't already been registered, add it to the count. </summary>
        /// <param name="index"></param>
        private void OnTaskCompleted(int index)
        {
            if (index < 0 || index >= this.subTasks.Count)
            {
                Debug.LogWarning(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : TaskCompleted fired from an non-valid index (" + index + ")", this);
                return;
            }

            if (!completionEvents[index]) //ensure we only register completion events once.
            {
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : SubTask at index " + index + " has completed!", this);
                completionEvents[index] = true;
                completed++;
                if (completed >= completionEvents.Length)
                {
                    //Debug.Log("All SubTasks have been completed. Time to clean 'em up!");
                    this.CompleteTask();
                }
            }

        }


        /// <summary> Skips all tasks in this group. </summary>
        public override void SkipTask()
        {
            if (this.State != TaskState.Active)
                return;

            for (int i=0; i<this.subTasks.Count; i++)
            {
                if (subTasks[i].State == TaskState.Active) //can only skip it while it is active. Not when it is already completed, for example...
                {
                    this.subTasks[i].SkipTask(); //this will fire Complete events, which I will catch here.
                }
            }
        }

    }
}