using UnityEngine;

namespace SG
{

    /// <summary> A class that contains material properties for a virtual objects, which can be customized, hard-coded or loaded during runtime. </summary>
    [DisallowMultipleComponent]
    public class SG_Material : MonoBehaviour
    {
        //----------------------------------------------------------------------------------
        // Properties

        #region Properties

        /// <summary> The maximum brake force [0..100%] that the material provides when the finger is at maxForceDist inside the collider. </summary>
        [Header("Force-Feedback Settings")]

        ///// <summary> The Force-Feedback response of an object. X axis [0..1] represtents the maxForceDist in relation to the y-axis, where [1] represents the maxForce. </summary>
        public SG_MaterialProperties materialProperties;

        /// <summary> Retrieve the maximum force (y-axis 1 on the forceResponse) from this SG_Material. </summary>
        public float MaxForce
        {
            get { return this.materialProperties != null ? this.materialProperties.maxForce : 1.0f; }
        }

        /// <summary> Retrieve the maximum force distance (x-axis 1 on the forceResponse) from this SG_Material. </summary>
        public float MaxForceDistance
        {
            get { return this.materialProperties != null ? this.materialProperties.maxForceDist : 0.0f; }
        }

        //public AnimationCurve forceRepsonse = AnimationCurve.Constant(0, 1, 1);

        //---------------------------------------------------------------------
        //  Breakable properties

        /// <summary> Indicates that this material can raise an OnBreak event. </summary>
        [Header("Breakable Material Settings")]
        public bool breakable = false;

        /// <summary> The distance [in m] before the material calls an OnBreak event. </summary>
        public float yieldDistance = 0.03f;

        /// <summary> this object must first be picked up before it can be broken. </summary>
        public bool mustBeGrabbed = false;

        /// <summary> This object must be crushed by the thumb before it can be broken </summary>
        public bool requiresThumb = false;

        /// <summary> The minimum amount of fingers (not thumb) that 'break' this object before it actually breaks. </summary>
        public int minimumFingers = 1;

        /// <summary> Check whether or not this object is broken. </summary>
        private bool isBroken = false;


        /// <summary> My (optional) interactable script </summary>
        private SG_Interactable myInteractable;

        /// <summary> (Optional) Connected Material Deformation Script, used to pass deformation paraeters? </summary>
        protected SG_MeshDeform deformScript;

        /// <summary> [thumb/palm, index, middle, pinky, ring] </summary>
        private bool[] raisedBreak = new bool[5];

        /// <summary> How many fingers [not thumb] have raised break events. </summary>
        private int brokenBy = 0;


        #endregion Properties

        //----------------------------------------------------------------------------------
        // Material Methods

        #region MaterialMethods

        public void Touch(SG_FingerFeedback script)
        {
            //TODO: Some form of hover(ing) scripts?
        }

        public void UnTouch(SG_FingerFeedback script)
        {
            //TODO: Event?
        }

        public bool CanDeform()
        {
            return this.deformScript != null;
        }

        /// <summary> Check if this material is broken </summary>
        /// <returns></returns>
        public bool IsBroken()
        {
            return this.isBroken;
        }

        /// <summary> Set whether or not this material is broken. </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetBroken(bool newBroken)
        {
            if (newBroken != this.isBroken)
            {
                if (this.isBroken && newBroken)
                {
                    this.UnBreak();
                }
                this.isBroken = newBroken;
            }

        }

        /// <summary> Unbreak the material, allowing it to give feedback and raise the break event again. </summary>
        public void UnBreak()
        {
            this.isBroken = false;
            this.brokenBy = 0;
            this.raisedBreak = new bool[5];

            if (this.deformScript != null)
                this.deformScript.ResetMesh();
        }


        /// <summary> Calculates the force on the finger based on material properties. </summary>
        /// <param name="displacement"></param>
        /// <param name="fingerIndex"></param>
        /// <returns></returns>
        public int CalculateForce(float displacement, int fingerIndex)
        {
            if (this.breakable)
            {
                if (!this.isBroken)
                {
                    //  SenseGlove_Debugger.Log("Disp:\t" + displacement + ",\t i:\t"+fingerIndex);
                    if (!this.mustBeGrabbed || (this.mustBeGrabbed && this.myInteractable.IsGrabbed()))
                    {
                        // SenseGlove_Debugger.Log("mustBeGrabbed = " + this.mustBeGrabbed + ", isInteracting: " + this.myInteractable.IsInteracting());

                        if (fingerIndex >= 0 && fingerIndex < 5)
                        {
                            bool shouldBreak = displacement >= this.yieldDistance;
                            if (shouldBreak && !this.raisedBreak[fingerIndex])
                            { this.brokenBy++; }
                            else if (!shouldBreak && this.raisedBreak[fingerIndex])
                            { this.brokenBy--; }
                            this.raisedBreak[fingerIndex] = shouldBreak;

                            // SenseGlove_Debugger.Log(displacement + " --> raisedBreak[" + fingerIndex + "] = " + this.raisedBreak[fingerIndex]+" --> "+this.brokenBy);
                            if (this.brokenBy >= this.minimumFingers && (!this.requiresThumb || (this.requiresThumb && this.raisedBreak[0])))
                            {
                                this.OnMaterialBreak();
                            }
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }


            if (this.materialProperties != null)
            {
                return (int)SG_Material.CalculateResponseForce(displacement, (int)(this.materialProperties.maxForce * 100), this.materialProperties.maxForceDist, ref this.materialProperties.forceRepsonse);
            }
            return 100; //just full FFB at this point.
        }

        public static int CalculateResponseForce(float disp, int maxForce, float maxForceDist, ref AnimationCurve ffbCurve)
        {
            if (maxForceDist > 0)
            {
                float mappedDispl = disp / maxForceDist;
                float forceMagn = ffbCurve.Evaluate(mappedDispl);
                return (int)SGCore.Kinematics.Values.Clamp(forceMagn * maxForce, 0, maxForce);
            }
            else if (disp > 0)
            {
                return maxForce;
            }
            return 0;
        }

        #endregion MaterialMethods


        //------------------------------------------------------------------------------------
        // Events

        public delegate void MaterialBreaksEventHandler(object source, System.EventArgs args);
        /// <summary> Fires when the material breaks under the conditions set through the Material Properties. </summary>
        public event MaterialBreaksEventHandler MaterialBreaks;

        /// <summary> Notify that this material is broken. </summary>
        protected void OnMaterialBreak()
        {
            if (MaterialBreaks != null)
            {
                MaterialBreaks(this, null);
            }
            this.isBroken = true;
            this.brokenBy = 0;
            this.raisedBreak = new bool[this.raisedBreak.Length];
        }


        //----------------------------------------------------------------------------------
        // Monobehaviour

        #region Monobehaviour

        protected virtual void Start()
        {
            //load grab options
            this.myInteractable = this.gameObject.GetComponent<SG_Interactable>();
            if (myInteractable == null && this.mustBeGrabbed)
            {
                this.mustBeGrabbed = false; //we cannot require this material to be grabbed if it's not an interactable.
            }
            if (this.deformScript == null)
            {
                this.deformScript = this.gameObject.GetComponent<SG_MeshDeform>();
            }
        }

        /// <summary> Unbreak this material if it is disabled. </summary>
        protected virtual void OnDisable()
        {
            if (!SG.Util.SG_Util.IsQuitting) //otherwise Unity will cry when we change parenting etc
            {
                this.UnBreak();
            }
        }



        #endregion Monobehaviour


    }
}