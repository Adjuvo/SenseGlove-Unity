using UnityEngine;

namespace SG
{

    /// <summary> Control script so the SimpleLogic can work with / operate on sliders. </summary>
    /// <remarks> Placed in a separate Monobehaviour script instead of inheriting from Slider so as to easily 'slot' into existing interactions. </remarks>
    public class SG_SliderController : MonoBehaviour, SG.IControlledBy01Value, SG.IOutputs01Value
    {
        /// <summary> The linked slider. Without this, the Script does not work. Will look for one on this GameObject if it's not already linked. </summary>
        [SerializeField] private UnityEngine.UI.Slider slider;

        /// <summary> Utility to check if we're not  </summary>
        private bool sliderIs01 = false;

        /// <summary> Evaluate whether or not this is a 0.0f .. 1.0f slider. Called automatically on Start. Here as Public function for you to call if you even update slider's minValue / maxValue </summary>
        public void CheckSliderValues()
        {
            if (slider != null)
                sliderIs01 = Mathf.Abs(slider.minValue) < 0.01f && Mathf.Abs(slider.maxValue - 1.0f) < 0.01f; //close enough to 0 .. 1.
        }

        /// <summary> Returns the slider's Normalized value 0.0f ... 1.0f </summary>
        /// <returns></returns>
        public float Get01Value()
        {
            if (slider == null)
                return 0.0f;
            return sliderIs01 ? slider.value : SG.Util.SG_Util.Map(slider.value, slider.minValue, slider.maxValue, 0.0f, 1.0f, true);
        }

        /// <summary> Sets the Slider's value between minValue [0.0f] and maxValue [1.0f] </summary>
        /// <param name="value01"></param>
        public void SetControlValue(float value01)
        {
            if (slider == null)
                return;

            if (sliderIs01)
                slider.value = Mathf.Clamp01(value01);
            else
                slider.value = SG.Util.SG_Util.Map(value01, 0.0f, 1.0f, slider.minValue, slider.maxValue);
        }

        void Start()
        {
            if (slider == null)
                slider = GetComponent<UnityEngine.UI.Slider>();
            CheckSliderValues();
        }
    }
}