using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;

/*
 * Version 2.0 of the initial SenseGlove Calibration Void. Runs the user through a series of steps with instructions, including the calibration of wrist tracking.
 * Makes the resulting calibration much better compared to the basic "open and close your hands" - but takes more time.
 * 
 * @author
 * amber@senseglove.com
 * max@senseglove.com
 */

namespace SG.Util
{
    /// <summary> Loads a Scene in the background, then waits until it is allowed to progess to fire it. </summary>
    public class SG_PreloadNextScene : MonoBehaviour
    {
        //----------------------------------------------------------------------------------------------------------------------------
        // Enums

        /// <summary> How to select the scene to load. </summary>
        public enum SceneSelection
        {
            Unknown,
            ByBuildIndex,
            BySceneName
        }

        //----------------------------------------------------------------------------------------------------------------------------
        // Components

        /// <summary> If true, this script will start loading the next level on startup. </summary>
        [SerializeField] private bool loadSceneOnStart = false;

        /// <summary> How the scene to laod is chosen </summary>
        [SerializeField] private SceneSelection selectScene = SceneSelection.ByBuildIndex;

        /// <summary> The BuildIndex to load </summary>
        [SerializeField] private int nextSceneIndex = -1;
        /// <summary> The Scene to load by name </summary>
        [SerializeField] private string nextSceneName = "";


        /// <summary> To ensure we don't start more than one. </summary>
        private Coroutine loadRoutine = null;
        /// <summary> If true, we escape a while loop. </summary>
        private bool continueToNextScene = false;

        private bool loadingValidScene = false;


        /// <summary> Fires when the transition is called - we will go to the next scene in a set amount of time! </summary>
        public UnityEngine.Events.UnityEvent SceneTransitionStarted;


        //----------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> If true, we will pass on to the next level as soon as possible. </summary>
        public bool GoToNextLevel => continueToNextScene;


        //----------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Start Loadign the next scene by sceneName or buildIndex as indicated in the inspector. </summary>
        public void StartLoadingNextScene()
        {
            if (loadRoutine == null)
            {
                loadingValidScene = false;

                int nextIndex = nextSceneIndex;
                if (selectScene == SceneSelection.BySceneName)
                {
                    UnityEngine.SceneManagement.Scene nextScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(nextSceneName);
                    if (nextScene != null)
                        nextSceneIndex = nextScene.buildIndex;
                }

                int sceneTotal = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

                if (nextIndex > -1 && nextIndex < sceneTotal)
                {
                    int currSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                    if (nextIndex == currSceneIndex)
                        Debug.Log("SG_PreloadNextScene is re-loading the current scene. Make sure that is your intention", this);
                    loadRoutine = StartCoroutine(LoadNextLevel(nextIndex));
                }
                else
                    Debug.LogError("SG_PreloadNextScene does not have a valid scene index!", this);
            }
        }

        /// <summary> Allowd you to continue to the next scene, optionally with a delay. </summary>
        /// <param name="afterTime"></param>
        public void ContinueToNextScene(float afterTime)
        {
            if ( !loadingValidScene ) //only care about this when we're loading a valid next scene.
                return;
            
            SceneTransitionStarted?.Invoke();
            if (afterTime > 0.0f)
                StartCoroutine( ContinueAfter(afterTime) );
            else
                continueToNextScene = true;
        }

        /// <summary> Working routine to set a variable after a delay </summary>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        private IEnumerator ContinueAfter(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            continueToNextScene = true;
        }

        /// <summary> Actual background worker thread. </summary>
        /// <returns></returns>
        private IEnumerator LoadNextLevel(int buildIndex)
        {
            yield return null; //wait one frame so other scripts can set the next Scene's Build Index / Name during their Start() / Awake().
            Debug.Log("Loading next scene (" + buildIndex + ") in the background...");
            AsyncOperation asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex);
            if (asyncOperation != null)
            {
                loadingValidScene = true;
                asyncOperation.allowSceneActivation = false; //Don't let the Scene activate until you allow it to
                while (!asyncOperation.isDone)
                {
                    if (asyncOperation.progress >= 0.90f)
                    {
                        if (continueToNextScene)
                            asyncOperation.allowSceneActivation = true;
                    }
                    yield return null;
                }
            }
            else
            {
                Debug.LogError("Something went wrong creating an AsyncOperation!");
            }
        }


        //----------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void Start()
        {
            if (loadSceneOnStart)
                StartLoadingNextScene();
        }

    }
}