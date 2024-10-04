
public class ProjectileCombat
{
    private ProjectileStateBased _projectile;

    public ProjectileCombat(ProjectileStateBased projectile)
    {
        _projectile = projectile;
    }

    public void ApplyDamage(IDamageable target)
    {
        float finalDamage = _projectile.damageAmount * _projectile.damageMultiplier;
        target.Damage(finalDamage);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _projectile.damageMultiplier = multiplier;
    }
}
