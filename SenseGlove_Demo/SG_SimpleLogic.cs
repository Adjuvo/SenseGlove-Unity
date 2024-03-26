using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{
	/// <summary> A base script that takes input from a Value01 provider, to do something with later. </summary>
	public class SG_SimpleLogic : MonoBehaviour
	{
		//------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Optional GameObject to take a value from. If left empty, we will attempt to get values from the attached GameObject. </summary>
		[Header("Input")]
		public MonoBehaviour takeValueFrom;

		/// <summary> The interface from which to collect data. </summary>
		public IOutputs01Value ValueSource
        {
			get; set;
        }

		public virtual float NormalizedValue
        {
			get { return this.ValueSource != null ? this.ValueSource.Get01Value() : 0; }
        }

		//------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		protected virtual void Setup()
        {
			if (ValueSource == null && takeValueFrom != null && takeValueFrom is IOutputs01Value)
			{
				ValueSource = (IOutputs01Value)takeValueFrom;
			}
		}

		protected virtual void Start()
		{
			Setup();
		}

	}
}