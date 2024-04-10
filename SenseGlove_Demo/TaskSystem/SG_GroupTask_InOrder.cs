using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Represents a group of SG_Tasks that must by completed in a specific order.
 * Calling the SkipTask function in this Task will call the SkipTask function of its subTask. If that was the last one, the whole sequence is completed.
 * 
 * @author Max Lammers
 */

namespace SG.Tasks
{
    /// <summary> Represents a Group of tasks that must be completed in a specific order. </summary>
    public class SG_GroupTask_InOrder : SG_Task
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The list of Tasks that cshould be completed in the order in this list. Can be filled manually, or by colelcting them fromits children. </summary>
        [Header("Tasks")]
        [SerializeField] protected List<SG_Task> subTasks = new List<SG_Task>();

        /// <summary> If true, this script will look for tasks in its direct children. Useful to quickly switch the order of tasks in the editor. </summary>
        public bool collectTasksFromChildren = true;

        /// <summary> Which of the subTasks are currently active. -1 means non of them are, and >= subTasks.Count means they've all been completed. </summary>
        private int subTaskIndex = -1;


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

        /// <summary> Retruns the name of this task + sub task (+ its subTask etc etc) For Debug Logging purposes. </summary>
        /// <returns></returns>
        public override string GetTaskName()
        {
            if (subTaskIndex > -1 && subTaskIndex < this.subTasks.Count)
            {
                return base.GetTaskName() + " - " + subTasks[subTaskIndex].GetTaskName();
            }
            return base.GetTaskName();
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
        // Setup / Creation

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

            //Validate: Filter myself from the list of tasks, and remove any NULL entries
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
                int before = this.subTasks.Count;
                int added = SG_TaskMaster.GetTasksFromDirectChildren(this.gameObject, ref this.subTasks);
                if (before > 0 && added > 0)
                {
                    Debug.LogWarning(this.name + ": We've automatically added " + added + " tasks, but " + before + " tasks were already linked. This may mess with your intended order. " +
                        "We recommend you either clear your list of tasks, or disable 'collectTasksFromChildren' ");
                }
                else if (subTasks.Count == 0)
                {
                    Debug.LogWarning(this.name + " has no Tasks assinged and we could not find any in its direct Children! Task logic may not work as intended.");
                }
            }

            //Finally, call Setup for each task
            for (int i = 0; i < this.subTasks.Count; i++)
            {
                subTasks[i].Setup(); //call setup on the sub-takss if they haven't done so themselves already.
            }
        }




        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic

        /// <summary> Call the normal StartTask() and Starts this task's first sub-task during the next frame. </summary>
        public override void StartTask()
        {
            base.StartTask();
            this.subTaskIndex = -1;
            StartCoroutine(BeginTasks()); //start the tasks next frame.
        }


        /// <summary> Begins the first task of this one, if it has any subtasks. Otherwise, it auto-completes. </summary>
        /// <returns></returns>
        private IEnumerator BeginTasks()
        {
            yield return null; //wait 1 frame

            //Start a coroutine to begin the first task in the next frame
            if (this.subTasks.Count == 0) //if we get here, we did not acquire any new tasks during Setup(). Which means the array is empty and none of it was collected automatically.
            {
                Debug.LogWarning("No tasks assigned to " + this.name + "'s Script.");
                this.CompleteTask();
            }
            else
            {
                //Just to be sure, I check for the first UNCOMPLETED task to start with.
                int startIndex = -1;
                for (int i = 0; i < this.subTasks.Count; i++)
                {
                    if (!subTasks[i].IsCompleted()) //This is the first uncompleted task
                    {
                        startIndex = i;
                        break;
                    }
                    else //somehow we've come across our next task that is already completed... At least make sure it's disabled, then.
                    {
                        subTasks[i].enabled = false;
                    }
                }
                // and make sure there were actually tasks to start
                if (startIndex > -1)
                {   //there is at least on single uncompleted task in there(!)
                    BeginTask(startIndex); //go to the very first one
                }
                else
                {
                    this.subTaskIndex = this.subTasks.Count - 1;
                    PrepareNextTask(); //this should fire the "AllTaskCompleted event(s)."
                }
            }
        }


        /// <summary> Starts a new sub task. Updates this script's index and enables the next script. </summary>
        /// <param name="taskIndex"></param>
        protected virtual void BeginTask(int taskIndex)
        {
            if (taskIndex > -1 && taskIndex < this.subTasks.Count)
            {
                this.subTaskIndex = taskIndex;
                this.subTasks[taskIndex].enabled = true;
                this.subTasks[taskIndex].StartTask();
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Started " + GetTaskName(), this);
            }
        }

        /// <summary> When called by our currently active task, we clean it up and launch the next task in the following frame </summary>
        /// <param name="index"></param>
        protected virtual void OnTaskCompleted(int index)
        {
            if (index < 0 || index >= this.subTasks.Count)
            {
                Debug.LogWarning(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : TaskCompleted fired from an non-valid index (" + index + ")", this);
                return;
            }
            if (index == this.subTaskIndex) //this the current task!
            {
                //PRepareNextTask....
                //complete the current task...
                this.subTasks[index].enabled = false; //disabled after setup so they don't update and use up CPU
                PrepareNextTask();
            }
        }

        /// <summary> Called when the correct subtask completes. If this was the last task, complete me. Otherwise, prepare the next task </summary>
        protected virtual void PrepareNextTask()
        {
            int nextTaskIndex = subTaskIndex + 1;

            //Becasue I'm paranoid: If our next task is already completed, we skip it.
            while (nextTaskIndex < this.subTasks.Count && this.subTasks[nextTaskIndex].IsCompleted())
            {
                this.subTasks[nextTaskIndex].enabled = false; //ensure it's no longer active...
                nextTaskIndex++;
            }

            if (nextTaskIndex < this.subTasks.Count)
            {
                //In this case, we start a new task in the next frame...
                StartCoroutine(StartTaskNextFrame(nextTaskIndex));
            }
            else if (nextTaskIndex == subTasks.Count) //we just completed the final task! (== catches the scenario where I Keep firing Complete ecents.
            {
                this.subTaskIndex = subTasks.Count;
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : All Subtasks have been completed! Congratulations", this);
                this.CompleteTask();
            }
        }

        /// <summary> Calls StartTask in the next frame. </summary>
        /// <returns></returns>
        private IEnumerator StartTaskNextFrame(int taskIndex)
        {
            yield return null; //wait 1 frame
            BeginTask(taskIndex);
        }



        /// <summary> If this task is active, skips the current Sub-task, which in turn calls the OnTaskCompleted function in this script. </summary>
        public override void SkipTask()
        {
            if (this.State != TaskState.Active)
                return;

            if (this.subTaskIndex < this.subTasks.Count)
            {
                this.subTasks[subTaskIndex].SkipTask();
            }
        }

    }
}