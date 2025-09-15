using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{

    public class SG_FadeUICanvas : MonoBehaviour
    {
        public CanvasGroup UItoFade;

        public bool startVisible = false;
        public bool startInvisible = false;


        [Header("The objects you want to turn of so the transparency and interactivity will be off")]
        public GameObject[] turnOffAtTheEnd;

        public SG_User user;


        private float _alpha = 0.0f;

        float Alpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                UItoFade.alpha = _alpha;
            }
        }
        public bool stopActiveFading = false; // stops transition and leaves alpha value. Will automatically jump to false again after being used once
        public bool finalizeActiveFading = false; // stops transition but will set it to end alpha value. Will automatically jump to false again after being used once

        Coroutine currentFade;

        private void Start()
        {
            if (UItoFade != null)
            {
                Alpha = UItoFade.alpha;
                if (startVisible) { FadeInUI(0.0f); }
                else if (startInvisible) { FadeOutUI(0.0f); }
            }
            else
                Debug.LogWarning("Please assign a UI to fade");
            if (user == null)
                user = GameObject.FindObjectOfType<SG_User>();
        }

        public void FadeInUI(float duration)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            UItoFade.gameObject.SetActive(true);
            if (UItoFade == null)
            {
                Debug.LogWarning("No UI has been assigned to fade");
                return;
            }

            currentFade = StartCoroutine(FadeStep(Alpha, 1.0f, duration));
        }

        public void FadeOutUI(float duration)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            if (UItoFade == null)
            {
                Debug.LogWarning("No UI has been assigned to fade");
                return;
            }

            currentFade = StartCoroutine(FadeStep(Alpha, 0, duration));
        }


        IEnumerator FadeStep(float startAlpha, float endAlpha, float duration)
        {
            float currTime = 0.0f;

            if (startAlpha > endAlpha)
            {
                while (Alpha > endAlpha)
                {
                    yield return null;
                    if (stopActiveFading)                      // check if fading must be stopped
                    {
                        stopActiveFading = false;
                        break;
                    }
                    if (finalizeActiveFading)
                    {
                        finalizeActiveFading = false;
                        Alpha = endAlpha;
                        break;
                    }

                    currTime += Time.deltaTime;

                    Alpha = Mathf.Lerp(startAlpha, endAlpha, currTime / duration);    // change alpha
                }
            }
            else if (startAlpha < endAlpha)
            {
                while (Alpha < endAlpha)
                {
                    yield return null;
                    if (stopActiveFading)                      // check if fading must be stopped
                    {
                        stopActiveFading = false;
                        break;
                    }
                    if (finalizeActiveFading)
                    {
                        finalizeActiveFading = false;
                        Alpha = endAlpha;
                        break;
                    }

                    currTime += Time.deltaTime;

                    Alpha = Mathf.Lerp(startAlpha, endAlpha, currTime / duration);    // change alpha
                }
            }


            //clamping on reaching the end alpha.
            if (startAlpha < endAlpha)                      // if fading in alpha
            {
                if (Alpha >= endAlpha)                          // clamp alpha
                    Alpha = endAlpha;

                ReleaseObjects();
                TurnOffObjects();
            }
            else                                             // if fading out alpha
            {
                if (Alpha <= endAlpha)                          // clamp alpha
                    Alpha = endAlpha;
            }

            if (Alpha == 0)
            {
                UItoFade.gameObject.SetActive(false);
            }
            else
            {
                UItoFade.gameObject.SetActive(true);
            }
        }

        private void ReleaseObjects()
        {
            if (user == null)
                return;
            if (user.leftHand != null && user.leftHand.grabScript != null)
                user.leftHand.grabScript.ReleaseAll();
            if (user.rightHand != null && user.rightHand.grabScript != null)
                user.rightHand.grabScript.ReleaseAll();
        }

        private void TurnOffObjects()
        {
            foreach (GameObject obj in turnOffAtTheEnd)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

    }
}