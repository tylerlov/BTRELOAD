using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ParticleSystemProfiler : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ParticleSystemInfo> particleSystems = new List<ParticleSystemInfo>();

    [MenuItem("Window/Particle System Profiler")]
    public static void ShowWindow()
    {
        GetWindow<ParticleSystemProfiler>("Particle Profiler");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Profile Particle Systems"))
        {
            ProfileParticleSystems();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (var psInfo in particleSystems)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"Name: {psInfo.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Particle Count: {psInfo.particleCount}");
            EditorGUILayout.LabelField($"Max Particles: {psInfo.maxParticles}");
            EditorGUILayout.LabelField($"Emission Rate: {psInfo.emissionRate:F2} particles/sec");
            EditorGUILayout.LabelField($"Estimated Performance Impact: {psInfo.performanceImpact:F2}");
            if (GUILayout.Button("Select in Scene"))
            {
                Selection.activeGameObject = psInfo.gameObject;
                SceneView.FrameLastActiveSceneView();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndScrollView();
    }

    void ProfileParticleSystems()
    {
        particleSystems.Clear();
        var allParticleSystems = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allParticleSystems)
        {
            var main = ps.main;
            var emission = ps.emission;

            float emissionRate = 0;
            if (emission.enabled)
            {
                emissionRate = emission.rateOverTime.constant;
            }

            float performanceImpact = CalculatePerformanceImpact(ps);

            particleSystems.Add(new ParticleSystemInfo
            {
                name = ps.name,
                gameObject = ps.gameObject,
                particleCount = ps.particleCount,
                maxParticles = main.maxParticles,
                emissionRate = emissionRate,
                performanceImpact = performanceImpact
            });
        }

        particleSystems.Sort((a, b) => b.performanceImpact.CompareTo(a.performanceImpact));
    }

    float CalculatePerformanceImpact(ParticleSystem ps)
    {
        var main = ps.main;
        int particleCount = ps.particleCount;
        float simSpeed = main.simulationSpeed;
        float lifetime = main.startLifetime.constant;

        // This is a simplified estimation. Adjust the formula as needed.
        return particleCount * simSpeed * (1 / lifetime);
    }

    class ParticleSystemInfo
    {
        public string name;
        public GameObject gameObject;
        public int particleCount;
        public int maxParticles;
        public float emissionRate;
        public float performanceImpact;
    }
}