using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
	/// <summary> Fades a list of Renderers from full transparency to 0 transparancy. </summary>
	public class SG_MaterialFader : MonoBehaviour
	{

		/// <summary> All renderers involved in drawing the wrist menu btn activator thingies </summary>
		public Renderer[] renderers = new Renderer[0];

		protected Material[] connectedMaterials = new Material[0];
		protected Color[] originalColors = new Color[0];
		protected Color fadedColor = new Color(0, 0, 0, 0); //the faded model.


        protected bool setup = true;

        protected float lastFadeLevel = 1;

        public float FadeLevel { get; private set; }

        public void SetupScript()
        {
            if (setup)
            {
                setup = false;

                List<Material> mats = new List<Material>(this.renderers.Length); //we'll likely have at least as many materials as we have renderers.
                for (int i = 0; i < this.renderers.Length; i++)
                {
                    for (int j = 0; j < this.renderers[i].materials.Length; j++)
                    {
                        mats.Add(renderers[i].materials[j]);
                    }
                }
                this.connectedMaterials = mats.ToArray();

                this.originalColors = new Color[this.connectedMaterials.Length];
                for (int i = 0; i < this.connectedMaterials.Length; i++)
                {
                    this.originalColors[i] = this.connectedMaterials[i].color;
                }
                FadeLevel = 1;
            }
        }

        protected void SetRenderers(bool enabled)
        {
            for (int i = 0; i < this.renderers.Length; i++)
            {
                if (this.renderers[i].enabled != enabled)
                {
                    this.renderers[i].enabled = enabled;
                }
            }
        }

        /// <summary> 0 - fully faded away, 1 is fully back to origignal color. </summary>
        /// <param name="fade01"></param>
        public void SetFadeLevel(float fade01)
        {
            SetRenderers(fade01 > 0.0f); //turn the renderers on if we need to show any transparency.
            for (int i = 0; i < this.connectedMaterials.Length; i++)
            {
                this.connectedMaterials[i].color = Color.Lerp(this.fadedColor, this.originalColors[i], fade01);
            }
            FadeLevel = fade01;
        }


        // Use this for initialization
        void Start()
		{
            this.SetupScript();
		}

	}
}