namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// Helping transitioning one type of LODs
    /// </summary>
    public class FOptimizers_LODTransition
    {
        /// <summary> Optimizer object controlling LODs </summary>
        public FComponentLODsController LODsController;
        /// <summary> From what LOD level we starting transition </summary>
        public FLOD_Base From;
        /// <summary> To which LOD level we will transition </summary>
        public FLOD_Base To;

        /// <summary> When transition done it's full work </summary>
        public bool done = false;

        /// <summary> Temporary LOD class in which we will store transitioned values of parameters </summary>
        private readonly FLOD_Base tempLOD;

        /// <summary> Temporary LOD class in which we will store transition start variables if transition was interrupted </summary>
        private FLOD_Base breakLOD;


        public FOptimizers_LODTransition(FComponentLODsController lodsController, FLOD_Base to)
        {
            LODsController = lodsController;
            From = LODsController.LODSet.LevelOfDetailSets[LODsController.CurrentLODLevel];

            tempLOD = From.CreateNewCopy();

            To = to;

            if (!LODsController.RootReference.SupportingTransitions)
            {
                LODsController.ApplyLODLevelSettings(To);
                To = null;
                done = true;
            }
        }


        /// <summary>
        /// Breaking transition, saving current component's parameters and fading from them to new LOD
        /// </summary>
        public void BreakCurrentTransition(int targetLODLevel)
        {
            //if (To == null) return;

            if (breakLOD == null) breakLOD = LODsController.RootReference.GetLODInstance();

            done = false;
            breakLOD = tempLOD.CreateNewCopy();
            From = breakLOD;
            To = LODsController.LODSet.LevelOfDetailSets[targetLODLevel];
        }


        public void Update(float progress, float secondsAfter = 0f)
        {
            if (To == null) return;

            tempLOD.InterpolateBetween(From, To, progress);
            LODsController.ApplyLODLevelSettings(tempLOD);

            if (progress >= 1f)
                if (To.Disable)
                {
                    if (To.ToCullDelay <= 0f) done = true;
                    else
                    {
                        if (secondsAfter >= To.ToCullDelay)
                        {
                            done = true;
                        }
                    }
                }
                else
                {
                    done = true;
                }
        }

        public void Finish()
        {
            if (To == null) return;
            done = true;
            LODsController.ApplyLODLevelSettings(To);
        }
    }
}
