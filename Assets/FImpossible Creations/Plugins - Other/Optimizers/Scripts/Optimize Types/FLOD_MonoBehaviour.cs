using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Helper class for single LOD level settings on MonoBehaviour
    /// </summary>
    //[CreateAssetMenu(fileName = "MonoBehaviour Reference - Move it to Resources - Optimizers - Custom", menuName = "Custom Optimizers/FLOD_MonoBehaviour Reference")]
    public sealed class FLOD_MonoBehaviour : FLOD_Base
    {
        //public Type TypeOptimized;

        public bool BaseLOD = false;

        public UnityEvent Event;
        public List<ParameterHelper> Parameters;
        public List<ParameterHelper> NotSupported;
        internal bool DrawNotSupported = false;

#if UNITY_EDITOR
        private bool drawEvent = false;
#endif

        public static readonly int intId = "int".GetHashCode();
        public static readonly int floatId = "float".GetHashCode();
        public static readonly int boolId = "bool".GetHashCode();
        //private readonly int v2Id = "Vector2".GetHashCode();
        //private readonly int v3Id = "Vector3".GetHashCode();
        public static readonly int colorId = "Color".GetHashCode();


        #region Initialization


        /// PROVIDE SETTINGS FOR YOUR LOD
        public FLOD_MonoBehaviour()
        {
            // If you don't want to use transitions (InterpolateBetween) - then set "SupportingTransitions" to false
            // But if you implement interpolation then set it to true
            SupportingTransitions = true;
            HeaderText = "MonoBehaviour LOD Settings";
            CustomEditor = true;
        }


        public override FLOD_Base GetLODInstance()
        {
            return CreateInstance<FLOD_MonoBehaviour>();
        }

        public override FLOD_Base CreateNewCopy()
        {
            FLOD_MonoBehaviour newMono = CreateInstance<FLOD_MonoBehaviour>();
            newMono.CopyBase(this);
            newMono.Parameters = new List<ParameterHelper>();

            if ( Parameters != null)
            for (int i = 0; i < Parameters.Count; i++)
            {
                ParameterHelper helper = new ParameterHelper(Parameters[i].ParamName, Parameters[i].ParamType);
                helper.SetValue(Parameters[i].TypeID, Parameters[i].GetValue(Parameters[i].TypeID));
                helper.Change = Parameters[i].Change;
                newMono.Parameters.Add(helper);
            }

            return newMono;
        }


        public override void SetSameValuesAsComponent(Component component)
        {
            if (component == null) Debug.LogError("[OPTIMIZERS] Given component is null instead of MonoBehaviour!");

            MonoBehaviour comp = component as MonoBehaviour;
            //TypeOptimized = component.GetType();

            if (Version == 0)
                if (comp != null)
                {
#if UNITY_EDITOR
                    // Finding parameters available to optimize
                    SerializedObject s = new UnityEditor.SerializedObject(component);
                    if (Parameters == null) Parameters = new List<ParameterHelper>();
                    if (NotSupported == null) NotSupported = new List<ParameterHelper>();

                    var prop = s.GetIterator();
                    int safeLimit = 0;
                    prop.NextVisible(true); // ignoring "Script" field

                    while (prop.NextVisible(false))
                    {
                        ParameterHelper helper = new ParameterHelper(prop.name, prop.type);
                        bool supported = true;

                        switch (prop.type)
                        {
                            case "float": helper.Float = prop.floatValue; break;
                            case "int": helper.Int = prop.intValue; break;
                            case "bool": helper.Bool = prop.boolValue; break;
                            //case "Vector2": helper.Vec2 = prop.vector2Value; break;
                            //case "Vector3": helper.Vec3 = prop.vector3Value; break;
                            case "Color": helper.Color = prop.colorValue; break;
                            default: supported = false; helper.Supported = false; break;
                        }

                        bool contains = false;
                        for (int p = 0; p < Parameters.Count; p++)
                            if (Parameters[p].ParamName == prop.name)
                            {
                                contains = true;
                                break;
                            }

                        if (!contains)
                        {
                            if (supported)
                                Parameters.Add(helper);
                            else
                                NotSupported.Add(helper);

                        }

                        if (++safeLimit > 1000) break;
                    }
#endif
                }
        }


        #endregion


        #region Operations


        public override void InterpolateBetween(FLOD_Base lodA, FLOD_Base lodB, float transitionToB)
        {
            base.InterpolateBetween(lodA, lodB, transitionToB);
            if (Version == 1) return;

            FLOD_MonoBehaviour a = lodA as FLOD_MonoBehaviour;
            FLOD_MonoBehaviour b = lodB as FLOD_MonoBehaviour;

            BaseLOD = b.BaseLOD;

            if ( Parameters != null)
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (b.Parameters[i].Change)
                {
                    Parameters[i].Change = true;
                }

                if (!a.BaseLOD)
                    if (!a.Parameters[i].Change)
                    {
                        Parameters[i].SetValue(Parameters[i].TypeID, b.Parameters[i].GetValue(Parameters[i].TypeID));
                        continue;
                    }

                if (Parameters[i].TypeID == intId)
                {
                    Parameters[i].Int = (int)Mathf.Lerp(a.Parameters[i].Int, b.Parameters[i].Int, transitionToB);
                }
                else if (Parameters[i].TypeID == floatId)
                {
                    Parameters[i].Float = Mathf.Lerp(a.Parameters[i].Float, b.Parameters[i].Float, transitionToB);
                }
                else if (Parameters[i].TypeID == boolId)
                {
                    if (transitionToB > 0.5f)
                        Parameters[i].Bool = b.Parameters[i].Bool;
                    else
                        Parameters[i].Bool = a.Parameters[i].Bool;
                }
                else if (Parameters[i].TypeID == colorId)
                {
                    Parameters[i].Color = Color.Lerp(a.Parameters[i].Color, b.Parameters[i].Color, transitionToB);
                }
            }
        }


        public override void ApplySettingsToComponent(Component component, FLOD_Base initialSettingsReference)
        {
            if (Version == 0)
            {
                // Casting LOD to correct type
                FLOD_MonoBehaviour initialSettings = initialSettingsReference as FLOD_MonoBehaviour;

                #region Security

                // Checking if casting is right
                if (initialSettings == null) { Debug.Log("[OPTIMIZERS] Target LOD is not MonoBehaviour LOD or is null"); return; }

                #endregion

            if ( Parameters != null)
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (!Parameters[i].Change && BaseLOD == false) continue;

                    FieldInfo field = component.GetType().GetField(Parameters[i].ParamName);

                    if (field != null)
                    {
                        if (Parameters[i].TypeID == FLOD_MonoBehaviour.intId)
                            field.SetValue(component, Parameters[i].Int);
                        else if (Parameters[i].TypeID == FLOD_MonoBehaviour.floatId)
                            field.SetValue(component, Parameters[i].Float);
                        else if (Parameters[i].TypeID == FLOD_MonoBehaviour.boolId)
                            field.SetValue(component, Parameters[i].Bool);
                        else if (Parameters[i].TypeID == FLOD_MonoBehaviour.colorId)
                            field.SetValue(component, Parameters[i].Color);
                    }
                    else
                    {
                        Debug.LogError("[OPTIMIZERS] Not found field with name " + Parameters[i].ParamName + " in " + component.GetType() + " of " + component + " " + component.name);
                    }
                }
            }

            if (Event != null) Event.Invoke();

            base.ApplySettingsToComponent(component, initialSettingsReference);
        }

        #endregion


        #region Auto Settings


        /// IMPLEMENT AUTO SETTING PARAMETERS FOR DIFFERENT LOD FOR LODS COUNT (IF YOU DONT WANT YOU DONT NEED TO IMPLEMENT THIS)
        public override void SetAutoSettingsAsForLODLevel(int lodIndex, int lodCount, Component source)
        {
            MonoBehaviour comp = source as MonoBehaviour;
            if (comp == null) Debug.LogError("[OPTIMIZERS] Given component for reference values is null or is not MonoBehaviour Component!");

            SetSameValuesAsComponent(source);

            name = "LOD" + (lodIndex + 2); // + 2 to view it in more responsive way for user inside inspector window
        }


        public override void SetSettingsAsForCulled(Component component)
        {
            base.SetSettingsAsForCulled(component);
            SetSameValuesAsComponent(component);
        }

        public override void SetSettingsAsForHidden(Component component)
        {
            base.SetSettingsAsForHidden(component);
            Disable = true;
        }

        public override void SetSettingsAsForNearest(Component component)
        {
            base.SetSettingsAsForNearest(component);
            SetSameValuesAsComponent(component);
            if (Parameters != null) for (int i = 0; i < Parameters.Count; i++) Parameters[i].Change = true;
        }


        #endregion


        public override void EditorWindow()
        {
            //if (Simple)
            //{
            //    base.EditorWindow();
            //    return;
            //}

            // Back compability
            if (Parameters == null) Parameters = new List<ParameterHelper>();
            if (Parameters.Count != 0) Version = 0;

#if UNITY_EDITOR

            if (NotSupported == null) NotSupported = new List<ParameterHelper>();

            SerializedObject s = new UnityEditor.SerializedObject(this);
            Undo.RecordObject(s.targetObject, "Changing custom component parameters");

            bool preEnabled = GUI.enabled;

            for (int i = 0; i < Parameters.Count; i++)
            {
                preEnabled = GUI.enabled;
                if (!Parameters[i].Change) GUI.enabled = false;
                bool viewX = true;

                EditorGUILayout.BeginHorizontal();

                if (Parameters[i].TypeID == intId)
                {
                    Parameters[i].Int = EditorGUILayout.IntField(Parameters[i].ParamName, Parameters[i].Int);
                }
                else if (Parameters[i].TypeID == floatId)
                {
                    Parameters[i].Float = EditorGUILayout.FloatField(Parameters[i].ParamName, Parameters[i].Float);
                }
                else if (Parameters[i].TypeID == boolId)
                {
                    Parameters[i].Bool = EditorGUILayout.Toggle(Parameters[i].ParamName, Parameters[i].Bool);
                }
                else if (Parameters[i].TypeID == colorId)
                {
                    Parameters[i].Color = EditorGUILayout.ColorField(Parameters[i].ParamName, Parameters[i].Color);
                }
                else
                {
                    viewX = false;
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.LabelField(new GUIContent("Not Supported Type (" + Parameters[i].ParamType + ")", "You can create custom implementation to support all your component variables, check documentation for more (" + Parameters[i].ParamName + ")"));
                    GUI.enabled = preEnabled;
                }

                if (viewX)
                {
                    GUILayout.FlexibleSpace();
                    GUI.enabled = true;
                    Parameters[i].Change = EditorGUILayout.Toggle("", Parameters[i].Change, new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(14) });
                    GUI.enabled = preEnabled;
                    EditorGUILayout.EndHorizontal();
                }

                GUI.enabled = preEnabled;
            }

            preEnabled = GUI.enabled;
            GUI.enabled = false;

            if (NotSupported.Count > 0)
            {
                EditorGUI.indentLevel++;
                DrawNotSupported = EditorGUILayout.Foldout(DrawNotSupported, "Not Supported Variables", true);
                if (DrawNotSupported)
                    for (int i = 0; i < NotSupported.Count; i++)
                    {
                        EditorGUILayout.LabelField(new GUIContent("Not Supported Type (" + NotSupported[i].ParamType + ")", "You can create custom implementation to support all your component variables, check documentation for more (" + NotSupported[i].ParamName + ")"));
                    }

                EditorGUI.indentLevel--;
            }

            GUI.enabled = preEnabled;

            EditorGUI.indentLevel++;
            drawEvent = EditorGUILayout.Foldout(drawEvent, "Draw Custom Event", true);
            EditorGUI.indentLevel--;

            if (drawEvent)
            {
                SerializedProperty eventProp = s.FindProperty("Event");
                if (eventProp != null) EditorGUILayout.PropertyField(eventProp, true);
            }

            s.ApplyModifiedProperties();
#endif

        }


        public override FComponentLODsController GenerateLODController(Component target, FOptimizer_Base optimizer)
        {
            MonoBehaviour m = target as MonoBehaviour;
            if (!m) m = target.GetComponentInChildren<MonoBehaviour>();
            if (m) if (!optimizer.ContainsComponent(m))
                {
                    return new FComponentLODsController(optimizer, m, "Custom Component", this);
                }

            return null;
        }


        /// <summary>
        /// Since I can't use dictionaries because they aren't serialable
        /// </summary>
        [System.Serializable]
        public class ParameterHelper
        {
            public bool Change = false;
            public int ParamID;
            public int TypeID;
            public string ParamName;
            public string ParamType;
            public bool Supported = true;

            // Can't use 'object' cause it is not serializable
            public int Int;
            public float Float;
            public Vector2 Vec2;
            public Vector3 Vec3;
            public Color Color;
            public bool Bool;

            public ParameterHelper(string name, string type)
            {
                ParamID = name.GetHashCode();
                ParamName = name;

                TypeID = type.GetHashCode();
                ParamType = type;
                Supported = true;
            }

            public void SetValue(int valueId, object value)
            {
                if (valueId == intId)
                    Int = (int)value;
                else if (valueId == floatId)
                    Float = (float)value;
                else if (valueId == boolId)
                    Bool = (bool)value;
                else if (valueId == colorId)
                    Color = (Color)value;
            }

            public object GetValue(int valueId)
            {
                if (valueId == intId) return Int;
                else if (valueId == floatId) return Float;
                else if (valueId == boolId) return Bool;
                else if (valueId == colorId) return Color;
                return null;
            }
        }
    }
}
