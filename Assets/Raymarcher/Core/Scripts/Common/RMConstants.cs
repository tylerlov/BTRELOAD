namespace Raymarcher.Constants
{
    public static class RMConstants
    {
        public const string RM_VERSION = "3.0.0";
        public const string RM_LAST_UPDATE = "March 1st 2024";
        public const string RM_DEV = "Matej Vanco";
        public const string RM_PATH = RM_DEV + "/Raymarcher/";
        public const string RM_MASTERSHADER_PATH = RM_PATH + "RaymarcherRenderer_";

        public const string HELP_SUPPORT = "https://matejvanco.com/contact";
        public const string HELP_DISCORD = "https://discord.gg/Rxtvf3B9uf";
        public const string HELP_DOCS = "https://docs.google.com/presentation/d/1YUm0sMA9yxtgpWE2cKo9HvEBlwjvhhYEwgUl7Qfigng/edit?usp=sharing";
        public const string HELP_API = "https://struct9.com/matejvanco-assets/raymarcher/Introduction";
        public const string HELP_ROADMAP = "https://trello.com/b/MFqllEZE/matej-vanco-unity-asset-store-roadmap";

        public const string RM_EDITOR_ROOT_PATH = "Raymarcher/";
        public const string RM_EDITOR_MATERIAL_PATH = RM_EDITOR_ROOT_PATH + "Materials/";
        public const string RM_EDITOR_OBJECT_PRIMITIVES_PATH = "GameObject/" + RM_EDITOR_ROOT_PATH + "Primitives/";
        public const string RM_EDITOR_OBJECT_FRACTALS_PATH = "GameObject/" + RM_EDITOR_ROOT_PATH + "Fractals/";
        public const string RM_EDITOR_OBJECT_VOLUMES_PATH = "GameObject/" + RM_EDITOR_ROOT_PATH + "Volumes/";
        public const string RM_EDITOR_OBJECT_TOOLKIT_PATH = "GameObject/" + RM_EDITOR_ROOT_PATH + "Toolkit/";

        public const string RM_EDITOR_OBJECT_MODIFIERS_PATH = RM_PATH + "Modifiers/Modifier ";

        public static class CommonBuildTimeConstants
        {
            public const string RM_COMMON_SDFOBJBUFFER_SdfInstances = "SdfInstances";
            public const string RM_COMMON_SDFOBJBUFFER_ModelData = "modelData";
        }

        public static class CommonReflection
        {
            public const string RP_BUILTIN_CAMFILTER = "Raymarcher.CameraFilters.RMCamFilterBuiltInRP";
            public const string METHOD_BUILTIN_SETUP = "SetupCamFilter";
            public const string METHOD_BUILTIN_DISPOSE = "DisposeCamFilter";

            public const string RP_URP_CAMFILTER = "Raymarcher.CameraFilters.RMCamFilterURPHandler";
            public const string METHOD_URP_SETUP = "SetupURPCam";
            public const string METHOD_URP_DISPOSE = "DisposeURPCam";
        }

        public static class CommonRendererProperties
        {
            // Camera Properties
            public const string CamSpaceToWorldMatrix = "RaymarcherCamWorldMatrix";

            // Renderer Settings
            public const string MaxRenderDistance = "RaymarcherMaxRenderDistance";
            public const string RenderPrecision = "RaymarcherRenderQuality";

            // Renderer Features
            public const string SceneDepthSmoothness = "RaymarcherSceneDepthSmoothness";

            public const string GlobalSdfObjectSmoothness = "RaymarcherGlobalSdfObjectSmoothness";
            public const string SceneGeometrySmoothness = "RaymarcherSceneGeometrySmoothness";

            public const string RendererColorTint = "RaymarcherRendererColorTint";
            public const string RendererExposure = "RaymarcherRendererExposure";

            public const string GlobalHueSaturation = "RaymarcherGlobalHueSaturation";
            public const string GlobalHueSpectrumOffset = "RaymarcherGlobalHueSpectrumOffset";
            public const string GrabSceneColor = "RaymarcherGrabSceneColor";

            public const string PixelationSize = "RaymarcherPixelationSize";

            public const string DistanceFogDistance = "RaymarcherDistanceFogDistance";
            public const string DistanceFogSmoothness = "RaymarcherDistanceFogSmoothness";
            public const string DistanceFogColor = "RaymarcherDistanceFogColor";
             
            // Lighting
            public const string DirectionalLightDirection = "RaymarcherDirectionalLightDir";
            public const string DirectionalLightColor = "RaymarcherDirectionalLightColor";
            public const string AdditionalLightsData = "RaymarcherAddLightsData";
        }

        public static class CommonRendererFeatures
        {
            public const string RAYMARCHER_SMOOTH_BLEND = "RAYMARCHER_SMOOTH_BLEND";
            public const string RAYMARCHER_REACT_GEOMETRY = "RAYMARCHER_REACT_GEOMETRY";

            public const string RAYMARCHER_DISTANCE_FOG = "RAYMARCHER_DISTANCE_FOG";
            public const string RAYMARCHER_SCENE_DEPTH = "RAYMARCHER_SCENE_DEPTH";
            public const string RAYMARCHER_PIXELATION = "RAYMARCHER_PIXELATION";

            public const string RAYMARCHER_MAIN_LIGHT = "RAYMARCHER_MAIN_LIGHT";
            public const string RAYMARCHER_ADDITIONAL_LIGHTS = "RAYMARCHER_ADDITIONAL_LIGHTS";

            public const string RAYMARCHER_ITERATIONSx16 = "ITERATIONSx16";
            public const string RAYMARCHER_ITERATIONSx32 = "ITERATIONSx32";
            public const string RAYMARCHER_ITERATIONSx64 = "ITERATIONSx64";
            public const string RAYMARCHER_ITERATIONSx128 = "ITERATIONSx128";
            public const string RAYMARCHER_ITERATIONSx256 = "ITERATIONSx256";
            public const string RAYMARCHER_ITERATIONSx512 = "ITERATIONSx512";
        }
    }
}