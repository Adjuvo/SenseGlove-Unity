using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
    public class SG_QuitKey : MonoBehaviour
    {
        public KeyCode exitKey = KeyCode.None;
        public KeyCode resetKey = KeyCode.None;

        public void Quit()
        {
            Application.Quit();
        }

        public void ResetScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // Update is called once per frame
        void Update()
        {
            if (SG_Util.keyBindsEnabled)
            {
                if (Input.GetKeyDown(exitKey))
                {
                    Quit();
                }
                else if (Input.GetKeyDown(resetKey))
                {
                    ResetScene();
                }
            }
        }
    }
}