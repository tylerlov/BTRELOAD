using UnityEngine;

namespace ScriptAnalysis.Core
{
    [System.Serializable]
    public class AnalysisSettings
    {
        [SerializeField] public int LargeMethodLineThreshold = 100;
        [SerializeField] public int GodClassMethodCount = 20;
        [SerializeField] public int GodClassDependencyCount = 15;
        [SerializeField] public int GodClassLineCount = 500;
        [SerializeField] public int HighComplexityThreshold = 30;
        [SerializeField] public int ModerateComplexityThreshold = 20;
        [SerializeField] public int HighDependencyCount = 10;
        [SerializeField] public int HighIncomingDependencyCount = 15;
        [SerializeField] public int UnstableDependencyThreshold = 3;
        [SerializeField] public int UpdateMethodComplexityThreshold = 5;
        [SerializeField] public int UpdateMethodLineThreshold = 20;
        [SerializeField] public bool DetectCircularDependencies = true;
        [SerializeField] public bool DetectLayerViolations = true;
        [SerializeField] public bool DetectNamingIssues = true;
        [SerializeField] public bool DetectPatternIssues = true;
        [SerializeField] public bool DetectUnitySpecificIssues = true;
        [SerializeField] public bool DetectGodClass = true;
    }
} 