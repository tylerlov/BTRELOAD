using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ProjectileEffectManager : MonoBehaviour
{
    public static ProjectileEffectManager Instance { get; private set; }

    [SerializeField]
    private GameObject deathEffectPrefab;

    [SerializeField]
    private int initialDeathEffectPoolSize = 10;
    private const string deathEffectKey = "DeathEffect";

    [SerializeField]
    private GameObject enemyShotFXPrefab;

    [SerializeField]
    private int initialEnemyShotFXPoolSize = 10;
    private const string enemyShotFXKey = "EnemyShotFX";

    public GameObject projectileRadarSymbol;
    [SerializeField]
    private int radarSymbolPoolSize = 50;
    private Queue<GameObject> radarSymbolPool = new Queue<GameObject>();

    [SerializeField]
    private GameObject lockedFXPrefab;

    [SerializeField]
    private int initialLockedFXPoolSize = 10;
    private const string lockedFXKey = "LockedFX";

    [SerializeField]
    private float poolWarningThreshold = 0.8f;

    private Queue<MaterialPropertyBlock> propertyBlockPool = new Queue<MaterialPropertyBlock>();
    private const int PROPERTY_BLOCK_POOL_SIZE = 20;

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

        // Initialize property block pool
        for (int i = 0; i < PROPERTY_BLOCK_POOL_SIZE; i++)
        {
            propertyBlockPool.Enqueue(new MaterialPropertyBlock());
        }
    }

    private void Start()
    {
        InitializeAllPools();
    }

    public void InitializeAllPools()
    {
        ParticleSystemManager.Instance.RegisterParticleSystem(deathEffectKey, deathEffectPrefab, initialDeathEffectPoolSize);
        ParticleSystemManager.Instance.RegisterParticleSystem(enemyShotFXKey, enemyShotFXPrefab, initialEnemyShotFXPoolSize);
        ParticleSystemManager.Instance.RegisterParticleSystem(lockedFXKey, lockedFXPrefab, initialLockedFXPoolSize);
        InitializeRadarSymbolPool();
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

    public void PlayDeathEffect(Vector3 position)
    {
        ParticleSystem effect = ParticleSystemManager.Instance.PlayParticleSystem(deathEffectKey, position, Quaternion.identity);
        if (effect == null)
        {
            ConditionalDebug.LogWarning($"Death effect pool is empty. Consider increasing pool size.");
        }
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
        if (radarSymbol != null)
        {
            radarSymbol.SetActive(false);
            radarSymbol.transform.SetParent(transform);
            radarSymbolPool.Enqueue(radarSymbol);
        }
    }

    public VisualEffect GetLockedFXFromPool()
    {
        ParticleSystem lockedFX = ParticleSystemManager.Instance.PlayParticleSystem(lockedFXKey, Vector3.zero, Quaternion.identity);
        return lockedFX?.GetComponent<VisualEffect>();
    }

    public void ReturnLockedFXToPool(VisualEffect effect)
    {
        if (effect != null)
        {
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystemManager.Instance.StopAndReturnToPool(ps, lockedFXKey);
            }
        }
    }

    public GameObject CreateEnemyShotFX(Transform parent, Vector3 localPosition, Vector3 scale)
    {
        ParticleSystem enemyShotFX = ParticleSystemManager.Instance.PlayParticleSystem(enemyShotFXKey, parent.TransformPoint(localPosition), Quaternion.identity);
        if (enemyShotFX != null)
        {
            enemyShotFX.transform.SetParent(parent);
            enemyShotFX.transform.localPosition = localPosition;
            enemyShotFX.transform.localScale = scale;
            SetChildrenScale(enemyShotFX.gameObject, scale);
            return enemyShotFX.gameObject;
        }
        return null;
    }

    private void SetChildrenScale(GameObject parent, Vector3 scale)
    {
        foreach (Transform child in parent.transform)
        {
            child.localScale = scale;
            SetChildrenScale(child.gameObject, scale);
        }
    }

    public void ClearPools()
    {
        // This method is no longer needed as ParticleSystemManager handles the pools
    }

    public MaterialPropertyBlock GetPropertyBlock()
    {
        if (propertyBlockPool.Count == 0)
        {
            propertyBlockPool.Enqueue(new MaterialPropertyBlock());
        }
        return propertyBlockPool.Dequeue();
    }

    public void ReturnPropertyBlock(MaterialPropertyBlock block)
    {
        if (block != null)
        {
            propertyBlockPool.Enqueue(block);
        }
    }
}
