using DG.Tweening;
using UnityEngine;

public class PlayerLockedState : ProjectileState
{
    public PlayerLockedState(ProjectileStateBased projectile)
        : base(projectile)
    {
        if (_projectile.isParried)
            return;

        RecycleProjectile();
    }

    private void RecycleProjectile()
    {
        // Deactivate the GameObject
        _projectile.gameObject.SetActive(false);

        // Return the projectile to the pool
        ProjectilePool.Instance.ReturnProjectileToPool(_projectile);

        // Unregister the projectile from the ProjectileManager
        ProjectileManager.Instance.UnregisterProjectile(_projectile);

        ConditionalDebug.Log($"Projectile {_projectile.GetInstanceID()} recycled and returned to pool.");
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        ConditionalDebug.Log($"Projectile {_projectile.GetInstanceID()} entered PlayerLockedState and was recycled.");
    }

    public override void OnStateExit()
    {
        base.OnStateExit();
        // Clean up any remaining effects or references if needed
    }

    public override void OnTriggerEnter(Collider other)
    {
        // This should not be called, but log if it does
        ConditionalDebug.LogWarning($"OnTriggerEnter called on recycled projectile {_projectile.GetInstanceID()}");
    }

    public override void Update()
    {
        // This should not be called, but log if it does
        ConditionalDebug.LogWarning($"Update called on recycled projectile {_projectile.GetInstanceID()}");
    }
}
