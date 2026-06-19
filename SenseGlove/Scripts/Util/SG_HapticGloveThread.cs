using SGCore;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SG
{
    /// <summary> Fires when glove connection(s) are done. </summary>
    [System.Serializable] public class SG_GloveConnectionEvent : UnityEngine.Events.UnityEvent<SGCore.HapticGlove, SGCore.HapticGlove> { }

    [System.Serializable] public class SG_NewPoseEvent : UnityEngine.Events.UnityEvent<SGCore.HandPose, SGCore.HandPose> { }

    /// <summary> Responisble for running some SenseGlove related process on Desktop to try and offload the work done in the render thread for up to two HapticGloves. </summary>
    public class SG_HapticGloveThread
    {
        private Thread workerThread;
        private volatile bool running;

        private int updateDelay;

        private bool leftConnected = false;
        private bool rightConnected = false;

        private SGCore.HapticGlove leftGlove = null, rightGlove = null;
        private SGCore.HandPose leftPose = null, rightPose = null;

        private int connectionTimer = 0;
        private int checkConnectionInterval = 1000;

        public SG_GloveConnectionEvent ConnectionsUpdated = new SG_GloveConnectionEvent();
        public SG_NewPoseEvent HandPosesUpdated = new SG_NewPoseEvent();

        //can't use System.Threading.Lock gloveInstanceLock = new(); since that doesn't exist in Unity 2022
        //private readonly object gloveInstanceLock = new object(); 
        //private readonly object leftDataLock = new object(), rightDataLock = new object(); 

        private static readonly object leftHapticLock = new object(), rightHapticLock = new object();
        private static readonly object leftGloveLock = new object(), rightGloveLock = new object();

        public SG_HapticGloveThread(int updateDelay_ms = 10) //sleep time to 10 ms = 100Hz update rate. TODO: Configurable per platform (Oculus vs Steam?)
        {
            updateDelay = Mathf.Clamp(updateDelay_ms, 1, int.MaxValue);
            leftPose = SGCore.HandPose.DefaultIdle(false);
            rightPose = SGCore.HandPose.DefaultIdle(true);


            rightWFqueue = new SGCore.CustomWaveform[((int)SGCore.HapticLocation.WholeHand) + 1]; //one size bigger since whileHand is also an option technically.
            leftWFqueue = new SGCore.CustomWaveform[((int)SGCore.HapticLocation.WholeHand) + 1]; //one size bigger since whileHand is also an option technically.
        }

        public void StartThread()
        {
            running = true;
            workerThread = new Thread(WorkerLoop);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        public void DisposeThread()
        {
            running = false;
            workerThread?.Join();
        }


        //Methods running in my worker thread

        void UpdateConnections()
        {
            //Debug.Log("Update Connection States");

            bool lWasConnected = leftConnected;
            bool rWasConnected = rightConnected;

            rightConnected = SGCore.HandLayer.GetGloveInstance(true, out SGCore.HapticGlove rGlove);
            leftConnected = SGCore.HandLayer.GetGloveInstance(false, out SGCore.HapticGlove lGlove);

            //lock (gloveInstanceLock) //lock glove instances while they are being assinged.
            //{
            lock (rightGloveLock)
            {
                rightGlove = rightConnected ? rGlove : null;
            }
            lock (leftGloveLock)
            {
                leftGlove = leftConnected ? lGlove : null;
            }
            //}

            //Only invoke them when there's been an update(?)
            if (lWasConnected != leftConnected || rWasConnected != rightConnected)
            {
                Debug.Log("Connection(s)  Updated!");
                ConnectionsUpdated.Invoke(leftGlove, rightGlove);
                //TODO: Something wiht left / right hands explicitly?
            }
        }

        void WorkerLoop()
        {
            //DO: Update connection?
            connectionTimer = 0;
            UpdateConnections();
            while (running)
            {
                // Check / Update Connection States 
                if (connectionTimer >= checkConnectionInterval)
                {
                    connectionTimer = 0;
                    UpdateConnections();
                }

                //Update sensor data
                bool rUpdate = false;
                if (rightConnected && SGCore.HandLayer.GetHandPose(true, out SGCore.HandPose rPose))
                {
                    //lock (rightDataLock)
                    //{
                    rightPose = rPose;
                    //}
                    rUpdate = true;
                }
                bool lUpdate = false;
                if (leftConnected && SGCore.HandLayer.GetHandPose(false, out SGCore.HandPose lPose))
                {
                    //lock (leftDataLock)
                    //{
                    leftPose = lPose;
                    //}
                    lUpdate = true; //TODo: Is there a safe way to invoke this? Or shall I just grab booleans etc?
                }
                if (rUpdate || lUpdate)
                {
                    HandPosesUpdated.Invoke(leftPose, rightPose);
                }
                //Update Haptics
                if (rightGlove != null)
                    rightGlove.SendHaptics();
                if (leftGlove != null)
                    leftGlove.SendHaptics();

                UpdateWaveforms();

                Thread.Sleep(updateDelay);
                connectionTimer += updateDelay;
            }
        }


        private SGCore.CustomWaveform[] leftWFqueue = new SGCore.CustomWaveform[0];
        private SGCore.CustomWaveform[] rightWFqueue = new SGCore.CustomWaveform[0];

        private void UpdateWaveforms()
        {
            lock (leftGloveLock) //can't change / update leftGlove until this passes
            {
                if (leftGlove != null)
                {
                    lock (leftHapticLock) //no changes allowed to the array yet...
                    {
                        for (int i = 0; i < leftWFqueue.Length; i++)
                        {
                            if (leftWFqueue[i] == null)
                                continue;
                            leftGlove.SendCustomWaveform(leftWFqueue[i], (SGCore.HapticLocation)i);
                            leftWFqueue[i] = null;
                        }
                    }
                }
            }
            if (rightConnected)
            {
                lock (rightGloveLock) //can't change / update leftGlove until this passes
                {
                    if (rightGlove != null)
                    {
                        lock (rightHapticLock) //no changes allowed to the array yet...
                        {
                            for (int i = 0; i < rightWFqueue.Length; i++)
                            {
                                if (rightWFqueue[i] == null)
                                    continue;
                                rightGlove.SendCustomWaveform(rightWFqueue[i], (SGCore.HapticLocation)i);
                                rightWFqueue[i] = null;
                            }
                        }
                    }
                }
            }
        }

        //END Methods running in my worker thread



        //--------------------------------------------------------------------------------------------------------
        // External Access

        public bool QueueWaveform(bool rightHand, SGCore.CustomWaveform waveform, HapticLocation location)
        {
            int loc = (int)location;
            if (loc < 0 || loc >= leftWFqueue.Length) //l/r should be of the same (static) size.
                return false;
            if (rightHand)
            {
                lock (rightHapticLock)
                {
                    rightWFqueue[loc] = waveform;
                }
            }
            else
            {
                lock (leftHapticLock)
                {
                    leftWFqueue[loc] = waveform;
                }
            }
            return true;
        }
    }
}