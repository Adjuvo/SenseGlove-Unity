using UnityEngine;

namespace SG.Util
{
	/// <summary> Utility class to reset or quit Unity Scenes. Can be called from anywhere trhough static functions, or be placed in a scene to access via buttons / hotkeys. </summary>
	public class SG_SceneControl : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Hotkey to reset the current scene </summary>
		public KeyCode resetKey = KeyCode.R;
		/// <summary> Hotkey to shut down the application </summary>
		public KeyCode quitKey = KeyCode.Escape;


		//--------------------------------------------------------------------------------------------------------------------------
		// Functions

		/// <summary> Resets the current scene. </summary>
		/// <remarks> Placed in a static function so we can call it from anywhere. </remarks>
		public static void ResetScene()
		{
			if (Application.isPlaying)
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
			}
		}


		/// <summary> Resets the current scene </summary>
		/// <remarks> nonstatic function to call via Buttons / UI. </remarks>
		public void Reset()
		{
			SG_SceneControl.ResetScene();
		}

		/// <summary> Shuts down this application. </summary>
		/// <remarks> Placed in a static function so we can call it from anywhere. </remarks>
		public static void QuitApplication()
		{
			Application.Quit();
		}


		/// <summary> Shuts down this application </summary>
		/// <remarks> nonstatic function to call via Buttons / UI. </remarks>
		public void Quit()
        {
			SG_SceneControl.QuitApplication();
		}


		//--------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(resetKey))
			{
				ResetScene();
			}
			else if (Input.GetKeyDown(quitKey))
			{
				Quit();
			}
		}
	}
}