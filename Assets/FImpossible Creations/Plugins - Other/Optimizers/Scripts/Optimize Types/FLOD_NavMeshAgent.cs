using UnityEngine;
using FIMSpace.FOptimizing;

/// <summary>
/// FM: Helper class for single LOD level settings on NavMeshAgent
/// </summary>
//[CreateAssetMenu(menuName = "Custom Optimizers/LOD_NavMeshAgent Reference")]
public sealed class FLOD_NavMeshAgent : FLOD_Base
{
    [Space(4)]
    [Range(0f, 1f)]
    //[FPD_Percentage(0f,1f)]
    public float Priority = 1f;
    public UnityEngine.AI.ObstacleAvoidanceType Quality = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

    
    #region Initialization


    public FLOD_NavMeshAgent()
    {
        // If you don't want to use transitions (InterpolateBetween) - then set "SupportingTransitions" to false
        // But if you implement interpolation then set it to true
        SupportingTransitions = true;
        HeaderText = "NavMeshAgent LOD Settings";
    }

    public override FLOD_Base GetLODInstance()
    {
        return CreateInstance<FLOD_NavMeshAgent>();
    }

    public override FLOD_Base CreateNewCopy()
    {
        FLOD_NavMeshAgent lodA = CreateInstance<FLOD_NavMeshAgent>();
        lodA.CopyBase(this);
        lodA.Priority = Priority;
        lodA.Quality = Quality;
        return lodA;
    }

    public override void SetSameValuesAsComponent(Component component)
    {
        if (component == null) Debug.LogError("[Custom OPTIMIZERS] Given component is null instead of NavMeshAgent!");

        UnityEngine.AI.NavMeshAgent comp = component as UnityEngine.AI.NavMeshAgent;

        if (comp != null)
        {
            Priority = comp.avoidancePriority;
            Quality = comp.obstacleAvoidanceType;
        }
    }


    #endregion


    #region Operations

    public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
    {
        base.InterpolateBetween(lodA, lodB, transitionToB);

        FLOD_NavMeshAgent a = lodA as FLOD_NavMeshAgent;
        FLOD_NavMeshAgent b = lodB as FLOD_NavMeshAgent;

        Priority = Mathf.Lerp(a.Priority, b.Priority, transitionToB);

        int ia = (int)a.Quality;
        int ib = (int)b.Quality;
        int avoidance = (int)Mathf.Lerp(ia, ib, transitionToB);
        Quality = (UnityEngine.AI.ObstacleAvoidanceType)avoidance;
    }


    public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
    {
        // Casting LOD to correct type
        FLOD_NavMeshAgent initialSettings = initialSettingsReference as FLOD_NavMeshAgent;

        #region Security

        // Checking if casting is right
        if (initialSettings == null) { Debug.Log("[Custom OPTIMIZERS] Target LOD is not NavMeshAgent LOD or is null"); return; }

        #endregion

        UnityEngine.AI.NavMeshAgent comp = component as UnityEngine.AI.NavMeshAgent;

        comp.avoidancePriority = (int)Mathf.Clamp(initialSettings.Priority * Priority, 0, 99);
        comp.obstacleAvoidanceType = Quality;

        base.ApplySettingsToComponent(component, initialSettingsReference);
    }

    #endregion


    #region Auto Settings


    public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
    {
        UnityEngine.AI.NavMeshAgent comp = source as UnityEngine.AI.NavMeshAgent;
        if (comp == null) Debug.LogError("[Custom OPTIMIZERS] Given component for reference values is null or is not NavMeshAgent Component!");

        // REMEMBER: LOD = 0 is not nearest but one after nearest
        // Trying to auto configure universal LOD settings

        float mul = GetValueForLODLevel(1f, 0f, lodIndex, lodCount); // Starts from 0.75 (LOD1), then 0.5, 0.25 and 0.0 (Culled) if lod count is = 4

        // Your auto settings depending of LOD count
        Priority = mul;
        int q = (int)Quality;
        q = (int)(q * mul);
        Quality = (UnityEngine.AI.ObstacleAvoidanceType)q;

        name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
    }


    public override void SetSettingsAsForCulled(Component component)
    {
        base.SetSettingsAsForCulled(component);
        Priority = 0;
        Quality = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
    }


    public override void SetSettingsAsForNearest(Component component)
    {
        base.SetSettingsAsForNearest(component);

        //UnityEngine.AI.NavMeshAgent comp = component as UnityEngine.AI.NavMeshAgent;
        Priority = 1;
        Quality = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    public override void SetSettingsAsForHidden(Component component)
    {
        base.SetSettingsAsForHidden(component);

        Priority = 0.2f;
        Quality = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    }


    #endregion


    public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
    {
        UnityEngine.AI.NavMeshAgent c = target as UnityEngine.AI.NavMeshAgent;
        if (!c) c = target.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
        if (c) if (!optimizer.ContainsComponent(c))
            {
                return new FComponentLODsController(optimizer, c, "NavMeshAgent", this);
            }

        return null;
    }

}
