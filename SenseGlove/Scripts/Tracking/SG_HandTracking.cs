﻿using SG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class to interface with SenseGlove Haptic Gloves. If differs from "HandTracking" because that one accepts all kinds of device inputs.
 * Consider this the layer 'before' we convert SenseGlove-Specific data into generic Hand Tracking Data.
 */

namespace SG
{

    /// <summary> Contains all classes related to SenseGlove Hand Tracking. Haptic Gloves only. There will be a different class to get generic hand tracking. </summary>
    public static class SG_HandTracking
    {
        //---------------------------------------------------------------------------------------------------------------------------------
        // Full Hand Tracking (leverages on the components below).

        public static bool HandTrackingSwapped
        {
            get;
            set;
        }

        public static bool GetSGHandPose(bool rightHand, out SG_HandPose handPose)
        {
            //Always make sure we've initialize
            SG_Core.Setup(); //ensures this is set up
            if (SGCore.HandLayer.GetHandPose(rightHand, out SGCore.HandPose iHandPose))
            {
                handPose = new SG_HandPose(iHandPose);
                if (HandTrackingSwapped)
                {
                    rightHand = !rightHand; //so we grab the wrist location of the 'other one'
                }
                GetWristLocation(rightHand, out handPose.wristPosition, out handPose.wristRotation);
                return true;
            }
            handPose = null;
            return false;
        }



        //---------------------------------------------------------------------------------------------------------------------------------
        // 6 DoF Spatial Positioning of Haptic Gloves (required input from various XR Plugins)

        /// <summary> Will output any enum except for AutoDetect, since it passes that one to SG_XR_Devices. </summary>
        public static TrackingHardware CurrentTrackingOffsets
        {
            get
            {
                //Grab these off the SG_XR_Devices if AutoDetect. Otherwise off Settings.
                //Also; if a config file exists, maybe grab that one too? But only on Windows...
                TrackingHardware res = SG_Core.Settings.GlobalWristTrackingOffsets;
                if (res == TrackingHardware.AutoDetect)
                {
                    res = SG_XR_Devices.GetDeterminedTrackingHardware();
                }
                return res;
            }
        }




        /// <summary> Retrieve the Controller / Tracker Location as determine by the SenseGlove Settings and XR Setup. </summary>
        /// <param name="rightHand"></param>
        /// <param name="trackerPosition"></param>
        /// <param name="trackerRotation"></param>
        /// <returns></returns>
        public static bool GetTrackingDeviceLocation(bool rightHand, out Vector3 trackerPosition, out Quaternion trackerRotation)
        {
            GlobalWristTracking tracking = SG_Core.Settings.WristTrackingMethod;
            if (tracking == GlobalWristTracking.UnityXR)
            {
                Transform root = SG_XR_SceneTrackingLinks.SceneXRRig;
                return SG_XR_Devices.GetTrackingDeviceLocation(rightHand, root, out trackerPosition, out trackerRotation);
            }
            else if (tracking == GlobalWristTracking.UseGameObject)
            {
                Transform obj = SG_XR_SceneTrackingLinks.GetTrackingObj(rightHand);
                if (obj != null)
                {
                    trackerPosition = obj.position;
                    trackerRotation = obj.rotation;
                    return true;
                }
            }
            trackerPosition = Vector3.zero;
            trackerRotation = Quaternion.identity;
            return false;
        }

        public static bool GetGloveLocation(bool rightHand, out Vector3 glovePosition, out Quaternion gloveRotation)
        {
            Vector3 trackerPos; Quaternion trackerRot;
            if (GetTrackingDeviceLocation(rightHand, out trackerPos, out trackerRot))
            {
                TrackingHardware offsets = CurrentTrackingOffsets; //this one should not return "AutoDetect ever - since it passes the call to SG_XR_Devices"
                if (offsets == TrackingHardware.AutoDetect)
                {
                    Debug.LogError("Somehow, we're automatically determined Tracking offsets to be... AutoDetected. This is an indication that something went wrong. Check your settings and try again...");
                }
                else if (offsets == TrackingHardware.Unknown)
                {
                    glovePosition = trackerPos;
                    gloveRotation = trackerRot;
                    return true;
                }
                else if (offsets == TrackingHardware.Custom)
                {
                    throw new System.NotImplementedException();
                }
                //TODO: Else if there's something loaded from a config file (win only?)
                else
                {
                    SGCore.PosTrackingHardware iHardware = SG.Util.SG_Conversions.ToInternalTracking(offsets);
                    SGCore.Kinematics.Vect3D iRefPos = SG.Util.SG_Conversions.ToPosition(trackerPos), iGlovePos;
                    SGCore.Kinematics.Quat iRefRot = SG.Util.SG_Conversions.ToQuaternion(trackerRot), iGloveRot;

                    if (SGCore.HandLayer.GetGloveInstance(rightHand, out SGCore.HapticGlove glove))
                    {
                        glove.GetGloveLocation(iRefPos, iRefRot, iHardware, out iGlovePos, out iGloveRot);
                        glovePosition = SG.Util.SG_Conversions.ToUnityPosition(iGlovePos);
                        gloveRotation = SG.Util.SG_Conversions.ToUnityQuaternion(iGloveRot);
                        return true;
                    }
                }
            }
            glovePosition = Vector3.zero;
            gloveRotation = Quaternion.identity;
            return false;
        }

        public static bool GetWristLocation(bool rightHand, out Vector3 wristPosition, out Quaternion wristRotation)
        {
            Vector3 trackerPos; Quaternion trackerRot;
            if (GetTrackingDeviceLocation(rightHand, out trackerPos, out trackerRot))
            {
                TrackingHardware offsets = CurrentTrackingOffsets; //this one should not return "AutoDetect ever - since it passes the call to SG_XR_Devices"
                if (offsets == TrackingHardware.AutoDetect)
                {
                    Debug.LogError("Somehow, we're automatically determined Tracking offsets to be... AutoDetected. This is an indication that something went wrong. Check your settings and try again...");
                }
                else if (offsets == TrackingHardware.Unknown)
                {
                    wristPosition = trackerPos;
                    wristRotation = trackerRot;
                    return true;
                }
                else if (offsets == TrackingHardware.Custom)
                {
                    Vector3 pOffset; Quaternion rOffset;
                    SG_Core.Settings.GetCustomOffsets(rightHand, out pOffset, out rOffset);
                    SG.Util.SG_Util.CalculateTargetLocation(trackerPos, trackerRot, pOffset, rOffset, out wristPosition, out wristRotation);
                    return true;
                }
                //TODO: Else if there's something loaded from a config file (win only?)
                else
                {
                    SGCore.PosTrackingHardware iOffsets = SG.Util.SG_Conversions.ToInternalTracking(offsets);
                    SGCore.Kinematics.Vect3D iRefPos = SG.Util.SG_Conversions.ToPosition(trackerPos), iWristPos;
                    SGCore.Kinematics.Quat iRefRot = SG.Util.SG_Conversions.ToQuaternion(trackerRot), iWristRot;
                    if (SGCore.HandLayer.GetWristLocation(rightHand, iRefPos, iRefRot, iOffsets, out iWristPos, out iWristRot)) //Returns false if there is no glove connected here...
                    {
                        wristPosition = SG.Util.SG_Conversions.ToUnityPosition(iWristPos);
                        wristRotation = SG.Util.SG_Conversions.ToUnityQuaternion(iWristRot);
                        return true;
                    }
                }
            }
            wristPosition = Vector3.zero;
            wristRotation = Quaternion.identity;
            return false;
        }


        //---------------------------------------------------------------------------------------------------------------------------------
        // Finger Tracking of Haptic Gloves (can be done with the Haptic Glove itself - no XR hardware needed)

        /// <summary> Set a specific SenseGlove Hand Model as the (new) default option </summary>
        /// <param name="handModel"></param>
        public static void SetBasicHandModel(SGCore.Kinematics.BasicHandModel handModel)
        {
            SGCore.HandLayer.SetDefaultHandModel(handModel);
        }

    }
}