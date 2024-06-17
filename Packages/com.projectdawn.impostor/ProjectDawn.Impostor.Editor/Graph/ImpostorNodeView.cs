using GraphProcessor;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDawn.Impostor.Editor
{
    [NodeCustomEditor(typeof(ImpostorNode))]
    public class ImpostorNodeView : BaseNodeView
    {
        public override void Enable()
        {
            base.Enable();

            if (TryGetHelpUrl(out _))
            {
                var settingButton = new Button(HelpButton)
                {
                    name = "mini-button",
                };

                settingButton.style.borderTopColor = new StyleColor(new Color(0, 0, 0, 0));
                settingButton.style.borderBottomColor = new StyleColor(new Color(0, 0, 0, 0));
                settingButton.style.borderRightColor = new StyleColor(new Color(0, 0, 0, 0));
                settingButton.style.borderLeftColor = new StyleColor(new Color(0, 0, 0, 0));

                settingButton.Add(new Image
                {
                    name = "icon",
                    scaleMode = ScaleMode.ScaleToFit,
                    image = EditorGUIUtility.IconContent("d_Help").image,
                });

                titleContainer.Add(settingButton);
            }
        }

        void HelpButton()
        {
            if (TryGetHelpUrl(out var url))
                Application.OpenURL(url);
        }

        bool TryGetHelpUrl(out string url)
        {
            var node = nodeTarget;
            var type = node.GetType();
            var attribute = Attribute.GetCustomAttribute(type, typeof(HelpURLAttribute)) as HelpURLAttribute;
            if (attribute != null)
            {
                url = attribute.URL;
                return true;
            }

            url = null;
            return false;
        }

        public override void OnCreated()
        {
            var node = nodeTarget;
            var type = node.GetType();
            foreach (var field in type.GetFields())
            {
                if (field.GetValue(node) != null)
                    continue;

                var attribute = Attribute.GetCustomAttribute(field, typeof(ReloadAttribute)) as ReloadAttribute;
                if (attribute != null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath(attribute.Path, field.FieldType);
                    if (asset == null)
                        throw new InvalidOperationException($"{type} failed to load asst at path {attribute.Path}.");
                    field.SetValue(node, asset);
                }

            }
        }
    }
}