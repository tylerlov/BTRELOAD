using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FC: Scriptable container for IFLOD
    /// </summary>
    //[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_Animator Reference")]
    public sealed class ScrLOD_Animator : ScrLOD_Base
    {
        [SerializeField]
        private LODI_Animator settings;
        public override ILODInstance GetLODInstance() { return settings; }
        public ScrLOD_Animator() { settings = new LODI_Animator(); }

        public override ScrLOD_Base GetScrLODInstance()
        { return CreateInstance<ScrLOD_Animator>(); }


        public override ScrLOD_Base CreateNewScrCopy()
        {
            ScrLOD_Animator newA = CreateInstance<ScrLOD_Animator>();
            newA.settings = settings.GetCopy() as LODI_Animator;
            return newA;
        }

        public override ScriptableLODsController GenerateLODController(Component target, ScriptableOptimizer optimizer)
        {
            Animator a = target as Animator;
            if (!a) a = target.GetComponentInChildren<Animator>();
            if (a) if (!optimizer.ContainsComponent(a))
                {
                    return new ScriptableLODsController(optimizer, a, -1, "Animator", this);
                }

            return null;
        }
    }
}
