using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> 
    /// An all-purpose script to toggle multiple GameObjects, Renderers and/or Monobehaviour Scripts at will. 
    /// </summary>
    /// <remarks> 
    /// Split into three different components because they do not all share the 'enabled' logic.
    /// You can extend from this script to add an auto-assign on Awake() or Start() and save yourself some manual assigning. 
    /// </remarks>
    public class SG_Activator : MonoBehaviour
    {
        /// <summary> Whether or not to enable / disable the elements during Start(). Don't want this script to touch it? Then you can disable the script to skip Start(). </summary>
        public bool activatedOnStart = false;

        /// <summary> Gameobjects to enable / disable. Used to toggle entire groups of objects and scripts.  </summary>
        [SerializeField] protected List<GameObject> linkedObjects = new List<GameObject>();

        /// <summary> MeshRenderers to enable / disable at will. Used only to toggle visuals. </summary>
        [SerializeField] protected List<Renderer> linkedRenderers = new List<Renderer>();

        /// <summary> Scripts to enable / disable at will. Used to activate certain logic. </summary>
        /// <remarks> Assigning this script itself will not alter functionality, except for maybe overriding the Start() function. </remarks>
        [SerializeField] protected List<MonoBehaviour> linkedScripts = new List<MonoBehaviour>();


        /// <summary> Whether or not the elements linked to this script are active and enabled. </summary>
        public bool Activated
        {
            get
            {
                return (linkedObjects.Count > 0 && linkedObjects[0].activeSelf) || (linkedRenderers.Count > 0 && linkedRenderers[0].enabled) || ((linkedScripts.Count > 0 && linkedScripts[0].enabled));
            }
            set
            {
                SetActive(value);
            }
        }

        /// <summary> Set the active/enabled states of this script's linked components. This function can be called from the inspector </summary>
        /// <param name="active"></param>
        public virtual void SetActive(bool active)
        {
            for (int i = 0; i < linkedObjects.Count; i++)
            {
                linkedObjects[i].SetActive(active);
            }
            for (int i = 0; i < linkedRenderers.Count; i++)
            {
                linkedRenderers[i].enabled = active;
            }
            for (int i = 0; i < linkedScripts.Count; i++)
            {
                linkedScripts[i].enabled = active;
            }
        }


        /// <summary> Set the Active/enabled states of this script's linked components. Can be called from the inspector. </summary>
        public virtual void ToggleActivated()
        {
            Activated = !Activated;
        }

        /// <summary> Change the Activator object's color(s) to a new one. Does nothing in the base class, but can be overriden to actually implement behaviour. </summary>
        /// <param name="newColor"></param>
        public virtual void SetColors(Color newColor)
        {

        }

        /// <summary> Add a generic object. If it's a GameObject, a Renderer or a Monobehaviour, we'll add it and return true; </summary>
        /// <param name="obj"></param>
        public virtual bool Add(object obj)
        {
            if (obj is GameObject)
            {
                return Add((GameObject)obj);
            }
            if (obj is Renderer)
            {
                return Add((Renderer)obj);
            }
            if (obj is MonoBehaviour)
            {
                return Add((MonoBehaviour)obj);
            }
            return false;
        }

        /// <summary> Adds a Monobehaviour Script to the list of scripts to enable/disable, if it's not there already </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public virtual bool Add(MonoBehaviour script)
        {
            return Util.SG_Util.SafelyAdd(script, linkedScripts);
        }

        /// <summary> Adds a Renderer to the list of Components to enable/disable, if it's not there already </summary>
        /// <param name="renderer"></param>
        /// <returns></returns>
        public virtual bool Add(Renderer renderer)
        {
            return Util.SG_Util.SafelyAdd(renderer, linkedRenderers);
        }

        /// <summary> Adds a GameObject to the list of objects to enable/disable, if it's not there already </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool Add(GameObject obj)
        {
            return Util.SG_Util.SafelyAdd(obj, linkedObjects);
        }



        /// <summary> Sets the Activated value on startup, provided the script is enabled. </summary>
        protected virtual void Start()
        {
            Activated = activatedOnStart;
        }
    }
}