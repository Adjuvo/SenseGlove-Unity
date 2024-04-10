using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A script to manage the "main gameplay logic" throughout the simulation. 
 * Define a sequence of tasks that must be completed in order.
 * If you have multiple 'blocks' of tasks that can be completed in any order, you can divide these into sections using an SG_GroupTask_AnyOrder or SG_GroupTask_InOrder script.
 * 
 * @author Max Lammers
 */

namespace SG.Tasks
{
    /// <summary> A script to manage the "main gameplay logic" throughout the simulation </summary>
    public class SG_TaskMaster : MonoBehaviour
    {

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Enums

        /// <summary> Enum to determine what happens when all tasks in this script have been completed. </summary>
        public enum SceneTransition
        { 
            /// <summary> nothing special. This TaskMaster fires its even and that's it. </summary>
            DoNothing,
            /// <summary> Reload the current Scene when autoResetAfter elapses. Unless this variable is set to a negative value. </summary>
            ReloadCurrentScene,
            /// <summary> Load the scene at BuildIndex resetToScene when autoResetAfter elapses. Unless autoResetAfter variable is set to a negative value. </summary>
            LoadSpecificScene
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Link to the SG_User to easily access left- and right hands for any sort of internal logic or set up. </summary>
        public SG.SG_User user;

        /// <summary> The list of Tasks that must be completed in this order. Can be filled manually, or by collecting them through its children. </summary>
        /// <remarks> Since you (should) know ahead of time what tasks are in the scene, you're not allowed to add more during gameplay... </remarks>
        [Header("Tasks")]
        [SerializeField] protected List<SG_Task> tasks = new List<SG_Task>();

        /// <summary> If true, this script will grab all SG_Task scripts in its direct children. </summary>
        public bool collectTasksFromChildren = true;

        /// <summary> The currently active task index, used to check for completion of inidividual tasks, as well as overall completion. -1 means no tasks have been started, and when equal to tasks.Count, we've finished! </summary>
        private int currTaskIndex = -1;

        /// <summary> Used to run Setup() only once. </summary>
        private bool setup = true;

        /// <summary> Fires once, when the first task is completed. Use it to clean up any 'pre-startup' assets, such as difficulty settings, calibration buttons, etc. </summary>
        [Header("Events")]
        public SG.Util.SGEvent FirstTasksCompleted = new SG.Util.SGEvent();
        /// <summary> Fires just after any task marks itself as completed. Useful when updating scores, statistics, or logging. </summary>
        public SG.Util.SGEvent AnyTaskCompleted = new SG.Util.SGEvent();
        /// <summary> This event fires once all tasks have been marked as completed. Use it to re-enable settings, display congratulatory messages, or activate the confetti cannons. </summary>
        public SG.Util.SGEvent AllTasksCompleted = new SG.Util.SGEvent();

        /// <summary> Determines what to do when the last task is finished. </summary>
        [Header("Completion Scene Transition")]
        public SceneTransition onCompletion = SceneTransition.DoNothing;
        /// <summary> The Build Index of the Scene to load once the timer expires, if onCompletion is set to SceneTransition.LoadSpecificScene </summary>
        public uint resetToScene = 0;
        /// <summary> If set to a value > 0, the simulation will auto-reset itself upon completion once the indicated time has passed. </summary>
        public float autoResetAfter = -1; //set > 0

        /// <summary> The 'timestamp' where the first task began, via GetCurrentTime(). Used to determine elapsed time and total time. </summary>
        private float startTime = 0.0f;
        /// <summary> The 'timestamp' where the last task was completed, via GetCurrentTime(). Used to determine the total time. </summary>
        private float endTime = 0.0f;


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors / Utility Functions

        /// <summary> Access the left hand, if this TaskMaster is linked to a User. </summary>
        public SG.SG_TrackedHand LeftHand
        {
            get { return this.user != null ? this.user.leftHand : null; }
        }

        /// <summary> Access the right hand, if this TaskMaster is linked to a User. </summary>
        public SG.SG_TrackedHand RightHand
        {
            get { return this.user != null ? this.user.rightHand : null; }
        }

        /// <summary> Return the current time (in seconds?) as a floating point. Used for time-keeping by this script and SG_Task scripts. </summary>
        /// <remarks> Placed in a convenient static function in case you want to change how it is determined later. </remarks>
        /// <returns></returns>
        public static float GetCurrentTime()
        {
            return Time.time;
        }


        /// <summary> If all tasks are completed, return the time spent on all tasks. Otherwise, returns the current elapsed time. </summary>
        /// <returns></returns>
        public float TotalTasksTime()
        {
            if (this.currTaskIndex < this.tasks.Count)
            {
                return GetCurrentTime() - startTime;
            }
            return this.endTime - this.startTime;
        }


        /// <summary> Returns the name of the current task. </summary>
        /// <returns></returns>
        public string CurrentTaskName()
        {
            return currTaskIndex > -1 && currTaskIndex < this.tasks.Count ? this.tasks[currTaskIndex].GetTaskName() : "N\\A";
        }

        /// <summary> The amount of direct children that this Task script has a reference to. If this == 0, there's something wrong. Note: This does not include any nested tasks; use GetTotalTaskCount() for that. </summary>
        /// <returns></returns>
        public int GetTopLevelTaskCount()
        {
            return this.tasks.Count;
        }

        /// <summary> Returns the total amount of (sub) tasks  </summary>
        /// <returns></returns>
        public int GetTotalTaskCount()
        {
            int res = 0;
            for (int i=0; i<this.tasks.Count; i++)
            {
                res += tasks[i].GetTaskCount();
            }
            return res;
        }

        /// <summary> Get the amount of tasks that have been completed or skipped. </summary>
        /// <returns></returns>
        public int GetCompletionCount()
        {
            int res = 0;
            for (int i = 0; i < this.tasks.Count; i++)
            {
                res += tasks[i].GetCompletionCount();
            }
            return res;
        }


        /// <summary> Returns the amount of tasks that are in the indicated state. If this Task in said state, it returns 1 by defult. More if it's a nested Task... </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public int TaskCountByState(TaskState state)
        {
            int res = 0;
            for (int i = 0; i < this.tasks.Count; i++)
            {
                res += this.tasks[i].TaskCountByState(state);
            }
            return res;
        }


        /// <summary> Returns true if all tasks in this sequence have been completed. </summary>
        /// <returns></returns>
        public bool FullyComplete()
        {
            return this.currTaskIndex >= this.tasks.Count;
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Setup / Creation


        /// <summary> Collects any SG_Task scripts connected to the direct children of a GameObject, but not their children's children. Returns the amount of tasks added by this function. </summary>
        /// <param name="obj"></param>
        /// <param name="tasksList"></param>
        /// <returns></returns>
        public static int GetTasksFromDirectChildren(GameObject obj, ref List<SG_Task> tasksList)
        {
            int tasksAdded = 0;
            Transform parent = obj.transform;
            for (int i = 0; i < parent.childCount; i++)
            {
                SG_Task[] connectedTasks = parent.GetChild(i).GetComponents<SG_Task>();
                for (int j = 0; j < connectedTasks.Length; j++)
                {
                    if (!tasksList.Contains(connectedTasks[j]))
                    {
                        tasksAdded++;
                        tasksList.Add(connectedTasks[j]);
                    }
                }
            }
            return tasksAdded;
        }




        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic

        /// <summary> Collect all nessecary components - but to not call any functions on them yet. </summary>
        public void Setup()
        {
            if (setup)
            {
                setup = false;

                //Do your setup safely. Ensure Eveerythign I want is linked and detected.
                if (this.user == null)
                {
                    this.user = GameObject.FindObjectOfType<SG.SG_User>();
                }
                if (this.tasks.Count == 0 && !collectTasksFromChildren)
                {
                    Debug.LogWarning(this.name + " has no Tasks assigned! If you'd like this script to look for some, enable the 'collectTasksFromChildren' option.");
                }
                if (collectTasksFromChildren)
                {
                    int before = this.tasks.Count;
                    int added = GetTasksFromDirectChildren(this.gameObject, ref this.tasks);
                    if (before > 0 && added > 0)
                    {
                        Debug.LogWarning(this.name + ": We've automatically added " + added + " tasks, but " + before + " tasks were already linked. This may mess with your intended order. " +
                            "We recommend you either clear your list of tasks, or disable 'collectTasksFromChildren' ");
                    }
                    else if (tasks.Count == 0)
                    {
                        Debug.LogWarning(this.name + " has no Tasks assinged and we could not find any in its direct Children! Task logic may not work as intended.");
                    }
                }
            }
        }


        /// <summary> Link this TaskMaster to the various tasks in its list and subscribe to the correct Events </summary>
        public void LinkToTasks()
        {
            //Link to all tasks
            for (int i = 0; i < this.tasks.Count; i++)
            {
                this.tasks[i].enabled = true; //allow them to run Start() etc
                this.tasks[i].LinkTo(this);
                this.tasks[i].enabled = false; //disabled after setup so they don't update and use up CPU
                int tempI = i; //need a 'deep copy' so the I parameter gets passed properly
                this.tasks[i].TaskCompleted.AddListener(delegate { OnTaskCompleted(tempI); });
            }
        }






        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Task Logic


        /// <summary> Entry point for tasks, called one frame after Start() is called. </summary>
        /// <returns></returns>
        private IEnumerator BeginTasks()
        {
            yield return null; //wait 1 frame

            //Start a coroutine to begin the first task in the next frame
            if (this.tasks.Count == 0) //if we get here, we did not acquire any new tasks during Setup(). Which means the array is empty and none of it was collected automatically.
            {
                Debug.LogWarning("No tasks assigned to " + this.name + "'s Script.");
            }
            else
            {
                LinkToTasks();
                this.currTaskIndex = -1; //ensures a change will occur.
                this.startTime = GetCurrentTime();

                //Just to be sure, I check for the first UNCOMPLETED task to start with.
                int startIndex = -1;
                for (int i = 0; i < this.tasks.Count; i++)
                {   
                    if ( !tasks[i].IsCompleted() ) //This is the first uncompleted task
                    {
                        startIndex = i;
                        break;
                    }
                    else //somehow we've come across our next task that is already completed... At least make sure it's disabled, then.
                    {
                        tasks[i].enabled = false;
                    }
                }
                // and make sure there were actually tasks to start
                if (startIndex > -1)
                {   //there is at least on single uncompleted task in there(!)
                    if (startIndex > 0)
                    {
                        this.FirstTasksCompleted.Invoke(); //still need to let events know that the first task has been completed
                        this.AnyTaskCompleted.Invoke(); //and yes, that any task has been completed for that matter.
                    }
                    BeginTask(startIndex); //go to the very first one
                }
                else
                {
                    this.currTaskIndex = this.tasks.Count - 1;
                    PrepareNextTask(); //this should fire the "AllTaskCompleted event(s)."
                }
            }
        }


        /// <summary> Starts the tasks at index (if it's valid). </summary>
        /// <param name="taskIndex"></param>
        private void BeginTask(int taskIndex)
        {
            if (taskIndex > -1 && taskIndex < this.tasks.Count)
            {
                this.currTaskIndex = taskIndex;
                this.tasks[taskIndex].enabled = true;
                this.tasks[taskIndex].StartTask();
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Started " + CurrentTaskName(), this);
            }
        }




        /// <summary> Fires when a task completes, either through the 'intended' method, or becasue someone called SkipTask(). </summary>
        /// <param name="index"></param>
        private void OnTaskCompleted(int index)
        {
            //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Task at index " + index + " has completed!", this);
            if (index == this.currTaskIndex)
            {
                //This is the relevant task
                if (currTaskIndex == 0)
                {
                    FirstTasksCompleted.Invoke();
                }
                AnyTaskCompleted.Invoke();
                PrepareNextTask();
            }
            else if (index > -1 && index < this.tasks.Count)
            {
                Debug.LogWarning(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : We're getting TaskCompleted() calls from a task that's not currently supposed to be active." + this.tasks, this);
            }
            else
            {
                Debug.LogError(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Someone is calling OnTaskCompleted with an invalid Index. Something went very wrong..." + this.tasks, this);
            }
        }



        /// <summary> When one of our tasks completes, attempt to go to the next task. But if this was the last one, complete the logic properly. </summary>
        private void PrepareNextTask()
        {
            int nextTaskIndex = currTaskIndex + 1;
            //Becasue I'm paranoid: If our next task is already completed, we skip it.
            while (nextTaskIndex < this.tasks.Count && this.tasks[nextTaskIndex].IsCompleted())
            {
                this.tasks[nextTaskIndex].enabled = false;
                nextTaskIndex++;
            }

            if (nextTaskIndex < this.tasks.Count)
            {
                //In this case, we start a new task in the next frame...
                this.tasks[currTaskIndex].enabled = false; //disabled after setup so they don't update and use up CPU
                StartCoroutine(StartTaskNextFrame(nextTaskIndex));
            }
            else if (nextTaskIndex == tasks.Count) //we just completed the final task! (== catches the scenario where I Keep firing Complete ecents.
            {
                if (this.currTaskIndex > -1 && this.currTaskIndex < this.tasks.Count)
                {
                    this.tasks[currTaskIndex].enabled = false; //Also do this just in case.
                }
                this.currTaskIndex = tasks.Count;
                this.endTime = GetCurrentTime();
                //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : All tasks have been completed! Congratulations", this);
                this.AllTasksCompleted.Invoke();
                if (this.onCompletion != SceneTransition.DoNothing && this.autoResetAfter > 0.0f)
                {
                    int index = this.onCompletion == SceneTransition.LoadSpecificScene ? (int)this.resetToScene : SG.Util.SG_SceneControl.GetCurrentSceneIndex(); //otherwise it's the current scene
                    if (SG.Util.SG_SceneControl.IsValidScene(index))
                    {
                        //Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Reloading scene " + this.resetToScene + " in " + autoResetAfter.ToString("0.00") + "s", this);
                        StartCoroutine(GoToSceneAfter(this.autoResetAfter, (uint)index));
                    }
                    else
                    {
                        Debug.LogError(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Cannot Reload Scene " + index + ":  Not a valid Build Index", this);
                    }
                }
            }
        }


        /// <summary> Calls StartTask during the next frame </summary>
        /// <returns></returns>
        private IEnumerator StartTaskNextFrame(int taskIndex)
        {
            yield return null; //wait 1 frame
            BeginTask(taskIndex);
        }


       

        /// <summary> Skips the current task, which may call the OnTaskCompleted event (unless we're skipping a nested task deeper down) </summary>
        /// <param name="skipGroupTasks"></param>
        public void SkipCurrentTask()
        {
            if (currTaskIndex > -1 && currTaskIndex < this.tasks.Count)
            {
                Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Trying to Skip " + this.currTaskIndex, this);
                this.tasks[currTaskIndex].SkipTask(); //this should fire the taskCompleted event(!)
            }
            else
            {
                Debug.LogWarning(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Cannot skip the current task at its index is " + this.currTaskIndex, this);
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Completion Logic


        /// <summary> Load a specific scene after a particular time has passed. </summary>
        /// <param name="time"></param>
        /// <param name="buildIndex"></param>
        /// <returns></returns>
        private IEnumerator GoToSceneAfter(float time, uint buildIndex)
        {
            yield return new WaitForSeconds(time);
            Debug.Log(this.name + ": " + Time.time.ToString("0.00") + " (" + Time.frameCount + ") : Reloading Scene #" + buildIndex, this);
            SG.Util.SG_SceneControl.LoadScene(buildIndex);
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Logging:

        /// <summary> Compiles the Logs of this task into file contents. </summary>
        /// <returns></returns>
        public List<string> CompileReport()
        {
            List<string> lines = new List<string>();
            lines.Add("Report created on: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            lines.Add("Project: " + Application.productName);
            lines.Add("Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            lines.Add("Number of Tasks: " + this.GetTotalTaskCount().ToString());
            lines.Add("Completed: " + this.TaskCountByState(TaskState.Completed).ToString());
            lines.Add("Skipped: " + this.TaskCountByState(TaskState.Skipped).ToString());
            lines.Add("Total Time: " + this.TotalTasksTime().ToString("0.000") + " seconds.");
            lines.Add("");
            for (int i = 0; i < this.tasks.Count; i++)
            {
                tasks[i].GenerateLogEntry(ref lines, "\t", "\t", 0);
            }
            return lines;
        }

        /// <summary> Prints a log report in the Console. </summary>
        public void PrintReport()
        {
            List<string> report = CompileReport();
            if (report.Count > 0)
            {
                string oneLine = report[0];
                for (int i = 1; i < report.Count; i++)
                {
                    oneLine += "\n" + report[i];
                }
                Debug.Log(oneLine);
            }
        }


        /// <summary> Print the state of the current Task Logic. </summary>
        public void PrintCurrentState()
        {
            int total = this.GetTotalTaskCount();
            int current = this.GetCompletionCount();

            Debug.Log( "Completed " + current + " of " + total + " tasks" );
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> Run before other scripts do a Start </summary>
        private void Awake()
        {
            Setup();
        }

        // Start is called before the first frame update
        private void Start()
        {
            StartCoroutine(BeginTasks()); //give other Scripts a change to add Tasks during Start().
        }

    }
}
