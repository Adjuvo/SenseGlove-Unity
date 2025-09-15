using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace SG.Util
{

	[ExecuteInEditMode]
	public class SG_RenderMateriaOnTop : MonoBehaviour
	{
		private const string shaderTestMode = "unity_GUIZTestMode"; //The magic property we need to set

		[SerializeField] UnityEngine.Rendering.CompareFunction desiredUIComparison = UnityEngine.Rendering.CompareFunction.Always; //If you want to try out other effects

		[Tooltip("Set to blank to automatically populate from the child UI elements")]
		[SerializeField] Graphic[] uiElementsToApplyTo;

		//Allows us to reuse materials
		private Dictionary<Material, Material> materialMappings = new Dictionary<Material, Material>();

		protected virtual void Start()
		{
			if (uiElementsToApplyTo.Length == 0)
			{
				uiElementsToApplyTo = gameObject.GetComponentsInChildren<Graphic>();
			}

			foreach (var graphic in uiElementsToApplyTo)
			{
				Material material = graphic.materialForRendering;
				if (material == null)
				{
					Debug.LogError($"{nameof(SG_RenderMateriaOnTop)}: skipping target without material {graphic.name}.{graphic.GetType().Name}");
					continue;
				}

				if (!materialMappings.TryGetValue(material, out Material materialCopy))
				{
					materialCopy = new Material(material);
					materialMappings.Add(material, materialCopy);
				}

				materialCopy.SetInt(shaderTestMode, (int)desiredUIComparison);
				graphic.material = materialCopy;
			}
		}
	}
}