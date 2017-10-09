using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Hologram : SenseGlove_HandModel
{
    /// <summary> 
    /// Collect a proper (finger x joint) array, and assign it to this.fingerJoints(). Use the handRoot variable to help you iterate. 
    /// For this HandModel, the actual rotation joints are hidden within 
    /// </summary>
    protected override void CollectFingerJoints()
    {
        this.fingerJoints.Clear();
        foreach (Transform fingerRoot in handRoot)
        {
            List<Transform> fingerList = new List<Transform>();
            {
                fingerList.Add(fingerRoot.GetChild(0));
                fingerList.Add(fingerRoot.GetChild(0).GetChild(0).GetChild(0));
                fingerList.Add(fingerRoot.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0));
            }
            this.fingerJoints.Add(fingerList);
        }
    }

}
