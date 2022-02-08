using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
    /// <summary> Add your Colliders to this layer so as to ignore their collision between SenseGlove Hands and them. </summary>
    public class SG_PhysicsHelper
    {
        /// <summary> HandBones colliders from different HandPysicsColliders, that need to be ignored </summary>
        private static List<SG_HandPhysics> handBones = new List<SG_HandPhysics>();

        /// <summary> Collider arrays that should ignore the hand bones, which move between objetcs and hands.. </summary>
        private static List<Collider[]> collidersToIgnore = new List<Collider[]>();

        /// <summary> Adds PhysicsLayers to the list, which will disable its collision with other colliders </summary>
        /// <param name="physicsLayer"></param>
        public static void AddHandColiders(SG_HandPhysics physicsLayer)
        {
            handBones.Add(physicsLayer);
            //Ignore collision bewteen this hand and the other sets of colliders
            for (int i=0; i<collidersToIgnore.Count; i++)
            {
                physicsLayer.SetIgnoreCollision(collidersToIgnore[i], true);
                //Debug.Log("HC00 Disabled Collision between " + physicsLayer.name + " and " + collidersToIgnore[i].Length + " colliders.");
            }
        }

        /// <summary> Add a new set of colliders to ignore collision with the SG_HandPhysics bones. </summary>
        /// <param name="colliders"></param>
        public static void AddCollidersToIgnore(Collider[] colliders)
        {
            if (colliders.Length > 0)
            {
                collidersToIgnore.Add(colliders);
                //Ignore collision between all hands and this particular collider[]
                for (int i=0; i<handBones.Count; i++)
                {
                    handBones[i].SetIgnoreCollision(colliders, true);
                    //Debug.Log("CH00 Disabled Collision between " + handBones[i].name + " and " + colliders.Length + " colliders.");
                }
            }
        }


    }
}