using UnityEngine;

public class PlayerShotState : ProjectileState
{
    private const float CLOSE_PROXIMITY_THRESHOLD = 0.5f;
    private const float TARGET_UPDATE_INTERVAL = 0.1f;
    private const float MAX_PREDICTION_DISTANCE = 10f;

    private bool _hasAssignedTarget;
    private Vector3 _lastKnownTargetPosition;
    private float _targetUpdateTimer;

    public PlayerShotState(
        ProjectileStateBased projectile,
        float playerAccuracyModifier = 1f,
        Transform initialTarget = null,
        bool hasAssignedTarget = false
    )
        : base(projectile)
    {
        _hasAssignedTarget = hasAssignedTarget;
        _projectile.isPlayerShot = true;
        _projectile.initialPosition = _projectile.transform.position;
        _projectile.initialDirection = _projectile.transform.forward;
        _projectile.homing = _hasAssignedTarget;

        // Set accuracy to 100% for player shots
        _projectile.SetAccuracy(1f);

        if (initialTarget != null && _hasAssignedTarget)
        {
            _projectile.currentTarget = initialTarget;
            _lastKnownTargetPosition = initialTarget.position;
        }
        else if (!_hasAssignedTarget)
        {
            _projectile.currentTarget = null;
            _lastKnownTargetPosition =
                _projectile.transform.position + _projectile.transform.forward * 100f;
        }
        else
        {
            FindNewTarget();
        }

        _projectile.bulletSpeed = Mathf.Min(_projectile.bulletSpeed, _projectile.maxSpeed);
    }

    public override void CustomUpdate(float timeScale)
    {
        base.CustomUpdate(timeScale);

        _targetUpdateTimer += Time.deltaTime * timeScale;
        if (_targetUpdateTimer >= TARGET_UPDATE_INTERVAL)
        {
            _targetUpdateTimer = 0f;
            UpdateTargetAndPrediction();
        }

        Vector3 targetPosition =
            _projectile.currentTarget != null
                ? _projectile.predictedPosition
                : _lastKnownTargetPosition;
        float distanceToTarget = Vector3.Distance(_projectile.transform.position, targetPosition);

        Debug.Log($"Projectile {_projectile.GetInstanceID()} - Position: {_projectile.transform.position}, Target: {targetPosition}, Distance: {distanceToTarget}, Velocity: {_projectile.rb.linearVelocity}");

        if (distanceToTarget <= CLOSE_PROXIMITY_THRESHOLD)
        {
            EnsureHit();
        }

        ConditionalDebug.Log(
            $"Projectile ID: {_projectile.GetInstanceID()}, Position: {_projectile.transform.position}, "
                + $"Target: {targetPosition}, Distance: {distanceToTarget}, Velocity: {_projectile.rb.linearVelocity}, "
                + $"Forward: {_projectile.transform.forward}, Rotation: {_projectile.transform.rotation.eulerAngles}"
        );
    }

    private void UpdateTargetAndPrediction()
    {
        if (_projectile.currentTarget == null && _hasAssignedTarget)
        {
            FindNewTarget();
        }

        if (_projectile.currentTarget != null)
        {
            Vector3 targetVelocity =
                (_projectile.currentTarget.position - _lastKnownTargetPosition)
                / TARGET_UPDATE_INTERVAL;
            float predictionTime = Mathf.Min(
                Vector3.Distance(_projectile.transform.position, _projectile.currentTarget.position)
                    / _projectile.bulletSpeed,
                MAX_PREDICTION_DISTANCE / _projectile.bulletSpeed
            );
            _projectile.predictedPosition =
                _projectile.currentTarget.position + targetVelocity * predictionTime;
            _lastKnownTargetPosition = _projectile.currentTarget.position;
        }
    }

    private void FindNewTarget()
    {
        if (_hasAssignedTarget)
        {
            Transform nearestEnemy = ProjectileManager.Instance.FindNearestEnemy(
                _projectile.transform.position
            );
            if (nearestEnemy != null)
            {
                _projectile.currentTarget = nearestEnemy;
                _lastKnownTargetPosition = nearestEnemy.position;
                _projectile.homing = true;
            }
            else
            {
                _hasAssignedTarget = false;
                _lastKnownTargetPosition =
                    _projectile.transform.position + _projectile.transform.forward * 100f;
            }
        }
    }

    private void EnsureHit()
    {
        if (_projectile.currentTarget != null)
        {
            _projectile.transform.position = _projectile.currentTarget.position;
            _projectile.rb.linearVelocity = Vector3.zero;

            Collider targetCollider = _projectile.currentTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                OnTriggerEnter(targetCollider);
            }
        }
        else
        {
            _projectile.Death(true);
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    public override void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject hitObject)
    {
        Debug.Log($"PlayerShotState projectile collided with: {hitObject.name}, Tag: {hitObject.tag}");
        
        if (hitObject.CompareTag("Enemy"))
        {
            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"Applying damage to: {hitObject.name}");
                _projectile.ApplyDamage(damageable);
                _projectile.hasHitTarget = true;
                
                _projectile.ReportPlayerProjectileHit(_projectile.currentTarget == hitObject.transform, hitObject.name);

                _projectile.Death(true);
                ProjectileManager.Instance.NotifyEnemyHit(hitObject, _projectile);
            }
            else
            {
                Debug.LogWarning($"Enemy does not implement IDamageable interface: {hitObject.name}");
            }

            GameManager.Instance.LogProjectileHit(_projectile.isPlayerShot, damageable != null, hitObject.tag);
        }
        else
        {
            Debug.Log($"Projectile hit non-enemy object: {hitObject.name}");
        }
    }

    private void OnDrawGizmos()
    {
        if (_projectile != null && _projectile.currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_projectile.transform.position, _projectile.currentTarget.position);
            Gizmos.DrawWireSphere(_projectile.currentTarget.position, 0.5f);
        }
    }
}
