using GraphProcessor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/builder.html")]
    /// <summary>
    /// Builds impostor assets by executing an ImpostorGraph with the specified parameters.
    /// </summary>
    public class ImpostorBuilder : ScriptableObject
    {
        static class Profile
        {
            public static ProfilerMarker Build = new("Build");
            public static ProfilerMarker Setup = new("Setup");
            public static ProfilerMarker PassParameters = new("PassParameters");
            public static ProfilerMarker Process = new("Process");
            public static ProfilerMarker Dispose = new("Dispose");
        }

        [SerializeField]
        ImpostorGraph m_Graph;

        [SerializeReference]
        Impostor m_Impostor;

        [SerializeField]
        SerializableDictionary<string, SerializedObject> m_Objects = new();

        [SerializeField]
        SerializableDictionary<string, SerializedInteger> m_Ints = new();

        [SerializeField]
        SerializableDictionary<string, SerializedFloat> m_Floats = new();

        public ImpostorGraph Graph
        {
            get => m_Graph;
            set => m_Graph = value;
        }

        public Impostor Impostor
        {
            get => m_Impostor;
            set => m_Impostor = value;
        }

        /// <summary>
        /// Sets the value of an integer parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to set.</param>
        /// <param name="value">The new value of the parameter.</param>
        public void SetInteger(string name, int value)
        {
            if (m_Ints.ContainsKey(name))
                m_Ints[name].Value = value;
            else
                m_Ints.Add(name, new SerializedInteger(value));
        }

        /// <summary>
        /// Retrieves the value of the integer parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to retrieve.</param>
        /// <returns>The value of the parameter.</returns>
        public int GetInteger(string name)
        {
            return m_Ints[name].Value;
        }

        /// <summary>
        /// Sets the value of an integer parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to set.</param>
        /// <param name="value">The new value of the parameter.</param>
        public void SetFloat(string name, float value)
        {
            if (m_Floats.ContainsKey(name))
                m_Floats[name].Value = value;
            else
                m_Floats.Add(name, new SerializedFloat(value));
        }

        /// <summary>
        /// Retrieves the value of the integer parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to retrieve.</param>
        /// <returns>The value of the parameter.</returns>
        public float GetFloat(string name)
        {
            return m_Floats[name].Value;
        }

        /// <summary>
        /// Sets the value of a game object parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to set.</param>
        /// <param name="value">The new value of the parameter.</param>
        public void SetGameObject(string name, GameObject value)
        {
            if (m_Objects.ContainsKey(name))
                m_Objects[name].Value = value;
            else
                m_Objects.Add(name, new SerializedObject(value));
        }

        /// <summary>
        /// Retrieves the value of the game object parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter to retrieve.</param>
        /// <returns>The value of the parameter.</returns>
        public GameObject GetGameObject(string name)
        {
            return (m_Objects[name].Value as GameObject);
        }

        public bool Contains(string name)
        {
            return m_Objects.ContainsKey(name) || m_Ints.ContainsKey(name) || m_Floats.ContainsKey(name);
        }

        /// <summary>
        /// Build the impostor.
        /// </summary>
        public Impostor Build()
        {
            // Return impostor
            var output = m_Graph.nodes.Find((x) => x.GetType() == typeof(OutputNode)) as OutputNode;
            if (output == null)
                throw new System.InvalidOperationException($"{m_Graph.name} does not contain OutputNode!");

            using (Profile.Build.Auto())
            {
                var nodes = new List<BaseNode>();
                var disposables = new List<System.IDisposable>();

                try
                {
                    using (Profile.Setup.Auto())
                    {
                        GetNodes(nodes, output);
                    }

                    using (Profile.PassParameters.Auto())
                    {
                        foreach (var parameter in m_Objects)
                        {
                            if (m_Objects.TryGetValue(parameter.Key, out var value))
                                m_Graph.SetParameterValue(parameter.Key, value.Value);
                        }
                        foreach (var parameter in m_Ints)
                        {
                            if (m_Ints.TryGetValue(parameter.Key, out var value))
                                m_Graph.SetParameterValue(parameter.Key, value.Value);
                        }
                        foreach (var parameter in m_Floats)
                        {
                            if (m_Floats.TryGetValue(parameter.Key, out var value))
                                m_Graph.SetParameterValue(parameter.Key, value.Value);
                        }
                    }

                    using (Profile.Process.Auto())
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            var node = nodes[i];

                            CheckNodeInputs(node);

#if UNITY_EDITOR
                            UnityEditor.EditorUtility.DisplayProgressBar("Building impostor", $"Processing {node.GetType().Name}", (float)i / (nodes.Count - 1));
#endif
                            UnityEngine.Profiling.Profiler.BeginSample(node.name);
                            if (node is System.IDisposable disposable)
                                disposables.Add(disposable);
                            node.OnProcess();
                            UnityEngine.Profiling.Profiler.EndSample();
                        }
                    }
                }
                finally
                {
                    using (Profile.Dispose.Auto())
                    {
                        foreach (var disposable in disposables)
                            disposable.Dispose();
                    }

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.ClearProgressBar();
#endif
                }
            }

            return output.Impostor;
        }

        [ContextMenu("UNITY_EDITOR")]
        /// <summary>
        /// Checks if all input fields are connected.
        /// </summary>
        void CheckNodeInputs(BaseNode node)
        {
            var type = node.GetType();
            // skip parameter node
            if (type == typeof(ParameterNode))
                return;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                // check if field has input attribute
                if (field.GetCustomAttributes(typeof(InputAttribute), true).Length > 0)
                {
                    var value = field.GetValue(node);
                    // check if field is connected
                    if (field.GetValue(node) == null)
                    {
                        throw new System.ArgumentException($"{node.name} field {field.Name} is null!");
                    }
                }
            }
        }

        void GetNodes(List<BaseNode> nodes, BaseNode output)
        {
            var set = new HashSet<BaseNode>();

            // Get nodes that directly or indirectly connected with output
            GetNodesRecursive(set, nodes, output);

            // Sort nodes based on execution order
            nodes.Sort((a, b) => a.computeOrder.CompareTo(b.computeOrder));
        }

        void GetNodesRecursive(HashSet<BaseNode> set, List<BaseNode> nodes, BaseNode node)
        {
            if (set.Contains(node))
                return;

            nodes.Add(node);
            set.Add(node);

            foreach (var port in node.inputPorts)
            {
                foreach (var edge in port.GetEdges())
                {
                    GetNodesRecursive(set, nodes, edge.outputNode);
                }
            }
        }

        [System.Serializable]
        class SerializedObject
        {
            public Object Value;

            public SerializedObject(Object value)
            {
                Value = value;
            }
        }


        [System.Serializable]
        class SerializedInteger
        {
            public int Value;

            public SerializedInteger(int value)
            {
                Value = value;
            }
        }

        [System.Serializable]
        class SerializedFloat
        {
            public float Value;

            public SerializedFloat(float value)
            {
                Value = value;
            }
        }
    }
}