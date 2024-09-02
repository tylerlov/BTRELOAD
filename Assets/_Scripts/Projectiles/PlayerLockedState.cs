using UnityEngine;
using DG.Tweening;

public class PlayerLockedState : ProjectileState
{
    public PlayerLockedState(ProjectileStateBased projectile) : base(projectile)
    {
        if (_projectile.isParried)
            return;

        _projectile.SetLifetime(200f);

        _projectile.currentTarget = null;
        _projectile.homing = false;
        _projectile.tag = "LaunchableBulletLocked";

        _projectile.TLine.ResetRecording();
        _projectile.TLine.rewindable = false;
        _projectile.TLine.globalClockKey = "Test";

        if (ProjectileStateBased.shootingObject != null)
        {
            _projectile.transform.SetParent(ProjectileStateBased.shootingObject.transform, true);
            Vector3 newPosition =
                ProjectileStateBased.shootingObject.transform.position
                + ProjectileStateBased.shootingObject.transform.forward * 2f;
            _projectile.transform.position = newPosition;
            _projectile.transform.rotation = ProjectileStateBased.shootingObject.transform.rotation;

            if (_projectile.rb != null)
            {
                _projectile.rb.isKinematic = true;
            }
        }
        else
        {
            ConditionalDebug.LogError(
                "Shooting not found. Make sure there is a GameObject tagged 'Shooting' in the scene."
            );
        }

        _projectile.playerProjPath.enabled = true;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();

        if (_projectile.modelRenderer != null && _projectile.lockedStateMaterial != null)
        {
            _projectile.modelRenderer.material = _projectile.lockedStateMaterial;
        }
        else
        {
            Debug.LogError("ModelRenderer or LockedStateMaterial is not set on the projectile.");
        }

        _projectile.currentLockedFX = ProjectileEffectManager.Instance.GetLockedFXFromPool();
        if (_projectile.currentLockedFX != null)
        {
            _projectile.currentLockedFX.transform.SetParent(_projectile.transform);
            _projectile.currentLockedFX.transform.localPosition = Vector3.zero;
            _projectile.currentLockedFX.transform.localRotation = Quaternion.identity;
            _projectile.currentLockedFX.gameObject.SetActive(true);
            _projectile.currentLockedFX.Play();
        }
        else
        {
            Debug.LogError("LockedFX VisualEffect could not be obtained from the pool.");
        }
    }

    public override void OnStateExit()
    {
        base.OnStateExit();

        if (_projectile.modelRenderer != null && _projectile.myMaterial != null)
        {
            _projectile.modelRenderer.material = _projectile.myMaterial;
        }
        else
        {
            Debug.LogError("ModelRenderer or myMaterial is not set on the projectile.");
        }

        if (_projectile.currentLockedFX != null)
        {
            ProjectileEffectManager.Instance.ReturnLockedFXToPool(_projectile.currentLockedFX);
            _projectile.currentLockedFX = null;
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        ConditionalDebug.Log("Player Locked State and hit something");
    }

    public override void Update()
    {
        Debug.Log("Player Locked State Update is happening");
    }

    public void LaunchBack()
    {
        if (ProjectileStateBased.shootingObject != null)
        {
            if (_projectile.rb != null)
            {
                _projectile.rb.isKinematic = false;
            }
            Vector3 launchDirection = ProjectileStateBased.shootingObject.transform.forward;

            _projectile.transform.parent = null;

            _projectile.homing = false;

            _projectile.transform.rotation = Quaternion.LookRotation(launchDirection);

            _projectile.rb.velocity = launchDirection * _projectile.bulletSpeed;

            if (_projectile.tRn != null)
            {
                _projectile.tRn.DOTime(5, 2);
            }

            PlayerShotState newState = new PlayerShotState(_projectile, 1f);
            _projectile.ChangeState(newState);
        }
        else
        {
            ConditionalDebug.LogError("Shooting object is not assigned.");
        }
    }

    public void LaunchAtEnemy(Transform target)
    {
        if (_projectile.rb != null)
        {
            _projectile.rb.isKinematic = false;
        }
        _projectile.transform.parent = null;
        _projectile.currentTarget = target;
        _projectile.transform.LookAt(target);

        ConditionalDebug.Log("Launch at enemy successfully called on " + target);

        _projectile.homing = true;

        PlayerShotState newState = new PlayerShotState(_projectile, 1f);
        _projectile.ChangeState(newState);
    }
}