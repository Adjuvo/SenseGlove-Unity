using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> For those forgetful devs; this script ensures that certain GameObjects are enabled at the start of a simulation, so you can disable them to your heart's content. </summary>
	public class SG_EnsureEnabled : MonoBehaviour
	{
		public enum EnableWhen
        {
			BuildOnly,
			EditorOnly,
			EditorAndBuild
        }

		public EnableWhen enableAt = EnableWhen.BuildOnly;

		/// <summary> All objects that should definitely be enabled in your build(s). </summary>
		public GameObject[] objectsToEnable = new GameObject[0];


		public void SetAllObjects(bool active)
        {
			for (int i=0; i<objectsToEnable.Length; i++)
            {
				objectsToEnable[i].SetActive(active);
            }
        }


		public bool ShouldEnable()
		{
#if UNITY_EDITOR
			return enableAt != EnableWhen.BuildOnly;
#else
			return enableAt != EnableWhen.EditorOnly;
#endif
		}

		// Use this for initialization
		void Start()
		{
			if (ShouldEnable())
            {
				this.SetAllObjects(true);
            }
		}

	}
}
