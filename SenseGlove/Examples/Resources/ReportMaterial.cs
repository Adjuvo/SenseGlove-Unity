using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SenseGlove_Examples
{
    public class ReportMaterial : MonoBehaviour
    {
        public SenseGlove_Material myMaterial;

        public TextMesh debugText;

        // Use this for initialization
        void Start()
        {
            UpdateMaterial();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void UpdateMaterial()
        {
            if (this.myMaterial != null && this.debugText != null)
            {
                List<string> myProps = new List<string>();

                //Name
                string name = "Custom Material";
                if (myMaterial.material == VirtualMaterial.FromDataBase)
                {
                    name = "Ext: " + myMaterial.materialName;
                }
                else if (myMaterial.material != VirtualMaterial.Custom)
                {
                    name = myMaterial.material.ToString();
                }
                myProps.Add(name);

                //other properties.
                myProps.Add("MaxForce:\t"+myMaterial.maxForce+"%");
                myProps.Add("MaxForceDist:\t" + myMaterial.maxForceDist + "m");

                string text = "";
                for (int i=0; i<myProps.Count; i++)
                {
                    text += myProps[i];
                    if (i < myProps.Count - 1)
                    {
                        text += "\r\n";
                    }
                }
                this.debugText.text = text;
            }
        }



    }
}