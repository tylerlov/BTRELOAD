using UnityEngine;
using UnityEngine.UI;

public class follow3DReticle : MonoBehaviour
{
    public Transform reticle;
    public Vector2 offset; // Add this line to include an offset
    public RectTransform rectTransform;
    public Canvas canvas;
    public CanvasScaler canvasScaler;

    // Start is called before the first frame update
    void Start()
    {
        reticle = GameObject.FindGameObjectWithTag("Reticle").transform;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasScaler = canvas.GetComponent<CanvasScaler>();
    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            reticle.position
        );

        // Adjust for canvas scaling
        float scaleFactor = canvasScaler.scaleFactor;
        screenPoint.x /= scaleFactor;
        screenPoint.y /= scaleFactor;

        // Update the anchored position with the offset
        rectTransform.anchoredPosition = screenPoint + offset; // Add the offset here
    }
}
