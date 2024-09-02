using UnityEngine;

public class ProjectileMovement
{
    private ProjectileStateBased _projectile;

    public ProjectileMovement(ProjectileStateBased projectile)
    {
        _projectile = projectile;
    }

    public void UpdateMovement(float timeScale)
    {
        if (_projectile.homing && _projectile.currentTarget != null)
        {
            Vector3 directionToTarget = (_projectile.predictedPosition - _projectile.transform.position).normalized;

            if (_projectile.accuracy < 1f)
            {
                float maxDeviationAngle = Mathf.Lerp(10f, 0f, _projectile.accuracy); // Reduced from 30f to 10f
                Vector3 randomDeviation = UnityEngine.Random.insideUnitSphere * maxDeviationAngle;
                directionToTarget = Quaternion.Euler(randomDeviation) * directionToTarget;
            }

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            _projectile.transform.rotation = Quaternion.RotateTowards(
                _projectile.transform.rotation,
                targetRotation,
                _projectile.turnRate * timeScale * Time.deltaTime
            );

            if (_projectile.rb != null && !_projectile.rb.isKinematic)
            {
                _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
            }
        }
    }

    public Vector3 CalculateTargetVelocity(GameObject target)
    {
        Vector3 currentPos = target.transform.position;
        Vector3 velocity = (currentPos - _projectile._previousPosition) / Time.deltaTime;

        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
        {
            ConditionalDebug.LogError("Calculated target velocity contains NaN values.");
            velocity = Vector3.zero;
        }

        _projectile._previousPosition = currentPos;
        return velocity;
    }

    public void OnPlayerRicochetDodge()
    {
        if (_projectile.GetCurrentState() is EnemyShotState)
        {
            _projectile.currentTarget = null;
            _projectile.homing = false;
            Vector3 oppositeDirection = -_projectile.transform.forward;
            _projectile.rb.velocity = oppositeDirection * _projectile.bulletSpeed;
            Debug.Log("Projectile is now moving in the opposite direction due to Ricochet Dodge.");
        }
    }
}