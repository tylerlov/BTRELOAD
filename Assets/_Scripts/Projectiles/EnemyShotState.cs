using UnityEngine;

public class EnemyShotState : ProjectileState
{
    private const float PREDICTION_MULTIPLIER = 1.5f; // Reduced from 2f
    private const float MAX_INACCURACY_ANGLE = 15f; // Reduced from 30f

    private ProjectileMovement _projectileMovement;

    public EnemyShotState(ProjectileStateBased projectile, Transform target = null)
        : base(projectile)
    {
        _projectile.currentTarget = target;
        _projectile.homing = target != null;
        _projectile.minTurnRadius = 5f;
        _projectile.maxTurnRadius = 20f;
        _projectile.approachAngle = UnityEngine.Random.Range(5f, 15f);
        _projectile.turnRate = 180f; // Reduced from 270f

        // Set initial accuracy (adjust this value as needed)
        _projectile.SetAccuracy(ProjectileManager.Instance.projectileAccuracy);

        _projectileMovement = new ProjectileMovement(projectile);
    }

    public override void CustomUpdate(float timeScale)
    {
        base.CustomUpdate(timeScale);

        if (_projectile.currentTarget != null)
        {
            _projectileMovement.UpdateMovement(timeScale);
        }
    }

    private Vector3 ApplyInaccuracy(Vector3 direction, float accuracy)
    {
        float inaccuracyAngle = Mathf.Lerp(MAX_INACCURACY_ANGLE, 0f, Mathf.Pow(accuracy, 2));
        if (Random.value > accuracy)
        {
            float randomAngle = Random.Range(0f, inaccuracyAngle);
            Vector3 randomAxis = Random.insideUnitSphere;
            return Quaternion.AngleAxis(randomAngle, randomAxis) * direction;
        }
        return direction;
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                _projectile.ApplyDamage(damageable);
                _projectile.projHitPlayer = true;
                _projectile.Death(true);
                ProjectileAudioManager.Instance.PlayPlayerImpactSound(_projectile.transform.position);
            }
        }
        else if (
            !other.gameObject.CompareTag("Enemy") && 
            !other.gameObject.CompareTag("LaunchableBullet")
        )
        {
            ConditionalDebug.Log(
                $"Projectile collided with non-target object: {other.gameObject.name}."
            );
        }
    }
}
