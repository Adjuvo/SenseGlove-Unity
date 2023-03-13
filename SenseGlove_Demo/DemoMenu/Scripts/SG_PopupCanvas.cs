using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Util
{
    /// <summary> Utility class to contain / allow for   </summary>
    public class SG_PopupCanvas : MonoBehaviour
    {
        public enum ExpandDirection
        {
            Height,
            Width
        }


        /// <summary> The original notification </summary>
        public SG_SimpleNotification notification { get; set; }

        public ExpandDirection expandDirection = ExpandDirection.Height;


        /// <summary> The item to show </summary>
        public RectTransform popupRect;

        /// <summary> Contains message text </summary>
        public Text textContainer;

        /// <summary> contains the sprite for the notification (optional) </summary>
        public Image spriteContainer;

        //public float BaseHeightScale { get; set; }

        protected Vector2 baseDimensions = new Vector2(1.0f, 1.0f);


        [Range(0, 1)] public float debugSlider = 1.0f;

        public const float showUIAbove = 0.75f;

        protected Color baseTextColor = Color.black;
        protected Color fadedTextColor;

        protected Color baseImgColor = Color.white;
        protected Color fadedimgColor;


        //public PopupState state = PopupState.Opening;
        protected Coroutine activeRoutine = null;
        protected bool safeGuard = false;


        public string MessageText
        {
            get { return this.textContainer != null ? this.textContainer.text : ""; }
            set { if (this.textContainer.text != null) { this.textContainer.text = value; } }
        }

        public void SetImage(Sprite img)
        {
            if (spriteContainer != null)
            {
                spriteContainer.sprite = img;
                spriteContainer.gameObject.SetActive(img != null); //turns it on only if we have a valid img.
            }
        }

        /// <summary> Returns the new size of the element based on a mapped avlue between 0..1 </summary>
        /// <param name="value01"></param>
        /// <returns></returns>
        public Vector2 GetSize(float step01)
        {
            Vector2 size = this.baseDimensions; //vect is a struct. This is a copy.
            if (this.expandDirection == ExpandDirection.Height)
            {
                float mappedY = this.baseDimensions.y != 0 ? SG.Util.SG_Util.Map(step01, 0.0f, 1.0f, 0.0f, this.baseDimensions.y, true) : 1.0f;
                size.y = mappedY;
            }
            else
            {
                float mappedX = this.baseDimensions.x != 0 ? SG.Util.SG_Util.Map(step01, 0.0f, 1.0f, 0.0f, this.baseDimensions.x, true) : 1.0f;
                size.x = mappedX;
            }
            return size;
        }

        public void LinkTo(SG_SimpleNotification _notification)
        {
            this.notification = _notification;
            this.SetImage(this.notification.Image);
            this.MessageText = this.notification.Message;
        }


        /// <summary> Sets the scale of this object  </summary>
        public void SetScale(float step01)
        {
            if (this.popupRect != null)
            {
                //float mappedY = this.BaseHeightScale != 0 ? SG.Util.SG_Util.Map(step01, 0.0f, 1.0f, 0.0f, this.BaseHeightScale, true) : 1.0f;

                this.popupRect.sizeDelta = GetSize(step01); //new Vector2(this.popupRect.sizeDelta.x, mappedY);
                //this.popupRect.localScale = new Vector3(popupRect.localScale.x, mappedY, popupRect.localScale.z);
            }

            float transpStep = Mathf.Clamp01( SG.Util.SG_Util.Map(step01, showUIAbove, 1.0f, 0.0f, 1.0f) );
            if (this.textContainer)
            {
                this.textContainer.enabled = step01 >= showUIAbove;
                this.textContainer.color = Color.Lerp(this.fadedTextColor, this.baseTextColor, transpStep);
            }
            if (this.spriteContainer != null)
            {
                this.spriteContainer.enabled = step01 >= showUIAbove;
                this.spriteContainer.color = Color.Lerp(this.fadedimgColor, this.baseImgColor, transpStep);
            }
        }

        protected void Awake()
        {
            if (popupRect == null)
            {
                popupRect = this.GetComponent<RectTransform>();
            }
            if (popupRect != null)
            {
                //BaseHeightScale = this.popupRect.sizeDelta.y;
                baseDimensions = this.popupRect.sizeDelta;
                //BaseHeightScale = this.popupRect.localScale.y;
            }

            if (this.textContainer != null)
            {
                this.baseTextColor = textContainer.color;
                this.fadedTextColor = new Color(baseTextColor.r, baseTextColor.g, baseTextColor.b, 0.0f); //0 alpha
            }
            if (this.spriteContainer != null)
            {
                this.baseImgColor = this.spriteContainer.color;
                this.fadedimgColor = new Color(baseImgColor.r, baseImgColor.g, baseImgColor.b, 0.0f); //0 alpha
            }
        }







        /// <summary> Begin the process of showing this notification. Calls AutoHideMe if hideTIme > 0 </summary>
        public void StartShowing(float showTime, float hideTime)
        {
            if (this.activeRoutine != null)
            {
                safeGuard = false;
                StopCoroutine(activeRoutine);
                this.activeRoutine = null;
            }
            this.activeRoutine = StartCoroutine( StartShowRoutine(showTime, hideTime) );
        }

        protected IEnumerator StartShowRoutine(float showTime, float hideTime)
        {
            //Debug.Log("Entered Idle Routine. showTime = " + showTime + ", hideTime = " + hideTime);
            if (showTime > 0)
            {
                this.SetScale(0.0f); //this will expand later
                float elapsedTime = 0;
                safeGuard = true;
                while (safeGuard && elapsedTime < showTime)
                {
                    yield return null; //wait for next frame
                    elapsedTime += Time.deltaTime;
                    float step01 = Mathf.Clamp01( elapsedTime / showTime );
                    this.SetScale(step01);
                }
            }
            this.SetScale(1.0f);

            //Debug.Log("Checking if we should play");
            if (this.notification != null && this.notification.LifeTime > 0) //this one can decay
            {
                this.activeRoutine = StartCoroutine(this.StartIdleRoutine(this.notification.LifeTime, hideTime));
            }
            //otherwise just let it end.
            //Debug.Log("Ended Show Routine");
        }

        protected IEnumerator StartIdleRoutine(float idleTime, float hideTime)
        {
            //Debug.Log("Entered Idle Routine. IdelTime = " + idleTime + ", hideTime = " + hideTime);
            yield return new WaitForSeconds(idleTime); //wait untill you're done

            this.activeRoutine = StartCoroutine(this.StartHideRoutine(hideTime));
            //Debug.Log("Ended Idle Routine");
        }


        /// <summary> Begin the process of hiding this notification. </summary>
        public void StartHiding(float hideTime)
        {
            if (this.activeRoutine != null)
            {
                safeGuard = false;
                StopCoroutine(activeRoutine);
                this.activeRoutine = null;
            }
            this.activeRoutine = StartCoroutine(StartHideRoutine(hideTime));
        }

        protected IEnumerator StartHideRoutine(float hideTime)
        {
            //Debug.Log("Entered Hide Routine. hideTime = " + hideTime);
            if (hideTime > 0)
            {
                this.SetScale(0.0f); //this will expand later
                float elapsedTime = 0.0f;
                safeGuard = true;
                while (safeGuard && elapsedTime < hideTime)
                {
                    yield return null; //wait for next frame
                    elapsedTime += Time.deltaTime;
                    float step01 = 1.0f - Mathf.Clamp01(elapsedTime / hideTime); //1- because we're going the other way
                    this.SetScale(step01);
                }
            }
            GameObject.Destroy(this.gameObject); //Destroy self
            //Debug.Log("Exited Idle Routine");
        }


       

        
        protected void OnDestroy()
        {
            safeGuard = false;
        }



//#if UNITY_EDITOR
//        protected void OnValidate()
//        {
//            if (Application.isPlaying)
//            {
//                this.SetScale(this.debugSlider);
//            }
//        }
//#endif


    }
}