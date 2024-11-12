using DG.Tweening;
using UnityEngine;

public class ProjectileVisualEffects
{
    private ProjectileStateBased projectile;
    private Material material;
    private Color originalColor;
    private float originalIllumination;

    public ProjectileVisualEffects(ProjectileStateBased projectile)
    {
        this.projectile = projectile;
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            material = renderer.material;
            if (material != null)
            {
                originalColor = material.color;
                originalIllumination = material.GetFloat("_SelfIllumination");
            }
        }
    }

    public void PlayDeathEffect(bool hitSomething = false)
    {
        if (material != null)
        {
            DOTween.To(
                () => material.GetFloat("_SelfIllumination"),
                x => material.SetFloat("_SelfIllumination", x),
                originalIllumination * 2f,
                0.1f
            ).OnComplete(() =>
            {
                DOTween.To(
                    () => material.GetFloat("_SelfIllumination"),
                    x => material.SetFloat("_SelfIllumination", x),
                    0f,
                    0.1f
                );
            });
        }
    }

    public void PlayImpactEffect(Collision collision)
    {
        if (material != null)
        {
            DOTween.To(
                () => material.GetFloat("_SelfIllumination"),
                x => material.SetFloat("_SelfIllumination", x),
                originalIllumination * 1.5f,
                0.05f
            ).OnComplete(() =>
            {
                DOTween.To(
                    () => material.GetFloat("_SelfIllumination"),
                    x => material.SetFloat("_SelfIllumination", x),
                    originalIllumination,
                    0.05f
                );
            });
        }
    }

    public void Cleanup()
    {
        if (material != null)
        {
            DOTween.Kill(material);
            material.color = originalColor;
            material.SetFloat("_SelfIllumination", originalIllumination);
        }
    }
}
