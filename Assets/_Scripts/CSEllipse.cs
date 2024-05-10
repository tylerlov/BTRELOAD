using UnityEngine;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy.Shapes
{
    [CurvyShapeInfo("2D/Ellipse")]
    [RequireComponent(typeof(CurvySpline))]
    [AddComponentMenu("Curvy/Shapes/Ellipse")]
    public class CSEllipse : CurvyShape2D
    {
        [Positive]
        [SerializeField]
        private float m_RadiusX = 1;
        public float RadiusX
        {
            get { return m_RadiusX; }
            set
            {
                float v = Mathf.Max(0, value);
                if (m_RadiusX != v)
                {
                    m_RadiusX = v;
                    Dirty = true;
                }
            }
        }

        [Positive]
        [SerializeField]
        private float m_RadiusY = 1;
        public float RadiusY
        {
            get { return m_RadiusY; }
            set
            {
                float v = Mathf.Max(0, value);
                if (m_RadiusY != v)
                {
                    m_RadiusY = v;
                    Dirty = true;
                }
            }
        }

        // New Y Offset attribute
        [SerializeField]
        private float m_YOffset = 0;
        public float YOffset
        {
            get { return m_YOffset; }
            set
            {
                if (m_YOffset != value)
                {
                    m_YOffset = value;
                    Dirty = true;
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();
            RadiusX = 1;
            RadiusY = 1;
            YOffset = 0; // Default offset to 0
        }

        protected override void ApplyShape()
        {
            base.ApplyShape();
            PrepareSpline(CurvyInterpolation.Linear, CurvyOrientation.Dynamic, 1, true);

            // Define the number of control points for the ellipse
            int controlPointsCount = 80;
            PrepareControlPoints(controlPointsCount);
            SetCGHardEdges();

            for (int i = 0; i < controlPointsCount; i++)
            {
                float angle = (i / (float)controlPointsCount) * 2 * Mathf.PI;
                float x = Mathf.Cos(angle) * RadiusX;
                float y = Mathf.Sin(angle) * RadiusY + YOffset; // Apply Y offset here
                SetPosition(i, new Vector3(x, y, 0));
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            RadiusX = m_RadiusX;
            RadiusY = m_RadiusY;
            YOffset = m_YOffset; // Ensure the offset is validated
        }
#endif
    }
}