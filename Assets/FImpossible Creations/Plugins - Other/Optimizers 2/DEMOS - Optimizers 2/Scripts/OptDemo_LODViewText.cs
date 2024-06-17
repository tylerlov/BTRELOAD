using UnityEngine;
using UnityEngine.UI;

namespace FIMSpace.FOptimizing
{
    public class OptDemo_LODViewText : MonoBehaviour
    {
        public Optimizer_Base optimizer;
        public Text TextToWriteOn;

        void Update()
        {
            if (!optimizer) return;
            if (!TextToWriteOn) return;

            TextToWriteOn.text = "Current LOD id: " + optimizer.CurrentLODLevel;
            TextToWriteOn.text += "  Distance: " + Mathf.Round(optimizer.GetReferenceDistance());
            if (optimizer.TransitionPercent >= 0)
                TextToWriteOn.text += "\nTransition: " + Mathf.Round(optimizer.TransitionPercent * 100f) + "%";
        }
    }
}