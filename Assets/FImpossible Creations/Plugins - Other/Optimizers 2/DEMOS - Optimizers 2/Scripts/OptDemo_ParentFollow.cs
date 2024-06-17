using System.Collections.Generic;
using UnityEngine;


namespace FIMSpace.FOptimizing
{
    public class OptDemo_ParentFollow : MonoBehaviour
    {
        [Range(1f, 30f)]
        public float followSpeed = 8f;

        private List<Transform> children;
        private List<Vector3> proceduralPos;
        private List<Vector3> parentOffset;

        void Start()
        {
            children = new List<Transform>();
            parentOffset = new List<Vector3>();
            proceduralPos = new List<Vector3>();

            Transform parent = transform;
            for (int i = 0; i < 100; i++)
            {
                if (parent.childCount == 0) break;

                Transform child = parent.GetChild(0);
                children.Add(child);
                parentOffset.Add(child.localPosition);
                proceduralPos.Add(child.position);
                parent = child;
            }
        }

        void Update()
        {
            for (int i = 0; i < children.Count; i++)
            {
                proceduralPos[i] = Vector3.Lerp(proceduralPos[i], children[i].parent.position + children[i].parent.TransformVector(parentOffset[i]), Time.deltaTime * followSpeed);
            }

            for (int i = 0; i < children.Count; i++)
            {
                children[i].position = proceduralPos[i];
            }
        }
    }
}