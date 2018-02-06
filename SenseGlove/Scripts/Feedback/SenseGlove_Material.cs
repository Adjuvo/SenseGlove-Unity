using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Material Properties that can be assinged to a GameObject </summary>
[HelpURL("https://github.com/Adjuvo/SenseGlove-Unity/wiki/SenseGlove_Material")]
public class SenseGlove_Material : MonoBehaviour
{

    //----------------------------------------------------------------------------------------------
    // Class Attributes

    #region Attributes

    /// <summary> The maximum force that this material can return. </summary>
    [Header("Material Properties")]
    [Tooltip("The maximum force level that this material can return.")]
    [Range(0, 255)]
    public int maxForce = 255;

    /// <summary> The distance (in m) before the maximum force is reached. </summary>
    [Tooltip("The distance (in m) before the maximum force is reached.")]
    public float maxForceDist = 0.00f;

    /// <summary> Determines if this Material gives Haptic feedback through buzzMotors. </summary>
    [Tooltip("Determines if this Material gives Haptic feedback through buzzMotors.")]
    public bool hapticFeedback = false;

    [Header("Break Properties")]

    /// <summary> Whether or not this material can raise a Break event. </summary>
    [Tooltip("Set this value to true if you wish for the material to break.")]
    public bool breakable = false;
    
    /// <summary> If a collider gets this deep into the object, it breaks. </summary>
    [Tooltip("If a collider passes this distance (in m) into the object, it breaks. ")]
    public float yieldDistance = 0.03f;

    /// <summary> this object must first be picked up before it can be broken. </summary>
    public bool mustBeGrabbed = false;

    public bool requiresThumb = false;

    // - Is assumed to be wapped in MustBeGrabbed
   // /// <summary> The object must be touched by the thumb or hand palm before it can be broken </summary>
   // /// <remarks> The object can only be crushed between the fingers. </remarks>
   // public bool mustTouchThumbOrPalm = false;

    /// <summary> The minimum amount of fingers (not thumb) that 'break' this object before it actually breaks. </summary>
    [Range(1, 4)]
    public int minimumFingers = 1;
    
    /// <summary> Check whether or not this object is broken. </summary>
    [Tooltip("Check if this object is broken.")]
    public bool isBroken = false;

    /// <summary> Set to true if you wish the Mesh to visually deform </summary>
    [Header("Mesh Properties")]
    [Tooltip(" Set to true if you wish the Mesh to visually deform.")]
    public bool deforms = false;

    /// <summary> Will be used to extract the Mesh variable without exposing it to other classes. </summary>
    [Tooltip("The filter used to extract the mesh of the object to deform.")]
    public MeshFilter meshFilter;
   
    /// <summary> Determines how the Vertices respond to the collider(s) </summary>
    [Tooltip("Determines how the Vertices respond to the collider(s)")]
    public DisplaceType displaceType = DisplaceType.Plane;

    /// <summary> The Maximum that a vertex can displace from its original position </summary>
    [Tooltip("The Maximum that a vertex can displace from its original position")]
    public float maxDisplacement = 0.01f;

    /// <summary> The actual Mesh to manipulate. </summary>
    private Mesh myMesh;

    /// <summary> The original vertices of the mesh, used for Deformation Logic </summary>
    private Vector3[] verts;

    /// <summary> The deformed mesh vertices, which are used to update the Mesh </summary>
    private Vector3[] deformVerts;

    /// <summary> Indicated that the Mesh should be defroming. No need to recalculate unless they are being touched by a Feedback Collider. </summary>
    private bool atRest = true;

    /// <summary> The indices (in myMesh.vertices) that represent points that may be shared with others </summary>
    private int[] uniqueVertices;
    
    /// <summary> The points shared by the Vertices at each indes of uniqueVertices. </summary>
    private int[][] sameVertices;


    //new

    /// <summary> My (optional) interactable </summary>
    private SenseGlove_Interactable myInteractable;

    /// <summary> [thumb/palm, index, middle, pinky, ring] </summary>
    private bool[] raisedBreak = new bool[5];

    /// <summary> How many fingers [not thumb] have raised break events. </summary>
    private int brokenBy = 0;




    #endregion Attributes

    //----------------------------------------------------------------------------------------------
    // Monobehaviour

    #region MonoBehaviour


    void Start()
    {
        this.myInteractable = this.gameObject.GetComponent<SenseGlove_Interactable>();
        if (myInteractable == null && this.mustBeGrabbed)
        {
            this.mustBeGrabbed = false; //we cannot require this material to be grabbed if it's not an interactable.
        }

        this.CollectMeshData();
    }

    void FixedUpdate()
    {
        this.UpdateMesh();
    }

    void LateUpdate()
    {

    }


    #endregion MonoBehaviour

    //----------------------------------------------------------------------------------------------
    // Force Feedback

    #region ForceFeedback

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
              //  Debug.Log("Disp:\t" + displacement + ",\t i:\t"+fingerIndex);
                if (!this.mustBeGrabbed || (this.mustBeGrabbed && this.myInteractable.IsInteracting()))
                {
                   // Debug.Log("mustBeGrabbed = " + this.mustBeGrabbed + ", isInteracting: " + this.myInteractable.IsInteracting());

                    if (fingerIndex >= 0 && fingerIndex < 5)
                    {
                        bool shouldBreak = displacement >= this.yieldDistance; 
                        if (shouldBreak && !this.raisedBreak[fingerIndex])
                        { this.brokenBy++; }
                        else if (!shouldBreak && this.raisedBreak[fingerIndex])
                        { this.brokenBy--; }
                        this.raisedBreak[fingerIndex] = shouldBreak;
                        
                       // Debug.Log(displacement + " --> raisedBreak[" + fingerIndex + "] = " + this.raisedBreak[fingerIndex]+" --> "+this.brokenBy);
                        if (this.brokenBy >= this.minimumFingers && ( !this.requiresThumb || (this.requiresThumb && this.raisedBreak[0]) ))
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
        return (int) SenseGloveCs.Values.Wrap(SenseGlove_Material.CalculateResponseForce(displacement, this.maxForce, this.maxForceDist), 0,  this.maxForce);
    }

    /// <summary> Calculate the haptic pulse based on material properties. </summary>
    /// <returns></returns>
    public float CalculateHaptics()
    {
        if (this.hapticFeedback)
        {
            if (this.maxForceDist > 0)
            {
                return SenseGloveCs.Values.Wrap(SenseGloveCs.Values.Interpolate(this.maxForce, 0, 255, 0, 255), 0, 255); //Validate this motor level.
            }
            else
            {
                return 255; //maximum buzz magnitude
            }
        }
        return 0;
    }
    

    public delegate void MaterialBreaksEventHandler(object source, System.EventArgs args);
    /// <summary> Fires when this Grabable is released. </summary>
    public event MaterialBreaksEventHandler MaterialBreaks;

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


    //----------------------------------------------------------------------------------------------
    // Default Material Properties - Used when no SenseGlove_Material Data is present on an Interactable

    #region basicProperties

    /// <summary> The passive force used when Simple ForceFeedback option is chosen but no SenseGlove_Matrial script can be found. </summary>
    public static int defaultPassiveForce = 255;

    public static int d_MaxForce = 255;

    public static float d_MaxForceDist = 0;

    /// <summary> The default buzz motor strength </summary>
    public static float d_BuzzMagn = 0; //off by default 

    /// <summary> The force used when the Material-Based ForceFeedback option is chosen but no SenseGlove_Matrial script can be found. </summary>
    /// /// <param name="displacement"></param>
    /// <returns></returns>
    public static int CalculateDefault(float displacement)
    {
        return SenseGlove_Material.CalculateResponseForce(displacement, SenseGlove_Material.d_MaxForce, SenseGlove_Material.d_MaxForceDist);
    }

    #endregion basicProperties

    //----------------------------------------------------------------------------------------------
    // Material-Based Maths

    /// <summary>
    /// The actual method to calculate things, used by both default and custom materials.
    /// </summary>
    /// <returns></returns>
    public static int CalculateResponseForce(float disp, int maxForce, float maxForceDist)
    {
        if (maxForceDist > 0)
        {
            return (int) SenseGloveCs.Values.Wrap( disp * (maxForce / maxForceDist), 0, 255);
        }
        else if (disp > 0)
        {
            return maxForce;
        }
        
        return 0;
    }


    #endregion ForceFeedback

    //----------------------------------------------------------------------------------------------
    // Mesh Deformation

    #region MeshDeform

    /// <summary> Queue of entryVectors, to be applied during fixedUpdate </summary>
    //private List<Vector3> d_entryVectors = new List<Vector3>();

    /// <summary> Queue of entryPoints, to be applies during fixedUpdate </summary>
    //private List<Vector3> d_entryPoints = new List<Vector3>();

    /// <summary> List of deformation distance(s) </summary>
   // private List<float> d_distances = new List<float>();

    
    private List<Deformation> deformationQueue = new List<Deformation>();


    /// <summary> Collect the Mesh Data for processing </summary>
    /// <remarks>Placed in a separate function so one can re-collect data on the fly.</remarks>
    public void CollectMeshData()
    {
        if (this.meshFilter != null)
        {
            this.myMesh = this.meshFilter.mesh;
            if (myMesh != null)
            {
                this.verts = myMesh.vertices;
                this.deformVerts = myMesh.vertices;

                List<int>[] samePoints = new List<int>[verts.Length];
                //List<int> distinctPoints = new List<int>();

                int uniquePoints = 0;

                for (int i = 0; i < this.verts.Length; i++)
                {
                    this.deformVerts[i] = this.verts[i];
                    samePoints[i] = new List<int>();
                    for (int j = 0; j < this.verts.Length; j++)
                    {
                        if (i != j && verts[i].Equals(verts[j]))
                        {
                            //Debug.Log("Vertex " + i + " is the same as Vertex " + j);
                            samePoints[i].Add(j);
                        }
                    }

                    bool alreadyCounted = false;
                    for (int s = 0; s < samePoints[i].Count; s++)
                    {
                        if (samePoints[i][s] < i) //if one of the same vertice index is smaller, we have already counted it. 
                        {
                            alreadyCounted = true;
                        }
                    }
                    if (!alreadyCounted)
                    {
                        uniquePoints++;
                    }

                }

                this.uniqueVertices = new int[uniquePoints];
                this.sameVertices = new int[uniquePoints][];

                int n = 0;
                for (int i = 0; i < this.verts.Length; i++)
                {
                    bool alreadyCounted = false;
                    for (int s = 0; s < samePoints[i].Count; s++)
                    {
                        if (samePoints[i][s] < i) //if one of the same vertice index is smaller, we have already counted it. 
                        {
                            alreadyCounted = true;
                        }
                    }
                    if (!alreadyCounted)
                    {
                        this.uniqueVertices[n] = i;
                        this.sameVertices[n] = samePoints[i].ToArray();
                        n++;
                    }
                }

                //Debug.Log("Found a mesh with " + this.verts.Length + " vertices; " + uniquePoints + " of which are unique, and " + this.myMesh.triangles.Length / 3 + " triangles.");
            }
        }
    }



    /// <summary> Check if one Vertex equals another </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public bool SameVertex(Vector3 v1, Vector3 v2)
    {
        return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
    }

    /// <summary> Add a deformation to calculate at the end of the fixedUpdate function. </summary>
    /// /// <param name="absEntryVector"></param>
    /// <param name="absDeformPoint"></param>
    public void AddDeformation(Vector3 absEntryVector, Vector3 absDeformPoint, float dist)
    {
        if (this.deforms)
        {
            Vector3 N = this.transform.InverseTransformDirection(absEntryVector);

            for (int i = 0; i < this.deformationQueue.Count;)
            {
                if (N.Equals(this.transform.InverseTransformDirection(deformationQueue[i].absEntryVector)))
                {
                    if (dist < this.deformationQueue[i].distance)
                    {
                        return;
                    }
                    else
                    {
                        RemoveDeform(i);
                    }
                }
                else
                {
                    i++;
                }
            }

            //if we're here, the distance is greater than the current version

           // this.ClearDeformations();

            this.AddDeform(absEntryVector, absDeformPoint, dist);

            this.atRest = this.deformationQueue.Count <= 0; //only if no new deformations were added is this mesh at rest.
            //    Debug.Log(this.atRest);
        }
    }


    /// <summary> Add a single Deformation to the queue </summary>
    /// <param name="absEntryVector"></param>
    /// <param name="absDeformPoint"></param>
    /// <param name="dist"></param>
    private void AddDeform(Vector3 absEntryVector, Vector3 absDeformPoint, float dist)
    {
        //ensure that the deformPoint is not max dist away from the entryvector.

        this.deformationQueue.Add(new Deformation(absEntryVector, absDeformPoint, dist));

    }

    /// <summary> Remove a deformation from the queue </summary>
    /// <param name="index"></param>
    private void RemoveDeform(int index)
    {
        if (index >= 0 && index < this.deformationQueue.Count)
        {
            //this.d_distances.RemoveAt(index);
            //this.d_entryPoints.RemoveAt(index);
            //this.d_entryVectors.RemoveAt(index);
            this.deformationQueue.RemoveAt(index);
        }
    }

    /// <summary> Clear the list of deforms after everything;s been applied. </summary>
    private void ClearDeformations()
    {
        //this.d_distances.Clear();
        //this.d_entryPoints.Clear();
        //this.d_entryVectors.Clear();
        this.deformationQueue.Clear();
    }

    /// <summary> Reset all (unique) vertices. </summary>
    /// <param name="resetAll">Set to true to reset all points, set to false to reset only the uniqueVertices (saves time)</param>
    private void ResetPoints(bool resetAll)
    {
        if (resetAll)
        {
            for (int i=0; i<this.deformVerts.Length; i++)
            {
                this.deformVerts[i] = this.verts[i];
            }
        }
        else //reset unique vertices only
        {
            for (int i = 0; i < this.uniqueVertices.Length; i++)
            {
                Vector3 originalPoint = this.verts[this.uniqueVertices[i]];
                this.deformVerts[this.uniqueVertices[i]] = originalPoint;
            }
        }
    }


    /// <summary> Actually deform the mesh </summary>
    /// <param name="absEntryVector"></param>
    /// <param name="absDeformPoint"></param>
    public void DeformMesh(Vector3 absEntryVector, Vector3 absDeformPoint)
    {
        //Debug.Log("DeformMesh");
        if (this.deforms)
        {
            if (displaceType == DisplaceType.Plane)
            {
                Vector3 localNormal = this.transform.InverseTransformDirection(absEntryVector.normalized);
                Vector3 localPoint = this.transform.InverseTransformPoint(absDeformPoint);
                
                // Debug.Log("Checking the deform at " + SenseGlove_Util.ToString(localPoint) + " in the direction of " + SenseGlove_Util.ToString(localNormal));

                int def = 0; //debug variable
                int max = 0; //debug variable

                for (int i = 0; i < this.uniqueVertices.Length; i++)
                {
                    Vector3 vert = this.deformVerts[this.uniqueVertices[i]];
                    Vector3 V = (vert - localPoint);
                    float dot = Vector3.Dot(localNormal, V);
                    bool abovePlane = dot > 0;

                    if (abovePlane)
                    {   //its above the normal D:

                        //Project the Vector onto the plane with normal and point.
                        Vector3 d = Vector3.Project(V, localNormal);
                        Vector3 projectedPoint = vert - d;

                        if ( this.transform.TransformVector(projectedPoint - this.verts[this.uniqueVertices[i]]).magnitude > this.maxDisplacement) //limit to max displacement
                        {
                            max++;
                            projectedPoint = vert - this.transform.InverseTransformVector(absEntryVector.normalized * this.maxDisplacement); 
                        }

                        this.UpdatePoint(i, projectedPoint);
                        def++;
                        def += this.sameVertices[i].Length;
                    }
                    else
                    {
                        //TODO: It's no longer being pushed, so move back
                    }
                }
                //Debug.Log("Deformed " + def + " vertices, " + max + " of which have reaced maximum displacement,");
                this.atRest = false;
            }
        }
    }

    /// <summary> Update a vertex in the uniqueVertices array, and its associated sameVertices. </summary>
    /// <param name="i"></param>
    /// <param name="newPos"></param>
    private void UpdatePoint(int uniqueVertIndex, Vector3 newPos)
    {
        this.deformVerts[this.uniqueVertices[uniqueVertIndex]] = newPos;
        for (int i = 0; i<this.sameVertices[uniqueVertIndex].Length; i++)
        {
            this.deformVerts[this.sameVertices[uniqueVertIndex][i]] = newPos;
        }
    }


    /// <summary> Apply all deformation in the Queue </summary>
    private void UpdateMesh()
    {
        if (this.deforms)
        {
            if (this.myMesh && !this.atRest)
            {
                this.ResetPoints(false); //reset only the unique vertices

                //   Debug.Log("Applying " + this.vectors.Count + " deformations");
                for (int i=0; i<this.deformationQueue.Count; i++)
                {
                    this.DeformMesh(this.deformationQueue[i].absEntryVector, this.deformationQueue[i].absDeformPosition);
                }
                this.ClearDeformations(); //empties the deformation queue only.

                //Debug.Log("UpdateMesh()");
                myMesh.vertices = deformVerts;
                myMesh.RecalculateBounds();
                myMesh.RecalculateNormals();

            }
        }
        else if (!this.atRest)
        {   //we were deforming before, reset to no longer deform.
            
            this.ResetMesh();
        }
    }

    /// <summary> Reset the points in the mesh to their original vertices. </summary>
    public void ResetMesh()
    {
        //Debug.Log("ResetMesh()");
        if (this.deforms && myMesh != null)
        {
            this.ResetPoints(true);

            myMesh.vertices = deformVerts;
            myMesh.RecalculateBounds();
            myMesh.RecalculateNormals();
        }
        this.atRest = true;
    }

    #endregion MeshDeform

}

public enum DisplaceType
{
    Plane = 0
}

/// <summary> Contains all variables needed to perform Deformations, and to evaluate two deformations. </summary>
public struct Deformation
{
    /// <summary> The absolute entry vector of the Deformation </summary>
    public Vector3 absEntryVector;

    /// <summary> The (current) absulute position of the deformation. </summary>
    public Vector3 absDeformPosition;

    /// <summary> How far the abdDeformPosition is from the  </summary>
    public float distance;

    /// <summary> Create a new Deformation data package. </summary>
    /// <param name="absEntryVect"></param>
    /// <param name="absPosition"></param>
    /// <param name="dist"></param>
    public Deformation(Vector3 absEntryVect, Vector3 absDefPosition, float dist)
    {
        this.absEntryVector = absEntryVect;
        this.absDeformPosition = absDefPosition;
        this.distance = dist;
    }

}
