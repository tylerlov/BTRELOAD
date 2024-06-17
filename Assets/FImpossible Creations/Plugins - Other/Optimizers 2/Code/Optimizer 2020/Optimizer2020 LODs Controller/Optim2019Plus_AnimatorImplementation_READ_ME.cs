#if UNITY_2019_4_OR_NEWER

using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public static partial class LODInstanceGenerator
    {

        // ---------------- ! READ ME ! ---------------- //
        // Copy line below:
        //
        // if (component is Animator) return GenerateInstanceOutOf(component as Animator);
        //
        // and paste to OptimizerLODInstanceGenerator.cs file,
        // anywhere inside "if (toIdentify != ESearchMode.JustCustomComponents)"
        // { brackets (line ~44), like:
        //
        // if (toIdentify != ESearchMode.JustCustomComponents)
        // {
        //      ...
        //      ...
        //      ...
        //      // if (component is Terrain) return GenerateInstanceOutOf(component as Terrain);
        //      if (component is Animator) return GenerateInstanceOutOf(component as Animator);
        //      ...
        // }
        // ---------------- ! READ ME END ! ---------------- //

        // This method below is important, you don't need to do with it anything
        public static ILODInstance GenerateInstanceOutOf(Animator component)
        {
            return new LODI_Animator();
        }

    }
}

#endif