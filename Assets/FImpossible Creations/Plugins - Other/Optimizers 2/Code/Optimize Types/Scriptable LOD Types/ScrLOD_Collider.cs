using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FC: Scriptable container for IFLOD
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_Collider Reference")]
    public sealed class ScrLOD_Collider : ScrLOD_Base
    {
        [SerializeField]
        private LODI_Collider settings;
        public override ILODInstance GetLODInstance() { return settings; }
        public ScrLOD_Collider() { settings = new LODI_Collider(); }

        public override ScrLOD_Base GetScrLODInstance()
        { return CreateInstance<ScrLOD_Collider>(); }


        public override ScrLOD_Base CreateNewScrCopy()
        {
            ScrLOD_Collider newA = CreateInstance<ScrLOD_Collider>();
            newA.settings = settings.GetCopy() as LODI_Collider;
            return newA;
        }

        public override ScriptableLODsController GenerateLODController(Component target, ScriptableOptimizer optimizer)
        {
            Collider a = target as Collider;
            if (!a) a = target.GetComponentInChildren<Collider>();
            if (a)
            {
                if (a is CharacterController) return null;

                if (!optimizer.ContainsComponent(a))
                {
                    return new ScriptableLODsController(optimizer, a, -1, "Collider", this);
                }
            }

            return null;
        }
    }
}
