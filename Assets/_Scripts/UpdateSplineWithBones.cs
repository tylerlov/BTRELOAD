using FluffyUnderware.Curvy;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BoneOffset
{
    public Transform Bone;
    public Vector3 Offset;
    public Transform SplineNode; // This will refer to the associated spline node
}

public class UpdateSplineWithBones : MonoBehaviour
{
    public CurvySpline Spline;
    public SkinnedMeshRenderer SkinnedMesh;
    public BoneOffset[] BoneOffsets;
    public float xOffset;
    public float yOffset;
    public float zOffset;
    public LayerMask groundLayer; // Add a LayerMask for the ground layer
    public bool ConstrainX;
    public bool ConstrainY;
    public bool ConstrainZ;

    private int currentControlPoint = 0; // Add a variable to keep track of the current control point

    void FixedUpdate()
    {
        int pointsPerFrame = Spline.ControlPointCount;

        for (int j = 0; j < pointsPerFrame; j++)
        {
            // Update the spline control points based on the current bone positions
            Transform cp = Spline.transform.GetChild(currentControlPoint);
            Vector3 newPosition = new Vector3(
                BoneOffsets[currentControlPoint].Bone.position.x
                    + xOffset
                    + BoneOffsets[currentControlPoint].Offset.x,
                BoneOffsets[currentControlPoint].Bone.position.y
                    + yOffset
                    + BoneOffsets[currentControlPoint].Offset.y,
                BoneOffsets[currentControlPoint].Bone.position.z
                    + zOffset
                    + BoneOffsets[currentControlPoint].Offset.z
            );

            // Check constraints
            float x = ConstrainX ? cp.position.x : newPosition.x;
            float y = ConstrainY ? cp.position.y : newPosition.y;
            float z = ConstrainZ ? cp.position.z : newPosition.z;

            cp.position = new Vector3(x, y, z);

            // Increment the current control point and reset it if it reaches the end
            currentControlPoint++;
            if (currentControlPoint >= Spline.Count + 1)
            {
                currentControlPoint = 0;
            }
        }

        Spline.Refresh();
    }

    public void GenerateSpline()
    {
        // Get the bones from the SkinnedMeshRenderer
        Transform[] bones = SkinnedMesh.bones;

        // Create a new BoneOffset array with the same length as the bones array
        BoneOffsets = new BoneOffset[bones.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            // Ensure there are enough control points in the spline
            if (i >= Spline.ControlPointCount)
            {
                Spline.Add();
            }

            Transform cp = Spline.transform.GetChild(i);
            cp.position = bones[i].position;

            // Create a new BoneOffset for each bone
            // and assign the spline control point to the SplineNode member
            BoneOffsets[i] = new BoneOffset
            {
                Bone = bones[i],
                Offset = Vector3.zero,
                SplineNode = cp,
            };
        }

        //Spline.Refresh();
    }

    void OnDrawGizmosSelected()
    {
        if (BoneOffsets != null)
        {
            foreach (var boneOffset in BoneOffsets)
            {
                if (boneOffset.Bone != null)
                {
                    Vector3 startPosition = boneOffset.Bone.position;
                    Vector3 endPosition = new Vector3(
                        boneOffset.Bone.position.x + xOffset + boneOffset.Offset.x,
                        boneOffset.Bone.position.y + yOffset + boneOffset.Offset.y,
                        boneOffset.Bone.position.z + zOffset + boneOffset.Offset.z
                    );

                    // Set the Gizmo color to something noticeable, like cyan.
                    Gizmos.color = Color.cyan;

                    // Draw a line from the bone's position to the new position (offset).
                    Gizmos.DrawLine(startPosition, endPosition);

                    // Draw a small sphere at the end for clarity.
                    Gizmos.DrawSphere(endPosition, 0.05f);
                }
            }
        }
    }
}
