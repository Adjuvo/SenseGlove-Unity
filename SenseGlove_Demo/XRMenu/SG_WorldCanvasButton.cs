using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG.XR
{
    /// <summary> A special kind of HandDetector attached to a convas in world space. Ensures elements are set up properly for detection </summary>
    public class SG_WorldCanvasButton : MonoBehaviour
    {
        //---------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> An event to pass when this 3D menu is touched or activated. It contains all you need. </summary>
        [System.Serializable] public class SG_WCBtnEvent : UnityEngine.Events.UnityEvent<SG_TrackedHand, SG_WorldCanvasButton> { }

        /// <summary> The button transforom within the Unity Canvas, used to determine the scale. </summary>
        [Header("World Canvas Elements")]
        public RectTransform buttonShape;

        /// <summary> Visual representation of how much longer one needs to push this button. </summary>
        public RectTransform progressBar;
        /// <summary> The start width of the progress bar, for animation purposes. </summary>
        protected float progressWidthStart = 0.0f;

        /// <summary> The 3D Collider that will have a handDetector assigned. We make it a box collider to scale it properly for now. </summary>
        public BoxCollider buttonCollider;
        /// <summary> Attached to the boxCollider to detect 3D Hands. </summary>
        public SG_HandDetector btnDetector;


        /// <summary> How ling it takes to activate the button. </summary>
        [Header("Timing")]
        public float activationTime = 0.25f;
        /// <summary> A flag to help clean up a while loop in case its internal logic breaks. </summary>
        protected bool safeGuardTimer = false;
        /// <summary> Value between 0 and 1 that represents how far along the activationTime we are. Used or animation. </summary>
        protected float btnProgress = 0.0f;

        /// <summary> Pixel offsets in X, Y direction between buttonCollider and buttonShape </summary>
        public const float buttonOffset = 2.5f; //pixels of offset

        /// <summary> Ignore this particular trackedHand. Used when this button is attached to a hand. </summary>
        protected SG_TrackedHand ignoredHand = null;
        /// <summary> The hand currently toching this collider </summary>
        protected SG_TrackedHand currentHand = null;

       // /// <summary> While the button is being activated, we send this haptic signal to the device. </summary>
       // protected SGCore.Haptics.SG_TimedBuzzCmd lowRumble = new SGCore.Haptics.SG_TimedBuzzCmd(SGCore.Finger.Index, 30, 0.02f); //I want a low rumble while you're selecting it

        /// <summary> Vibration level while tapping this canvas button. </summary>
        [Header("Haptics")]
        [Range(0, 1)] public float rumbleLevel = 1.0f;

        /// <summary> A waveform to play when the button is activated. </summary>
        public SG.SG_CustomWaveform successWaveform;

        /// <summary> Fires when the button is touched for the first time </summary>
        [Header("Events")]
        public SG_WCBtnEvent ButtonTouched = new SG_WCBtnEvent();
        /// <summary> Fires when the button is no longer touched. </summary>
        public SG_WCBtnEvent ButtonUnTouched = new SG_WCBtnEvent();
        /// <summary> Fires when a hand is inside our BoxCollider for a certain perioud of time. </summary>
        public SG_WCBtnEvent ButtonActivated = new SG_WCBtnEvent();

        /// <summary> If true, we are allowed to send ButtonActivated events. </summary>
        protected bool canActivate = true;
        /// <summary> CoRoutine which will keep track of time. Cached so I can cancel it whenever someone untouches this button. </summary>
        protected Coroutine btnTimeRoutine = null;


        //---------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Determines whether or not this button can fire its ButtonActivated event and animate its progress. </summary>
        public bool ActivationAllowed
        {
            get { return canActivate; }
            set 
            {
                EndBtnTimer(); //if it's still running;
                canActivate = value; 
            }
        }

        /// <summary> Get/Set the progress bar foll amount. </summary>
        public float ProgressBarFill
        {
            get { return this.progressBar != null ?  progressBar.sizeDelta.x / progressWidthStart : 0.0f; }
            set
            {
                if (this.progressBar != null)
                {
                    this.progressBar.gameObject.SetActive( value > 0.0f );
                    progressBar.sizeDelta = new Vector2(progressWidthStart * value, progressBar.sizeDelta.y);
                }
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------
        // Member Functions

        /// <summary> Retur0ns true if this object is currently being touched by the hand. </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public bool TouchedBy(SG_TrackedHand hand)
        {
            return this.currentHand != null && this.currentHand == hand;
        }


        /// <summary> Tell this button to ignore touch events coming from this hand. </summary>
        /// <param name="ignoreMe"></param>
        public void SetIgnoreHand(SG_TrackedHand ignoreMe)
        {
            ignoredHand = ignoreMe;
        }


        /// <summary> Event Handler for when a hand is detected inside this button zone </summary>
        /// <param name="hand"></param>
        public void HandDetected(SG_TrackedHand hand)
        {
            if (hand != this.ignoredHand && hand.IsConnected())
            {
                this.currentHand = hand;
                this.ButtonTouched.Invoke(hand, this);
                if (this.canActivate)
                {
                    //Debug.Log(hand + " pressed " + this.name);
                    StartBtnTimer();
                }
            }
        }

        /// <summary> Event Handler for when a hand is removed from this zone. </summary>
        /// <param name="hand"></param>
        public void HandRemoved(SG_TrackedHand hand)
        {
            if (this.ignoredHand == null || hand != this.ignoredHand)
            {
                this.currentHand = null;
                this.ButtonUnTouched.Invoke(hand, this);
                // Debug.Log(hand + " unpressed " + this.name);
                EndBtnTimer(); //could check for toher hands, but there;s only one other hand that could be touching this menu...
            }
        }



        /// <summary> Update the UI during the next frame. Contained in a coroutine becasue Unity UI is not updated on start. </summary>
        /// <returns></returns>
        protected IEnumerator UpdateDimensions()
        {
            yield return null;
            if (this.buttonCollider != null && buttonShape != null)
            {
                //scale it to the button size
                float btnWidth = this.buttonShape.rect.width;
                float btnHeight = this.buttonShape.rect.height;
               // Debug.Log("Button = " + btnWidth + "x" + btnHeight);
                this.buttonCollider.transform.localScale = new Vector3(btnWidth - buttonOffset, btnHeight - buttonOffset, buttonCollider.transform.localScale.z);
                this.buttonCollider.transform.localPosition = new Vector3(0, 0, 0);
            }
            if (this.progressBar != null)
            {
                this.progressWidthStart = this.progressBar.rect.width;
            }
            ProgressBarFill = 0.0f;
        }



        /// <summary> Updates te button timer and the associated UI / haptics </summary>
        /// <returns></returns>
        private IEnumerator UpdateBtnTimer()
        {
            //Start a while loop with
            float elapsedTime = 0;
            safeGuardTimer = true;
            this.ProgressBarFill = 0.0f;
            while (safeGuardTimer && elapsedTime < this.activationTime)
            {
                //Update the vidsual
                this.btnProgress = this.activationTime > 0.0f ? elapsedTime /  this.activationTime : 1.0f;
                this.ProgressBarFill = btnProgress;

                //Low rumble
                if (rumbleLevel > 0)
                {
                    SG_TrackedHand[] handsInside = this.btnDetector.HandsInZone();
                    for (int i = 0; i < handsInside.Length; i++)
                    {
                        if (handsInside[i] != ignoredHand)
                        {
                            handsInside[i].SendVibrationCmd(VibrationLocation.WholeHand, this.rumbleLevel, 0.05f, 60.0f);
                            //Debug.LogError("TODO: Implement this!");
                        }
                    }
                }
                //Debug.Log("updating... " + elapsedTime + "(" + btnProgress + ")");

                yield return null; //wait untill the next frame.
                elapsedTime += Time.deltaTime;
            }

            this.ProgressBarFill = 0.0f;

            //if we're here, time's up. hide the visual effect and fire the event.
            if (safeGuardTimer) //we;re here because I did not get cancelled
            {
                SuccessfulClick();
            }
            this.btnProgress = 0.0f;
            safeGuardTimer = false;
        }


        /// <summary> Fired when a button is succesfully activated </summary>
        public void SuccessfulClick()
        {
            if (this.btnDetector != null && this.successWaveform != null)
            {
                SG_TrackedHand[] handsInside = this.btnDetector.HandsInZone();
                for (int i = 0; i < handsInside.Length; i++)
                {
                    if (handsInside[i] != ignoredHand)
                    {
                        handsInside[i].SendCustomWaveform(this.successWaveform, this.successWaveform.intendedMotor);
                    }
                }
            }
            this.ButtonActivated.Invoke(currentHand, this);
        }

        /// <summary> Starts the button timer CoRoutine if another is not yet running. </summary>
        protected void StartBtnTimer()
        {
            //if one isn't already running
            if (this.btnTimeRoutine == null)
            {
                StartCoroutine(UpdateBtnTimer());
            }
        }

        /// <summary> Clean up the timing coroutine </summary>
        protected void EndBtnTimer()
        {
            //else...
            safeGuardTimer = false;
            if (btnTimeRoutine != null)
            {
                StopCoroutine(btnTimeRoutine);
            }
            this.btnProgress = 0.0f;
            this.ProgressBarFill = 0.0f;
        }




        //---------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected void Start()
        {
            if (this.buttonCollider == null) { this.buttonCollider = this.GetComponent<BoxCollider>(); }
            if (this.buttonShape == null) { this.buttonShape = this.GetComponent<RectTransform>(); }
            if (this.btnDetector == null) { this.btnDetector = this.GetComponentInChildren<SG_HandDetector>(); }
            if (this.btnDetector != null)
            {
                this.btnDetector.detectionTime = 0; //always detect me
            }

            if (this.buttonCollider != null) //set essentials
            {
                this.buttonCollider.isTrigger = true;
                MeshRenderer boxRender = this.buttonCollider.GetComponent<MeshRenderer>();
                if (boxRender != null) { boxRender.enabled = false; } //don't need to see the collision box.
            }
            StartCoroutine(UpdateDimensions());
        }



        protected void OnEnable()
        {
            if (this.btnDetector == null) { this.btnDetector = this.GetComponentInChildren<SG_HandDetector>(); }
            if (this.btnDetector != null)
            {
                btnDetector.HandDetected.AddListener(HandDetected);
                btnDetector.HandRemoved.AddListener(HandRemoved);
            }
        }

        protected void OnDisable()
        {
            if (this.btnDetector != null)
            {
                btnDetector.HandDetected.RemoveListener(HandDetected);
                btnDetector.HandRemoved.RemoveListener(HandRemoved);
            }
            EndBtnTimer();
        }


    }
}