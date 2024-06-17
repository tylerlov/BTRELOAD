using UnityEngine;
using UnityEditor;

namespace ProjectDawn.Impostor.Editor
{
    class ImpostorBuilderCreator : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public GameObject SelectedGameObject;
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var newAsset = ImpostorBuilderEditor.CreateDefaultImpostorBuilder(pathName);
            if (SelectedGameObject != null)
                newAsset.SetGameObject("Source", SelectedGameObject);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
        }

        [MenuItem("Assets/Create/Impostor", priority = 150)]
        static void CreateUniversalRenderPipelineGlobalSettings()
        {
            var creator = ScriptableObject.CreateInstance<ImpostorBuilderCreator>();
            creator.SelectedGameObject = Selection.activeGameObject;

            string defaultName;
            if (Selection.activeGameObject == null)
            {
                defaultName = "New Impostor.asset";
            }
            else
            {
                defaultName = $"{Selection.activeGameObject.name}.asset";
            }

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, creator, defaultName, null, null);
        }
    }
}
