using UnityEngine;

public class DottedLine : MonoBehaviour
{
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private int numberOfDots = 20;

    [SerializeField]
    private float dotSpacing = 1.0f;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            Debug.LogWarning(
                "DottedLine: Line Renderer not assigned, Adding and Using default Line Renderer."
            );
            CreateDefaultLineRenderer();
        }
    }

    private void CreateDefaultLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = true;
    }

    public void DrawDottedLine(GameObject projectileTarget)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = projectileTarget.transform.position;

        RaycastHit hit;
        if (Physics.Linecast(startPosition, targetPosition, out hit, groundLayer))
        {
            targetPosition = hit.point;
        }

        Vector3 direction = (targetPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, targetPosition);
        int actualNumberOfDots = Mathf.RoundToInt(distance / dotSpacing);

        lineRenderer.positionCount = actualNumberOfDots;

        for (int i = 0; i < actualNumberOfDots; i++)
        {
            Vector3 dotPosition = startPosition + direction * dotSpacing * i;
            lineRenderer.SetPosition(i, dotPosition);
        }
    }
}
