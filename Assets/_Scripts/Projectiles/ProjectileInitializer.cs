using UnityEngine;

[DefaultExecutionOrder(-200)] // Ensure this runs before other scripts
public class ProjectileInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Initialize the projectile without using MaterialPropertyBlock
        var projectile = GetComponent<ProjectileStateBased>();
        if (projectile != null)
        {
            projectile.InitializeProjectile();
        }
    }
}
