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

        if (_projectile.modelRenderer != null)
        {
            var propertyBlock = new MaterialPropertyBlock();
            _projectile.modelRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_AdvancedDissolveCutoutStandardClip", 0.05f);
            _projectile.modelRenderer.SetPropertyBlock(propertyBlock);
            
            DOVirtual.Float(1f, 0f, 0.1f, (value) => {
                propertyBlock.SetFloat("_Opacity", value);
                _projectile.modelRenderer.SetPropertyBlock(propertyBlock);
            }).OnComplete(() => {
                _projectile.gameObject.SetActive(false);
            });
        }
        else
        {
            _projectile.gameObject.SetActive(false);
        }
    }
}
