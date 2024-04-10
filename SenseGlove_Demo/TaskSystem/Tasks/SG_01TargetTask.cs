using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * A task that can take input from any script that implements the IOutputs01Value interface, and checks if its value is in a specific range.
 * If skipped and that task has a IControlledBy01Value, we can choose to set it to a specific value.
 * 
 * @author Max Lammers
 */

namespace SG.Tasks
{
    /// <summary> This task is completed once a IOutputs01Value's output reaches a specific range for a set amount of time. </summary>
    public class SG_01TargetTask : SG_Task
    {
        //---------------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> The Script that  </summary>
        [Header("01 Target Task")]
        public MonoBehaviour inputScript;

        /// <summary> If true, we invert the input of your NormalizedValue. Useful if you want to swap directions etc on the fly. </summary>
        public bool invertInput = false;

        /// <summary> Our input value must be above or equal to this value </summary>
        [Range(0.0f, 1.0f)] [SerializeField] protected float targetMin = 0.45f;
        /// <summary> Our input value must be below or equal to this value </summary>
        [Range(0.0f, 1.0f)] [SerializeField] protected float targetMax = 0.55f;

        /// <summary> After entering the range, we add a bit of extra tolerance to stop triggering the events / logic every frame. </summary>
        [SerializeField] protected float targetTolerance = 0.01f; 

        /// <summary> The amount of time this script should at the target value for. If set to < 0.0f, this script will complete as soon as it reaches said target. </summary>
        public float timeAtTarget = 0.1f;

        /// <summary> Event that fires when we enter the target range </summary>
        public UnityEngine.Events.UnityEvent EnterTargetRange = new UnityEngine.Events.UnityEvent();
        /// <summary> Event that fires when we exit the target range. </summary>
        public UnityEngine.Events.UnityEvent ExitTargetRange = new UnityEngine.Events.UnityEvent();


        /// <summary> Keeping track of whether or not we';re in the target. </summary>
        protected bool isAtTarget = false;

        /// <summary> Internal timer to check how far we've passed the target </summary>
        protected float timer_forTarget = 0.0f;

        /// <summary> The internal value used to enable / disable the Checking Logic. Default = true </summary>
        protected bool canCheck = true;


        //---------------------------------------------------------------------------------------------------------
        // Accessors


        /// <summary> Interface through which we collect input. </summary>
        public IOutputs01Value ValueSource
        {
            get; set;
        }

        /// <summary> Used to enable / disable the Checking Logic (E.g. when an object is being held). Default = true. </summary>
        public bool CheckValueEnabled
        {
            get { return canCheck; }
            set { canCheck = value; }
        }

        /// <summary> The minimum value theinputScript should have to be completed </summary>
        public float TargetMinValue
        {
            get { return this.targetMin; }
            set 
            {
                value = Mathf.Clamp01(value);
                if (value > this.targetMax)
                {
                    Debug.LogWarning(this.name + ": Attempting to set a TargetMinValue above TargetMaxValue. Correcting TargetMaxValue to the same value.", this);
                    targetMax = value;
                }
                targetMin = value;
            }
        }

        /// <summary> The maximum value theinputScript should have to be completed </summary>
        public float TargetMaxValue
        {
            get { return this.targetMax; }
            set 
            {
                value = Mathf.Clamp01(value);
                if (value < targetMin)
                {
                    Debug.LogWarning(this.name + ": Attempting to set a TargetMaxValue below TargetMinValue. Correcting TargetMinValue to the same value.", this);
                    targetMin = value;
                }
                this.targetMax = value;
            }
        }

        /// <summary> Returns true if the inputscript value is in the target range. </summary>
        public bool InRange
        {
            get { return this.isAtTarget; }
        }


        /// <summary> Returns the normalized value of my inputScript, if a valid one is assigned. Used for my own ease-of-access as well as for other scripts. Also does the inversion! </summary>
        public virtual float NormalizedValue
        {
            get 
            { 
                float val = this.ValueSource != null ? this.ValueSource.Get01Value() : 0.0f;
                return this.invertInput ? 1.0f - val : val;
            }
        }


        //---------------------------------------------------------------------------------------------------------
        // Task Logic

        /// <summary> Collect proper script. </summary>
        protected override void SetupTask()
        {
            base.SetupTask();
            // Ensure you have a valid script assigned.
            if (this.ValueSource == null)
            {
                if (this.inputScript != null)
                {
                    if (this.inputScript == this)
                    {
                        Debug.LogError(this.name + " Cannot assing itself as its own input!", this);
                    }
                    else if (this.inputScript is IOutputs01Value)
                    {
                        this.ValueSource = (IOutputs01Value)this.inputScript;
                    }
                    else
                    {
                        Debug.LogError(this.name + " has an invalid inputScript assigned! It should implement the IOutputs01Value interface! It cannot be completed!", this);
                    }
                }
                else
                {
                    Debug.LogError(this.name + " has no input script assigned. It cannot be completed!", this);
                }
            }

            //Also check the input values?
            if (this.targetMin > this.targetMax)
            {
                Debug.LogWarning(this.name + " Has a targetMin higher than targetMax. Swapping the two", this);
                float actualMax = this.targetMin;
                this.targetMin = this.targetMax;
                this.targetMax = actualMax;
            }
        }

        /// <summary> StartTask resets the timer. </summary>
        public override void StartTask()
        {
            base.StartTask();
            timer_forTarget = 0.0f;
        }

        /// <summary> Checks Target logic; events and completion </summary>
        /// <param name="dT"></param>
        protected virtual void CheckTarget(float dT)
        {
            float currValue = this.NormalizedValue;

            // Check if we're currently in range
            bool inRange = false;
            if (this.isAtTarget)
            {
                float tolr = Mathf.Abs(this.targetTolerance);
                if (currValue >= this.targetMin - tolr && currValue <= targetMax + tolr)
                {
                    inRange = true;
                }
            }
            else if (currValue >= this.targetMin && currValue <= targetMax)
            {
                inRange = true;
            }

            // Call the events when applicatble (and make sure the target is set correctly.
            if (inRange)
            {
                if (!this.isAtTarget)
                {
                    this.isAtTarget = inRange; //redundant, but it shows we're currently in range is someone was to check while handline the event.
                    this.EnterTargetRange.Invoke();
                }
            }
            else
            {
                if (this.isAtTarget)
                {
                    this.isAtTarget = inRange; //redundant, but it shows we're currently in range is someone was to check while handline the event.
                    this.ExitTargetRange.Invoke();
                }
            }
            this.isAtTarget = inRange; //Do this regardless.
            
            // Check Timiing / Task Completion
            if (this.isAtTarget)
            {
                if (this.timeAtTarget <= 0.0f)
                {
                    this.CompleteTask();
                }
                else
                {
                    timer_forTarget += dT;
                    if (timer_forTarget >= timeAtTarget)
                    {
                        this.CompleteTask();
                    }
                }
            }
            else
            {
                this.timer_forTarget = 0.0f; //reset the timer if we're not at target
            }
        }

        //---------------------------------------------------------------------------------------------------------
        // Monobehaviour

        /// <summary> Update runs every frame while this script is enabled. </summary>
        protected virtual void Update()
        {
            if ( !this.IsCompleted() )
            {
                this.CheckTarget(Time.deltaTime);
            }
        }


#if UNITY_EDITOR
        /// <summary> In-Editor; make sure the values are fine.. </summary>
        protected virtual void OnValidate()
        {
            if (this.targetMin > this.targetMax)
            {
                this.targetMax = this.targetMin;
            }
            if (this.targetMax < this.targetMin)
            {
                this.targetMin = this.targetMax;
            }
        }
#endif

    }
}