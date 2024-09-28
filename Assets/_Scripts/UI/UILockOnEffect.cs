using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

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

    private Tween boxSizeTween;

    private void Start()
    {
        mainCamera = Camera.main;
        boxSize = initialBoxSize;
    }

    private void Update()
    {
        // Update rotation angle
        rotationAngle += Time.deltaTime * 360f;
        if (rotationAngle >= 360f)
            rotationAngle -= 360f;
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
        GUI.DrawTexture(
            new Rect(boxPos.x - boxSize / 2, boxPos.y - boxSize / 2, boxSize, boxSize),
            wireframeBoxTexture,
            ScaleMode.ScaleToFit,
            true
        );
        GUIUtility.RotateAroundPivot(-rotationAngle, boxPos); // End rotation
    }

    public void LockOnTarget(Transform target)
    {
        this.enemy = target;

        // Reset box size and rotation angle
        boxSize = initialBoxSize;
        rotationAngle = 0f;

        // Stop any existing tween
        boxSizeTween.Stop();

        // Animate the box size from initialBoxSize to 0 over 0.6 seconds
        boxSizeTween = Tween.Custom(this, initialBoxSize, 0f, 0.6f, (target, value) => target.boxSize = value);
    }

    public void UnlockTarget(Transform target)
    {
        if (this.enemy == target)
        {
            this.enemy = null;
        }
    }

    public void ClearAllTargets()
    {
        this.enemy = null;
    }

    public void UpdateTargets(List<Transform> targets)
    {
        // This method might not be needed for this implementation
        // but you can use it to update multiple targets if needed
    }

    private void OnDisable()
    {
        // Ensure the tween is stopped when the object is disabled
        boxSizeTween.Stop();
    }
}