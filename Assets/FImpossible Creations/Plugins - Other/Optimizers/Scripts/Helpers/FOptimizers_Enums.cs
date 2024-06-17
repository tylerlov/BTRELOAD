namespace FIMSpace.FOptimizing
{
    public enum FEOptimizingMethod
    {
        /// <summary> Using just Unity's Culling Groups API, detection sphere and static distance ranges from initial position </summary>
        Static,
        /// <summary> No Unity's Culing Groups API involved, just Optimizers Manager different interval clocks </summary>
        Dynamic,
        /// <summary> Detecting if object stays in one place, then using refreshing Culling Groups API with Optimizers Manager clocks to effectively detect object visibility and detect distances like Dynamic method </summary>
        Effective,
        /// <summary> Defining optimization levels with trigger colliders setup </summary>
        TriggerBased
    }

    public enum FEOptimizingDistance : int
    {
        Nearest,
        Near,
        MidFar,
        Far,
        Farthest
    }
}