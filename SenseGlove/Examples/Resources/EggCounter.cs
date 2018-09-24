using UnityEngine;

namespace SenseGlove_Examples
{

    /// <summary> Shows a Golden egg at certain intervals, and keeps track of the total amount of eggs crushed. </summary>
    public class EggCounter : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------
        // Properties

        /// <summary> The total amount of Eggs Crushed on this PC. </summary>
        public int eggCount;

        /// <summary> The egg which is checked for breaking. </summary>
        public SenseGlove_Breakable checkEgg;

        /// <summary> Displays the egg count. </summary>
        public TextMesh eggText;

        /// <summary> Refrence to the object that shows the Egg. </summary>
        public CycleObjects swapper;

        /// <summary> Used to detect a change in swapper. </summary>
        private int prevIndex = -1;

        /// <summary> Two materials to change between the egg. </summary>
        public Material whiteEgg, GoldEgg;

        /// <summary> Show a golden egg in this interval. </summary>
        public int multiples = 2;

        /// <summary> The SenseGlove_Material of this egg, which will change spending on the egg. </summary>
        protected SenseGlove_Material eggMaterial;

        // Egg Properties
        protected int eggForce;
        protected float eggForceDist;
        protected int eggFingers;

        // Golden Egg Properties (hard coded)
        protected int goldForce = 100;
        protected float goldForceDist = 0.00f;
        protected int goldFingers = 3;


        //--------------------------------------------------------------------------------------
        // Class Methods

        /// <summary> Set uo the script refrences and material properties. </summary>
        protected void Setup()
        {
            this.eggCount = this.LoadCount();
            this.UpdateText();
            this.eggText.gameObject.SetActive(false);

            this.eggMaterial = this.checkEgg.wholeObject.GetComponent<SenseGlove_Material>();
            if (eggMaterial != null)
            {
                eggMaterial.MaterialBreaks += EggCounter_MaterialBreaks;

                this.eggForce = eggMaterial.maxForce;
                this.eggForceDist = eggMaterial.maxForceDist;
                this.eggFingers = eggMaterial.minimumFingers;
            }

        }

        /// <summary> Check if the EggTest needs to be active. </summary>
        protected void CheckSwapper()
        {
            if (this.swapper.index != this.prevIndex)
            {
                if (this.swapper.index >= 0 && this.swapper.objectsToSwap[this.swapper.index].name.Contains("Egg"))
                {
                    this.eggText.gameObject.SetActive(true);
                }
                else
                {
                    this.eggText.gameObject.SetActive(false);
                }
                this.prevIndex = this.swapper.index;
            }
        }


        /// <summary> Fires when the Material breaks. </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void EggCounter_MaterialBreaks(object source, System.EventArgs args)
        {
            this.eggCount++;

            if ((this.eggCount + 1) % multiples == 0) //every X eggs
            { //the next one is multiple of X
              //get golden egg

                SetMaterial(this.GoldEgg);
                SetEggProps(this.goldForce, this.goldForceDist, this.goldFingers);
                //  Debug.Log("Gold");
            }
            else if (this.eggCount % multiples == 0)
            { //this one is a multiple of X

                SetMaterial(this.whiteEgg);
                SetEggProps(this.eggForce, this.eggForceDist, this.eggFingers);
                //  Debug.Log("White");

            }
            this.UpdateText();
        }

        /// <summary> Apply new material properties for the Egg </summary>
        /// <param name="force"></param>
        /// <param name="dist"></param>
        /// <param name="minFingers"></param>
        private void SetEggProps(int force, float dist, int minFingers)
        {
            if (this.eggMaterial != null)
            {
                this.eggMaterial.maxForce = force;
                this.eggMaterial.maxForceDist = dist;
                this.eggMaterial.minimumFingers = minFingers;
            }
        }

        /// <summary>  Set the render component for the egg. </summary>
        /// <param name="newMaterial"></param>
        private void SetMaterial(Material newMaterial)
        {
            this.eggMaterial.GetComponent<Renderer>().material = newMaterial;
        }

        /// <summary> Update the Egg Text. </summary>
        public void UpdateText()
        {
            this.eggText.text = "Eggs Crushed\r\n" + this.eggCount;
        }

        /// <summary> Load the EggsCrushed from PlayerPrefs. </summary>
        /// <returns></returns>
        private int LoadCount()
        {
            if (PlayerPrefs.HasKey("eggCount"))
            {
                int count = PlayerPrefs.GetInt("eggCount", 0);
                return count;
            }

            return 0;
        }

        /// <summary> Save the EggsCrushed into the PlayerPrefs. </summary>
        private void SaveCount()
        {
            PlayerPrefs.SetInt("eggCount", this.eggCount);
        }

        //--------------------------------------------------------------------------------------
        // Monobehaviour

        // Use this for initialization
        void Start()
        {
            this.Setup();
        }

        // Update is called once per frame
        void Update()
        {
            this.CheckSwapper();
        }

        //ensure saving of the eggCount.
        private void OnApplicationQuit()
        {
            this.SaveCount();
        }
    }

}