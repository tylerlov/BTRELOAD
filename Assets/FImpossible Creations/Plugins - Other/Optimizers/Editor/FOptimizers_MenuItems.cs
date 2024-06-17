using System.IO;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public static class FOptimizers_MenuItems
    {
        //[MenuItem("Assets/Create/FImpossible Games/Optimizers/Create Custom Component's LODs Controller Script (rename after)", false, 0)]
        //static void CreateCustomLODsController()
        //{
        //    string fullPath = Application.dataPath + "/FImpossible Games/Optimizers/Scripts/Optimize Types/Templates/FLODSController_Template.txt";
        //    CreateScriptAsset("Assets/FImpossible Games/Optimizers/Scripts/Optimize Types/Templates/FLODSController_Template.txt", "YourComponentNameHere.cs", fullPath);
        //}

        [MenuItem("Assets/Create/FImpossible Creations/Optimizers/Create Custom Component's LODs Parameters Script (rename after)", false, 1)]
        static void CreateCustomLODParamsScript()
        {
            string fullPath = Application.dataPath + "/FImpossible Creations/Optimizers/Scripts/Templates/FLODType_Template.txt";
            CreateScriptAsset("Assets/FImpossible Creations/Optimizers/Scripts/Templates/FLODType_Template.txt", "YourComponentNameHere.cs", fullPath);
        }

        static void CreateScriptAsset(string templatePath, string targetName, string fullPath)
        {
            if (File.Exists(fullPath))
            {
#if UNITY_2019_1_OR_NEWER
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, targetName);
#else
                typeof(ProjectWindowUtil).GetMethod("CreateScriptAsset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { templatePath, targetName });
#endif
            }
            else
                Debug.LogError("[OPTIMIZERS] File under path '" + fullPath + "' doesn't exist, directory probably was moved");
        }
    }
}