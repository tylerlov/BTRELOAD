using GraphProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectDawn.Impostor.Editor
{
    public class ImpostorGraphView : BaseGraphView
    {
        public ImpostorGraphView(EditorWindow window) : base(window)
        {
        }

        protected override bool canCopySelection => base.canCopySelection && selection.All(e =>
        {
            // Prevent copy output
            if (e is BaseNodeView view && view.nodeTarget.GetType() == typeof(OutputNode))
            {
                return false;
            }
            return true;
        });

        protected override bool canCutSelection => base.canCopySelection && selection.All(e =>
        {
            // Prevent cut output
            if (e is BaseNodeView view && view.nodeTarget.GetType() == typeof(OutputNode))
            {
                return false;
            }
            return true;
        });

        public override IEnumerable<(string path, Type type)> FilterCreateNodeMenuEntries()
        {
            // Prevent create output
            foreach (var nodeMenuItem in base.FilterCreateNodeMenuEntries())
            {
                if (!nodeMenuItem.type.IsSubclassOf(typeof(ImpostorNode)))
                    continue;
                yield return nodeMenuItem;
            }
        }
    }
}
