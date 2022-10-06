using UnityEngine;

namespace SG.Examples
{
    /// <summary> Selects one of two SG_TrackedHands based on which hand is connected first. </summary>
    public class SGEx_SelectHandModel : MonoBehaviour
    {
        public SG.Util.SGEvent ActiveHandConnect = new Util.SGEvent();
        public SG.Util.SGEvent ActiveHandDisconnect = new Util.SGEvent();

        [Header("Left Hand Components")]
        public SG_TrackedHand leftHand;
        public SG_HapticGlove leftGlove;


        [Header("Right Hand Components")]
        public SG_TrackedHand rightHand;
        public SG_HapticGlove rightGlove;




        public SG_TrackedHand ActiveHand
        {
            get; private set;
        }

        public bool Connected
        {
            get { return this.ActiveHand != null; }
        }

        public SG_HapticGlove ActiveGlove
        {
            get
            {
                if (this.ActiveHand != null && ActiveHand.RealHandSource != null && ActiveHand.RealHandSource is SG.SG_HapticGlove)
                {
                    return (SG.SG_HapticGlove) this.ActiveHand.RealHandSource;
                }
                return null;
            }
        }



        void Start()
        {
            //leftGlove.connectsTo = HandSide.LeftHand;
            ////leftHand.SetTrackingProvider(leftGlove);
            //throw new System.NotImplementedException();
            leftHand.HandModelEnabled = false;

            //rightGlove.connectsTo = HandSide.RightHand;
            ////rightHand.SetTrackingProvider(rightGlove);
            //throw new System.NotImplementedException();
            rightHand.HandModelEnabled = false;
        }

        void Update()
        {
            if (this.ActiveHand == null)
            {
                if (this.rightHand.IsConnected())
                {
                    this.rightHand.HandModelEnabled = true;
                    this.leftHand.gameObject.SetActive(false);
                    Debug.Log("Connected to a right hand!");
                    ActiveHand = this.rightHand;
                    ActiveHandConnect.Invoke();
                }
                else if (this.leftHand.IsConnected())
                {
                    this.leftHand.HandModelEnabled = true;
                    this.rightHand.gameObject.SetActive(false);
                    Debug.Log("Connected to a left hand!");
                    ActiveHand = this.leftHand;
                    ActiveHandConnect.Invoke();
                }
            }
            else
            {
                if (ActiveHand.RealHandSource == null || !ActiveHand.RealHandSource.IsConnected())
                {
                    //Disconnection
                    Debug.Log(ActiveHand.name + " disconnected!");
                    this.rightHand.HandModelEnabled = false;
                    this.rightHand.gameObject.SetActive(true);
                    this.leftHand.HandModelEnabled = false;
                    this.leftHand.gameObject.SetActive(true);
                    ActiveHandDisconnect.Invoke();
                }
            }
        }


    }

}