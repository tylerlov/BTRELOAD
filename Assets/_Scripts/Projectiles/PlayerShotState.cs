using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Movement;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;

public class PlayerShotState : ProjectileState
{
    private const float CLOSE_PROXIMITY_THRESHOLD = 0.5f;
    private const float TARGET_UPDATE_INTERVAL = 0.1f;
    private const float MAX_PREDICTION_DISTANCE = 10f;

    private bool _hasAssignedTarget;
    private Vector3 _lastKnownTargetPosition;
    private float _targetUpdateTimer;

    public PlayerShotState(ProjectileStateBased projectile, float playerAccuracyModifier = 1f, Transform initialTarget = null, bool hasAssignedTarget = false)
        : base(projectile)
    {
        _hasAssignedTarget = hasAssignedTarget;
        _projectile.isPlayerShot = true;
        _projectile.initialPosition = _projectile.transform.position;
        _projectile.initialDirection = _projectile.transform.forward;
        _projectile.homing = _hasAssignedTarget;

        float finalAccuracy = ProjectileManager.Instance.projectileAccuracy * playerAccuracyModifier;
        _projectile.SetAccuracy(finalAccuracy);

        if (initialTarget != null && _hasAssignedTarget)
        {
            _projectile.currentTarget = initialTarget;
            _lastKnownTargetPosition = initialTarget.position;
        }
        else if (!_hasAssignedTarget)
        {
            _projectile.currentTarget = null;
            _lastKnownTargetPosition = _projectile.transform.position + _projectile.transform.forward * 100f;
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

        Vector3 targetPosition = _projectile.currentTarget != null ? _projectile.predictedPosition : _lastKnownTargetPosition;
        float distanceToTarget = Vector3.Distance(_projectile.transform.position, targetPosition);

        if (distanceToTarget <= CLOSE_PROXIMITY_THRESHOLD)
        {
            EnsureHit();
        }

        ConditionalDebug.Log($"Projectile ID: {_projectile.GetInstanceID()}, Position: {_projectile.transform.position}, " +
            $"Target: {targetPosition}, Distance: {distanceToTarget}, Velocity: {_projectile.rb.velocity}, " +
            $"Forward: {_projectile.transform.forward}, Rotation: {_projectile.transform.rotation.eulerAngles}");
    }

    private void UpdateTargetAndPrediction()
    {
        if (_projectile.currentTarget == null && _hasAssignedTarget)
        {
            FindNewTarget();
        }

        if (_projectile.currentTarget != null)
        {
            Vector3 targetVelocity = (_projectile.currentTarget.position - _lastKnownTargetPosition) / TARGET_UPDATE_INTERVAL;
            float predictionTime = Mathf.Min(Vector3.Distance(_projectile.transform.position, _projectile.currentTarget.position) / _projectile.bulletSpeed, MAX_PREDICTION_DISTANCE / _projectile.bulletSpeed);
            _projectile.predictedPosition = _projectile.currentTarget.position + targetVelocity * predictionTime;
            _lastKnownTargetPosition = _projectile.currentTarget.position;
        }
    }

    private void FindNewTarget()
    {
        if (_hasAssignedTarget)
        {
            Transform nearestEnemy = ProjectileManager.Instance.FindNearestEnemy(_projectile.transform.position);
            if (nearestEnemy != null)
            {
                _projectile.currentTarget = nearestEnemy;
                _lastKnownTargetPosition = nearestEnemy.position;
                _projectile.homing = true;
            }
            else
            {
                _hasAssignedTarget = false;
                _lastKnownTargetPosition = _projectile.transform.position + _projectile.transform.forward * 100f;
            }
        }
    }

    private void EnsureHit()
    {
        if (_projectile.currentTarget != null)
        {
            _projectile.transform.position = _projectile.currentTarget.position;
            _projectile.rb.velocity = Vector3.zero;

            Collider targetCollider = _projectile.currentTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                OnTriggerEnter(targetCollider);
            }
        }
        else
        {
            _projectile.Death();
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            bool isTargetedEnemy = (_projectile.currentTarget != null && other.transform == _projectile.currentTarget);
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                _projectile.ApplyDamage(damageable);
                _projectile.hasHitTarget = true;
                _projectile.ReportPlayerProjectileHit(isTargetedEnemy, other.gameObject.name);
                
                _projectile.Death();
                ProjectileManager.Instance.NotifyEnemyHit(other.gameObject, _projectile);
                PlayerLocking.Instance.RemoveLockedEnemy(other.transform);
            }
            GameManager.Instance.LogProjectileHit(
                _projectile.isPlayerShot,
                true,
                other.gameObject.tag
            );
        }
        else
        {
            _projectile.LogProjectileHit(other.gameObject.name);
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