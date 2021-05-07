using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> Handles SenseGlove connections with SenseCom and/or Android. </summary>
	public class SG_Connections : MonoBehaviour
	{
		//----------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Singleton instance of the SG_Connections </summary>
		private static SG_Connections instance = null;

	//	/// <summary> Putting this variable here for now. Need a way to change that in .ini or Build settings. </summary>
	//	private static bool standaloneMode = false;

		/// <summary> If true, Initialization of communication resources was succesful, so we must dispose of it OnApplicationQuit. </summary>
		private static bool initialized = false;

		/// <summary> a queue of debug messages from SG_Conections, used to show the last few messages from this back-end. </summary>
		private static List<string> msgQueue = new List<string>();
		/// <summary> The maximum number of messages in the queue. </summary>
		private static int maxQueue = 5;
		/// <summary> Optional 3D Text element on which to project the last few messages. </summary>
		private static TextMesh debugElement = null;

		// Instance Members

		/// <summary> When added into the scene manually, you can assign a 3D text to host any debug messages. </summary>
		public TextMesh debugText;


#if UNITY_ANDROID && !UNITY_EDITOR

		/// <summary> The time in between hardware updates to check for new devices. Set slightly faster than the Update Rate of the Android Library. </summary>
		private static readonly float hardwareUpdateTime = 0.5f; //check for new devices every x amount of seconds

		/// <summary> Internal timer to keep track of the next HardwareTick. </summary>
		private float hwTime = 0;

		/// <summary> The number of available SGDevices in the last tick. </summary>
		private static int lastDevices = 0;

		/// <summary> If true, we should unlink the class during OnApplicationQuit </summary>
		private static bool classLinked = false;

		/// <summary> If 1, we initialized the library, and should therefore dispose during OnApplicationQuit. </summary>
		private static int libraryInit = -1;


		
		//----------------------------------------------------------------------------------------------------
		// Data Updates

		/// <summary> Updates deviceStrings only. Since they;re longer and usually don't get updated that often (ever second) </summary>
		private static void Andr_HardwareUpdate()
		{
			if (SG.Util.SG_IAndroid.Andr_ActiveDevices(out lastDevices))
			{
				for (int i = 0; i < lastDevices; i++)
				{
					string devString;
					if (SG.Util.SG_IAndroid.Andr_GetDeviceString(i, out devString))
					{
						SGCore.Util.SGConnect_Android.An_PostDeviceString(i, devString);
					}
				}
			}
		}

		/// <summary> Updates Haptics and Sensor Data </summary>
		private static void Andr_FrameUpdate()
		{
			for (int i = 0; i < lastDevices; i++)
			{
				string hapticsToSend;
				if (SGCore.Util.SGConnect_Android.An_GetHaptics(i, out hapticsToSend))
				{
					SG.Util.SG_IAndroid.Andr_WriteHaptics(i, hapticsToSend);
				}
				string sData;
				if (SG.Util.SG_IAndroid.Andr_GetSensorData(i, out sData))
				{
					SGCore.Util.SGConnect_Android.An_PostSensorData(i, sData);
				}
			}
		}

#endif

		/// <summary> If true, an instance of SG_Connections exists within the scene. </summary>
		public static bool ExistsInScene
		{
			get { return instance != null; }
		}


		//----------------------------------------------------------------------------------------------------
		// Initalization / Disposing of Android Resources.

		/// <summary> Initialized the Android Library is we haven't already </summary>
		/// <remarks> This function could be called every time we switch scenes, and thatshouldn't break things. </remarks>
		private static void Andr_TryInitialize()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
			{
				if (!classLinked)
				{
					classLinked = SG_IAndroid.SetupLink();
					if (classLinked)
					{
						Log("Android Bridge Succesful!");
					}
					else
					{
						Log("Could not link Android resources. SGConnect.aar might be missing or broken.");
					}
				}
				if (classLinked && libraryInit != 1) //we've not yet initialized the Android library!
				{
					libraryInit = SG_IAndroid.Andr_Init();
					Log("Initialized Android Back-End with code " + libraryInit);
					if (libraryInit == 1)
					{
						SGCore.Util.SGConnect_Android.An_Init(); //let our C# back-end know we succeeded.
					}
				}

			}
			else
			{
				Log("C# Library does not support Android back-end! It supports " + SGCore.Library.GetBackEndType().ToString());
			}
#endif
		}

		/// <summary> Disposes of the Android Library if we haven't already. </summary>
		private static void Andr_TryDispose()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (libraryInit == 1) //we did initialize the library!
			{
				int disposeCode = SG.Util.SG_IAndroid.Andr_Dispose(); //TDOD: Something with the code
				SGCore.Util.SGConnect_Android.An_Dispose(); //let our back-end know we've finished.
				Log("Disposed of Android Back-End with code " + disposeCode);
			}
			if (classLinked)
			{
				bool unlinked = SG.Util.SG_IAndroid.DisposeLink(); //explicitly remove the link
				classLinked = false;
			}
#endif
		}


		/// <summary> Utility method to log to Unity, but also to write the last few messages to a 3D text in the scene. </summary>
		/// <param name="message"></param>
		public static void Log(string message)
        {
			Debug.Log("[SenseGlove] " + message);

			if (msgQueue.Count + 1 > maxQueue) { msgQueue.RemoveAt(0); }
			msgQueue.Add(message);

			if (debugElement != null)
            {
				string msg = "";
				for (int i=0; i<msgQueue.Count; i++)
                {
					msg += msgQueue[i];
					if (i < msgQueue.Count - 1) { msg += "\r\n"; } 
                }
				debugElement.text = msg;
            }
        }



		/// <summary> Create a new instance of SG_Connection in the Scene, if we don't have on e yet. </summary>
		public static void SetupConnections()
        {
			//Log("Ensuring we have some form of connections available to us!");
			if (instance == null)
            {
				GameObject connectionObj = new GameObject("[SG Connections]");
				//Log("Created a new instance!");
				instance = connectionObj.AddComponent<SG_Connections>();
            }
        }

		/// <summary> Initialize the SenseGlove back-end, be it SenseCom or Android Class wrappers </summary>
		protected static void Initialize()
        {
			if (!initialized)
			{
				initialized = true;
				
				if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
                {
					Log("Initalizing AndroidStrings");
					Andr_TryInitialize();
				}
				else
                {
					Log("Initalizing SenseCom");
					if (!SGCore.SenseCom.ScanningActive()) //TODO: Standalone Mode?
                    {
						if (SGCore.SenseCom.StartupSenseCom())
                        {
							Log("Started up SenseCom.");
						}
						else
                        {
							Log("SenseCom is not currently running. You won't be able to communicate with your glove(s) until it is running.");
						}
					}
                }
			}
			else
            {
				//Log("Already initialized a scene before this one.");
            }
        }

		/// <summary> Dispose of any Android resources. </summary>
		protected static void Dispose()
        {
			if (initialized)
			{
				initialized = false;
				if (SGCore.Library.GetBackEndType() == SGCore.Library.BackEndType.AndroidStrings)
				{
					Andr_TryDispose();
					Log("Disposed Communications (Standalone modes only)");
				}
			}
			else
            {
				//Log("We never initialized, so we don't have to dispose either");
            }
		}


		//----------------------------------------------------------------------------------------------------
		// Monobehaviour

		void Awake()
		{
			if (instance == null) //when created and in the scene, be sure to add one of these.
			{
				//Log("Linked SenseGlove Connections to " + this.name);
				instance = this;
				debugElement = this.debugText;
				Initialize(); //initialized me!
			}
		}


		void OnDestroy() //called after OnApplicationQuit for some insane reason.
        {
			if (instance ==  this)
            {
				//Log("Unlinked SenseGlove Connections from " + this.name);
				debugElement = null;
				instance = null; //clear my refernce to the instance, so another can take its place.
            }
        }


		void OnApplicationQuit() //is called before OnDestory
        {
			if (instance == this)
			{
				Dispose();
			}
        }


		void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			if (classLinked)
			{
				hwTime += Time.deltaTime;
				if (hwTime >= hardwareUpdateTime)
				{
					hwTime = 0;
					Andr_HardwareUpdate();
				}
				Andr_FrameUpdate();
			}
#endif
		}

	}

}