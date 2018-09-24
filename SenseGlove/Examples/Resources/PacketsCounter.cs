using UnityEngine;

namespace SenseGlove_Examples
{

    public class PacketsCounter : MonoBehaviour
    {
        public SenseGlove_Object senseGlove;
        public TextMesh text;

        // Update is called once per frame
        void Update()
        {
            if (senseGlove != null && senseGlove.GloveReady() && text != null)
            {
                text.text = "Recieving " + senseGlove.GloveData().packetsPerSecond + " packets / second";
            }
        }
    }

}
