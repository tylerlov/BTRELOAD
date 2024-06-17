using System;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Universal class for base type of optimization basing on 
    /// sphere visibility detection (Culling Groups api) and others.
    /// (All code is inside FOptimizer_Base class)
    /// </summary>
    [AddComponentMenu("FImpossible Creations/Optimizers/Basic Optimizer")]
    public class FOptimizer : FOptimizer_Base, UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        public string EditorIconPath { get { return "FIMSpace/FOptimizing/Optimizers Icon"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }
    }
}