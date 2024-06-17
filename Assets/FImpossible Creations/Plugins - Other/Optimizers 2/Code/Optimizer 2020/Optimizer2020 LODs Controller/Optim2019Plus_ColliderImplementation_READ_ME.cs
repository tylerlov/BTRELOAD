#if UNITY_2019_4_OR_NEWER

using System.ComponentModel;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public static partial class LODInstanceGenerator
    {

        // ---------------- ! READ ME ! ---------------- //
        // Copy line below:
        //
        // if (component is Collider) return GenerateInstanceOutOf(component as Collider);
        //
        // and paste to OptimizerLODInstanceGenerator.cs file,
        // anywhere inside "if (toIdentify != ESearchMode.JustCustomComponents)"
        // around line 56, like:
        //
        // ... if (toIdentify != ESearchMode.JustCustomComponents)
        // ... {
        //      ...
        //      ...
        //      ... if (component is AudioSource) return GenerateInstanceOutOf(component as AudioSource);
        //      ... if (deepSearch) if (component is Rigidbody) return GenerateInstanceOutOf(component as Rigidbody);
        //      ... // if (component is Terrain) return GenerateInstanceOutOf(component as Terrain);
        //
        //      if (component is Collider) return GenerateInstanceOutOf(component as Collider);
        //
        //      ...
        // ... }
        //
        // Now collider will be added to the To Optimize list only when drag & dropped into inspector.
        // To allow adding Collider component automatically, you need to add line like this:
        //
        // else if (type is Collider) return true;
        //
        // inside Optimizer2020Selector.cs file, inside method "IsTypeAllowed()", at line about 33, before "return false".
        //
        // ---------------- ! READ ME END ! ---------------- //

        // This method below is important, but you don't need to do with it anything
        public static ILODInstance GenerateInstanceOutOf(Collider component)
        {
            if (component is CharacterController) return null;
            return new LODI_Collider();
        }

    }
}

#endif