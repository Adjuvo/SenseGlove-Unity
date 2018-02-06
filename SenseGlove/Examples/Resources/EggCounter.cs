using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EggCounter : MonoBehaviour
{

    public int eggCount;

    public SenseGlove_Breakable checkEgg;

    public TextMesh EggText;

    public SwapObjects swapper;

    private int prevIndex = -1;

    public Material whiteEgg, GoldEgg;

    public int multiples = 2;

	// Use this for initialization
	void Start ()
    {
        this.eggCount = this.LoadCount();
        this.UpdateText();
        this.EggText.gameObject.SetActive(false);
        this.checkEgg.wholeObject.GetComponent<SenseGlove_Material>().MaterialBreaks += EggCounter_MaterialBreaks;

        if (this.eggCount + 1 % multiples == 0) //every X eggs
        { //the next one is multiple of X
            //get golden egg


        }

    }

    private void EggCounter_MaterialBreaks(object source, System.EventArgs args)
    {
        this.eggCount++;

        if ((this.eggCount + 1) % multiples == 0) //every X eggs
        { //the next one is multiple of X
            //get golden egg

            SetMaterial(this.GoldEgg);
          //  Debug.Log("Gold");
        }
        else if (this.eggCount % multiples == 0)
        { //this one is a multiple of X

            SetMaterial(this.whiteEgg);
          //  Debug.Log("White");

        }
        this.UpdateText();
    }

    private void SetMaterial(Material newMaterial)
    {
        this.checkEgg.wholeObject.GetComponent<Renderer>().material = newMaterial;
    }

    


    public void UpdateText()
    {
        this.EggText.text = "Eggs Crushed\r\n" + this.eggCount;
    }

    // Update is called once per frame
    void Update ()
    {
		if (this.swapper.index != this.prevIndex)
        {
            if (this.swapper.index >= 0 && this.swapper.objectsToSwap[this.swapper.index].name.Contains("Egg"))
            {
                this.EggText.gameObject.SetActive(true);
            }
            else
            {
                this.EggText.gameObject.SetActive(false);
            }
            this.prevIndex = this.swapper.index;
        }
	}

    private void OnApplicationQuit()
    {
        this.SaveCount();
    }

    private int LoadCount()
    {
        try
        {
            string[] lines = File.ReadAllLines("EggCount.txt");
            if (lines.Length > 0)
            {
                return SenseGloveCs.Values.toInt(lines[0]);
            }
        }
        catch
        {
            return 0;
        }
        return 0;
    }

    private void SaveCount()
    {
        StreamWriter writer = new StreamWriter("EggCount.txt");
        writer.WriteLine(this.eggCount);
        writer.Close();
    }

}
