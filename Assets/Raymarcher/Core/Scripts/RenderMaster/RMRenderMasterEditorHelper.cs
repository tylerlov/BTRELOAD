using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Raymarcher.Constants;
#endif

namespace Raymarcher
{
    public sealed class RMRenderMasterEditorHelper
#if UNITY_EDITOR 
        : EditorWindow
#endif
    {
#if UNITY_EDITOR

        private static readonly Color neutralRecompilationButton = Color.white / 2f;
        private static readonly Color requiredRecompilationButton = Color.white + Color.red / 4f;

        private static readonly Texture2D eIcon_ConvertObjects = Resources.Load<Texture2D>("RMEditorIcon_Conv_SdfObjects");
        private static readonly Texture2D eIcon_ConvertMaterials = Resources.Load<Texture2D>("RMEditorIcon_Conv_Materials");

        public static void GenerateContactLayout()
        {
            Color backup = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            GUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("Support"))
                Application.OpenURL(RMConstants.HELP_SUPPORT);
            if (GUILayout.Button("Discord"))
                Application.OpenURL(RMConstants.HELP_DISCORD);
            if (GUILayout.Button("Documentation"))
                Application.OpenURL(RMConstants.HELP_DOCS);
            if (GUILayout.Button("Roadmap"))
                Application.OpenURL(RMConstants.HELP_ROADMAP);
            if (GUILayout.Button("APi"))
                Application.OpenURL(RMConstants.HELP_API);

            GUILayout.EndHorizontal();
            GUI.backgroundColor = backup;
        }

        public static void GenerateRecompilationLayout(RMRenderMaster renderMaster, bool shortVersion = false, bool makeSpace = true)
        {
            if(makeSpace)
                GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            bool recomp = renderMaster.RecompilationRequiredSdfObjectBuffer || renderMaster.RecompilationRequiredMaterialBuffer;
            GUI.color = GUI.backgroundColor = recomp ? requiredRecompilationButton : neutralRecompilationButton;
            if (GUILayout.Button(new GUIContent(shortVersion ? "Session" : "Recompile Raymarcher Session", "Auto-recompile Object or Material Buffer including Raymarcher session shader")))
                renderMaster.RecompileTarget();
            GUILayout.EndHorizontal();

            if (!renderMaster.RecompilationRequiredSdfObjectBuffer && !renderMaster.RecompilationRequiredMaterialBuffer)
                GUI.color = GUI.backgroundColor = Color.white;
            else
            {
                GUI.backgroundColor = Color.white;
                GUI.color = requiredRecompilationButton;
            }

            GUILayout.BeginHorizontal();
            if (shortVersion)
            {
                if (GUILayout.Button(new GUIContent(eIcon_ConvertObjects, "Recompile Object Buffer")))
                    renderMaster.RecompileTarget(true);
                if (GUILayout.Button(new GUIContent(eIcon_ConvertMaterials, "Recompile Material Buffer")))
                    renderMaster.RecompileTarget(false);
            }
            else
            {
                if (GUILayout.Button(new GUIContent( "  Recompile Object Buffer", eIcon_ConvertObjects)))
                    renderMaster.RecompileTarget(true);
                if (GUILayout.Button(new GUIContent("  Recompile Material Buffer", eIcon_ConvertMaterials)))
                    renderMaster.RecompileTarget(false);
            }
            GUILayout.EndHorizontal();
            if (!shortVersion && recomp)
                EditorGUILayout.HelpBox("Changes have been made. Recompilation is required.", MessageType.None);
        }

        private static bool initialized = false;
        private static RMRenderMaster currentInstance;

        public static bool IsInitialized => initialized;

        public static void Enable(RMRenderMaster instance)
        {
            if (initialized)
                return;
            currentInstance = instance;
            SceneView.duringSceneGui += SceneView_duringSceneGui;
            initialized = true;
        }

        public static void Disable()
        {
            if (!initialized)
                return;
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            initialized = false;
            currentInstance = null;
        }

        private static readonly Vector2 SHORTCUT_WINDOW_SIZE = new Vector2(100, 90);
        private static void SceneView_duringSceneGui(SceneView obj)
        {
            if (currentInstance == null)
                return;
            Handles.BeginGUI();
            Vector2 location = new Vector2(obj.camera.pixelRect.width - SHORTCUT_WINDOW_SIZE.x - 10, obj.camera.pixelRect.height - SHORTCUT_WINDOW_SIZE.y - 10);
            GUILayout.Window(2, new Rect(location, SHORTCUT_WINDOW_SIZE), (id) =>
            {
                GenerateRecompilationLayout(currentInstance, true);
            }, "RM Convertor");
            Handles.EndGUI();
        }
#endif
    }
}