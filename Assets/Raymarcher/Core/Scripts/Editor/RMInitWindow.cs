using System.Linq;
using System.IO;
using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using Raymarcher.RendererData;
using Raymarcher.Constants;
using Raymarcher.Convertor;

namespace Raymarcher.UEditor
{
    public sealed class RMInitWindow : EditorWindow
    {
        [MenuItem("Window/Raymarcher/Setup Wizard")]
        private static void InitRaymarcher()
        {
            var window = GetWindow(typeof(RMInitWindow));
            window.titleContent = new GUIContent("Raymarcher Setup");
            window.maxSize = new Vector2(450, 650);
            window.minSize = window.maxSize;

            var win = window as RMInitWindow;
            if (Type.GetType(RMConstants.CommonReflection.RP_BUILTIN_CAMFILTER) != null)
                win.targetPipeline = RMRenderMaster.TargetPipeline.BuiltIn;
            else if (Type.GetType(RMConstants.CommonReflection.RP_URP_CAMFILTER) != null)
                win.targetPipeline = RMRenderMaster.TargetPipeline.URP;
        }

        public Texture2D eIcon_Head;
        public Texture2D eIcon_EntityAssetName;

        public Texture2D eIcon_PipelineBuiltIn;
        public Texture2D eIcon_PipelineURP;
        public Texture2D eIcon_PipelineHDRP;

        public Texture2D eIcon_RenderQuality;
        public Texture2D eIcon_RenderStandard;
        public Texture2D eIcon_RenderPerformant;

        public Texture2D eIcon_PlatformPCConsole;
        public Texture2D eIcon_PlatformPCVR;
        public Texture2D eIcon_PlatformMobile;
        public Texture2D eIcon_PlatformMobileVR;
        public Texture2D eIcon_PlatformWebGL;

        private string sessionName;
        private RMRenderMaster.TargetPipeline targetPipeline;
        private RMRenderMaster.TargetPlatform targetPlatform;
        private RMCoreRenderMasterRenderingData.RenderTypeOptions renderType;
        private Camera initCamera;

        private void OnGUI()
        {
            RMs();

            RMimage(eIcon_Head);
            RMRenderMasterEditorHelper.GenerateContactLayout();

            RMs();

            RMbv();

            RMinfoBox("Enter a unique Raymarcher asset name for this session." +
                     "\nThis identifier will be used to generate all the necessary shaders." +
                     "\nUse regular characters, numbers, and avoid special characters." +
                     "\nAssign the initial camera.", eIcon_EntityAssetName);
            RMi();
            sessionName = EditorGUILayout.TextField("Session Name", sessionName);
            initCamera = (Camera)EditorGUILayout.ObjectField("Initial Camera", initCamera, typeof(Camera), true);
            RMie();
            DrawUILine(Color.gray, 1, 5);

            string txt = "Choose a target renderer pipeline for this session.\n";
            Texture2D tex = null;
            switch (targetPipeline)
            {
                case RMRenderMaster.TargetPipeline.BuiltIn:
                    tex = eIcon_PipelineBuiltIn;
                    txt += "\nBuiltIn Render Pipeline (Legacy Renderer) is Unity’s\n" +
                        "general purpose render pipeline with limited options." +
                        "\nEach camera has its own camera filter for rendering.";
                    break;
                case RMRenderMaster.TargetPipeline.URP:
                    tex = eIcon_PipelineURP;
                    txt += "\nURP (Universal Renderer Pipeline) provides artist-friendly\nworkflows with optimized graphics across a range of platforms." +
                        "\nEach camera is processed in the specific renderer feature.";
                    break;
                case RMRenderMaster.TargetPipeline.HDRP:
                    tex = eIcon_PipelineHDRP;
                    txt += "\nHDRP (High-Definition Rendering Pipeline) is designed\n" +
                        "to produce high-quality visuals with ease." +
                        "\nEach camera is processed in the specific Custom Pass feature.";
                    break;
            }
            RMinfoBox(txt, tex);
            RMi();
            targetPipeline = (RMRenderMaster.TargetPipeline)EditorGUILayout.EnumPopup("Target Pipeline", targetPipeline);
            RMie();
            DrawUILine(Color.gray, 1, 5);

            txt = "Choose a target platform for this session.\n";
            tex = null;
            switch (targetPlatform)
            {
                case RMRenderMaster.TargetPlatform.PCConsole:
                    tex = eIcon_PlatformPCConsole;
                    txt += "\n<b>PC/ Console</b> platform." +
                        "\nSuitable for Windows 10+, MacOSX, Linux, Xbox One, PS4, PS5." +
                        "\nUtilizing DirectX to its full potential.";
                    break;
                case RMRenderMaster.TargetPlatform.PCVR:
                    tex = eIcon_PlatformPCVR;
                    txt += "\n<b>PC-VR</b> platform." +
                        "\nSuitable for HTC Vive, Valve Index, PSVR, Meta Quest-PC, Primax." +
                        "\nUtilizing DirectX to its full potential.";
                    break;
                case RMRenderMaster.TargetPlatform.Mobile:
                    tex = eIcon_PlatformMobile;
                    txt += "\n<b>Mobile</b> platform." +
                       "\nSuitable for iPhone, Android and low-end devices." +
                       "\nCompatible with OpenGL and Vulkan graphics.";
                    break;
                case RMRenderMaster.TargetPlatform.MobileVR:
                    tex = eIcon_PlatformMobileVR;
                    txt += "\n<b>Mobile VR</b> platform (standalone)." +
                       "\nSuitable for Meta Quest 2+, PICO 3+, Mobile AR/VR." +
                       "\nCompatible with OpenGL and Vulkan graphics.";
                    break;
                case RMRenderMaster.TargetPlatform.WebGL:
                    tex = eIcon_PlatformWebGL;
                    txt += "\n<b>WebGL</b> platform (pc only)." +
                     "\nSuitable for web applications." +
                     "\nCompatible with OpenGL and Vulkan graphics.";
                    break;
            }
            RMinfoBox(txt, tex);
            RMi();
            targetPlatform = (RMRenderMaster.TargetPlatform)EditorGUILayout.EnumPopup("Target Platform", targetPlatform);
            RMie();
            DrawUILine(Color.gray, 1, 5);

            txt = "Choose a target render type for this session.\n";
            tex = null;
            switch (renderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    tex = eIcon_RenderQuality;
                    txt += "\n<b>Quality</b> (experimental) supports:" +
                        "\n- Full object transform (active state, position, rotation, scale)" +
                        "\n- Color per object (full range in RGB)" +
                        "\n- Highest precision & data allocation (32 bytes per object)" +
                        "\n- Global/per object materials + Texture per object" +
                        "\nSuitable for high-end devices only.";
                    break;
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    tex = eIcon_RenderStandard;
                    txt += "\n<b>Standard</b> (recommended) supports:" +
                        "\n- Full object transform (active state, position, rotation, scale)" +
                        "\n- Hue shift per object (single float)" +
                        "\n- High precision & data allocation (16 bytes per object)" +
                        "\n- Global/per object materials" +
                        "\nSuitable for all devices, mostly mid-end.";
                    break;
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    tex = eIcon_RenderPerformant;
                    txt += "\n<b>Performant</b> supports:" +
                       "\n- Object transform's position only" +
                       "\n- Hue shift per object (single float)" +
                       "\n- Low precision & data allocation (8 bytes per object)" +
                       "\n- Global materials only" +
                       "\nSuitable for mobiles and low-end devices.";
                    break;
            }
            RMinfoBox(txt, tex);
            if (!RMRenderMaster.CanSwitchRenderType(targetPlatform, renderType))
            {
                EditorUtility.DisplayDialog("Warning", "Quality Render Type is not allowed for Mobile/ WebGL platforms! Please choose 'Standard' or 'Performant' (recommended) instead.", "OK");
                renderType = RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard;
            }
            RMi();
            renderType = (RMCoreRenderMasterRenderingData.RenderTypeOptions)EditorGUILayout.EnumPopup("Target Render Type", renderType);
            RMie();
            DrawUILine(Color.gray, 1, 5);

            RMs(5);

            if (targetPipeline != RMRenderMaster.TargetPipeline.BuiltIn && (targetPlatform == RMRenderMaster.TargetPlatform.PCVR || targetPlatform == RMRenderMaster.TargetPlatform.MobileVR))
                EditorGUILayout.HelpBox("VR for URP and HDRP is still in an experimental version!", MessageType.Warning);

            if (RMb("Setup Raymarcher"))
            {
                if (!CheckSessionFormat(sessionName))
                    return;
                if (initCamera == null)
                {
                    EditorUtility.DisplayDialog("Warning", "Initial Camera field cannot be empty!", "OK");
                    return;
                }
                CreateNewRaymarcherInstance();
                Close();
            }

            RMbve();
        }

        private static bool CheckSessionFormat(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                EditorUtility.DisplayDialog("Warning", "Raymarcher Session name cannot be empty!", "OK");
                return false;
            }
            if (sessionName.Length <= 2)
            {
                EditorUtility.DisplayDialog("Warning", "Raymarcher Session name is too short!\nMin 3 characters allowed.", "OK");
                return false;
            }
            if (sessionName.Length > 20)
            {
                EditorUtility.DisplayDialog("Warning", "Raymarcher Session name is too long!\nMax 20 characters allowed.", "OK");
                return false;
            }
            if (sessionName.Any(Path.GetInvalidFileNameChars().Contains))
            {
                EditorUtility.DisplayDialog("Warning", "Raymarcher Session name is invalid.\nPlease remove all special characters!", "OK");
                return false;
            }
            return true;
        }

        private void CreateNewRaymarcherInstance()
        {
            if (!EditorUtility.DisplayDialog("Warning", "The system is going to prepare the current scene for the Raymarcher renderer. Please close your programming environment to prevent " +
                 "further issues. If you've already created the Raymarcher, " +
                 "the new one will replace the current raymarcher renderer instance, so you will lose your current progress. Are you sure you want to proceed with this action?", "Yes", "No"))
                return;

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            RMRenderMaster currentRenderMaster = FindObjectOfType<RMRenderMaster>();
            if (currentRenderMaster != null)
                RemoveExistingRaymarcherInstance(currentRenderMaster);

            GameObject newRenderMaster = new GameObject("RaymarcherRenderer");
            RMRenderMaster raymarcherRenderMaster = newRenderMaster.AddComponent<RMRenderMaster>();
            raymarcherRenderMaster.RenderingData.SetRenderType(renderType);
            raymarcherRenderMaster.SetPipelineInEditor(targetPipeline);

            if (!RMConvertorCore.CreateNewRaymarcherInstance(raymarcherRenderMaster, sessionName, out string exception))
            {
                DestroyImmediate(newRenderMaster);
                EditorUtility.DisplayDialog("Error", "Raymarcher setup was unsuccessful. Exception: " + exception, "OK");
                return;
            }

            AssetDatabase.Refresh();

            CreateRaymarcherSessionMaterial(raymarcherRenderMaster, sessionName, targetPipeline);

            raymarcherRenderMaster.SetupRaymarcherInEditor(
                targetPipeline, 
                targetPlatform, 
                sessionName,
                initCamera);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            RMDebug.Debug(typeof(RMRenderMaster), "Raymarcher renderer is now ready to use");

            Selection.activeGameObject = newRenderMaster;
        }

        public static void CreateRaymarcherSessionMaterial(RMRenderMaster renderMaster, string sessionName, RMRenderMaster.TargetPipeline targetPipeline)
        {
            Material sessionMaterial = new Material(Shader.Find(RMConstants.RM_MASTERSHADER_PATH + sessionName));
            sessionMaterial.name = sessionName;

            if (targetPipeline != RMRenderMaster.TargetPipeline.BuiltIn)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Session Material Asset", sessionName, "mat", "Select a path for the session material asset");
                AssetDatabase.CreateAsset(sessionMaterial, path);
            }

            renderMaster.RenderingData.SetRendererMaterialSource(sessionMaterial);
        }

        public static void CloneExistingRaymarcherInstance(RMRenderMaster targetRenderMaster, string newSessionName)
        {
            if (!CheckSessionFormat(newSessionName))
                return;
            if(targetRenderMaster.RegisteredSessionName == newSessionName)
            {
                EditorUtility.DisplayDialog("Warning", "The new session name equals to the current session name! Please choose a different name.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Warning", "You are about to clone the current Raymarcher session. The current one will still exist in the assets with all its dependencies. The name of the new session will replace an existing session (if there is any). Are you sure to proceed this action? There is no undo and you will be responsible for the current session dependencies.", "Yes", "No") == false)
                return;

            string oldName = targetRenderMaster.RegisteredSessionName;
            if (!RMConvertorCore.CreateNewRaymarcherInstance(targetRenderMaster, newSessionName, out string exception))
            {
                EditorUtility.DisplayDialog("Error", "Raymarcher session clonning was unsuccessful. Exception: " + exception, "OK");
                return;
            }

            AssetDatabase.Refresh();

            CreateRaymarcherSessionMaterial(targetRenderMaster, newSessionName, targetRenderMaster.CompiledTargetPipeline);

            targetRenderMaster.SetOverrideSessionName(newSessionName);

            RMConvertorSdfObjectBuffer.ConvertAndWriteToSdfObjectBuffer(targetRenderMaster.MappingMaster);
            RMConvertorMaterialBuffer.ConvertAndWriteToMaterialBuffer(targetRenderMaster);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            RMDebug.Debug(typeof(RMRenderMaster), $"New Raymarcher session has been successfully cloned from '{oldName}' to '{newSessionName}'");

            Selection.activeGameObject = targetRenderMaster.gameObject;
        }

        public static void RemoveExistingRaymarcherInstance(RMRenderMaster targetRenderMaster)
        {
            Selection.objects = null;

            targetRenderMaster.Dispose();

            RMConvertorCore.RemoveExistingRaymarcherInstance(targetRenderMaster, targetRenderMaster.RegisteredSessionName);

            DestroyImmediate(targetRenderMaster.gameObject);

            AssetDatabase.Refresh();
            EditorSceneManager.SaveOpenScenes();
        }

        #region GUI Helpers

        private void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        private void RMinfoBox(string text, Texture2D addIcon)
        {
            RMbh();
            RMl("?  ", true, 20);
            RMl(text, false, 10, TextAnchor.LowerLeft);
            GUILayout.FlexibleSpace();
            if (addIcon != null)
                RMimage(addIcon, 64, 64);
            RMbhe();
        }

        private void RMbv(bool box = true)
        {
            if (box)
                GUILayout.BeginVertical("Box");
            else
                GUILayout.BeginVertical();
        }

        private void RMbve()
        {
            GUILayout.EndVertical();
        }

        private void RMbh(bool box = true)
        {
            if (box)
                GUILayout.BeginHorizontal("Box");
            else
                GUILayout.BeginHorizontal();
        }

        private void RMbhe()
        {
            GUILayout.EndHorizontal();
        }

        private void RMimage(Texture entry, int width, int height)
        {
            GUILayout.Label(entry, GUILayout.Width(width), GUILayout.Height(height));
        }

        private void RMimage(Texture entry)
        {
            GUILayout.Label(entry);
        }

        private void RMs(float index = 10)
        {
            GUILayout.Space(index);
        }

        private bool RMb(string innerText, float Width = 0, string tooltip = "")
        {
            if (Width == 0)
                return GUILayout.Button(new GUIContent(innerText, tooltip));
            else
                return GUILayout.Button(new GUIContent(innerText, tooltip), GUILayout.Width(Width));
        }

        private void RMl(string text, bool bold = false, int size = 0, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { richText = true };
            style.alignment = alignment;
            if (size != 0) style.fontSize = size;
            if (bold) style.fontStyle = FontStyle.Bold;
            GUILayout.Label(text, style);
        }

        private void RMi()
        {
            GUI.backgroundColor = Color.cyan;
        }

        private void RMie()
        {
            GUI.backgroundColor = Color.white;
        }

        #endregion
    }
}