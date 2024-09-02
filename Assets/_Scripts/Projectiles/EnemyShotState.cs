using UnityEngine;
using BehaviorDesigner.Runtime.Tactical;
using BehaviorDesigner.Runtime.Tasks.Movement;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;

public class EnemyShotState : ProjectileState
{
    public EnemyShotState(ProjectileStateBased projectile) : base(projectile)
    {
        _projectile.currentTarget = GameObject.FindWithTag("Player Aim Target").transform;
        _projectile.minTurnRadius = 5f;
        _projectile.maxTurnRadius = 20f;
        _projectile.approachAngle = UnityEngine.Random.Range(15f, 30f);
        _projectile.turnRate = 180f;
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            IDamageable damageable = other.gameObject.GetComponent("PlayerHealth") as IDamageable;
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
        else
        {
            ConditionalDebug.Log(
                "Collided with an object "
                    + other.gameObject
                    + " at transform "
                    + other.gameObject.transform
                    + "and behaviour may not be expected"
            );
        }
    }
}