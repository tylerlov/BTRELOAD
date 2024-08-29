using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasManager : MonoBehaviour
    {
        [SerializeField] private List<Canvas> additionalCanvases = new List<Canvas>();
        private List<CanvasScaler> canvasScalers = new List<CanvasScaler>();

        private void Awake()
        {
            // Add the main canvas scaler
            canvasScalers.Add(GetComponent<CanvasScaler>());

            // Add additional canvas scalers
            foreach (var canvas in additionalCanvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    canvasScalers.Add(scaler);
                }
            }
        }

        public void SetScale(int scale = 1080)
        {
            foreach (var scaler in canvasScalers)
            {
                scaler.referenceResolution = new Vector2(scaler.referenceResolution.x, scale);
            }
        }
    }
}