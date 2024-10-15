using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticleSystemManager : MonoBehaviour
{
    private static ParticleSystemManager _instance;
    public static ParticleSystemManager Instance => _instance;

    [System.Serializable]
    public class ParticleSystemInfo
    {
        public string key;
        public int poolSize;
        public int activeCount;
        [Range(0.1f, 1f)]
        public float performanceScale = 1f;
    }

    [SerializeField] private List<ParticleSystemInfo> particleSystemInfos = new List<ParticleSystemInfo>();

    private Dictionary<string, Queue<ParticleSystem>> particlePools = new Dictionary<string, Queue<ParticleSystem>>();
    private Dictionary<string, ParticleSystem> particlePrefabs = new Dictionary<string, ParticleSystem>();

    [SerializeField] private bool logPerformanceImpact = false;
    [SerializeField] [Range(0.1f, 1f)] private float globalPerformanceScale = 1f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterParticleSystem(string key, GameObject particlePrefab, int initialPoolSize)
    {
        if (!particlePools.ContainsKey(key))
        {
            ParticleSystem prefabPS = particlePrefab.GetComponent<ParticleSystem>();
            if (prefabPS == null)
            {
                Debug.LogError($"Particle prefab {particlePrefab.name} does not have a ParticleSystem component.");
                return;
            }

            GameObject poolContainer = new GameObject($"{key}Pool");
            poolContainer.transform.SetParent(transform);
            
            Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                ParticleSystem ps = CreateParticleSystem(prefabPS, poolContainer.transform);
                pool.Enqueue(ps);
            }

            particlePools[key] = pool;
            particlePrefabs[key] = prefabPS;

            ParticleSystemInfo info = new ParticleSystemInfo { key = key, poolSize = initialPoolSize, activeCount = 0 };
            particleSystemInfos.Add(info);
        }
    }

    private ParticleSystem CreateParticleSystem(ParticleSystem prefab, Transform parent)
    {
        ParticleSystem ps = Instantiate(prefab, parent);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        ApplyPerformanceSettings(ps);
        return ps;
    }

    public ParticleSystem PlayParticleSystem(string key, Vector3 position, Quaternion rotation)
    {
        if (particlePools.TryGetValue(key, out Queue<ParticleSystem> pool))
        {
            ParticleSystem ps;
            if (pool.Count > 0)
            {
                ps = pool.Dequeue();
            }
            else
            {
                ps = CreateParticleSystem(particlePrefabs[key], transform.Find($"{key}Pool"));
            }

            ps.transform.SetPositionAndRotation(position, rotation);
            ps.gameObject.SetActive(true);
            ps.Play(true);

            StartCoroutine(ReturnToPoolAfterLifetime(ps, key));

            UpdateParticleSystemInfo(key, 1);

            return ps;
        }
        Debug.LogWarning($"Particle system with key {key} not found.");
        return null;
    }

    private IEnumerator ReturnToPoolAfterLifetime(ParticleSystem ps, string key)
    {
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
        StopAndReturnToPool(ps, key);
    }

    public void StopAndReturnToPool(ParticleSystem ps, string key)
    {
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);

        if (particlePools.TryGetValue(key, out Queue<ParticleSystem> pool))
        {
            pool.Enqueue(ps);
            UpdateParticleSystemInfo(key, -1);
        }
        else
        {
            Debug.LogWarning($"Pool for particle system {key} not found.");
        }
    }

    private void UpdateParticleSystemInfo(string key, int countChange)
    {
        ParticleSystemInfo info = particleSystemInfos.Find(x => x.key == key);
        if (info != null)
        {
            info.activeCount += countChange;
        }
    }

    private void ApplyPerformanceSettings(ParticleSystem ps)
    {
        ParticleSystemInfo info = particleSystemInfos.Find(x => x.key == ps.transform.parent.name.Replace("Pool", ""));
        float scale = info != null ? info.performanceScale : 1f;
        scale *= globalPerformanceScale;

        var main = ps.main;
        main.maxParticles = Mathf.RoundToInt(main.maxParticles * scale);

        var emission = ps.emission;
        if (emission.enabled)
        {
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(emission.rateOverTime.constant * scale);
        }
    }

    public void ApplyGlobalPerformanceSettings()
    {
        foreach (var pool in particlePools.Values)
        {
            foreach (var ps in pool)
            {
                ApplyPerformanceSettings(ps);
            }
        }
    }

    private void Update()
    {
        if (logPerformanceImpact)
        {
            LogPerformanceImpact();
        }
    }

    private void LogPerformanceImpact()
    {
        string log = "Particle System Performance Impact:\n";
        foreach (var info in particleSystemInfos)
        {
            log += $"{info.key}: Active: {info.activeCount}, Pool Size: {info.poolSize}, Performance Scale: {info.performanceScale}\n";
        }
        Debug.Log(log);
    }
}
