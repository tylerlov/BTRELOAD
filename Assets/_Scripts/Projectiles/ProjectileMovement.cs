using UnityEngine;

public class ProjectileMovement
{
    private ProjectileStateBased _projectile;
    private const float MIN_VELOCITY = 5f;
    private const float MAX_ROTATION_DELTA = 360f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const int STUCK_FRAME_COUNT = 10;
    private const float PREDICTION_TIME = 0.25f;
    private const float VERY_CLOSE_RANGE = 3f;
    private const float CLOSE_RANGE = 10f;
    private const float FAR_RANGE = 50f;
    private const float TURN_FACTOR_VERY_CLOSE = 0.1f;
    private const float MIN_SPEED_FACTOR = 0.8f;
    private const float OVERSHOOT_SPEED_MULTIPLIER = 2f;
    private const float OVERSHOOT_TURN_MULTIPLIER = 3f;
    private const float CLOSE_PROXIMITY_TIME_THRESHOLD = 0.5f;
    private const float MAX_PREDICTION_VARIANCE = 0.1f;
    private const float MAX_INACCURACY_ANGLE = 60f;
    private const float INACCURACY_CURVE_STEEPNESS = 2f;
    private const float MIN_ACCURACY_SPEED_FACTOR = 0.5f;
    private const float MIN_ACCURACY_TURN_FACTOR = 0.3f;
    private const float MAX_ACCURACY_PREDICTION_TIME = 0.5f;
    private const float MIN_ACCURACY_PREDICTION_TIME = 0.1f;
    private const float VELOCITY_DEVIATION_FACTOR = 0.2f;
    private const float BASE_APPROACH_VARIATION_RADIUS = 5f;
    private const float MAX_BEHIND_ANGLE = 30f;
    private const float SPEED_INCREASE_FACTOR = 1.2f;

    private Vector3 _lastPosition;
    private int _stuckFrameCount;
    private Vector3 _lastTargetPosition;
    private Vector3 _targetVelocity;
    private bool _hasPassedTarget;
    private float _closeProximityTimer;
    private bool _aggressiveTurnAroundTriggered;

    public ProjectileMovement(ProjectileStateBased projectile)
    {
        _projectile = projectile;
        _lastPosition = _projectile.transform.position;
        _stuckFrameCount = 0;
        _lastTargetPosition = Vector3.zero;
        _targetVelocity = Vector3.zero;
        _hasPassedTarget = false;
        _closeProximityTimer = 0f;
        _aggressiveTurnAroundTriggered = false;
    }

    private float CalculateTurnFactor(float distanceToTarget)
    {
        if (distanceToTarget <= VERY_CLOSE_RANGE)
        {
            return TURN_FACTOR_VERY_CLOSE;
        }
        else if (distanceToTarget <= CLOSE_RANGE)
        {
            return Mathf.Lerp(
                TURN_FACTOR_VERY_CLOSE,
                1f,
                (distanceToTarget - VERY_CLOSE_RANGE) / (CLOSE_RANGE - VERY_CLOSE_RANGE)
            );
        }
        else if (distanceToTarget >= FAR_RANGE)
        {
            return 0.2f;
        }
        else
        {
            return Mathf.Lerp(
                1f,
                0.2f,
                (distanceToTarget - CLOSE_RANGE) / (FAR_RANGE - CLOSE_RANGE)
            );
        }
    }

    public void UpdateMovement(float timeScale)
    {
        Vector3 currentPosition = _projectile.transform.position;
        float distanceMoved = Vector3.Distance(_lastPosition, currentPosition);

        if (distanceMoved < STUCK_THRESHOLD)
        {
            _stuckFrameCount++;
            if (_stuckFrameCount >= STUCK_FRAME_COUNT)
            {
                ResetProjectileMovement();
            }
        }
        else
        {
            _stuckFrameCount = 0;
        }

        if (_projectile.homing && _projectile.currentTarget != null)
        {
            UpdateTargetVelocity();
            Vector3 predictedPosition = PredictTargetPosition();
            Vector3 directionToTarget = (predictedPosition - currentPosition).normalized;

            directionToTarget = ApplyInaccuracy(directionToTarget);

            float distanceToTarget = Vector3.Distance(currentPosition, predictedPosition);
            float turnFactor = CalculateTurnFactor(distanceToTarget);
            float speedFactor = CalculateSpeedFactor(distanceToTarget);

            float accuracy = _projectile.GetAccuracy();
            turnFactor *= CalculateAccuracyFactor(accuracy, MIN_ACCURACY_TURN_FACTOR);
            speedFactor *= CalculateAccuracyFactor(accuracy, MIN_ACCURACY_SPEED_FACTOR);

            CheckIfPassedTarget(currentPosition, predictedPosition);
            UpdateCloseProximityBehavior(
                distanceToTarget,
                _projectile.GetCurrentState() is EnemyShotState ? timeScale : 1f
            );

            if (_hasPassedTarget || _aggressiveTurnAroundTriggered)
            {
                turnFactor *= OVERSHOOT_TURN_MULTIPLIER;
                speedFactor *= OVERSHOOT_SPEED_MULTIPLIER;
            }

            Vector3 newForward = Vector3.RotateTowards(
                _projectile.transform.forward,
                directionToTarget,
                turnFactor
                    * MAX_ROTATION_DELTA
                    * Mathf.Deg2Rad
                    * Time.deltaTime
                    * (_projectile.GetCurrentState() is EnemyShotState ? timeScale : 1f),
                0f
            );

            _projectile.transform.rotation = Quaternion.LookRotation(newForward);

            if (_projectile.rb != null)
            {
                Vector3 velocityDeviation =
                    Random.insideUnitSphere
                    * CalculateInaccuracyFactor(accuracy)
                    * _projectile.bulletSpeed
                    * VELOCITY_DEVIATION_FACTOR;

                float speedMultiplier =
                    Vector3.Distance(predictedPosition, _projectile.currentTarget.position)
                    > BASE_APPROACH_VARIATION_RADIUS
                        ? SPEED_INCREASE_FACTOR
                        : 1f;

                float appliedTimeScale =
                    _projectile.GetCurrentState() is EnemyShotState ? timeScale : 1f;

                Vector3 newVelocity =
                    (
                        newForward
                        * _projectile.bulletSpeed
                        * speedFactor
                        * speedMultiplier
                        * appliedTimeScale
                    ) + velocityDeviation;

                if (_projectile.rb.isKinematic)
                {
                    _projectile.rb.MovePosition(_projectile.rb.position + newVelocity * Time.fixedDeltaTime);
                }
                else
                {
                    _projectile.rb.velocity = newVelocity;
                }
            }
        }
        else if (_projectile.rb != null)
        {
            Vector3 velocityDeviation = Random.insideUnitSphere * CalculateInaccuracyFactor(_projectile.GetAccuracy()) * _projectile.bulletSpeed * VELOCITY_DEVIATION_FACTOR * 0.5f;
            Vector3 newVelocity = (_projectile.transform.forward * _projectile.bulletSpeed) + velocityDeviation;

            if (_projectile.rb.isKinematic)
            {
                _projectile.rb.MovePosition(_projectile.rb.position + newVelocity * Time.fixedDeltaTime);
            }
            else
            {
                _projectile.rb.velocity = newVelocity;
            }
        }

        if (_projectile.rb != null && !_projectile.rb.isKinematic)
        {
            if (_projectile.rb.velocity.magnitude < MIN_VELOCITY)
            {
                _projectile.rb.velocity = _projectile.rb.velocity.normalized * MIN_VELOCITY;
            }
        }
        else if (_projectile.rb != null && _projectile.rb.isKinematic)
        {
            Vector3 currentVelocity = (_projectile.rb.position - _lastPosition) / Time.fixedDeltaTime;
            if (currentVelocity.magnitude < MIN_VELOCITY)
            {
                Vector3 newVelocity = currentVelocity.normalized * MIN_VELOCITY;
                _projectile.rb.MovePosition(_projectile.rb.position + newVelocity * Time.fixedDeltaTime);
            }
        }

        _lastPosition = currentPosition;
    }

    private Vector3 ApplyInaccuracy(Vector3 direction)
    {
        if (_projectile.GetCurrentState() is EnemyShotState)
        {
            float inaccuracyFactor = CalculateInaccuracyFactor(_projectile.GetAccuracy());
            float inaccuracyAngle = inaccuracyFactor * MAX_INACCURACY_ANGLE;
            Quaternion randomRotation = Quaternion.AngleAxis(
                Random.Range(-inaccuracyAngle, inaccuracyAngle),
                Random.onUnitSphere
            );
            return randomRotation * direction;
        }
        return direction;
    }

    private float CalculateInaccuracyFactor(float accuracy)
    {
        return Mathf.Pow(1f - accuracy, INACCURACY_CURVE_STEEPNESS);
    }

    private float CalculateAccuracyFactor(float accuracy, float minFactor)
    {
        return Mathf.Lerp(minFactor, 1f, Mathf.Pow(accuracy, 1f / INACCURACY_CURVE_STEEPNESS));
    }

    private void UpdateCloseProximityBehavior(float distanceToTarget, float timeScale)
    {
        if (distanceToTarget <= CLOSE_RANGE && distanceToTarget > VERY_CLOSE_RANGE)
        {
            _closeProximityTimer += Time.deltaTime * timeScale;
            if (_closeProximityTimer >= CLOSE_PROXIMITY_TIME_THRESHOLD)
            {
                _aggressiveTurnAroundTriggered = true;
            }
        }
        else
        {
            _closeProximityTimer = 0f;
            _aggressiveTurnAroundTriggered = false;
        }
    }

    private void CheckIfPassedTarget(Vector3 currentPosition, Vector3 predictedPosition)
    {
        Vector3 toTarget = predictedPosition - _lastPosition;
        Vector3 toProjectile = currentPosition - _lastPosition;

        if (Vector3.Dot(toTarget, toProjectile) < 0)
        {
            _hasPassedTarget = true;
        }
        else
        {
            _hasPassedTarget = false;
        }
    }

    private float CalculateSpeedFactor(float distanceToTarget)
    {
        if (distanceToTarget <= CLOSE_RANGE)
        {
            return MIN_SPEED_FACTOR;
        }
        else if (distanceToTarget >= FAR_RANGE)
        {
            return 1f;
        }
        else
        {
            return Mathf.Lerp(
                MIN_SPEED_FACTOR,
                1f,
                (distanceToTarget - CLOSE_RANGE) / (FAR_RANGE - CLOSE_RANGE)
            );
        }
    }

    private void ResetProjectileMovement()
    {
        ConditionalDebug.Log("Projectile stuck detected. Resetting movement.");
        _projectile.homing = true;
        if (_projectile.rb.isKinematic)
        {
            _projectile.rb.MovePosition(_projectile.rb.position + _projectile.transform.forward * _projectile.bulletSpeed * Time.fixedDeltaTime);
        }
        else
        {
            _projectile.rb.velocity = _projectile.transform.forward * _projectile.bulletSpeed;
        }
        _stuckFrameCount = 0;
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

        float accuracy = _projectile.GetAccuracy();
        float predictionTime = Mathf.Lerp(
            MIN_ACCURACY_PREDICTION_TIME,
            MAX_ACCURACY_PREDICTION_TIME,
            Mathf.Pow(accuracy, 1f / INACCURACY_CURVE_STEEPNESS)
        );
        float varianceFactor =
            Random.Range(-MAX_PREDICTION_VARIANCE, MAX_PREDICTION_VARIANCE)
            * CalculateInaccuracyFactor(accuracy);
        float adjustedPredictionTime = predictionTime * (1f + varianceFactor);

        adjustedPredictionTime = Mathf.Min(adjustedPredictionTime, timeToReachTarget * 0.5f);

        Vector3 predictedPosition =
            targetPosition + _targetVelocity * (timeToReachTarget + adjustedPredictionTime);

        float approachVariationRadius = BASE_APPROACH_VARIATION_RADIUS * (2f - accuracy);

        Vector3 randomDirection = GenerateRandomApproachDirection(
            _projectile.transform.position,
            targetPosition
        );

        Vector3 offset = randomDirection * approachVariationRadius;
        predictedPosition += offset;

        return predictedPosition;
    }

    private Vector3 GenerateRandomApproachDirection(
        Vector3 projectilePosition,
        Vector3 targetPosition
    )
    {
        Vector3 toTarget = targetPosition - projectilePosition;
        Vector3 perpendicularDir = Vector3.Cross(toTarget, Vector3.up).normalized;

        float angle = Random.Range(0f, 360f - MAX_BEHIND_ANGLE * 2f) + MAX_BEHIND_ANGLE;

        Quaternion rotation = Quaternion.AngleAxis(angle, toTarget);

        return rotation * perpendicularDir;
    }

    public void OnPlayerRicochetDodge()
    {
        if (_projectile.GetCurrentState() is EnemyShotState)
        {
            _projectile.homing = false;
            Vector3 oppositeDirection = -_projectile.transform.forward;
            _projectile.rb.velocity = oppositeDirection * _projectile.bulletSpeed;
            Debug.Log("Projectile is now moving in the opposite direction due to Ricochet Dodge.");
        }
    }
}
