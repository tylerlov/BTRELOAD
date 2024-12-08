using UnityEngine;

public class DebugProjectileShooter : MonoBehaviour
{
    private PlayerLocking playerLocking;
    private PlayerShooting playerShooting;
    private CrosshairCore crosshairCore;

    [SerializeField] private float debugProjectileDamage = 10f;
    [SerializeField] private float debugProjectileSpeed = 50f;
    [SerializeField] private float debugProjectileLifetime = 5f;

    private void Start()
    {
        FindRequiredComponents();
        ConditionalDebug.Log($"PlayerLocking: {playerLocking != null}, PlayerShooting: {playerShooting != null}, CrosshairCore: {crosshairCore != null}");
    }

    private void FindRequiredComponents()
    {
        playerLocking = FindFirstObjectByType<PlayerLocking>();
        playerShooting = FindFirstObjectByType<PlayerShooting>();
        crosshairCore = FindFirstObjectByType<CrosshairCore>();

        if (playerLocking == null || playerShooting == null || crosshairCore == null)
        {
            ConditionalDebug.LogError("DebugProjectileShooter: One or more required components not found in the scene!");
            enabled = false;
        }
        else
        {
            ConditionalDebug.Log("DebugProjectileShooter: All required components found successfully.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ConditionalDebug.Log("M key pressed");
            ShootDebugProjectile();
        }
    }

    private void ShootDebugProjectile()
    {
        ConditionalDebug.Log("ShootDebugProjectile called");

        // Get a projectile from the pool
        ProjectileStateBased newProjectile = ProjectilePool.Instance.GetProjectile();
        if (newProjectile != null)
        {
            // Set up the projectile
            SetupDebugProjectile(newProjectile);

            // Simulate shooting the projectile
            playerShooting.HandleShootingEffects();

            ConditionalDebug.Log("Debug projectile shot!");
        }
        else
        {
            ConditionalDebug.LogWarning("Failed to get projectile from pool for debug shooting.");
        }
    }

    private void SetupDebugProjectile(ProjectileStateBased projectile)
    {
        if (ProjectileStateBased.shootingObject != null)
        {
            projectile.transform.position = ProjectileStateBased.shootingObject.transform.position;
            projectile.transform.rotation = crosshairCore.RaySpawn.transform.rotation;

            projectile.SetupProjectile(debugProjectileDamage, debugProjectileSpeed, debugProjectileLifetime, false, 1f, null, false);
            projectile.ChangeState(new PlayerShotState(projectile, 1f, null, false));
            ProjectileManager.Instance.RegisterProjectile(projectile);

            // Ensure the projectile is active and visible
            projectile.gameObject.SetActive(true);

            // Set the tag to 'LaunchableBullet'
            projectile.gameObject.tag = "LaunchableBullet";

            // Set up the collider and rigidbody
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = true;
            }
            else
            {
                ConditionalDebug.LogWarning("Projectile does not have a Collider component.");
            }

            // Set the velocity directly and ensure the Rigidbody is not kinematic
            if (projectile.rb != null)
            {
                projectile.rb.isKinematic = false; // This line is crucial
                projectile.rb.linearVelocity = crosshairCore.RaySpawn.transform.forward * debugProjectileSpeed;
                projectile.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            // Add debug ray
            Debug.DrawRay(projectile.transform.position, projectile.transform.forward * 100f, Color.red, 5f);
            ConditionalDebug.Log($"Debug projectile fired from {projectile.transform.position} in direction {projectile.transform.forward}");

            ConditionalDebug.Log($"Debug projectile set up at position {projectile.transform.position} with velocity {projectile.rb.linearVelocity}, isKinematic: {projectile.rb.isKinematic}");
        }
        else
        {
            ConditionalDebug.LogError("Shooting object is not assigned.");
        }
    }
}