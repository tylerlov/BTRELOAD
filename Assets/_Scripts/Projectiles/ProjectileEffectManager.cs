using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ProjectileEffectManager : MonoBehaviour
{
    public static ProjectileEffectManager Instance { get; private set; }

    [SerializeField]
    private ParticleSystem deathEffectPrefab;

    [SerializeField]
    private int initialDeathEffectPoolSize = 10;
    private Queue<ParticleSystem> deathEffectPool = new Queue<ParticleSystem>(50);

    [SerializeField]
    private GameObject enemyShotFXPrefab;

    public GameObject projectileRadarSymbol;

    [SerializeField]
    private int radarSymbolPoolSize = 50;
    private Queue<GameObject> radarSymbolPool = new Queue<GameObject>(50);

    [SerializeField]
    private VisualEffect lockedFXPrefab;

    [SerializeField]
    private int initialLockedFXPoolSize = 10;
    private Queue<VisualEffect> lockedFXPool = new Queue<VisualEffect>();

    [SerializeField]
    private int maxDeathEffectPoolSize = 50;
    [SerializeField]
    private float poolWarningThreshold = 0.8f;

    private void Start()
    {
        InitializeDeathEffectPool();
        InitializeRadarSymbolPool();
        InitializeLockedFXPool();
    }

    public void InitializeAllPools()
    {
        InitializeDeathEffectPool();
        InitializeRadarSymbolPool();
        InitializeLockedFXPool();
    }

    private void InitializeDeathEffectPool()
    {
        for (int i = 0; i < initialDeathEffectPoolSize; i++)
        {
            CreateAndAddDeathEffectToPool();
        }
    }

    private ParticleSystem CreateAndAddDeathEffectToPool()
    {
        ParticleSystem effect = Instantiate(deathEffectPrefab, transform);
        DisableEffect(effect);
        deathEffectPool.Enqueue(effect);
        return effect;
    }

    private void InitializeRadarSymbolPool()
    {
        for (int i = 0; i < radarSymbolPoolSize; i++)
        {
            GameObject radarSymbol = Instantiate(projectileRadarSymbol, transform);
            radarSymbol.SetActive(false);
            radarSymbolPool.Enqueue(radarSymbol);
        }
    }

    private void InitializeLockedFXPool()
    {
        for (int i = 0; i < initialLockedFXPoolSize; i++)
        {
            VisualEffect effect = Instantiate(lockedFXPrefab, transform);
            effect.gameObject.SetActive(false);
            lockedFXPool.Enqueue(effect);
        }
    }

    private void SetChildrenScale(GameObject parent, Vector3 scale)
    {
        foreach (Transform child in parent.transform)
        {
            child.localScale = scale;
            SetChildrenScale(child.gameObject, scale);
        }
    }

    public void PlayDeathEffect(Vector3 position)
    {
        ParticleSystem effect;
        if (deathEffectPool.Count == 0)
        {
            if (transform.childCount < maxDeathEffectPoolSize)
            {
                effect = CreateAndAddDeathEffectToPool();
                ConditionalDebug.LogWarning($"Death effect pool empty. Created new effect. Current pool size: {transform.childCount}");
            }
            else
            {
                ConditionalDebug.LogError("Maximum death effect pool size reached. Cannot create more effects.");
                return;
            }
        }
        else
        {
            effect = deathEffectPool.Dequeue();
        }

        effect.transform.position = position;
        EnableAndPlayEffect(effect);

        StartCoroutine(ReturnEffectToPoolAfterFinished(effect));

        // Check if pool is close to empty
        if ((float)deathEffectPool.Count / maxDeathEffectPoolSize < (1 - poolWarningThreshold))
        {
            ConditionalDebug.LogWarning($"Death effect pool is running low. Current count: {deathEffectPool.Count}");
        }
    }

    private IEnumerator ReturnEffectToPoolAfterFinished(ParticleSystem effect)
    {
        float checkInterval = 0.5f; // Check every half second
        while (effect.IsAlive(true))
        {
            yield return new WaitForSeconds(checkInterval);
        }
        DisableEffect(effect);
        deathEffectPool.Enqueue(effect);
    }

    private void EnableAndPlayEffect(ParticleSystem effect)
    {
        effect.gameObject.SetActive(true);
        var mainModule = effect.main;
        mainModule.stopAction = ParticleSystemStopAction.None;
        effect.Clear();
        effect.Play();
    }

    private void DisableEffect(ParticleSystem effect)
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var mainModule = effect.main;
        mainModule.stopAction = ParticleSystemStopAction.Disable;
        effect.gameObject.SetActive(false);
    }

    public GameObject GetRadarSymbolFromPool()
    {
        if (radarSymbolPool.Count > 0)
        {
            GameObject radarSymbol = radarSymbolPool.Dequeue();
            radarSymbol.SetActive(true);
            return radarSymbol;
        }
        return null;
    }

    public void ReturnRadarSymbolToPool(GameObject radarSymbol)
    {
        radarSymbol.SetActive(false);
        radarSymbol.transform.SetParent(transform);
        radarSymbolPool.Enqueue(radarSymbol);
    }

    public VisualEffect GetLockedFXFromPool()
    {
        if (lockedFXPool.Count == 0)
        {
            VisualEffect newEffect = Instantiate(lockedFXPrefab, transform);
            return newEffect;
        }
        return lockedFXPool.Dequeue();
    }

    public void ReturnLockedFXToPool(VisualEffect effect)
    {
        effect.Stop();
        effect.gameObject.SetActive(false);
        effect.transform.SetParent(transform);
        lockedFXPool.Enqueue(effect);
    }

    public void ClearPools()
    {
        deathEffectPool.Clear();
        radarSymbolPool.Clear();
        lockedFXPool.Clear();
    }

    public GameObject CreateEnemyShotFX(Transform parent, Vector3 localPosition, Vector3 scale)
    {
        if (enemyShotFXPrefab != null)
        {
            GameObject enemyShotFX = Instantiate(enemyShotFXPrefab, parent);
            enemyShotFX.transform.localPosition = localPosition;
            enemyShotFX.transform.localScale = scale;
            SetChildrenScale(enemyShotFX, scale);
            enemyShotFX.SetActive(true);
            return enemyShotFX;
        }
        return null;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
