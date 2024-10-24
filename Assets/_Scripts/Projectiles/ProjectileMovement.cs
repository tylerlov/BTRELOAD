using UnityEngine;

public class ProjectileMovement
{
    private ProjectileStateBased _projectile;
    private const float PREDICTION_TIME = 0.5f;
    private const float MAX_TURN_RATE = 90f; // degrees per second
    private const float ACCELERATION = 10f; // units per second squared
    private Vector3 _targetVelocity;
    private Vector3 _lastTargetPosition;
    private Vector3 _currentVelocity;
    private bool _accuracyApplied = false;

    // Add these new constants
    private const float SLOW_DOWN_DISTANCE = 5f; // Distance at which projectile starts slowing down
    private const float MIN_SPEED_MULTIPLIER = 0.5f; // Minimum speed as a fraction of original speed

    public ProjectileMovement(ProjectileStateBased projectile)
    {
        _projectile = projectile;
        _currentVelocity = projectile.transform.forward * projectile.bulletSpeed;
        _accuracyApplied = false; // Reset accuracy application
    }

    public void UpdateMovement(float timeScale)
    {
        Vector3 moveDirection;

        if (_projectile.homing && _projectile.currentTarget != null)
        {
            // Homing logic
            Vector3 directionToTarget = (_projectile.currentTarget.position - _projectile.transform.position).normalized;
            float distanceToTarget = Vector3.Distance(_projectile.transform.position, _projectile.currentTarget.position);

            // Apply accuracy offset once at the start
            if (!_accuracyApplied)
            {
                directionToTarget = ApplyAccuracyOffset(directionToTarget);
                _accuracyApplied = true;
            }

            float turnRate = MAX_TURN_RATE * timeScale * Mathf.Deg2Rad;
            Vector3 newForward = Vector3.RotateTowards(_projectile.transform.forward, directionToTarget, turnRate * Time.deltaTime, 0f);
            _projectile.transform.rotation = Quaternion.LookRotation(newForward);

            moveDirection = newForward;

            // Calculate speed multiplier based on distance to target
            float speedMultiplier = 1f;
            if (distanceToTarget < SLOW_DOWN_DISTANCE)
            {
                speedMultiplier = Mathf.Lerp(MIN_SPEED_MULTIPLIER, 1f, distanceToTarget / SLOW_DOWN_DISTANCE);
            }

            _currentVelocity = moveDirection * _projectile.bulletSpeed * speedMultiplier;
        }
        else
        {
            moveDirection = _projectile.transform.forward;
            _currentVelocity = moveDirection * _projectile.bulletSpeed;
        }

        // Apply movement
        if (_projectile.rb != null)
        {
            if (_projectile.rb.isKinematic)
            {
                _projectile.rb.MovePosition(_projectile.rb.position + _currentVelocity * Time.deltaTime);
            }
            else
            {
                _projectile.rb.linearVelocity = _currentVelocity;
            }
        }
    }

    private void UpdateTargetVelocity()
    {
        if (_projectile.currentTarget != null)
        {
            Vector3 currentTargetPosition = _projectile.currentTarget.position;
            _targetVelocity = (currentTargetPosition - _lastTargetPosition) / Time.deltaTime;
            _lastTargetPosition = currentTargetPosition;
        }
    }

    private Vector3 PredictTargetPosition()
    {
        if (_projectile.currentTarget == null)
        {
            return _projectile.transform.position + _projectile.transform.forward * 10f;
        }

        Vector3 targetPosition = _projectile.currentTarget.position;
        float distanceToTarget = Vector3.Distance(_projectile.transform.position, targetPosition);
        float timeToReachTarget = distanceToTarget / _projectile.bulletSpeed;

        return targetPosition + _targetVelocity * timeToReachTarget * PREDICTION_TIME;
    }

    private Vector3 ApplyAccuracyOffset(Vector3 direction)
    {
        float accuracy = _projectile.GetAccuracy();
        float inaccuracyAngle = Mathf.Lerp(20f, 0f, accuracy); // Max 20 degrees of inaccuracy

        // Apply inaccuracy only if the projectile is missing
        if (Random.value > accuracy)
        {
            _projectile.isMissing = true;
            float randomAngleX = Random.Range(-inaccuracyAngle, inaccuracyAngle);
            float randomAngleY = Random.Range(-inaccuracyAngle, inaccuracyAngle);
            Quaternion inaccuracyRotation = Quaternion.Euler(randomAngleX, randomAngleY, 0);
            direction = inaccuracyRotation * direction;
        }
        else
        {
            _projectile.isMissing = false;
        }

        return direction.normalized;
    }

    public void OnPlayerRicochetDodge()
    {
        if (_projectile.GetCurrentState() is EnemyShotState)
        {
            _projectile.homing = false;
            _currentVelocity = -_currentVelocity;
            _projectile.transform.rotation = Quaternion.LookRotation(_currentVelocity);
        }
    }
}
