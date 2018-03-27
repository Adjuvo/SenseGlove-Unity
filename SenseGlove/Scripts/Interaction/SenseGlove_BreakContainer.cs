using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A SenseGlove_Breakable that contains objects and optionally spawns shards of itself upon breaking. </summary>
public class SenseGlove_BreakContainer : SenseGlove_Breakable
{
    /// <summary> Contains the GameObjects that represent the shards of the broken object. </summary>
    [Tooltip("Contains the GameObjects that represent the shards of the broken object.")]
    public GameObject shardContainer;

    /// <summary> Contains SenseGlove_Interactable objects that will be released upon the container breaking. </summary>
    [Tooltip(" Contains SenseGlove_Interactable objects that will be released upon the container breaking.")]
    public GameObject contentsContainer;

    /// <summary> All GameObjects within the shardsContainer. Will be spawned at the time of breaking.  </summary>
    private GameObject[] brokenShards = new GameObject[0];

    /// <summary> All SenseGlove_Interactables within the container. Will be set to interactable at the time of breaking. </summary>
    private SenseGlove_Interactable[] contents = new SenseGlove_Interactable[0];

    /// <summary> The localRotations of the shards, applied on a reset. </summary>
    private Quaternion[] contentRotations;
    /// <summary> The localPositions of the shards, applied on reset. </summary>
    private Vector3[] contentPositions;

    /// <summary> The localRotations of the shards, applied on a reset. </summary>
    private Quaternion[] shardRotations;
    /// <summary> The localPositions of the shards, applied on reset. </summary>
    private Vector3[] shardPositions;

    //Collect object data before wakeup.
    private void Awake()
    {
        if (shardContainer != null)
        {
            this.shardContainer.SetActive(true);
            this.brokenShards = new GameObject[this.shardContainer.transform.childCount];
            this.shardPositions = new Vector3[brokenShards.Length];
            this.shardRotations = new Quaternion[brokenShards.Length];
            for (int i = 0; i < this.brokenShards.Length; i++)
            {
                this.brokenShards[i] = this.shardContainer.transform.GetChild(i).gameObject;
                this.shardRotations[i] = this.brokenShards[i].transform.localRotation;
                this.shardPositions[i] = this.brokenShards[i].transform.localPosition;
                SetRB(this.brokenShards[i], false, true);

                this.brokenShards[i].SetActive(false);
            }
            this.ResetShards();
        }
        else
        {
            this.brokenShards = new GameObject[0];
        }

        if (this.contentsContainer != null)
        {
            this.contentsContainer.SetActive(true);

            this.contents = new SenseGlove_Interactable[this.contentsContainer.transform.childCount];
            this.contentPositions = new Vector3[contents.Length];
            this.contentRotations = new Quaternion[contents.Length];
            for (int i = 0; i < this.contents.Length; i++)
            {
                this.contents[i] = this.contentsContainer.transform.GetChild(i).GetComponent<SenseGlove_Interactable>();
                this.contentRotations[i] = this.contents[i].transform.localRotation;
                this.contentPositions[i] = this.contents[i].transform.localPosition;
            }
            this.ResetContents();
        }
    }

    /// <summary> Called when the breakable material of the wholeObject is broken </summary>
    public override void Break()
    {
        base.Break();
        this.SpawnContents();
        this.SpawnShards();
    }

    /// <summary> Called when the breakable material is reset. </summary>
    public override void UnBreak()
    {
        this.ResetShards();
        this.ResetContents();
        base.UnBreak();
    }

    /// <summary> Called when the object should be reset. </summary>
    public override void ResetObject()
    {
       // this.SetContents(false, this.contentsContainer.transform);
       // this.ResetShards();
        base.ResetObject(); //unbreak is called here
    }

    /// <summary> Spawns the Shards when the container breaks. </summary>
    protected void SpawnShards()
    {
        for (int i = 0; i < this.brokenShards.Length; i++)
        {
            this.brokenShards[i].transform.parent = null;
            SetRB(this.brokenShards[i], true, false);
            this.brokenShards[i].SetActive(true);
        }
    }

    /// <summary> Put all the shards back to their original (local) transform. </summary>
    protected void ResetShards()
    {
        for (int i = 0; i < this.brokenShards.Length; i++)
        {
            SetRB(this.brokenShards[i], false, true);
            this.brokenShards[i].transform.parent = this.shardContainer.transform;
            this.brokenShards[i].transform.localPosition = this.shardPositions[i];
            this.brokenShards[i].transform.localRotation = this.shardRotations[i];
            this.brokenShards[i].SetActive(false);
        }
    }

    /// <summary> Spawns Contents when the container breaks </summary>
    protected void SpawnContents()
    {
        for (int i = 0; i < this.contents.Length; i++)
        {
            this.contents[i].transform.parent = null;
        }
        SetContents(true);
    }

    /// <summary> Resets the contents back to their original (local) transforms. </summary>
    protected void ResetContents()
    {
        SetContents(false);
        for (int i = 0; i < this.contents.Length; i++)
        {
            this.contents[i].transform.parent = this.contentsContainer.transform;
            this.contents[i].transform.localPosition = this.contentPositions[i];
            this.contents[i].transform.localRotation = this.contentRotations[i];
        }
    }

    /// <summary> Set the contents within the container. </summary>
    /// <param name="active"></param>
    /// <param name="newParent"></param>
    private void SetContents(bool active)
    {
        for (int i = 0; i < this.contents.Length; i++)
        {
            this.contents[i].isInteractable = active;
            Collider[] cols = this.contents[i].GetComponents<Collider>(); //todo: make this part of the Interactable class?
            for (int j = 0; j < cols.Length; j++)
            {
                cols[j].isTrigger = !active;
            }
            SetRB(this.contents[i].gameObject, active, !active);
        }
    }

    /// <summary> Set the Rigidbody options of a particular gameObject. </summary>
    /// <param name="Obj"></param>
    /// <param name="gravity"></param>
    /// <param name="kinematic"></param>
    public void SetRB(GameObject Obj, bool gravity, bool kinematic)
    {
        Rigidbody BR = Obj.GetComponent<Rigidbody>();
        if (BR != null)
        {
            BR.useGravity = gravity;
            BR.isKinematic = kinematic;
        }
    }



}
