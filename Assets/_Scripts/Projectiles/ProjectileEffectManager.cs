using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProjectileEffectManager : MonoBehaviour
{
    public static ProjectileEffectManager Instance { get; private set; }

    public enum EffectType
    {
        ProjectileDeath,
        EnemyShot,
        Locked,
        RadarSymbol
    }

    [System.Serializable]
    public class EffectConfig
    {
        public EffectType effectType;
        public GameObject effectPrefab;
        public float minTimeBetweenSpawns = 0.01f;
        public int maxConcurrentInstances = 200;
        public int poolSize = 200;
        public Material effectMaterial;
    }

    [Header("Effect Configurations")]
    [SerializeField] private EffectConfig[] effectConfigs;
    [SerializeField] private bool enableSpawnLimits = true;
    
    [Header("Position Control")]
    [SerializeField] private float minDistanceBetweenEffects = 0.05f;
    [SerializeField] private float positionCheckTimeWindow = 0.05f;

    private Dictionary<EffectType, Queue<GameObject>> effectPools;
    private Dictionary<EffectType, float> lastEffectSpawnTimes;
    private Dictionary<EffectType, int> activeEffectCounts;
    private List<EffectPositionData> recentEffectPositions;
    private Queue<MaterialPropertyBlock> propertyBlockPool;
    private Dictionary<GameObject, MaterialPropertyBlock> activePropertyBlocks;

    private struct EffectPositionData
    {
        public Vector3 position;
        public float time;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSystem()
    {
        effectPools = new Dictionary<EffectType, Queue<GameObject>>();
        lastEffectSpawnTimes = new Dictionary<EffectType, float>();
        activeEffectCounts = new Dictionary<EffectType, int>();
        recentEffectPositions = new List<EffectPositionData>();
        propertyBlockPool = new Queue<MaterialPropertyBlock>();
        activePropertyBlocks = new Dictionary<GameObject, MaterialPropertyBlock>();

        InitializeAllPools();
    }

    public void InitializeAllPools()
    {
        foreach (var config in effectConfigs)
        {
            InitializePool(config);
            lastEffectSpawnTimes[config.effectType] = 0f;
            activeEffectCounts[config.effectType] = 0;
        }

        // Initialize property block pool
        for (int i = 0; i < 100; i++)
        {
            propertyBlockPool.Enqueue(new MaterialPropertyBlock());
        }
    }

    public void ClearPools()
    {
        foreach (var pool in effectPools.Values)
        {
            foreach (var obj in pool)
            {
                if (obj != null) Destroy(obj);
            }
            pool.Clear();
        }
        effectPools.Clear();
        propertyBlockPool.Clear();
        activePropertyBlocks.Clear();
    }

    public MaterialPropertyBlock GetPropertyBlock()
    {
        return propertyBlockPool.Count > 0 ? propertyBlockPool.Dequeue() : new MaterialPropertyBlock();
    }

    public void ReturnPropertyBlock(GameObject target)
    {
        if (activePropertyBlocks.TryGetValue(target, out MaterialPropertyBlock block))
        {
            propertyBlockPool.Enqueue(block);
            activePropertyBlocks.Remove(target);
        }
    }

    public GameObject CreateEnemyShotFX(Vector3 position, Quaternion rotation)
    {
        var fx = SpawnEffectObject(EffectType.EnemyShot, position, rotation);
        return fx;
    }

    public void ReturnEnemyShotFXToPool(GameObject fx)
    {
        ReturnEffectToPool(fx, EffectType.EnemyShot);
    }

    public GameObject GetRadarSymbolFromPool()
    {
        return SpawnEffectObject(EffectType.RadarSymbol, Vector3.zero, Quaternion.identity);
    }

    public void ReturnRadarSymbolToPool(GameObject symbol)
    {
        ReturnEffectToPool(symbol, EffectType.RadarSymbol);
    }

    public void ReturnLockedFXToPool(GameObject fx)
    {
        ReturnEffectToPool(fx, EffectType.Locked);
    }

    private GameObject SpawnEffectObject(EffectType type, Vector3 position, Quaternion rotation)
    {
        if (!effectPools.ContainsKey(type) || effectPools[type].Count == 0)
            return null;

        var effect = effectPools[type].Dequeue();
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.SetActive(true);
        TrackEffectSpawn(type);
        return effect;
    }

    private void ReturnEffectToPool(GameObject effect, EffectType type)
    {
        if (effect == null || !effectPools.ContainsKey(type)) return;

        effect.SetActive(false);
        effectPools[type].Enqueue(effect);
        OnEffectComplete(type);
    }

    public void PlayEffect(EffectType effectType, Vector3 position, Quaternion rotation = default)
    {
        var config = effectConfigs.FirstOrDefault(c => c.effectType == effectType);
        if (config == null)
        {
            Debug.LogWarning($"No configuration found for effect: {effectType}");
            return;
        }

        if (IsPositionTooClose(position)) return;
        if (!CanSpawnEffect(effectType)) return;

        if (effectPools[effectType].Count > 0)
        {
            var effect = effectPools[effectType].Dequeue();
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.SetActive(true);
            
            StartCoroutine(ReturnToPool(effect, effectType, 2f));
            TrackEffectSpawn(effectType);
        }
    }

    private System.Collections.IEnumerator ReturnToPool(GameObject effect, EffectType effectType, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (effect != null)
        {
            effect.SetActive(false);
            effectPools[effectType].Enqueue(effect);
            OnEffectComplete(effectType);
        }
    }

    private bool IsPositionTooClose(Vector3 position)
    {
        float currentTime = Time.time;
        recentEffectPositions.RemoveAll(p => currentTime - p.time > positionCheckTimeWindow);

        foreach (var data in recentEffectPositions)
        {
            if (Vector3.Distance(position, data.position) < minDistanceBetweenEffects)
            {
                return true;
            }
        }

        recentEffectPositions.Add(new EffectPositionData 
        { 
            position = position, 
            time = currentTime 
        });

        return false;
    }

    private bool CanSpawnEffect(EffectType effectType)
    {
        if (!enableSpawnLimits) return true;

        var config = effectConfigs.FirstOrDefault(c => c.effectType == effectType);
        if (config == null) return false;

        float timeSinceLastSpawn = Time.time - lastEffectSpawnTimes[effectType];
        return timeSinceLastSpawn >= config.minTimeBetweenSpawns && 
               activeEffectCounts[effectType] < config.maxConcurrentInstances;
    }

    private void TrackEffectSpawn(EffectType effectType)
    {
        lastEffectSpawnTimes[effectType] = Time.time;
        activeEffectCounts[effectType]++;
    }

    private void OnEffectComplete(EffectType effectType)
    {
        activeEffectCounts[effectType] = Mathf.Max(0, activeEffectCounts[effectType] - 1);
    }

    public void ResetEffectCounts()
    {
        foreach (var key in activeEffectCounts.Keys.ToList())
        {
            activeEffectCounts[key] = 0;
        }
    }

    // Helper methods
    public void PlayDeathEffect(Vector3 position) => 
        PlayEffect(EffectType.ProjectileDeath, position);

    public void PlayEnemyShotEffect(Vector3 position, Quaternion rotation) => 
        PlayEffect(EffectType.EnemyShot, position, rotation);

    public void PlayLockedEffect(Vector3 position) => 
        PlayEffect(EffectType.Locked, position);

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            ClearPools();
        }
    }

    private void InitializePool(EffectConfig config)
    {
        Queue<GameObject> pool = new Queue<GameObject>();
        for (int i = 0; i < config.poolSize; i++)
        {
            GameObject obj = Instantiate(config.effectPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        effectPools[config.effectType] = pool;
    }
}
