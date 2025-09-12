using SGCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SG.Util
{

    public class PairedDeviceInfo
    {
        public string Address { get; set; }

        public string Localname { get; set; }

        public string Connection { get; set; }

        public SGCore.DeviceType DeviceType { get; set; }


        public PairedDeviceInfo(string addr, string name, string connType, SGCore.DeviceType deviceType)
        {
            Address = addr;
            Localname = name;
            Connection = connType;
            DeviceType = deviceType;
        }

        public bool SameDevice(PairedDeviceInfo other)
        {
            return this.Localname.Equals(other.Localname, System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValid()
        {
            return this.Connection.Length > 0 /*&& this.Address.Length > 0*/ && this.Localname.Length > 0;
        }

        public string Serialize()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary> Convert a single PairedDeviceInfo json into a valid class </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static PairedDeviceInfo Deserialize(string json)
        {
            return JsonUtility.FromJson<PairedDeviceInfo>(json);
        }
    }




    public class PairedDevicesList
    {
        private List<PairedDeviceInfo> pairedDevices;


        public PairedDevicesList()
        {
            pairedDevices = new List<PairedDeviceInfo>();
        }

        public PairedDevicesList(List<PairedDeviceInfo> list)
        {
            pairedDevices = list;
        }

        public void ClearList()
        {
            pairedDevices.Clear();
        }

        public int DeviceIndex(PairedDeviceInfo info)
        {
            for (int i = 0; i < pairedDevices.Count; i++)
            {
                if (pairedDevices[i].SameDevice(info))
                {
                    return i;
                }
            }
            return -1;
        }

        public void AddToList(PairedDeviceInfo info, bool updateIfExits = true)
        {
            if (info == null)
                return;

            int index = DeviceIndex(info);
            if (index < 0) //does not yet exist.
                pairedDevices.Add(info);
            else if (updateIfExits)
                pairedDevices[index] = info;
        }


        public void RemoveFromList(PairedDeviceInfo info)
        {
            if (info == null)
                return;

            int index = DeviceIndex(info);
            if (index > -1) //does not yet exist.
                pairedDevices.RemoveAt(index);
        }


        /// <summary> Get a list of devices filered by name (must conain X) and connection type (must equal Y) </summary>
        /// <param name="nameContains"></param>
        /// <param name="connEquals"></param>
        /// <returns></returns>
        public PairedDeviceInfo[] GetDevices(string nameContains, string connEqual, System.StringComparison comparison = System.StringComparison.InvariantCultureIgnoreCase)
        {
            List<PairedDeviceInfo> res = new List<PairedDeviceInfo>();
            for (int i = 0; i < pairedDevices.Count; i++)
            {
                if (pairedDevices[i].Localname.Contains(nameContains, comparison)
                    && pairedDevices[i].Connection.Equals(connEqual, comparison))
                {
                    res.Add(pairedDevices[i]);
                }
            }
            return res.ToArray();
        }

        public PairedDeviceInfo[] GetDevices(string connEqual)
        {
            List<PairedDeviceInfo> res = new List<PairedDeviceInfo>();
            for (int i = 0; i < pairedDevices.Count; i++)
            {
                if (pairedDevices[i].Connection.Equals(connEqual, System.StringComparison.OrdinalIgnoreCase))
                {
                    res.Add(pairedDevices[i]);
                }
            }
            return res.ToArray();
        }

        public int GetValidDeviceCount()
        {
            int res = 0;
            foreach (PairedDeviceInfo inf in pairedDevices)
            {
                if (inf.IsValid())
                    res++;
            }
            return res;
        }


        public string Serialize()
        {
            //return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            Internal_PairList paired = new Internal_PairList();
            Internal_PairDevice[] devices = new Internal_PairDevice[pairedDevices.Count];
            for (int i = 0; i < pairedDevices.Count; i++)
            {
                devices[i] = new Internal_PairDevice();
                devices[i].Address = pairedDevices[i].Address;
                devices[i].Connection = pairedDevices[i].Connection;
                devices[i].DeviceType = (int)pairedDevices[i].DeviceType;
                devices[i].Localname = pairedDevices[i].Localname;
            }
            paired.pairedDevices = devices;
            return JsonUtility.ToJson(paired, true);
        }

        /// <summary> Convert a single PairedDeviceInfo json into a valid class </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static PairedDevicesList Deserialize(string json)
        {
            Internal_PairList list = JsonUtility.FromJson<Internal_PairList>(json);

            List<PairedDeviceInfo> info = new List<PairedDeviceInfo>(list.pairedDevices.Length);
            foreach (Internal_PairDevice device in list.pairedDevices)
            {
                info.Add(new PairedDeviceInfo(device.Address, device.Localname, device.Connection, (SGCore.DeviceType)device.DeviceType));
            }
            PairedDevicesList res = new PairedDevicesList(info);
            return res;
        }
    }


    /// <summary> Interface for the BLE Pairing List between SenseCom <> SGConnect. Only relevant on non-android devices. On Android, this is done via the normal pairing strategy. </summary>
    public static class SG_PairingList
    {
        private const string fileName = "pairedDevices.txt";
        private static PairedDevicesList deviceList = null;


        /// <summary> Placed in a separate function so I can change the directory more easily (e,g, for beta builds I'll just have it next to the exe folder). </summary>
        /// <returns></returns>
        public static string GetPairedDirectory()
        {
            return SGCore.Util.FileIO.GetSenseGloveCacheDirectory();
        }

        public static void TryLoadList(bool forceReload = false)
        {
            string location = System.IO.Path.Combine(GetPairedDirectory(), fileName);
#if UNITY_STANDALONE_LINUX
        location = location.Replace('\\', '/'); //input sanitation because it apparently should be / and not \\. Yet somehow Combine adds a // instead.
#endif

            if (System.IO.File.Exists(location))
            {
                string content = System.IO.File.ReadAllText(location);
                deviceList = PairedDevicesList.Deserialize(content);
                if (deviceList != null)
                    return; //otherwise we'll create a new instace down there.
            }

            //File did not exist so we get here...
            deviceList = new PairedDevicesList();
            //deviceList.AddToList( new PairedDeviceInfo("", "Left Glove", "N\\A", SGCore.DeviceType.NOVA_2_GLOVE) );
            //deviceList.AddToList( new PairedDeviceInfo("", "Right Glove", "N\\A", SGCore.DeviceType.NOVA_2_GLOVE) );
            StoreList();
        }


        public static void StoreList()
        {
            if (deviceList == null)
                return;
            SGCore.Util.FileIO.SaveTxtFile(GetPairedDirectory(), fileName, new string[] { deviceList.Serialize() }, false);
        }


        /// <summary> For the fast(er) implementation: Get the list of Nova 2 BLE connections for Startup. </summary>
        /// <returns></returns>
        public static PairedDeviceInfo[] GetDevices(string nameContains, string connEqual, System.StringComparison comparison = System.StringComparison.InvariantCultureIgnoreCase)
        {
            TryLoadList();
            return deviceList.GetDevices(nameContains, connEqual, comparison);
        }

        public static PairedDeviceInfo[] GetDevices(string connectionEquals)
        {
            TryLoadList();
            return deviceList.GetDevices(connectionEquals);
        }





        public static void AddToPairingList(PairedDeviceInfo device, bool notifySGConnect)
        {
            TryLoadList();
            deviceList.AddToList(device);
            StoreList();
            if (notifySGConnect)
            {
                if (device.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
                    SGCore.SGConnect.RegisterNova2BLEConnection(device.Localname);
                else
                {
                    Debug.LogWarning("Developer note: Implement notifying SGConnect for other Non-Nova2 devices.");
                }
            }
        }

        public static void RemoveFromPairingList(PairedDeviceInfo device, bool notifySGConnect)
        {
            TryLoadList();
            deviceList.RemoveFromList(device);
            StoreList();
            if (notifySGConnect)
            {
                //Debug.LogError("TODO: Implement SGConnect generic parser...");
                if (device.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
                    SGCore.SGConnect.UnregisterNova2BLEConnection(device.Localname);
            }
        }

        public static void ClearPairingList(bool notifySGConnect)
        {
            if (notifySGConnect)
            {
                //Debug.LogError("TODO: Implement SGConnect generic parser...");
                PairedDeviceInfo[] list = deviceList.GetDevices("BLE");
                foreach (PairedDeviceInfo info in list)
                {
                    //unpair with all known devices...

                    if (info.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
                        SGCore.SGConnect.UnregisterNova2BLEConnection(info.Localname);
                }
            }
            deviceList = new PairedDevicesList();
            StoreList();
        }

        public static int GetValidDeviceCount()
        {
            TryLoadList();
            return deviceList.GetValidDeviceCount();
        }

    }




    [System.Serializable]
    public class Internal_PairList
    {
        public Internal_PairDevice[] pairedDevices;
    }


    [System.Serializable]
    public class Internal_PairDevice
    {
        public string Address;
        public string Localname;
        public string Connection;
        public int DeviceType;
    }
}