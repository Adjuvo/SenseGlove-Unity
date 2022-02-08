using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    public class SG_PhysicsGrab : SG_GrabScript
    {
        /// <summary> The Hand Palm collider, used when grabbing objects between the palm and finger (tool/handle grips) </summary>
        [Header("Physics Grab Components")]
        public SG_HoverCollider palmTouch;
        /// <summary> Thumb collider, used to determine finger/thumb collision </summary>
        public SG_HoverCollider thumbTouch;
        /// <summary> Index collider, used to determine finger/thumb and finger/palm collision </summary>
        public SG_HoverCollider indexTouch;
        /// <summary> Index collider, used to determine finger/thumb and finger/palm collision </summary>
        public SG_HoverCollider middleTouch;

        /// <summary> Keeps track of the 'grabbing' pose of fingers </summary>
        protected bool[] wantsGrab = new bool[3];
        /// <summary> Above these flexions, the hand is considered 'open' </summary>
        protected static float[] openHandThresholds = new float[5] { 0.1f, 0.2f, 0.2f, 0.2f, 0.2f };
        /// <summary> below these flexions, the hand is considered 'open' </summary>
        protected static float[] closedHandThresholds = new float[5] { 2, 2, 2, 2, 2 }; //set to -360 so it won;t trigger for now

        protected float releaseThreshold = 0.05f;
        protected bool[] grabRelevance = new bool[5];

        /// <summary> All fingers, used to iterate through the fingers only. </summary>
        protected SG_HoverCollider[] fingerScripts = new SG_HoverCollider[0];
        /// <summary> All HoverScripts, easier to iterate trhough </summary>
        protected SG_HoverCollider[] hoverScripts = new SG_HoverCollider[0];

        protected static float overrideGrabThreshold = 0.01f;

        protected float[] lastNormalized = new float[5];
        protected float[] normalizedOnGrab = new float[5];

        protected override void CreateComponents()
        {
            base.CreateComponents();
            fingerScripts = new SG_HoverCollider[3];
            fingerScripts[0] = thumbTouch;
            fingerScripts[1] = indexTouch;
            fingerScripts[2] = middleTouch;

            hoverScripts = new SG_HoverCollider[4];
            hoverScripts[0] = thumbTouch;
            hoverScripts[1] = indexTouch;
            hoverScripts[2] = middleTouch;
            hoverScripts[3] = palmTouch;
        }

        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);
            for (int i = 0; i < this.hoverScripts.Length; i++)
            {
                Util.SG_Util.CollectComponent(hoverScripts[i], renderers);
                Util.SG_Util.CollectGameObject(hoverScripts[i].debugTxt, objects);
            }
        }

        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> res = base.CollectPhysicsColliders();
            for (int i = 0; i < this.hoverScripts.Length; i++)
            {
                SG.Util.SG_Util.GetAllColliders(this.hoverScripts[i].gameObject, ref res);
            }
            return res;
        }

        protected override void LinkToHand(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand(newHand, firstLink);
            //link colliders
            SG_HandPoser3D trackingTargets = newHand.GetPoser(SG_TrackedHand.TrackingLevel.VirtualPose);
            for (int i = 0; i < this.hoverScripts.Length; i++) //link them to wherever they want to be
            {
                trackingTargets.ParentObject(hoverScripts[i].transform, hoverScripts[i].linkMeTo); //Instead of following a frame behind, we're childing.
                hoverScripts[i].updateTime = SG_SimpleTracking.UpdateDuring.Off; //we still need it for hovering(!)
                //Transform target = trackingTargets.GetTransform(hoverScripts[i].linkMeTo);
                //hoverScripts[i].SetTrackingTarget(target, true);
                //hoverScripts[i].updateTime = SG_SimpleTracking.UpdateDuring.Off; //no longer needs to update...
            }
        }


        //----------------------------------------------------------------------------------------------
        // PhysicsGrab Functions


        /// <summary> Returns true if an SG_Interactable is inside a list of other SG_Interactables </summary>
        /// <param name="heldObject"></param>
        /// <param name="objectsToGrab"></param>
        /// <returns></returns>
        public static bool IsInside(SG_Interactable heldObject, List<SG_Interactable> objectsToGrab)
        {
            for (int i = 0; i < objectsToGrab.Count; i++)
            {
                if (GameObject.ReferenceEquals(objectsToGrab[i].gameObject, heldObject.gameObject))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary> Returns all grabables that both fingers are touching </summary>
        /// <param name="finger1"></param>
        /// <param name="finger2"></param>
        /// <returns></returns>
        public SG_Interactable[] GetMatching(int finger1, int finger2)
        {
            return GetMatching(finger1, fingerScripts[finger2]);
        }


        /// <summary> Returns all grabables that both fingers are touchign </summary>
        /// <param name="finger1"></param>
        /// <param name="finger2"></param>
        /// <returns></returns>
        public SG_Interactable[] GetMatching(int finger1, SG_HoverCollider touch)
        {
            if (fingerScripts[finger1] != null && touch != null)
            {
                return fingerScripts[finger1].GetMatchingObjects(touch);
            }
            return new SG_Interactable[] { };
        }


        /// <summary> Returns true if a specific fingers wants to grab on (when not grabbing). </summary>
        /// <param name="finger"></param>
        /// <returns></returns>
        protected bool WantsGrab(int finger)
        {
            return lastNormalized[finger] >= openHandThresholds[finger] && lastNormalized[finger] <= closedHandThresholds[finger];
        }


        /// <summary> Returns a list of all objects that are grabable at this moment. </summary>
        /// <returns></returns>
        public List<SG_Interactable> ObjectsGrabableNow()
        {
            List<SG_Interactable> res = new List<SG_Interactable>();
            // Thumb - Finger only for now.
            if (thumbTouch.HoveredCount() > 0)
            {
                for (int f = 1; f < fingerScripts.Length; f++) //go through each finger -but- the thumb.
                {
                    if (wantsGrab[f]) //this finger wants to grab on to objects
                    {
                        SG_Interactable[] matching = fingerScripts[0].GetMatchingObjects(fingerScripts[f]);
                        // Debug.Log("Found " + matching.Length + " matching objects between " + fingerScripts[0].name + " and " + fingerScripts[f].name);
                        for (int i = 0; i < matching.Length; i++)
                        {
                            SG.Util.SG_Util.SafelyAdd(matching[i], res);
                        }
                    }
                }
            }
            return res;
        }

        /// <summary> Returns a list of fingers that are currently touching a particular interactable </summary>
        /// <returns></returns>
        public bool[] FingersTouching(SG_Interactable obj)
        {
            bool[] res = new bool[5];
            for (int f = 0; f < this.fingerScripts.Length; f++)
            {
                res[f] = this.fingerScripts[f].IsTouching(obj);
            }
            return res;
        }





        public static bool[] GetGrabIntent(float[] normalizedFlex)
        {
            bool[] res = new bool[5];
            for (int f = 0; f < normalizedFlex.Length; f++) //go through each finger -but- the thumb.?
            {
                res[f] = normalizedFlex[f] >= openHandThresholds[f] && normalizedFlex[f] <= closedHandThresholds[f];
            }
            return res;
        }


        public override void UpdateDebugger()
        {
            //Doesn't do anything
        }

        public override void UpdateGrabLogic(float dT)
        {
            base.UpdateGrabLogic(dT);  //updates reference location(s).

            //Update Physics Colliders
            for (int i = 0; i < this.hoverScripts.Length; i++)
            {
                this.hoverScripts[i].UpdateLocation();
            }

            // Re-collect Normalized Flexion
            //We'l try our best to retrieve the last flexions. If it fails, we use the one before it as backup.
            if (this.handPoseProvider != null && this.handPoseProvider.IsConnected())
            {
                float[] currFlex;
                if (this.handPoseProvider.GetNormalizedFlexion(out currFlex)) //can fail because of a parsing error, in which case we must not assign it.
                {
                    this.lastNormalized = currFlex;
                }
            }
            this.wantsGrab = GetGrabIntent(this.lastNormalized); //doing this here so I can evaluate from inspector


            List<SG_Interactable> objToGrab = this.ObjectsGrabableNow();

            if (this.IsGrabbing) //check for release - semi-gesture based
            {
                SG_Interactable heldObj = this.heldObjects[0];
                bool[] currentTouched = this.FingersTouching(heldObj); //the fingers that are currently touching the held object


                //Evaluate Intent - If ever there was any
                if (this.grabRelevance.Length == 0)
                {
                    //first time after snapping. HoverColliders should have had a frame to catch up.
                    bool oneGrabRelevance = false;
                    for (int f=1; f<currentTouched.Length; f++) //start at 1. Because of the bullshit snapping, I don't want to evaluate releasing until at lease a finger touches the object
                    {                                           //this should always be true unless we're snapping.
                        if (currentTouched[f])
                        {
                            oneGrabRelevance = true;
                            break;
                        }
                    }
                    if (oneGrabRelevance) //there is a t least one relevant finger now touching.
                    {
                        this.grabRelevance = currentTouched;
                        this.normalizedOnGrab = Util.SG_Util.ArrayCopy(this.lastNormalized);
                        //Debug.Log("First time since grabbing. \n"
                        //    + SG.Util.SG_Util.PrintArray(grabRelevance)
                        //    + "\n" + SG.Util.SG_Util.ToString(normalizedOnGrab, 2));
                    }
                }

                if (virtualHoverCollider.HoveredCount() == 0 && !heldObj.KinematicChanged) //nothing is being hovered over anymore and it's not becasue of Unity derpiness
                {
                    //Debug.Log("Releasing because we're not hovering over anything anymore");
                    this.ReleaseAll();
                }
                else
                {
                    //We will release if all relevant fingers are either above the "open threshold" OR have are relevant and have extended above 
                        
                    //Step 1: Evaluate Intent - We do this separately because I'm always interested in this.
                    float[] grabDiff = new float[5]; //DEBUG
                    int[] grabCodes = new int[5]; // 0 and up means grab, < zero means release.
                    for (int f=0; f<fingerScripts.Length; f++)
                    {
                        //update Grab Relevance?
                        if (!grabRelevance[f] && currentTouched[f])
                        {
                            grabRelevance[f] = true;
                            normalizedOnGrab[f] = lastNormalized[f]; //store this for later.
                        }
                        grabDiff[f] = this.normalizedOnGrab[f] - this.lastNormalized[f];
                        if (lastNormalized[f] < openHandThresholds[f])
                        {
                            grabCodes[f] = -2; //release because you;re above the max threshold.
                        }
                        else if (this.normalizedOnGrab[f] - this.lastNormalized[f] > releaseThreshold) //i'd normally use latest - ongrab, but then extension is negative and I'd have to invert releaseThreshold. So we subract it the other way around. very tiny optimizations make me happy,
                        {
                            grabCodes[f] = -1;
                        }
                    }
                    //Step 2 - After evaluating finger states, determine grab intent.
                    //This is a separate step so later down the line, we can make a difference between finger-thumb, finger-palm, and thumb-palm grabbing
                    bool wantsGrab = false;
                    for (int f=1; f<this.fingerScripts.Length; f++) //Assuming only thumb-finger and finger-palm (NOT thumb-palm) grasps. So skipping 0 (thumb)
                    {
                        if (grabRelevance[f] && grabCodes[f] > -1) //there's one finger that wants to hold on.
                        {
                            wantsGrab = true;
                            break; //can break here because evaluation is done in a separate loop
                        }
                    }
                    //Step 3 - Compare with override to make a final judgement. Can be optimized by placing this before Step 2 and skipping it entirely while GrabOverride is true.
                    bool overrideGrab = this.handPoseProvider != null && this.handPoseProvider.OverrideGrab() > overrideGrabThreshold; //we start with wanting to release based on overriding.
                    bool shouldRelease = !(wantsGrab || overrideGrab);

                    //Optional: Debug States
                    //if (debugTxt != null && this.debugEnabled)
                    //{
                    //    this.GrabbedText = "Hovering Over " + virtualHoverCollider.HoveredCount()
                    //        + "\nCurr Flex: " + SG.Util.SG_Util.ToString(this.lastNormalized, 2)
                    //        + "\nOnGrab: " + SG.Util.SG_Util.ToString(this.normalizedOnGrab, 2)
                    //        + "\nGrabRelevance: " + SG.Util.SG_Util.PrintArray(this.grabRelevance)
                    //        + "\nFingerCodes: " + SG.Util.SG_Util.ToString(grabCodes)
                    //        + "\nDiffs: " + SG.Util.SG_Util.ToString(grabDiff, 2)
                    //        + "\nWantsGrab: " + wantsGrab + " / Override: " + overrideGrab + " => Release: " + shouldRelease;
                    //}
                    // And actually release
                    if (shouldRelease) //can not longer grab anything
                    {
                        //Debug.Log("Detected no grab intent anymore; " + SG.Util.SG_Util.PrintArray(this.grabRelevance) + ", " + SG.Util.SG_Util.ToString(grabCodes));
                        this.ReleaseAll();
                    }
                }
            }
            else //check for grab
            {
                //TODO; Check for a littlest bit of intent. Some for of flexion. Because now you can still slam your hand into something.
                if (objToGrab.Count > 0)
                {
                    SG_Interactable[] sortedGrabables = SG.Util.SG_Util.SortByProximity(this.ProximitySource.position, objToGrab.ToArray());
                    //attempt to grab each object I can, starting with the closest
                    for (int i = 0; i < sortedGrabables.Length; i++)
                    {
                        TryGrab(sortedGrabables[i]);
                        if (!CanGrabNewObjects) { break; } //stop going through the objects if we can no longer grab one
                    }
                }
                else if (this.handPoseProvider != null && this.handPoseProvider.OverrideGrab() > overrideGrabThreshold)
                {
                    SG_Interactable[] grabablesInHover = this.virtualHoverCollider.GetTouchedObjects(this.ProximitySource);
                    //attempt to grab each object I can, starting with the first
                    for (int i = 0; i < grabablesInHover.Length; i++)
                    {
                        TryGrab(grabablesInHover[i]);
                        if (!CanGrabNewObjects) { break; } //stop going through the objects if we can no longer grab one
                    }
                }
                if (this.IsGrabbing)
                {
                    this.grabRelevance = new bool[0]; //clear this so we re-register it the first frame
                }
                //if (this.debugTxt != null)
                //{
                //    string db = "Hovering over " + this.virtualHoverCollider.HoveredCount() + " objects"
                //        + "\nCan Grab " + objToGrab.Count + " objects"
                //        + "\nCurr Flex: " + SG.Util.SG_Util.ToString(this.lastNormalized, 2)
                //        + "\nOverrdie: " + (this.handPoseProvider != null ? this.handPoseProvider.OverrideGrab().ToString() : "0") + " vs " + overrideGrabThreshold
                //        + "\nWantsGrab: " + Util.SG_Util.PrintArray(this.wantsGrab);
                //    this.debugTxt.text = db;
                //}
            }

            

        }


    }
}