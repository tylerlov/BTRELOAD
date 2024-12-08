using UnityEngine;
using UnityEngine.Rendering;
using OccaSoftware.LSPP.Runtime;

namespace OccaSoftware.LSPP.Demo
{
    [AddComponentMenu("OccaSoftware/LSPP/Change Light Scattering Color")]
    public class ChangeLightScatteringColor : MonoBehaviour
    {
        [SerializeField]
        private Volume volume;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (volume.profile.TryGet(out LightScatteringPostProcess temp))
                {
                    temp.tint.overrideState = true;
                    temp.tint.value = Random.ColorHSV(0f, 1f);
                }
            }
        }
    }
}
