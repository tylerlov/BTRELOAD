using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers; // This is the namespace for Curvy Spline package components.
using Pathfinding;
using UnityEngine;

public class DisableAstarOnPlayer : MonoBehaviour
{
    public SplineController splineController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AIPathAlignedToSurface aiPath = other.GetComponent<AIPathAlignedToSurface>();
            if (aiPath != null)
            {
                aiPath.enabled = false;
            }

            splineController.PositionMode = CurvyPositionMode.Relative;
            splineController.Position = 0f;
        }
    }
}
