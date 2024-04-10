using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Defines a generic task that can be completed.
 * Created wtih Monobehaviour so you can define & link it via the inspector. Interfaces are much more annoying.
 * Note: These scripts will be disabled when not in use to save on computational time. So if you have something that needs to be done in Update() make sure to delegate that to another script.
 * 
 * @author Max Lammers
 */

namespace SG.Tasks
{

    /// <summary> TaskState which indicates to other scripts where we're at, and is also used in internal logic (skipping) and data logging. </summary>
    public enum TaskState
    {
        /// <summary> The task has not started yet. </summary>
        NotStarted,
        /// <summary> The task has started, and its script is currently active / enabled. </summary>
        Active,
        /// <summary> The task has been completed in the way we intended it to. </summary>
        Completed,
        /// <summary> The task has been skipped for one reason or another. It still fires a Completed() event. </summary>
        Skipped
    }


    /// <summary> Represents an action that must be performed within the simulation in order to complete it. Override it for your specific logic, use use the base and just call CompleteTask() whenever you want. </summary>
    public class SG_Task : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Link back to the Task Master, in case you need access to a user or timing. Avoid cross-linking this task with others, though. </summary>
        /// <remarks> Set to private becasuse even my (sub)classes should use GetTaskMaster() </remarks>
        private SG_TaskMaster taskMaster = null;

        /// <summary> The Task's Current State, used for logging / activation checks. </summary>
        public TaskState State { get; protected set; }

        /// <summary> List of items that are only relevant for this one task. They are activated at the start, and deactivated when it ends. Use this for instructions etc. </summary>
        [SerializeField] protected SG.SG_Activator singleUseAssets;


        /// <summary> Fires when this specific task is started </summary>
        [Header("Events")]
        public SG.Util.SGEvent TaskStarted = new SG.Util.SGEvent();

        /// <summary>  Fires when this task is completed (Either by skipping or by natural completion). Check State to see which of the two happened. </summary>
        public SG.Util.SGEvent TaskCompleted = new SG.Util.SGEvent();


        /// <summary> User-Friendly name to identify this task when logging. If empty, will use the GameObject name instead. </summary>
        [Header("Data Logging")]
        [SerializeField] protected string taskName = "";

        /// <summary> Ensures setup is done once, either during Start() or when the TaskMaster calls Setup(); </summary>
        protected bool setup = true;

        /// <summary> The 'timestamp' where the first task began, via SG_TaskMaster.GetCurrentTime(). Used to determine elapsed time and total time. </summary>
        protected float taskStartTime = 0.0f;
        /// <summary> The 'timestamp' where the last task was completed, via SG_TaskMaster.GetCurrentTime(). Used to determine the total time. </summary>
        protected float taskEndTime = 0.0f;


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors / Useful Stuff

        /// <summary> Retrieve the main task master logic that this task is linked to. </summary>
        /// <remarks> In a virtual method in case something odd happens nesting-wise. </remarks>
        /// <returns></returns>
        public virtual SG_TaskMaster GetTaskMaster()
        {
            return this.taskMaster;
        }

        /// <summary> Returns this task's name for Data logging Purposes. </summary>
        /// <returns></returns>
        public virtual string GetTaskName()
        {
            return this.taskName.Length > 0 ? this.taskName : this.gameObject.name;
        }

        /// <summary> If true, this task requires some form of user inpt to complete. If False, this task is fully automated (like waiting on a propt, or animation to finish). 
        /// It can therefore be skipped in timekeeping. This is logged </summary>
        public virtual bool RequiresUserInteraction()
        {
            return true;
        }


        /// <summary> Sets the state of all assets only relevant to this task; highlighters, gameobjects, instructions, etc. </summary>
        /// <param name="active"></param>
        public virtual void SetSingleUseAssets(bool active)
        {
            if (singleUseAssets != null)
            {
                this.singleUseAssets.Activated = active;
            }
        }

        /// <summary> Returns the time spent on this task (so far). </summary>
        /// <returns></returns>
        public virtual float GetTaskTime()
        {
            switch (this.State)
            {
                case TaskState.NotStarted:
                    return 0.0f;
                case TaskState.Active:
                    return SG_TaskMaster.GetCurrentTime() - this.taskStartTime;
            }
            return this.taskEndTime - this.taskStartTime;
        }

        /// <summary> Returns the amount of (sub)tasks contained within this task. Default = 1. </summary>
        public virtual int GetTaskCount()
        {
            return 1;
        }


        /// <summary> Returns true if this task has been sucessfully completed. </summary>
        /// <returns></returns>
        public virtual bool IsCompleted()
        {
            return this.State == TaskState.Completed || this.State == TaskState.Skipped;
        }


        /// <summary> Returns the amount of "Completed" tasks contained within this one (default is 1 if 'Done', and 0 if active or not started. </summary>
        /// <returns></returns>
        public virtual int GetCompletionCount()
        {
            return this.IsCompleted() ? 1 : 0;
        }

        
        /// <summary> Returns the amount of tasks contained in this one, that are in the indicated state. If this Task in said state, it returns 1. If this is a GroupTask, result can be > 1. </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual int TaskCountByState(TaskState state)
        {
            return this.State == state ? 1 : 0;
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Setup / Creation

        /// <summary> Link this task to a global task manager, so we can ask it for various info (like left / right hand). </summary>
        /// <param name="taskMaster"></param>
        public virtual void LinkTo(SG_TaskMaster taskManager)
        {
            this.taskMaster = taskManager;
            this.Setup(); //if you haven't already
        }

        /// <summary> Safely set up the assets for this Task if you haven't already. This one in not overrideable. You're looking for SetupTask(). </summary>
        public void Setup()
        {
            if (setup)
            {
                setup = false;
                if (this.singleUseAssets == null)
                {
                    this.singleUseAssets = this.GetComponent<SG.SG_Activator>();
                }
                State = TaskState.NotStarted;
                SetSingleUseAssets(true); //give these a chance to run setup / awake
                this.SetupTask();
                SetSingleUseAssets(false); //and then turn them off before (Update) occurs.
            }
        }

        /// <summary> Set up your task at the start of the simulation. Your single-use assets will be activated before this function runs. They will be de-activated after you've set this up. </summary>
        protected virtual void SetupTask()
        {
            if (this.taskName.Length == 0) { this.taskName = this.gameObject.name; }
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic


        /// <summary> Called when this task becomes active. Enables the Single-Use assets and invokes TaskStarted. </summary>
        public virtual void StartTask()
        {
            //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Starting " + this.GetTaskName(), this);
            this.taskStartTime = SG_TaskMaster.GetCurrentTime();
            this.SetSingleUseAssets(true);
            this.State = TaskState.Active;
            this.TaskStarted.Invoke();
        }


        /// <summary> Called when this task is finished, either by this script or by another script in your scene. Invokes TaskCompleted, and calls the CleanupAssets() function. </summary>
        public virtual void CompleteTask()
        {
            if (this.State != TaskState.Active)
            {
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : " + this.GetTaskName() + " is asked to Complete but it isn't running!", this);
                return;
            }

            //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Completing " + this.GetTaskName(), this);
            this.State = TaskState.Completed;
            taskEndTime = SG_TaskMaster.GetCurrentTime();
            this.TaskCompleted.Invoke();
            this.CleanupAssets();
        }

        /// <summary> When TaskComplete has been is invoked (via CompleteTask OR SkipTask), this function cleans up any remaining assets, including the ones in "SingleUseAssets". </summary>
        public virtual void CleanupAssets()
        {
            this.SetSingleUseAssets(false); //turn off all the single use assets for this task off.
        }


        /// <summary> Skip this task, or any of its sub-tasks. Invokes TaskCompleted, and calls the CleanupAssets() function </summary>
        public virtual void SkipTask()
        {
            if (this.State != TaskState.Active)
            {
                //Debug.LogWarning(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : " + this.GetTaskName() + " is asked to Skip but it isn't running", this);
                return;
            }
            //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : " + this.GetTaskName() + " is being skipped...", this);
            this.State = TaskState.Skipped;
            taskEndTime = SG_TaskMaster.GetCurrentTime();

            this.TaskCompleted.Invoke();
            this.CleanupAssets();
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Data Logging


        /// <summary> Generates a single line entry for data logging. </summary>
        /// <param name="report"></param>
        /// <param name="delimiter"></param>
        /// <param name="subTaskDelim"> Use "" for no delimiters. </param>
        /// <param name="level"> How far down we are compared to the 'top level' tasks. Used to add more subTaskDelim as we go deeper down </param>
        public virtual void GenerateLogEntry(ref List<string> report, string delimiter = "\t", string subTaskDelim = "\t", int level = 0)
        {
            string res = "";
            for (int i = 0; i < level; i++)
            {
                res += subTaskDelim;
            }
            res += this.GetTaskName();
            res += delimiter + this.State.ToString();
            res += delimiter + this.GetTaskTime().ToString("0.000");
            res += delimiter + (this.RequiresUserInteraction() ? "Manual" : "Automatic");
            report.Add(res);
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> Calls Setup on itself. </summary>
        protected virtual void Awake()
        {
            Setup();
        }

    }
}