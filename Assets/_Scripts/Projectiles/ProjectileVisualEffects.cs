using DG.Tweening;
using UnityEngine;

public class ProjectileVisualEffects
{
    private ProjectileStateBased _projectile;

    public ProjectileVisualEffects(ProjectileStateBased projectile)
    {
        _projectile = projectile;
    }

    public void UpdateVisuals()
    {
        // This method can be expanded to handle any continuous visual updates
    }

    public void PlayDeathEffect(bool hitSomething)
    {
        if (hitSomething)
        {
            ProjectileEffectManager.Instance.PlayDeathEffect(_projectile.transform.position);
        }

        if (_projectile.playerProjPath != null)
        {
            _projectile.playerProjPath.enabled = false;
        }

        if (
            _projectile.myMaterial != null
            && _projectile.myMaterial.HasProperty("_AdvancedDissolveCutoutStandardClip")
        )
        {
            _projectile
                .myMaterial.DOFloat(0.05f, "_AdvancedDissolveCutoutStandardClip", 0.1f)
                .OnComplete(() =>
                {
                    _projectile.gameObject.SetActive(false);
                });
        }
        else
        {
            _projectile.gameObject.SetActive(false);
        }
    }
}
