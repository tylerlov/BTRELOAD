using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public abstract partial class FOptimizer_Base
    {
        protected int isSelected = -1;
        protected int isResizing = -1;

        public void Gizmos_IsResizingLOD(int lod) { isResizing = lod; }
        public void Gizmos_StopChanging() { isResizing = -1; }
        public void Gizmos_SelectLOD(int lod) { isSelected = lod; }


        protected virtual void OnDrawGizmos()
        {
            if (!FOptimizers_Manager.DrawGizmos) return;
            if (GizmosAlpha <= 0f) return;
            Gizmos.DrawIcon(transform.position, "FIMSpace/FOptimizing/Optimizers Gizmo Icon.png", true);
        }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (gameObject.activeInHierarchy == false) return;
            if (LODPercent == null) return; // If is initialized to work with editor
            if (GizmosAlpha <= 0f) return;

            Color preCol = Gizmos.color;
            Vector3 centerPos = transform.position + transform.TransformVector(DetectionOffset);

            if (Application.isPlaying)
            {
                if (visibilitySpheres != null)
                    if (visibilitySpheres.Length > 0)
                        centerPos = GetReferencePosition();
            }

            if (drawDetectionSphere)
            {
                if (OptimizingMethod == FEOptimizingMethod.Static || OptimizingMethod == FEOptimizingMethod.Effective)
                {
                    float radius = DetectionRadius * transform.lossyScale.x;

                    if (visibilitySpheres != null)
                        if (visibilitySpheres.Length == 1)
                        {
                            centerPos = mainVisibilitySphere.position;
                            radius = mainVisibilitySphere.radius;
                        }

                    if (radius > 0f)
                        if (CullIfNotSee)
                        {
                            Gizmos.color = new Color(0.85f, 0.85f, 0.85f, 0.7f * GizmosAlpha);
                            Gizmos.DrawWireSphere(centerPos, radius);
                            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.4f * GizmosAlpha);
                            Gizmos.DrawSphere(centerPos, radius);

                            Vector3 infoDir = new Vector3(1f, 1f, 0f).normalized;

                            if (!Application.isPlaying)
                                UnityEditor.Handles.Label(centerPos + transform.TransformDirection(infoDir * DetectionRadius), new GUIContent("[i]", "If this white sphere is not seen by camera view then object will be culled/hidden"));
                        }
                }
            }

            if (OptimizingMethod == FEOptimizingMethod.Dynamic)
                if (CullIfNotSee)
                {
                    Vector3 bounds = Vector3.Scale(DetectionBounds, transform.lossyScale);
                    Gizmos.color = new Color(0.85f, 0.85f, 0.85f, 0.7f * GizmosAlpha);
                    Gizmos.DrawWireCube(GetReferencePosition(), bounds);
                    Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.4f * GizmosAlpha);
                    Gizmos.DrawCube(GetReferencePosition(), bounds);
                }

            DrawLODRangeSpheres(centerPos, GetAddRadius());
            Gizmos.color = preCol;
        }

        protected void DrawLODRangeSpheres(Vector3 centerPos, float addRadius = 0f)
        {
            Color preCol = Gizmos.color;

            if (isResizing == -1)
            {
                if (isSelected == LODLevels)
                {
                    float radius = MinMaxDistance.y + addRadius;
                    Color lodColor = culledLODColor;

                    Gizmos.color = lodColor * new Color(1.35f, 1f, 1f, 1.1f * GizmosAlpha);
                    Gizmos.DrawWireSphere(centerPos, radius);
                    Gizmos.color = lodColor * new Color(1f, 1f, 1f, 0.88f * GizmosAlpha);

                    float stepAngle = 360f / 24f;
                    for (int x = 0; x < 24; x++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, stepAngle * x, 0f) * Vector3.forward;
                        Gizmos.DrawRay(centerPos + dir * (radius), dir * (MinMaxDistance.y));
                    }

                    Gizmos.color = lodColor * new Color(1f, 1f, 1f, 0.67f * GizmosAlpha);
                    for (int x = 0; x < 24; x++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, stepAngle * x, 0f) * Vector3.forward;
                        Gizmos.DrawRay(centerPos + dir * (radius) + dir * (MinMaxDistance.y), dir * MinMaxDistance.y * 3f);
                    }


                }
                else
                    for (int i = 0; i < LODLevels; i++)
                    {
                        if (isSelected >= 0) if (i > isSelected + 1 && i != LODLevels) continue;

                        Color lodColor = lODColors[i];
                        lodColor.a = i == isSelected ? 0.9f * GizmosAlpha : 0.15f * GizmosAlpha;
                        Gizmos.color = lodColor;

                        if (i >= LODPercent.Count) continue;
                        float radius = Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[i]) + addRadius;

                        Gizmos.DrawWireSphere(centerPos, radius);

                        if (isSelected == i) Gizmos.DrawWireSphere(centerPos, radius*1.0025f);

                        if (i == isSelected)
                        {
                            Gizmos.color = lodColor * new Color(1.25f, 1.25f, 1.25f, 0.43f);
                            Gizmos.DrawSphere(centerPos, radius);

                            Gizmos.color = lodColor * new Color(1.25f, 1.25f, 1.25f, 0.24f);
                            float stepAngle = 360f / 12f;
                            float inRadius = radius;
                            if (i != 0) inRadius = radius - Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[i - 1]) - addRadius;
                            for (int x = 0; x < 12; x++)
                            {
                                Vector3 dir = Quaternion.Euler(0f, stepAngle * x, 0f) * Vector3.forward;
                                Gizmos.DrawRay(centerPos + dir * (radius), -dir * inRadius);
                            }

                            Gizmos.DrawRay(centerPos + Vector3.up * (radius), Vector3.down * inRadius);
                            Gizmos.DrawRay(centerPos + Vector3.down * (radius), Vector3.up * inRadius);
                        }
                    }
            }
            else // when resizing sphere
            {
                Color lodColor = lODColors[isResizing];
                lodColor.a = 0.9f * GizmosAlpha;
                Gizmos.color = lodColor;

                float radius = Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[isResizing]) + addRadius;
                Gizmos.DrawWireSphere(centerPos, radius);
                Gizmos.color = lodColor * new Color(1f, 1f, 1f, 0.2f);
                Gizmos.DrawSphere(centerPos, radius);

                // Drawing lines to help visualize LOD area range
                float inRadius = radius;
                float stepAngle = 360f / 12f;
                Gizmos.color = lodColor * new Color(1f, 1f, 1f, 0.24f);

                if (isResizing != 0) inRadius = radius - Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[isResizing - 1]) - addRadius;

                for (int x = 0; x < 12; x++)
                {
                    Vector3 dir = Quaternion.Euler(0f, stepAngle * x, 0f) * Vector3.forward;
                    Gizmos.DrawRay(centerPos + dir * (radius), -dir * inRadius);
                }

                Gizmos.DrawRay(centerPos + Vector3.up * (radius), Vector3.down * inRadius);
                Gizmos.DrawRay(centerPos + Vector3.down * (radius), Vector3.up * inRadius);

                // Drawing other LOD spheres as range guides
                for (int i = isResizing - 1; i <= isResizing + 1; i++)
                {
                    if (i < 0) continue;
                    if (isResizing == i) continue;
                    if (i > LODLevels) break;

                    lodColor = lODColors[i];
                    lodColor.a = 0.05f * GizmosAlpha;
                    Gizmos.color = lodColor;

                    radius = Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[i]) + addRadius;
                    Gizmos.DrawWireSphere(centerPos, radius);
                }
            }

            Gizmos.color = culledLODColor * new Color(1f, 1f, 1f, GizmosAlpha);
            Gizmos.DrawWireSphere(centerPos, MinMaxDistance.y + addRadius + 1f);

            Vector3 infoDir = new Vector3(1f, 1f, 0f).normalized;

            if (!Application.isPlaying)
                UnityEditor.Handles.Label(centerPos + transform.TransformDirection(infoDir * MinMaxDistance.y), new GUIContent("[i]", "This spheres in different colors indicates distance levels from the optimized object for LODs"));

            Gizmos.color = preCol;
        }
#endif
    }
}
