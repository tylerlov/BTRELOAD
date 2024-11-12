using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class UILockOnEffect : MonoBehaviour
{
    [Header("Projectile Lock Effect")]
    public Texture2D wireframeBoxTexture;
    public Color projectileLockColor = Color.white;
    public float initialBoxSize = 100f;
    public float projectileLockDuration = 0.6f;

    [Header("Enemy Lock Effect")]
    public Texture2D enemyLockTexture;
    public Color enemyLockColor = Color.red;
    public float enemyLockInitialSize = 50f;
    public float enemyLockMaxSize = 100f;
    public float enemyLockDuration = 0.3f;

    private Camera mainCamera;
    private float rotationAngle = 0f;

    private Dictionary<Transform, ProjectileLockEffect> projectileLocks = new Dictionary<Transform, ProjectileLockEffect>();
    private Dictionary<Transform, EnemyLockEffect> enemyLocks = new Dictionary<Transform, EnemyLockEffect>();

    private class ProjectileLockEffect
    {
        public Vector2 position;
        public float size;
        public float rotation;
        public Tween sizeTween;
    }

    private class EnemyLockEffect
    {
        public Vector2 position;
        public float size;
        public Tween sizeTween;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        rotationAngle += Time.deltaTime * 360f;
        if (rotationAngle >= 360f)
            rotationAngle -= 360f;
    }

    private void LateUpdate()
    {
        UpdateProjectileLockPositions();
        UpdateEnemyLockPositions();
    }

    private void UpdateProjectileLockPositions()
    {
        foreach (var kvp in projectileLocks)
        {
            if (kvp.Key != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(kvp.Key.position);
                kvp.Value.position = new Vector2(screenPos.x, Screen.height - screenPos.y);
            }
        }
    }

    private void UpdateEnemyLockPositions()
    {
        foreach (var kvp in enemyLocks)
        {
            if (kvp.Key != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(kvp.Key.position);
                kvp.Value.position = new Vector2(screenPos.x, Screen.height - screenPos.y);
            }
        }
    }

    private void OnGUI()
    {
        DrawProjectileLocks();
        DrawEnemyLocks();
    }

    private void DrawProjectileLocks()
    {
        GUI.color = projectileLockColor;
        foreach (var kvp in projectileLocks)
        {
            if (kvp.Key != null)
            {
                var effect = kvp.Value;
                GUIUtility.RotateAroundPivot(rotationAngle, effect.position);
                GUI.DrawTexture(
                    new Rect(
                        effect.position.x - effect.size / 2,
                        effect.position.y - effect.size / 2,
                        effect.size,
                        effect.size
                    ),
                    wireframeBoxTexture,
                    ScaleMode.ScaleToFit,
                    true
                );
                GUIUtility.RotateAroundPivot(-rotationAngle, effect.position);
            }
        }
    }

    private void DrawEnemyLocks()
    {
        GUI.color = enemyLockColor;
        foreach (var kvp in enemyLocks)
        {
            if (kvp.Key != null)
            {
                var effect = kvp.Value;
                GUI.DrawTexture(
                    new Rect(
                        effect.position.x - effect.size / 2,
                        effect.position.y - effect.size / 2,
                        effect.size,
                        effect.size
                    ),
                    enemyLockTexture,
                    ScaleMode.ScaleToFit,
                    true
                );
            }
        }
    }

    public void LockOnTarget(Transform target)
    {
        if (!projectileLocks.ContainsKey(target))
        {
            var effect = new ProjectileLockEffect
            {
                size = initialBoxSize
            };

            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
            effect.position = new Vector2(screenPos.x, Screen.height - screenPos.y);

            effect.sizeTween = Tween.Custom(
                this,
                initialBoxSize,
                0f,
                projectileLockDuration,
                (target, value) => effect.size = value
            );

            projectileLocks.Add(target, effect);
        }
    }

    public void LockOnEnemy(Transform enemy)
    {
        if (!enemyLocks.ContainsKey(enemy))
        {
            var effect = new EnemyLockEffect
            {
                size = enemyLockInitialSize
            };

            Vector3 screenPos = mainCamera.WorldToScreenPoint(enemy.position);
            effect.position = new Vector2(screenPos.x, Screen.height - screenPos.y);

            effect.sizeTween = Tween.Custom(
                this,
                enemyLockInitialSize,
                enemyLockMaxSize,
                enemyLockDuration,
                (target, value) => effect.size = value
            );

            enemyLocks.Add(enemy, effect);
        }
    }

    public void UnlockTarget(Transform target)
    {
        if (projectileLocks.TryGetValue(target, out var projectileEffect))
        {
            projectileEffect.sizeTween.Stop();
            projectileLocks.Remove(target);
        }

        if (enemyLocks.TryGetValue(target, out var enemyEffect))
        {
            enemyEffect.sizeTween.Stop();
            enemyLocks.Remove(target);
        }
    }

    public void ClearAllTargets()
    {
        foreach (var effect in projectileLocks.Values)
        {
            effect.sizeTween.Stop();
        }
        foreach (var effect in enemyLocks.Values)
        {
            effect.sizeTween.Stop();
        }
        projectileLocks.Clear();
        enemyLocks.Clear();
    }

    private void OnDisable()
    {
        ClearAllTargets();
    }
}