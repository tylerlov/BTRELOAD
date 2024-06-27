using UnityEngine;
using FluffyUnderware.Curvy;

[CreateAssetMenu(fileName = "New Spline Data", menuName = "Spline Data")]
public class SplineDataSO : ScriptableObject
{
    public GameObject Spline;
    public float BaseSpeed;
    public CurvyClamping Clamping;
}