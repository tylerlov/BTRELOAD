using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tactical;
using Chronos;
using FMODUnity;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class SnakeData
{
    public GameObject snakeObject;
    public Animator animator;
    public Transform projectileOrigin;
    public float currentHealth;
    public Timeline timeline;
    public string clockName;
}

[RequireComponent(typeof(Timeline))]
public class EnemyTwinSnakeBoss : MonoBehaviour, ILimbDamageReceiver
{
    [Header("Snake Data")]
    [SerializeField]
    private SnakeData[] snakes = new SnakeData[2];

    // Basic Enemy Information
    [Header("Basic Enemy Information")]
    [SerializeField]
    private string enemyType;

    [SerializeField]
    private float startHealth = 100;

    // Shooting Functionality
    [Header("Shooting Functionality")]
    [SerializeField]
    private float shootSpeed = 20f;

    [SerializeField]
    private float projectileLifetime = 5f;

    [SerializeField]
    private float projectileScale = 1f;

    [SerializeField]
    private Material alternativeProjectileMaterial;

    [SerializeField]
    private float noKoreoShootInterval = 2f;
    public float shootDelay = 0.5f;

    // Animation Triggers
    [Header("Animation Triggers")]
    [SerializeField]
    private string attackLeftCondition = "Attack Left";

    [SerializeField]
    private string attackRightCondition = "Attack Right";

    // Koreographer Shooting
    [Header("Koreographer Shooting")]
    [SerializeField]
    private string[] shootEventIDs = new string[1];

    [EventID]
    public string activeShootEventID;

    private Clock[] clocks = new Clock[2];

    public Timeline MyTime
    {
        get { return snakes[0].timeline; }
        set { snakes[0].timeline = value; }
    }

    public float ShootInterval => noKoreoShootInterval;

    private int currentShootingSnakeIndex = 0;

    private ProjectileManager projectileManager;

    private Crosshair crosshair;

    private GameObject playerTarget;

    [Header("Eye Colliders")]
    private List<ColliderHitCallback> leftSnakeEyes = new List<ColliderHitCallback>();
    private List<ColliderHitCallback> rightSnakeEyes = new List<ColliderHitCallback>();

    private void Awake()
    {
        if (shootEventIDs.Length > 0)
        {
            activeShootEventID = shootEventIDs[0];
        }
    }

    private void OnEnable()
    {
        InitializeEnemy();
        if (!string.IsNullOrEmpty(activeShootEventID))
        {
            ConditionalDebug.Log($"Registering for Koreographer event: {activeShootEventID}");
            Koreographer.Instance.RegisterForEvents(activeShootEventID, OnMusicalShoot);
        }
        else
        {
            ConditionalDebug.LogWarning("activeShootEventID is empty or null");
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(activeShootEventID))
        {
            Koreographer.Instance.UnregisterForEvents(activeShootEventID, OnMusicalShoot);
        }
    }

    private void Start()
    {
        SetupEnemy();
        projectileManager = ProjectileManager.Instance;
        if (projectileManager == null)
        {
            ConditionalDebug.LogError("ProjectileManager not found!");
        }

        // Find and store the Crosshair reference
        crosshair = FindObjectOfType<Crosshair>();
        if (crosshair == null)
        {
            ConditionalDebug.LogError("Crosshair not found!");
        }

        // Initialize clocks
        for (int i = 0; i < snakes.Length; i++)
        {
            clocks[i] = Timekeeper.instance.Clock(snakes[i].clockName);
        }

        // Subscribe to rewind events
        if (crosshair != null)
        {
            crosshair.OnRewindStart += HandleRewindStart;
            crosshair.OnRewindEnd += HandleRewindEnd;
        }

        playerTarget = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnDestroy()
    {
        // Unsubscribe from rewind events
        if (crosshair != null)
        {
            crosshair.OnRewindStart -= HandleRewindStart;
            crosshair.OnRewindEnd -= HandleRewindEnd;
        }
    }

    private void HandleRewindStart(float timeScale)
    {
        int targetSnakeIndex = DetermineTargetSnake();
        if (targetSnakeIndex != -1)
        {
            ApplyTimeScaleToSnake(targetSnakeIndex, timeScale);
        }
    }

    private void HandleRewindEnd()
    {
        // Reset time scale for both snakes
        for (int i = 0; i < snakes.Length; i++)
        {
            ApplyTimeScaleToSnake(i, 1f);
        }
    }

    private int DetermineTargetSnake()
    {
        if (crosshair == null)
            return -1;

        Vector3 aimPoint = crosshair.RaycastTarget();
        float closestDistance = float.MaxValue;
        int closestSnakeIndex = -1;

        for (int i = 0; i < snakes.Length; i++)
        {
            float distance = Vector3.Distance(aimPoint, snakes[i].snakeObject.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSnakeIndex = i;
            }
        }

        return closestSnakeIndex;
    }

    private void ApplyTimeScaleToSnake(int snakeIndex, float timeScale)
    {
        if (snakeIndex >= 0 && snakeIndex < snakes.Length)
        {
            clocks[snakeIndex].localTimeScale = timeScale;
        }
    }

    public bool HasShootEventID() => !string.IsNullOrEmpty(activeShootEventID);

    public bool IsAlive() => snakes[0].currentHealth > 0 || snakes[1].currentHealth > 0;

    public void Damage(float amount, int snakeIndex)
    {
        if (snakeIndex >= 0 && snakeIndex < snakes.Length)
        {
            HandleDamage(amount, snakeIndex);
        }
    }

    private void InitializeEnemy()
    {
        for (int i = 0; i < snakes.Length; i++)
        {
            snakes[i].currentHealth = startHealth;
            snakes[i].clockName = $"Boss Time {i + 1}";
            clocks[i] = Timekeeper.instance.Clock(snakes[i].clockName);
        }
    }

    private void SetupEnemy()
    {
        for (int i = 0; i < snakes.Length; i++)
        {
            snakes[i].timeline = snakes[i].snakeObject.GetComponent<Timeline>();
        }
    }

    private void HandleDamage(float amount, int snakeIndex)
    {
        snakes[snakeIndex].currentHealth = Mathf.Max(snakes[snakeIndex].currentHealth - amount, 0);

        if (snakes[snakeIndex].currentHealth <= 0)
        {
            StartCoroutine(Death(snakeIndex));
        }
    }

    public void DamageFromLimb(string limbName, float amount)
    {
        ConditionalDebug.Log($"Damaged limb: {limbName}");
        int snakeIndex = DetermineSnakeIndex(limbName);
        HandleDamage(amount, snakeIndex);
        UpdateEyeLockStatus(snakeIndex, false);
    }

    private int DetermineSnakeIndex(string limbName)
    {
        return limbName.Contains("Snake1") ? 0 : 1;
    }

    private void UpdateEyeLockStatus(int snakeIndex, bool isLocked)
    {
        List<ColliderHitCallback> eyesToUpdate = snakeIndex == 0 ? leftSnakeEyes : rightSnakeEyes;
        foreach (var eye in eyesToUpdate)
        {
            if (eye != null)
            {
                eye.SetLockedStatus(isLocked);
            }
        }
    }

    public void LockOnEye(string eyeName)
    {
        int snakeIndex = DetermineSnakeIndex(eyeName);
        UpdateEyeLockStatus(snakeIndex, true);
    }

    public void UnlockEye(string eyeName)
    {
        int snakeIndex = DetermineSnakeIndex(eyeName);
        UpdateEyeLockStatus(snakeIndex, false);
    }

    public void RegisterEye(ColliderHitCallback eye)
    {
        string eyeName = eye.gameObject.name.ToLower();
        if (eyeName.Contains("snake1") || eyeName.Contains("left"))
        {
            leftSnakeEyes.Add(eye);
        }
        else if (eyeName.Contains("snake2") || eyeName.Contains("right"))
        {
            rightSnakeEyes.Add(eye);
        }
        else
        {
            Debug.LogWarning($"Unable to determine which snake the eye '{eyeName}' belongs to.");
        }
    }

    private void DisableEyeColliders(int snakeIndex)
    {
        List<ColliderHitCallback> eyesToDisable = snakeIndex == 0 ? leftSnakeEyes : rightSnakeEyes;
        foreach (var eye in eyesToDisable)
        {
            if (eye != null)
            {
                eye.gameObject.SetActive(false);
            }
        }
    }

    public void ShootProjectile()
    {
        if (string.IsNullOrEmpty(activeShootEventID))
        {
            for (int i = 0; i < snakes.Length; i++)
            {
                StartCoroutine(DelayedShoot(i));
            }
        }
    }

    private IEnumerator DelayedShoot(int snakeIndex)
    {
        ConditionalDebug.Log($"DelayedShoot started for snake {snakeIndex}");

        // Set the appropriate attack condition based on the snake index
        string attackCondition = (snakeIndex == 0) ? attackLeftCondition : attackRightCondition;
        snakes[snakeIndex].animator.SetBool(attackCondition, true);

        yield return new WaitForSeconds(shootDelay);

        // Reset the attack condition
        snakes[snakeIndex].animator.SetBool(attackCondition, false);

        if (projectileManager == null)
        {
            ConditionalDebug.LogError("ProjectileManager instance not found.");
            yield break;
        }

        ConditionalDebug.Log($"Shooting projectile for snake {snakeIndex}");
        if (playerTarget != null && playerTarget.activeInHierarchy)
        {
            Vector3 targetPosition = playerTarget.transform.position;
            Quaternion rotationTowardsTarget = Quaternion.LookRotation(
                targetPosition - snakes[snakeIndex].projectileOrigin.position
            );

            // Use ProjectileManager to shoot
            projectileManager.ShootProjectileFromEnemy(
                snakes[snakeIndex].projectileOrigin.position,
                rotationTowardsTarget,
                shootSpeed,
                projectileLifetime,
                projectileScale,
                enableHoming: true,
                alternativeProjectileMaterial,
                snakes[snakeIndex].clockName
            );

            ConditionalDebug.Log($"Snake {snakeIndex} fired a projectile");
        }
        else
        {
            ConditionalDebug.LogWarning("Player target not found or inactive.");
        }
    }

    private void OnMusicalShoot(KoreographyEvent evt)
    {
        ConditionalDebug.Log("OnMusicalShoot triggered");
        ShootProjectileAlternating();
    }

    private void ShootProjectileAlternating()
    {
        ConditionalDebug.Log(
            $"ShootProjectileAlternating called for snake {currentShootingSnakeIndex}"
        );
        StartCoroutine(DelayedShoot(currentShootingSnakeIndex));
        currentShootingSnakeIndex = (currentShootingSnakeIndex + 1) % snakes.Length;
    }

    private IEnumerator Death(int snakeIndex)
    {
        snakes[snakeIndex].animator.SetTrigger("Die");

        yield return new WaitUntil(
            () => snakes[snakeIndex].animator.GetCurrentAnimatorStateInfo(0).IsName("Death")
        );
        float animationLength = snakes[snakeIndex].animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);

        FMODUnity.RuntimeManager.PlayOneShot(
            "event:/Enemy/" + enemyType + "/Death",
            snakes[snakeIndex].snakeObject.transform.position
        );

        if (!IsAlive())
        {
            ConditionalDebug.Log("Both Twin Snakes are defeated!");
            // Call to GameManager or any other logic to handle the defeat of both snakes
        }

        // Disable all eye colliders for the defeated snake
        UpdateEyeLockStatus(snakeIndex, false);
        DisableEyeColliders(snakeIndex);
    }

    public void UpdateActiveShootEventID(int index)
    {
        if (index >= 0 && index < shootEventIDs.Length)
        {
            if (!string.IsNullOrEmpty(activeShootEventID))
            {
                Koreographer.Instance.UnregisterForEvents(activeShootEventID, OnMusicalShoot);
            }

            activeShootEventID = shootEventIDs[index];

            if (!string.IsNullOrEmpty(activeShootEventID))
            {
                Koreographer.Instance.RegisterForEvents(activeShootEventID, OnMusicalShoot);
            }
        }
        else
        {
            ConditionalDebug.LogError("Invalid shootEventID index.");
        }
    }
}
