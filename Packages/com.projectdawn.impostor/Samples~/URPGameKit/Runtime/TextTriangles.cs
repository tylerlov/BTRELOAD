using Unity.Profiling;
using UnityEngine;

namespace ProjectDawn.Impostor.Samples.GameKit
{
    [ExecuteInEditMode]
    public class TextTriangles : MonoBehaviour
    {
        public int FontSize = 24;
        public bool IncludeSkyBoxAndGroundTriangles = false;

        ProfilerRecorder m_Triangles;

        private void OnEnable()
        {
            m_Triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void OnDisable()
        {
            m_Triangles.Dispose();
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = FontSize;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            //style.alignment = TextAnchor.MiddleCenter;
            //style.shadowColor = Color.black;
            //style.shadowOffset = new Vector2(2, -2);

            int triangleCount = (int)m_Triangles.LastValue;

            // Here we substract the triangle count of skybox and grid plane
            // Number here is hard coded to avoid doing complicated operations to find out it 2554
            if (!IncludeSkyBoxAndGroundTriangles)
                triangleCount = Mathf.Max(triangleCount - 2554, 0);

            string text = $"{triangleCount:n0} tris";
            GUI.Label(new Rect(10, 10, Screen.width, 100), text, style);
        }
    }
}
