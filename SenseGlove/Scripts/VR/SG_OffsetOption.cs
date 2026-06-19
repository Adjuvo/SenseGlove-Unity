using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Contains a 3D model, Sphere transfrom and a defined offset. </summary>
    public class SG_OffsetOption : MonoBehaviour
    {
        [SerializeField] private SphereCollider rayCastTarget;

        [SerializeField] private MeshRenderer highlight;

        [SerializeField] private TrackingHardware linkedWristOffset = TrackingHardware.Unknown;

        public TrackingHardware WristOffsets
        { 
            get { return linkedWristOffset; } 
        }

        public bool SameCollider(Collider other)
        {
            return rayCastTarget == other;
        }


        private bool _selected = false;
        public bool Selected
        {
            get { return _selected; }
            set 
            { 
                _selected = value;
                if (highlight != null)
                    highlight.enabled = value;
            }
        }

        private void Awake()
        {
            if (rayCastTarget == null)
                rayCastTarget = this.gameObject.GetComponent<SphereCollider>();
            if (highlight == null)
                highlight = this.gameObject.GetComponent<MeshRenderer>();
            //Selected = _selected;
        }

        
    }
}
