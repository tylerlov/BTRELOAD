using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.Impostors.Core
{
    public class ImpostorLODGroupPreset : ScriptableObject
    {
        public bool setupLOD = true;
        public float lodPerformance = 0.15f;
        public float lodSizeCulling = 0.01f;
    }
}
