using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public partial class FOptimizers_Manager
    {
        //private readonly List<FOptimizer_Base> staticOptimizers = new List<FOptimizer_Base>();
        //private readonly List<FOptimizer_Base> dynamicOptimizers = new List<FOptimizer_Base>();
        //private readonly List<FOptimizer_Base> effectiveOptimizers = new List<FOptimizer_Base>();
        //private readonly List<FOptimizer_Base> triggerOptimizers = new List<FOptimizer_Base>();

        public List<FOptimizer_Base> notContainedStaticOptimizers = new List<FOptimizer_Base>();
        public List<FOptimizer_Base> notContainedDynamicOptimizers = new List<FOptimizer_Base>();
        public List<FOptimizer_Base> notContainedEffectiveOptimizers = new List<FOptimizer_Base>();
        public List<FOptimizer_Base> notContainedTriggerOptimizers = new List<FOptimizer_Base>();

        public void RegisterNotContainedOptimizer(FOptimizer_Base optimizer, bool init = false)
        {
            switch (optimizer.OptimizingMethod)
            {
                case FEOptimizingMethod.Static: RegisterNotContainedStaticOptimizer(optimizer, init); break;
                case FEOptimizingMethod.Dynamic: RegisterNotContainedDynamicOptimizer(optimizer, init); break;
                case FEOptimizingMethod.Effective: RegisterNotContainedEffectiveOptimizer(optimizer, init); break;
                case FEOptimizingMethod.TriggerBased: RegisterNotContainedTriggerOptimizer(optimizer, init); break;
            }
        }

        public void RegisterNotContainedStaticOptimizer(FOptimizer_Base optimizer, bool init = false)
        {
            if (init) notContainedStaticOptimizers.Add(optimizer); else if (!notContainedStaticOptimizers.Contains(optimizer)) notContainedStaticOptimizers.Add(optimizer);
        }

        public void RegisterNotContainedDynamicOptimizer(FOptimizer_Base optimizer, bool init = false)
        {
            if (init) notContainedDynamicOptimizers.Add(optimizer); else if (!notContainedDynamicOptimizers.Contains(optimizer)) notContainedDynamicOptimizers.Add(optimizer);
        }

        public void RegisterNotContainedEffectiveOptimizer(FOptimizer_Base optimizer, bool init = false)
        {
            if (init) notContainedEffectiveOptimizers.Add(optimizer); else if (!notContainedEffectiveOptimizers.Contains(optimizer)) notContainedEffectiveOptimizers.Add(optimizer);
        }

        public void RegisterNotContainedTriggerOptimizer(FOptimizer_Base optimizer, bool init = false)
        {
            if (init) notContainedTriggerOptimizers.Add(optimizer); else if (!notContainedTriggerOptimizers.Contains(optimizer)) notContainedTriggerOptimizers.Add(optimizer);
        }



        public void UnRegisterOptimizer(FOptimizer_Base optimizer)
        {
            if (optimizer.AddToContainer) return;

            switch (optimizer.OptimizingMethod)
            {
                case FEOptimizingMethod.Static: UnRegisterStaticOptimizer(optimizer); break;
                case FEOptimizingMethod.Dynamic: UnRegisterDynamicOptimizer(optimizer); break;
                case FEOptimizingMethod.Effective: UnRegisterEffectiveOptimizer(optimizer); break;
                case FEOptimizingMethod.TriggerBased: UnRegisterTriggerOptimizer(optimizer); break;
            }
        }

        public void UnRegisterStaticOptimizer(FOptimizer_Base optimizer)
        {
            if (notContainedStaticOptimizers.Contains(optimizer)) notContainedStaticOptimizers.Remove(optimizer);
        }

        public void UnRegisterDynamicOptimizer(FOptimizer_Base optimizer)
        {
            if (!notContainedDynamicOptimizers.Contains(optimizer)) notContainedDynamicOptimizers.Remove(optimizer);
        }

        public void UnRegisterEffectiveOptimizer(FOptimizer_Base optimizer)
        {
            if (!notContainedEffectiveOptimizers.Contains(optimizer)) notContainedEffectiveOptimizers.Remove(optimizer);
        }

        public void UnRegisterTriggerOptimizer(FOptimizer_Base optimizer)
        {
            if (!notContainedTriggerOptimizers.Contains(optimizer)) notContainedTriggerOptimizers.Remove(optimizer);
        }

    }
}
