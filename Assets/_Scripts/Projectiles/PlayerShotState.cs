using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Movement;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;

public class PlayerShotState : ProjectileState
{
    private const float CLOSE_PROXIMITY_THRESHOLD = 2f;
    private const float MAX_PREDICTION_TIME = 2f;
    private const float MIN_SPEED_MULTIPLIER = 1.2f;
    private const float PARRIED_SPEED_MULTIPLIER = 1.5f;

    public PlayerShotState(ProjectileStateBased projectile, float playerAccuracyModifier = 1f)
        : base(projectile)
    {
        projectile.bulletSpeed *= MIN_SPEED_MULTIPLIER;

        if (projectile.isParried)
        {
            projectile.bulletSpeed *= PARRIED_SPEED_MULTIPLIER;
        }
        _projectile.SetLifetime(10f);
        _projectile.isPlayerShot = true;
        _projectile.initialPosition = _projectile.transform.position;
        _projectile.initialDirection = _projectile.transform.forward;
        _projectile.homing = true;
        float finalAccuracy =
            ProjectileManager.Instance.projectileAccuracy * playerAccuracyModifier;
        _projectile.SetAccuracy(finalAccuracy);
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
    }

    public override void CustomUpdate(float timeScale)
    {
        base.CustomUpdate(timeScale);

        if (_projectile.currentTarget != null)
        {
            UpdateProjectileMovement(timeScale);
        }
        else
        {
            FindNewTarget();
        }

        CheckCollision();
    }

    private void UpdateProjectileMovement(float timeScale)
    {
        if (_projectile.currentTarget == null) return;

        ProjectileVector3 targetPosition = _projectile.currentTarget.position;
        ProjectileVector3 toTarget = new ProjectileVector3(
            targetPosition.x - _projectile.transform.position.x,
            targetPosition.y - _projectile.transform.position.y,
            targetPosition.z - _projectile.transform.position.z
        );
        float distanceToTarget = ProjectileVector3.Distance(targetPosition, (ProjectileVector3)_projectile.transform.position);

        if (distanceToTarget <= CLOSE_PROXIMITY_THRESHOLD)
        {
            EnsureHit();
            return;
        }

        Vector3 targetVelocity = ProjectileManager.Instance.CalculateTargetVelocity(
            _projectile.currentTarget.gameObject
        );
        float predictionTime = Mathf.Min(
            distanceToTarget / _projectile.bulletSpeed,
            MAX_PREDICTION_TIME
        );
        _projectile.predictedPosition = new ProjectileVector3(
            targetPosition.x + targetVelocity.x * predictionTime,
            targetPosition.y + targetVelocity.y * predictionTime,
            targetPosition.z + targetVelocity.z * predictionTime
        );

        Vector3 directionToTarget = ((Vector3)_projectile.predictedPosition - _projectile.transform.position).normalized;

        if (_projectile.accuracy < 1f)
        {
            float maxDeviationAngle = Mathf.Lerp(5f, 0f, _projectile.accuracy);
            Vector3 randomDeviation = UnityEngine.Random.insideUnitSphere * maxDeviationAngle;
            directionToTarget = Quaternion.Euler(randomDeviation) * directionToTarget;
        }

        _projectile.transform.rotation = Quaternion.RotateTowards(
            _projectile.transform.rotation,
            Quaternion.LookRotation(directionToTarget),
            _projectile.turnRate * timeScale * Time.deltaTime
        );

        _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
    }

    private void FindNewTarget()
    {
        Transform nearestEnemy = ProjectileManager.Instance.FindNearestEnemy(
            _projectile.transform.position
        );
        if (nearestEnemy != null)
        {
            _projectile.currentTarget = nearestEnemy;
        }
        else
        {
            _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
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
    }

    private void CheckCollision()
    {
        int layerMask = LayerMask.GetMask("Enemy", "Environment");
        Collider[] hitColliders = Physics.OverlapSphere(_projectile.transform.position, 0.1f, layerMask);
        
        if (hitColliders.Length > 0)
        {
            OnTriggerEnter(hitColliders[0]);
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                _projectile.ApplyDamage(damageable);
                _projectile.hasHitTarget = true;
                ConditionalDebug.Log(
                    $"Player projectile hit enemy: {other.gameObject.name}. Distance: {_projectile.distanceTraveled}, Time: {_projectile.timeAlive}"
                );
                _projectile.Death();
                ProjectileManager.Instance.NotifyEnemyHit(other.gameObject, _projectile);
                PlayerLocking.Instance.RemoveLockedEnemy(other.transform);
            }
        }
        else
        {
            ConditionalDebug.Log(
                $"Player projectile hit non-enemy object: {other.gameObject.name}. Distance: {_projectile.distanceTraveled}, Time: {_projectile.timeAlive}"
            );
        }
        GameManager.Instance.LogProjectileHit(
            _projectile.isPlayerShot,
            other.gameObject.CompareTag("Enemy"),
            other.gameObject.tag
        );
    }
}