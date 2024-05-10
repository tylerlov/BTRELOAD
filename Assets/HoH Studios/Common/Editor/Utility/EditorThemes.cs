using UnityEditor;
using UnityEngine;

namespace HohStudios.Editor
{
    /// <summary>
    /// Class to hold themes for editor IMGUI
    /// </summary>
    public sealed class EditorThemes : UnityEditor.Editor
    {
        // The initial theme is the default unity theme used when restoring the editor settings
        private static EditorTheme _initialTheme = new EditorTheme();
        public static float InitialLabelWidth;

        // Allows the ability to force default editor themes/fonts/colors ontop of custom ones to "short circuit" back to defaults
        private static bool _forceDefaultEditorTheme;
        private static bool _forceDefaultEditorThemeFonts;
        private static bool _forceDefaultEditorThemeColors;
        /// <summary>
        /// The editor theme template is an abstraction of basic editor drawing theme properties
        /// Makes it a convenient way to change editor drawing properties without settings everying manually
        /// </summary>
        public static EditorTheme DefaultTheme
        {
            get
            {
                if (_initialTheme.NormalColor == default)
                {
                    // First set initial theme
                    _initialTheme.SetLabels(EditorStyles.label.font, EditorStyles.label.fontSize,
                        EditorStyles.label.fontStyle,
                        EditorGUIUtility.labelWidth, EditorStyles.label.alignment);
                    _initialTheme.SetColors(EditorStyles.label.normal.textColor, EditorStyles.label.hover.textColor,
                        EditorStyles.label.focused.textColor, EditorStyles.label.active.textColor);

                    if (InitialLabelWidth < 0.01f)
                        InitialLabelWidth = EditorGUIUtility.labelWidth;
                }

                return _initialTheme;
            }
        }

        /// <summary>
        /// Sets the editor theme to the label and content theme provided, setting all of the scattered editor utility settings.
        /// Defaults to the unity default theme when entries are default. (NEEDS TO BE CALLED IN AN UPDATE LOOP TO WORK)
        /// </summary>
        public static void SetEditorTheme(EditorTheme labelTheme, EditorTheme contentTheme = new EditorTheme())
        {
            if (_forceDefaultEditorTheme)
            {
                labelTheme = new EditorTheme();
                contentTheme = new EditorTheme();
            }

            // Label width fraction must be between 0 & 1
            labelTheme.LabelWidthFraction = Mathf.Clamp01(labelTheme.LabelWidthFraction);

            // Get the rect & width of the inspector currently
            var inspectorRect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelTheme.LabelWidthFraction < 0.01f
                ? InitialLabelWidth * 1.33f
                : inspectorRect.width * labelTheme.LabelWidthFraction;

            SetEditorThemeFonts(labelTheme, contentTheme);
            SetEditorThemeColors(labelTheme, contentTheme);
        }
        /// <summary>
        /// Sets the editor theme to the label and content theme provided, setting all of the scattered editor utility settings.
        /// Defaults to the unity default theme when entries are default. (NEEDS TO BE CALLED IN AN UPDATE LOOP TO WORK)
        /// </summary>
        public static void SetEditorTheme(EditorDualTheme labelTheme, EditorDualTheme contentTheme = new EditorDualTheme())
        {
            SetEditorTheme(labelTheme.CurrentTheme, contentTheme.CurrentTheme);
        }

        /// <summary>
        /// Sets the editor theme to the label and content theme provided, setting all of the scattered editor utility settings.
        /// Defaults to the unity default theme when entries are default. (NEEDS TO BE CALLED IN AN UPDATE LOOP TO WORK)
        /// </summary>
        public static void SetEditorThemeFonts(EditorTheme labelTheme, EditorTheme contentTheme)
        {
            var defaultTheme = DefaultTheme;

            EditorStyles.label.alignment =
                labelTheme.Alignment == default ? defaultTheme.Alignment : labelTheme.Alignment;
            GUI.skin.label.alignment =
                contentTheme.Alignment == default ? defaultTheme.Alignment : contentTheme.Alignment;

            if (_forceDefaultEditorThemeFonts)
            {
                labelTheme = new EditorTheme();
                contentTheme = new EditorTheme();
            }

            // Set all theme attributes into the editor drawing properties, defaulting to initial theme if the value == default
            EditorStyles.label.font = labelTheme.LabelFont == default ? defaultTheme.LabelFont : labelTheme.LabelFont;
            EditorStyles.label.fontSize = labelTheme.LabelFontSize == default
                ? defaultTheme.LabelFontSize
                : labelTheme.LabelFontSize;
            EditorStyles.label.fontStyle = labelTheme.LabelFontStyle == default
                ? defaultTheme.LabelFontStyle
                : labelTheme.LabelFontStyle;
            EditorStyles.label.fontSize = EditorStyles.label.fontSize;

            GUI.skin.label.font = contentTheme.LabelFont == default ? defaultTheme.LabelFont : contentTheme.LabelFont;
            GUI.skin.label.fontSize = contentTheme.LabelFontSize == default
                ? defaultTheme.LabelFontSize
                : contentTheme.LabelFontSize;
            GUI.skin.label.fontStyle = contentTheme.LabelFontStyle == default
                ? defaultTheme.LabelFontStyle
                : contentTheme.LabelFontStyle;
            GUI.skin.label.fontSize = GUI.skin.label.fontSize;
        }

        /// <summary>
        /// Sets the editor theme to the label and content theme provided, setting all of the scattered editor utility settings.
        /// Defaults to the unity default theme when entries are default. (NEEDS TO BE CALLED IN AN UPDATE LOOP TO WORK)
        /// </summary>
        public static void SetEditorThemeColors(EditorTheme labelTheme, EditorTheme contentTheme)
        {
            if (_forceDefaultEditorThemeColors)
            {
                labelTheme = new EditorTheme();
                contentTheme = new EditorTheme();
            }

            var defaultTheme = DefaultTheme;

            EditorStyles.label.normal = new GUIStyleState()
            {
                textColor = labelTheme.NormalColor == default ? defaultTheme.NormalColor : labelTheme.NormalColor
            };
            EditorStyles.label.hover = new GUIStyleState()
            {
                textColor = labelTheme.HoverColor == default ? defaultTheme.HoverColor : labelTheme.HoverColor
            };
            EditorStyles.label.focused = new GUIStyleState()
            {
                textColor = labelTheme.FocusedColor == default ? defaultTheme.FocusedColor : labelTheme.FocusedColor
            };
            EditorStyles.label.active = new GUIStyleState()
            {
                textColor = labelTheme.ActiveColor == default ? defaultTheme.ActiveColor : labelTheme.ActiveColor
            };

            GUI.skin.label.normal = new GUIStyleState()
            {
                textColor = contentTheme.NormalColor == default ? defaultTheme.NormalColor : contentTheme.NormalColor
            };
            GUI.skin.label.hover = new GUIStyleState()
            {
                textColor = contentTheme.HoverColor == default ? defaultTheme.HoverColor : contentTheme.HoverColor
            };
            GUI.skin.label.focused = new GUIStyleState()
            {
                textColor = contentTheme.FocusedColor == default ? defaultTheme.FocusedColor : contentTheme.FocusedColor
            };
            GUI.skin.label.active = new GUIStyleState()
            {
                textColor = contentTheme.ActiveColor == default ? defaultTheme.ActiveColor : contentTheme.ActiveColor
            };

#if !UNITY_2018 && !UNITY_2017
            EditorStyles.objectField.normal = new GUIStyleState() { textColor =
 contentTheme.NormalColor == default ? defaultTheme.NormalColor : contentTheme.NormalColor };
#endif
            EditorStyles.objectField.hover = new GUIStyleState()
            {
                textColor = contentTheme.HoverColor == default ? defaultTheme.HoverColor : contentTheme.HoverColor
            };
            EditorStyles.objectField.focused = new GUIStyleState()
            {
                textColor = contentTheme.FocusedColor == default ? defaultTheme.FocusedColor : contentTheme.FocusedColor
            };
            EditorStyles.objectField.active = new GUIStyleState()
            {
                textColor = contentTheme.ActiveColor == default ? defaultTheme.ActiveColor : contentTheme.ActiveColor
            };

#if !UNITY_2018 && !UNITY_2017
            EditorStyles.numberField.normal = new GUIStyleState() { textColor =
 contentTheme.NormalColor == default ? defaultTheme.NormalColor : contentTheme.NormalColor };
#endif
            EditorStyles.numberField.hover = new GUIStyleState()
            {
                textColor = contentTheme.HoverColor == default ? defaultTheme.HoverColor : contentTheme.HoverColor
            };
            EditorStyles.numberField.focused = new GUIStyleState()
            {
                textColor = contentTheme.FocusedColor == default ? defaultTheme.FocusedColor : contentTheme.FocusedColor
            };
            EditorStyles.numberField.active = new GUIStyleState()
            {
                textColor = contentTheme.ActiveColor == default ? defaultTheme.ActiveColor : contentTheme.ActiveColor
            };

#if !UNITY_2018 && !UNITY_2017
            EditorStyles.textField.normal = new GUIStyleState() { textColor =
 contentTheme.NormalColor == default ? defaultTheme.NormalColor : contentTheme.NormalColor };
#endif
            EditorStyles.textField.hover = new GUIStyleState()
            {
                textColor = contentTheme.HoverColor == default ? defaultTheme.HoverColor : contentTheme.HoverColor
            };
            EditorStyles.textField.focused = new GUIStyleState()
            {
                textColor = contentTheme.FocusedColor == default ? defaultTheme.FocusedColor : contentTheme.FocusedColor
            };
            EditorStyles.textField.active = new GUIStyleState()
            {
                textColor = contentTheme.ActiveColor == default ? defaultTheme.ActiveColor : contentTheme.ActiveColor
            };
        }

        /// <summary>
        /// Forces the editor to use the default editor theme until RestoreEditorTheme is called, meant to be used at start of DrawCustomInspector
        /// </summary>
        public static void ForceEditorDefaultTheme()
        {
            SetEditorTheme(new EditorTheme());
            _forceDefaultEditorTheme = true;
        }

        /// <summary>
        /// Forces the editor to use the default editor theme until RestoreEditorTheme is called, meant to be used at start of DrawCustomInspector
        /// </summary>
        public static void ForceEditorDefaultThemeFonts()
        {
            SetEditorThemeFonts(new EditorTheme(), new EditorTheme());
            _forceDefaultEditorThemeFonts = true;
        }

        /// <summary>
        /// Forces the editor to use the default editor theme until RestoreEditorTheme is called, meant to be used at start of DrawCustomInspector
        /// </summary>
        public static void ForceEditorDefaultThemeColors()
        {
            SetEditorThemeColors(new EditorTheme(), new EditorTheme());
            _forceDefaultEditorThemeColors = true;
        }


        /// <summary>
        /// Allow an in-line function call to apply the current label and content editor themes to whatever the inspetor draws afterwards.
        /// Used to restore editor drawing to defaults after calling SetEditorTheme()
        /// </summary>
        public static void RestoreEditorTheme()
        {
            _forceDefaultEditorTheme = false;
            _forceDefaultEditorThemeColors = false;
            _forceDefaultEditorThemeFonts = false;
            SetEditorTheme(new EditorTheme());
        }


    }


    /// <summary>
    /// This dual theme was added to support easy theme management between light/dark unity profiles
    /// </summary>
    public struct EditorDualTheme
    {
        public EditorTheme CurrentTheme => EditorGUIUtility.isProSkin ? DarkTheme : LightTheme;
        public EditorTheme LightTheme;
        public EditorTheme DarkTheme;
    }

    /// <summary>
    /// This struct is the "abstraction" for styling the editor with convenient function calls for styling templates.
    /// Typical GUI Styling is very confusing to get the styling right, so this struct acts as a template that can be converted into a GUIStyle
    /// </summary>
    public struct EditorTheme
    {
        // The basic editor styling settings
        public Font LabelFont;
        public int LabelFontSize;
        public FontStyle LabelFontStyle;

        public float
            LabelWidthFraction; // The fraction of the horizontal label width, where 1 is the entire width of the inspector

        public TextAnchor Alignment;

        public Color NormalColor;
        public Color HoverColor;
        public Color FocusedColor;
        public Color ActiveColor;

        public Color OnNormalColor;
        public Color OnHoverColor;
        public Color OnFocusedColor;
        public Color OnActiveColor;

        /// <summary>
        /// Easy initializer for label customization (to be called at start of frame or OnEnable typically)
        /// </summary>
        public void SetLabels(Font labelFont, int labelFontSize, FontStyle labelFontStyle, float labelWidth,
            TextAnchor alignment)
        {
            LabelFont = labelFont;
            LabelFontSize = labelFontSize;
            LabelFontStyle = labelFontStyle;

            LabelWidthFraction = labelWidth;
            Alignment = alignment;
        }

        /// <summary>
        /// Easy initializer for general color customization (to be called at start of frame or OnEnable typically)
        /// </summary>
        public void SetColors(Color normalColor, Color hoverColor, Color focusedColor, Color activeColor)
        {
            NormalColor = normalColor;
            HoverColor = hoverColor;
            FocusedColor = focusedColor;
            ActiveColor = activeColor;
            OnNormalColor = normalColor;
            OnHoverColor = hoverColor;
            OnFocusedColor = focusedColor;
            OnActiveColor = activeColor;
        }

        /// <summary>
        /// Converts the struct into a GUIStyle so it can be used with standard editor drawing functions
        /// </summary>
        /// <returns></returns>
        public GUIStyle Style(GUIStyle displayStyle)
        {
            return new GUIStyle(displayStyle)
            {
                font = LabelFont == default ? EditorStyles.label.font : LabelFont,
                fontSize = LabelFontSize == default ? EditorStyles.label.fontSize : LabelFontSize,
                fontStyle = LabelFontStyle == default ? EditorStyles.label.fontStyle : LabelFontStyle,
                alignment = Alignment == default ? EditorStyles.label.alignment : Alignment,
                normal = new GUIStyleState()
                {
                    textColor = NormalColor == default ? EditorStyles.label.normal.textColor : NormalColor
                },
                hover = new GUIStyleState()
                {
                    textColor = HoverColor == default ? EditorStyles.label.hover.textColor : HoverColor
                },
                focused = new GUIStyleState()
                {
                    textColor = FocusedColor == default ? EditorStyles.label.focused.textColor : FocusedColor
                },
                active = new GUIStyleState()
                {
                    textColor = ActiveColor == default ? EditorStyles.label.active.textColor : ActiveColor
                },
                onNormal = new GUIStyleState()
                {
                    textColor = OnNormalColor == default ? EditorStyles.label.normal.textColor : OnNormalColor
                },
                onHover = new GUIStyleState()
                {
                    textColor = OnHoverColor == default ? EditorStyles.label.hover.textColor : OnHoverColor
                },
                onFocused = new GUIStyleState()
                {
                    textColor = OnFocusedColor == default ? EditorStyles.label.focused.textColor : OnFocusedColor
                },
                onActive = new GUIStyleState()
                {
                    textColor = OnActiveColor == default ? EditorStyles.label.active.textColor : OnActiveColor
                },
            };
        }
    }
}