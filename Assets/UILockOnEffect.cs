using UnityEngine;
using DG.Tweening;

public class UILockOnEffect : MonoBehaviour
{
    public Texture2D wireframeBoxTexture; // Assign your wireframe square texture here
    public Color textureColor = Color.white; // The color of the texture
    public float initialBoxSize = 100f; // Initial size of the box, you can modify this in the editor
    private float boxSize;
    private Vector2 boxPos;
    private Transform enemy;
    private Camera mainCamera;
    private float rotationAngle = 0f; // Rotation angle of the box

    private void Start()
    {
        mainCamera = Camera.main;
        boxSize = initialBoxSize;
    }

    private void Update()
    {
        // Update rotation angle
        rotationAngle += Time.deltaTime * 360f;
        if (rotationAngle >= 360f) rotationAngle -= 360f;
    }

    private void LateUpdate()
    {
        // Update enemy position if enemy is not null
        if (enemy != null)
        {
            Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(enemy.position);
            boxPos = new Vector2(enemyScreenPos.x, Screen.height - enemyScreenPos.y);
        }
    }

    private void OnGUI()
    {
        if (enemy == null)
            return;

        // Draw the wireframe box
        GUIUtility.RotateAroundPivot(rotationAngle, boxPos); // Start rotation
        GUI.color = textureColor;
        GUI.DrawTexture(new Rect(boxPos.x - boxSize / 2, boxPos.y - boxSize / 2, boxSize, boxSize), wireframeBoxTexture,
            ScaleMode.ScaleToFit, true);
        GUIUtility.RotateAroundPivot(-rotationAngle, boxPos); // End rotation
    }

    public void LockOnTarget(Transform enemy)
    {
        this.enemy = enemy;

        // Reset box size and rotation angle
        boxSize = initialBoxSize;
        rotationAngle = 0f;

        // Animate the box size from initialBoxSize to 0 over 1 second
        DOVirtual.Float(boxSize, 0, 0.6f, value => { boxSize = value; });
    }
}