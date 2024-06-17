using UnityEngine;
using UnityEditor;

using Raymarcher.Objects.Modifiers;

namespace Raymarcher.UEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMObjectModifierBase), true)]
    public sealed class RMObjectModifierBaseEditor : RMEditorUtilities
    {
        private RMObjectModifierBase objectModifierBase;
        private bool supportSharedModifier = false;
        private RMObjectModifierSharedContainer container;
        private bool isValid = true;

        private void OnEnable()
        {
            objectModifierBase = (RMObjectModifierBase)target;
            supportSharedModifier = objectModifierBase.ModifierSupportsSharedContainer;
            if (supportSharedModifier)
            {
                container = objectModifierBase.SharedModifierContainer;
                if(container && container.SharedContainerInstance != null)
                {
                    if (container.SharedContainerInstance.GetType() != objectModifierBase.CreateSharedModifierContainer.GetType())
                        isValid = false;
                    else
                        objectModifierBase.PassSharedContainerDataToModifier();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (supportSharedModifier)
            {
                if (objectModifierBase.SdfTarget.RenderMaster && objectModifierBase.SdfTarget.RenderMaster.RecompilationRequiredSdfObjectBuffer)
                    RMhelpbox("Recompilation of Sdf buffer is required. Please recompile the Sdf buffer above");
                if (!isValid)
                    RMhelpbox($"Invalid shared container type. The shared container must contain the exactly defined container type as it's on this modifier. Please assign a correct shared container", MessageType.Warning);
                else if (container)
                    RMhelpbox($"This object uses a shared modifier instance called '{container.SharedContainerIdentifier}'. All the changes will be synchronized with the shared modifier", MessageType.None);
                RMs(5);
                if (RMb("Create Shared Container"))
                {
                    string path = EditorUtility.SaveFilePanel("Select Path To Create New Shared Container", Application.dataPath, nameof(RMObjectModifierSharedContainer), "asset");
                    if (string.IsNullOrEmpty(path))
                        return;
                    string relativePath = path.Substring(path.IndexOf("Assets/"));
                    RMObjectModifierSharedContainer shared = ScriptableObject.CreateInstance<RMObjectModifierSharedContainer>();
                    shared.SetSharedContainer(objectModifierBase.CreateSharedModifierContainer);

                    AssetDatabase.CreateAsset(shared, relativePath);
                }
                RMproperty("sharedContainer");
                RMs(5);
            }
            RMbh();
            if (RMb("Remove", 120))
            {
                DestroyImmediate(objectModifierBase);
                return;
            }
            if (RMb("Move Up", 80))
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(objectModifierBase);
                objectModifierBase.SdfTarget.RefreshModifiers();
            }
            if (RMb("Move Down", 80))
            {
                UnityEditorInternal.ComponentUtility.MoveComponentDown(objectModifierBase);
                objectModifierBase.SdfTarget.RefreshModifiers();
            }
            RMbhe();
            DrawPropertiesExcluding(serializedObject, "sharedContainer");
            serializedObject.ApplyModifiedProperties();
        }
    }
}