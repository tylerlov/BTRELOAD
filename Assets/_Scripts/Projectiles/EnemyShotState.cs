using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Movement;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;

public class EnemyShotState : ProjectileState
{
    private const float PREDICTION_MULTIPLIER = 2f; // Increase this for more aggressive prediction
    private const float MAX_INACCURACY_ANGLE = 30f; // Increased for more noticeable effect

    public EnemyShotState(ProjectileStateBased projectile) : base(projectile)
    {
        _projectile.currentTarget = GameObject.FindWithTag("Player").transform;
        _projectile.minTurnRadius = 3f; // Reduced for tighter turns
        _projectile.maxTurnRadius = 15f; // Reduced for more accurate aiming
        _projectile.approachAngle = UnityEngine.Random.Range(10f, 20f); // Reduced for more direct approach
        _projectile.turnRate = 270f; // Increased for faster turning
    }

    public override void CustomUpdate(float timeScale)
    {
        base.CustomUpdate(timeScale);

        if (_projectile.currentTarget != null)
        {
            Vector3 targetVelocity = _projectile.currentTarget.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
            Vector3 predictedPosition = _projectile.currentTarget.position + targetVelocity * PREDICTION_MULTIPLIER;
            Vector3 directionToTarget = (predictedPosition - _projectile.transform.position).normalized;

            // Apply inaccuracy based on the projectile's accuracy value
            float accuracy = _projectile.GetAccuracy();
            float inaccuracyAngle = (1f - accuracy) * MAX_INACCURACY_ANGLE;
            Vector3 inaccurateDirection = ApplyInaccuracy(directionToTarget, inaccuracyAngle);

            float distanceToTarget = Vector3.Distance(_projectile.transform.position, predictedPosition);
            float approachFactor = Mathf.Clamp01(distanceToTarget / _projectile.maxTurnRadius);
            float currentTurnRate = Mathf.Lerp(_projectile.turnRate, _projectile.turnRate * 0.5f, approachFactor);

            Vector3 newForward = Vector3.RotateTowards(_projectile.transform.forward, inaccurateDirection, 
                currentTurnRate * Mathf.Deg2Rad * Time.deltaTime * timeScale, 0f);
            _projectile.transform.rotation = Quaternion.LookRotation(newForward);

            _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
        }
    }

    private Vector3 ApplyInaccuracy(Vector3 direction, float maxAngle)
    {
        Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(-maxAngle, maxAngle), Random.onUnitSphere);
        return randomRotation * direction;
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //May be wrong - refer to previous code if not working
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                _projectile.ApplyDamage(damageable);
                _projectile.projHitPlayer = true;
                _projectile.Death();
                ProjectileManager.Instance.PlayOneShotSound(
                    "event:/Projectile/Basic/Impact",
                    _projectile.transform.position
                );
            }
        }
        else if (!other.gameObject.CompareTag("Enemy") && !other.gameObject.CompareTag("LaunchableBullet"))
        {
            ConditionalDebug.Log(
                $"Projectile collided with non-target object: {other.gameObject.name}."
            );
        }
    }
}