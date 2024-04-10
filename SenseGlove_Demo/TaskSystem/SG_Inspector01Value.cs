using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{ 
    /// <summary> Provides a value between 0, 1, adjustable via the Inspector. Use this to Debug any of the SimpleLogic Scripts before you attach it to an object. </summary>
    public class SG_Inspector01Value : MonoBehaviour, SG.IOutputs01Value
    {
        /// <summary> Use the Inspector to set / slide this exact value. </summary>
        [Range(0, 1)] public float normalizedValue = 0;

        /// <summary> The script this input source controls in the inspector. </summary>
        public MonoBehaviour[] controlsScripts = new MonoBehaviour[0];

        /// <summary> The actual percieved input source </summary>
        private IControlledBy01Value[] controls = new IControlledBy01Value[0];

        /// <summary> Returns the Value set to this script. </summary>
        /// <returns></returns>
        public float Get01Value()
        {
            return normalizedValue;
        }

        /// <summary> Upply the 01 value of this script to all those it controls. </summary>
        public void UpdateControlledScripts()
        {
            for (int i = 0; i < controls.Length; i++)
            {
                if (controls[i] != null)
                {
                    controls[i].SetControlValue(this.normalizedValue);
                }
            }
        }

        private void Start()
        {
            List<IControlledBy01Value> ctrls = new List<IControlledBy01Value>();
            for(int i = 0; i < controlsScripts.Length; i++)
            {
                if (controlsScripts[i] is IControlledBy01Value)
                {
                    ctrls.Add( (IControlledBy01Value)controlsScripts[i] );
                }
            }
            this.controls = ctrls.ToArray();
            UpdateControlledScripts();
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateControlledScripts();
            }
        }
#endif

    }
}