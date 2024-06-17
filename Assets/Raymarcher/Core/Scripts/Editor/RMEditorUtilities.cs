using UnityEngine;
using UnityEditor;

namespace Raymarcher.UEditor
{
    public abstract class RMEditorUtilities : Editor
    {
        protected static void RMproperty(SerializedProperty sp, string text = "DEFAULT", string tooltip = "", bool hasChildren = false)
        {
            string txt = text;
            if (txt == "DEFAULT")
                EditorGUILayout.PropertyField(sp, hasChildren);
            else
                EditorGUILayout.PropertyField(sp, new GUIContent(txt, tooltip), hasChildren);
        }

        protected void RMproperty(string name, string text = "DEFAULT", string tooltip = "", bool hasChildren = false)
		{
            var prop = serializedObject.FindProperty(name);
            if (prop == null)
            {
                Debug.LogError($"Property '{name}' cannot be found'");
                return;
            }
            string txt = text;
			if(txt == "DEFAULT")
                EditorGUILayout.PropertyField(prop, hasChildren);
            else
                EditorGUILayout.PropertyField(prop, new GUIContent(txt, tooltip), hasChildren);
        }

        protected void RMkeyWordProperty(Material mat, string property, string kwd, bool entry, string head = "DEFAULT", string tooltip = "")
        {
            RMproperty(property, head, tooltip);
            RMcheckKeyword(mat, kwd, entry);
        }

        protected static void RMcheckKeyword(Material mat, string kwd, bool entry)
        {
            if (!entry && mat.IsKeywordEnabled(kwd))
                mat.DisableKeyword(kwd);
            else if (entry && !mat.IsKeywordEnabled(kwd))
                mat.EnableKeyword(kwd);
        }

        protected static void RMhelpbox(string text, MessageType m = MessageType.Warning)
        {
            EditorGUILayout.HelpBox(text, m);
        }

        protected static bool RMDDialog(string title, string message, string yes = "Yes", string no = "No")
            => EditorUtility.DisplayDialog(title, message, yes, no);

        protected static void RMlvlPlus(int lvl = 2)
        {
            EditorGUI.indentLevel += lvl;
        }

        protected static void RMlvlMinus(int lvl = 2)
        {
            EditorGUI.indentLevel -= lvl;
        }

        protected static void RMbv(bool box = true)
		{
            if (box)
                GUILayout.BeginVertical("Box");
            else
                GUILayout.BeginVertical();
		}

        protected static void RMbve()
        {
            GUILayout.EndVertical();
		}

        protected static void RMbh(bool box = true)
        {
            if (box)
                GUILayout.BeginHorizontal("Box");
            else
                GUILayout.BeginHorizontal();
        }

        protected static void RMbhe()
        {
            GUILayout.EndHorizontal();
        }

        protected static void RMimage(Texture entry)
        {
            GUILayout.Label(entry);
        }

        protected static void RMimage(Texture entry, int width, int height)
        {
            GUILayout.Label(entry, GUILayout.Width(width), GUILayout.Height(height));
        }

        protected static void RMs(float index = 10)
		{
			GUILayout.Space(index);
		}

        protected static bool RMb(string innerText, float width = 0, string tooltip = "", RectOffset margin = null)
		{
            if (width != 0)
                return GUILayout.Button(new GUIContent(innerText, tooltip), GUILayout.Width(width));
            else
                return GUILayout.Button(new GUIContent(innerText, tooltip));
        }

        protected static bool RMb(string innerText, RectOffset margin, string tooltip = "", TextAnchor alighnment = TextAnchor.MiddleCenter)
        {
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.margin = margin;
            buttonStyle.alignment = alighnment;
            return GUILayout.Button(new GUIContent(innerText, tooltip), buttonStyle);
        }

        protected static void RMl(string text, bool bold = false, int size = 0, int topPadding = 0)
		{
            GUIStyle style = new GUIStyle(GUI.skin.label) { richText = true };
            if (size != 0) style.fontSize = size;
            if(bold) style.fontStyle = FontStyle.Bold;
            var pad = style.padding;
            pad.top += topPadding;
            GUILayout.Label(text, style);
		}

        protected static void RMleditor(string text, bool bold = false, int size = 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { richText = true };
            if (size != 0) style.fontSize = size;
            if (bold) style.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(text, style);
        }


        private static readonly Color DefaultLineColor = Color.gray / 1.75f;

        protected static void RMline(Color color = default, int thickness = 2, int padding = 10, int widthAdd = 0)
        {
            if (color == default)
                color = DefaultLineColor;

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2 + (widthAdd / 3);
            r.width += 6 + widthAdd;
            EditorGUI.DrawRect(r, color);
        }
    }
}