using UnityEngine;

public abstract class ProjectileState
{
    protected ProjectileStateBased _projectile;

    public ProjectileState(ProjectileStateBased projectile)
    {
        _projectile = projectile;
    }

    public virtual void FixedUpdate(float timeScale) { }

    public virtual void OnTriggerEnter(Collider other) { }

    public virtual void OnCollisionEnter(Collision collision) { }

    public virtual void OnDeath() { }

    public virtual void OnStateEnter() { }

    public virtual void OnStateExit() { }

    public virtual void Update() { }

    public virtual void CustomUpdate(float timeScale) { }

    public virtual void UpdatePosition()
    {
        if (_projectile != null && _projectile.rb != null)
        {
            _projectile.transform.position += _projectile.rb.linearVelocity * Time.deltaTime;
        }
    }

    public ProjectileStateBased GetProjectile()
    {
        return _projectile;
    }
}
