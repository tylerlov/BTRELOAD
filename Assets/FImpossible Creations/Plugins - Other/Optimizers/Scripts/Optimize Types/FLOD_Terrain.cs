using UnityEngine;
using FIMSpace.FOptimizing;

/// <summary>
/// FM: Helper class for single LOD level settings on Terrain
/// </summary>
//[CreateAssetMenu(menuName = "Custom Optimizers/FLOD_Terrain Reference")]
public sealed class FLOD_Terrain : FLOD_Base
{
    [Range(1, 200)]
    public float PixelError = 5;
    [Range(0, 2000)]
    public float BasemapDistance = 1250f;

    [Space(3f)]
    [Range(0, 250)]
    public float DetailDistance = 100f;
    [Range(0, 1)]
    public float DetailDensity = 1f;

    [Space(3f)]
    [Range(0, 2000)]
    public float TreeDistance = 2000f;
    [Range(1f, 5f)]
    public float TreeLODBias = 1f;
    [Range(5, 2000)]
    public float BillboardStart = 50f;

    [Space(3f)]
    public bool DrawFoliage = true;

//#if UNITY_2019_1_OR_NEWER
    public UnityEngine.Rendering.ShadowCastingMode Mode;
//#else
    public bool CastShadows = true;
//#endif


    public bool DrawHeightmap = true;

    [Tooltip("Dividing resolution of heightmap")]
    [Range(0, 3)]
    public int ResolutionDivider = 0;

    [Space(3f)]
    [Tooltip("Optional - Replace drawing terrain with target gameObject with mesh renderer for final optimization when terrain is far away (terrain collider will still work)")]
    public GameObject MeshReplacement = null;


    #region Initialization


    public FLOD_Terrain()
    {
        SupportingTransitions = true;
        HeaderText = "Terrain LOD Settings";
    }


    public override FLOD_Base GetLODInstance()
    {
        return CreateInstance<FLOD_Terrain>();
    }


    public override FLOD_Base CreateNewCopy()
    {
        FLOD_Terrain lodA = CreateInstance<FLOD_Terrain>();
        lodA.CopyBase(this);
        lodA.PixelError = PixelError;
        lodA.BasemapDistance = BasemapDistance;
        lodA.DetailDistance = DetailDistance;
        lodA.DetailDensity = DetailDensity;
        lodA.TreeDistance = TreeDistance;
        lodA.BillboardStart = BillboardStart;
        lodA.DrawFoliage = DrawFoliage;

//#if UNITY_2019_1_OR_NEWER
        lodA.Mode = Mode;
//#else
        lodA.CastShadows = CastShadows;
//#endif

        lodA.TreeLODBias = TreeLODBias;
        lodA.DrawHeightmap = DrawHeightmap;
        lodA.MeshReplacement = MeshReplacement;
        lodA.ResolutionDivider = ResolutionDivider;

        return lodA;
    }


    public override void SetSameValuesAsComponent(Component component)
    {
        if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of Terrain!");

        Terrain comp = component as Terrain;

        if (comp != null)
        {
            PixelError = comp.heightmapPixelError;
            BasemapDistance = comp.basemapDistance;
            DetailDistance = comp.detailObjectDistance;
            DetailDensity = comp.detailObjectDensity;
            TreeDistance = comp.treeDistance;
            BillboardStart = comp.treeBillboardDistance;
            DrawFoliage = comp.drawTreesAndFoliage;

#if UNITY_2019_1_OR_NEWER
            Mode = comp.shadowCastingMode;
#else
            CastShadows = comp.castShadows;
#endif

            TreeLODBias = comp.treeLODBiasMultiplier;
            ResolutionDivider = comp.heightmapMaximumLOD;
            DrawHeightmap = comp.drawHeightmap;
            MeshReplacement = null;
        }
    }


    #endregion


    #region Operations

    public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
    {
        base.InterpolateBetween(lodA, lodB, transitionToB);

        FLOD_Terrain a = lodA as FLOD_Terrain;
        FLOD_Terrain b = lodB as FLOD_Terrain;

        PixelError = Mathf.Lerp(a.PixelError, b.PixelError, transitionToB);
        BasemapDistance = Mathf.Lerp(a.BasemapDistance, b.BasemapDistance, transitionToB);
        DetailDistance = Mathf.Lerp(a.DetailDistance, b.DetailDistance, transitionToB);
        DetailDensity = Mathf.Lerp(a.DetailDensity, b.DetailDensity, transitionToB);
        TreeDistance = Mathf.Lerp(a.TreeDistance, b.TreeDistance, transitionToB);
        BillboardStart = Mathf.Lerp(a.BillboardStart, b.BillboardStart, transitionToB);
        TreeLODBias = Mathf.Lerp(a.TreeLODBias, b.TreeLODBias, transitionToB);
        ResolutionDivider = (int)Mathf.Lerp(a.ResolutionDivider, b.ResolutionDivider, transitionToB);

        DrawFoliage = BoolTransition(DrawFoliage, a.DrawFoliage, b.DrawFoliage, transitionToB);

#if UNITY_2019_1_OR_NEWER
        if (transitionToB > 0) Mode = b.Mode;
#else
        CastShadows = BoolTransition(CastShadows, a.CastShadows, b.CastShadows, transitionToB);
#endif


        DrawHeightmap = BoolTransition(DrawHeightmap, a.DrawHeightmap, b.DrawHeightmap, transitionToB);
        MeshReplacement = (GameObject)ObjectTransition(MeshReplacement, a.MeshReplacement, b.MeshReplacement, transitionToB);
    }




    public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
    {
        // Initital settings not needed for this type of component (terrain)
        Terrain comp = component as Terrain;
        if (comp == null) { Debug.LogError("[OPTIMIZERS] Target component is null or is not Terrain! (" + component + ")"); return; }
        FLOD_Terrain initLOD = initialSettingsReference as FLOD_Terrain;

        if (MeshReplacement == null)
        {
            if (Disable)
            {
                comp.enabled = false;
            }
            else
            {
                if (comp.enabled == false) comp.enabled = true;

                comp.heightmapPixelError = PixelError;

                if (comp.detailObjectDistance != BasemapDistance) comp.detailObjectDistance = BasemapDistance;
                if (comp.detailObjectDensity != DetailDistance) comp.detailObjectDensity = DetailDistance;
                if (comp.detailObjectDensity != DetailDensity) comp.detailObjectDensity = DetailDensity;
                if (comp.treeDistance != TreeDistance) comp.treeDistance = TreeDistance;
                if (comp.treeBillboardDistance != BillboardStart) comp.treeBillboardDistance = BillboardStart;

                comp.drawTreesAndFoliage = DrawFoliage;

#if UNITY_2019_1_OR_NEWER
                comp.shadowCastingMode = Mode;
#else
comp.castShadows = CastShadows;
#endif

                comp.treeLODBiasMultiplier = TreeLODBias;
                comp.drawHeightmap = DrawHeightmap;

                if (comp.drawTreesAndFoliage == false || comp.drawHeightmap == false)
                    comp.collectDetailPatches = false;
                else
                    comp.collectDetailPatches = true;

                comp.heightmapMaximumLOD = ResolutionDivider;
            }

            if (initLOD.MeshReplacement) initLOD.MeshReplacement.SetActive(false);
        }
        else
        {
#if UNITY_2019_1_OR_NEWER
            comp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
            comp.castShadows = false;
#endif


            comp.drawHeightmap = false;
            comp.drawTreesAndFoliage = false;
            comp.collectDetailPatches = false;

            Transform mesh = comp.transform.Find(comp.name);

            if (!mesh)
            {
                GameObject instantiated = Instantiate(MeshReplacement);
                mesh = instantiated.transform;
                mesh.name = comp.name;
                mesh.position = comp.transform.position;
                mesh.SetParent(comp.transform, true);

                initLOD.MeshReplacement = mesh.gameObject;
            }

            mesh.gameObject.SetActive(true);
        }
    }

    #endregion


    #region Auto Settings 


    public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
    {
        Terrain comp = source as Terrain;
        if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not Terrain Component!");

        // REMEMBER: LOD = 0 is not nearest but one after nearest
        // Trying to auto configure universal LOD settings

        float mul = GetValueForLODLevel(1f, 0f, lodIndex, lodCount); // Starts from 0.75 (LOD1), then 0.5, 0.25 and 0.0 (Culled) if lod count is = 4

        // Your auto settings depending of LOD count
        // For example LOD count = 3, you want every next LOD go with parameters from 1f, to 0.6f, 0.3f, 0f - when culled

        PixelError = (int)Mathf.Lerp(comp.heightmapPixelError + 22, comp.heightmapPixelError, mul);
        BasemapDistance = Mathf.Lerp(comp.basemapDistance / 5f, comp.basemapDistance / 1f, mul);
        DetailDistance = Mathf.Lerp(comp.detailObjectDistance / 4f, comp.detailObjectDistance, mul);
        DetailDensity = Mathf.Lerp(comp.detailObjectDensity / 5f, comp.detailObjectDensity, mul);
        TreeDistance = comp.treeDistance;
        BillboardStart = comp.treeBillboardDistance;
        TreeLODBias = 1f;
        DrawHeightmap = true;
        DrawDisableOption = false;
        ResolutionDivider = 0;

#if UNITY_2019_1_OR_NEWER
        Mode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
        CastShadows = false;
#endif


        DrawFoliage = false;

        if (lodIndex >= 1)
        {
            DrawFoliage = false;
            TreeLODBias = Mathf.Lerp(2f, 1f, mul);
            if (lodCount <= 3) PixelError = comp.heightmapPixelError + 16;
        }

        if (lodIndex >= 2)
        {
            ResolutionDivider = 1;
            PixelError = comp.heightmapPixelError + 18;
        }

        //if (lodCount > 2)
        //{
        //    if (lodIndex == lodCount - 2)
        //    {
        //        CastShadows = false;
        //    }
        //}

        name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
    }


    public override void SetSettingsAsForCulled(Component component)
    {
        base.SetSettingsAsForCulled(component);
        Disable = false;
        PixelError = 200;
        BasemapDistance = 500;
        DetailDistance = 0;
        DetailDensity = 0;
        TreeDistance = 0;
        BillboardStart = 5;
        DrawFoliage = false;

#if UNITY_2019_1_OR_NEWER
        Mode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
        CastShadows = false;
#endif


        TreeLODBias = 1f;
        ResolutionDivider = 0;
        DrawHeightmap = false;
        DrawDisableOption = false;
    }

    public override void SetSettingsAsForHidden(Component component)
    {
        base.SetSettingsAsForHidden(component);
        DrawFoliage = false;

#if UNITY_2019_1_OR_NEWER
        Mode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
        CastShadows = false;
#endif

        TreeLODBias = 1f;
        ResolutionDivider = 0;
        DrawHeightmap = false;
        DrawDisableOption = false;
    }


    public override void SetSettingsAsForNearest(Component component)
    {
        base.SetSettingsAsForNearest(component);

        Terrain comp = component as Terrain;
        SetSameValuesAsComponent(comp);
        DrawDisableOption = false;
    }


    #endregion


    public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
    {
        Terrain t = target as Terrain;
        if (!t) t = target.GetComponentInChildren<Terrain>();
        if (t) if (!optimizer.ContainsComponent(t))
            {
                return new FComponentLODsController(optimizer, t, "Terrain", this);
            }

        return null;
    }
}
