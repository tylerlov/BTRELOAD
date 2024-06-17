using GraphProcessor;
using UnityEditor;

namespace ProjectDawn.Impostor.Editor
{
	public class ImpostorToolbarView : ToolbarView
	{
		public ImpostorToolbarView(BaseGraphView graphView) : base(graphView) { }

		protected override void AddButtons()
		{
			//AddButton("Save Asset", () => this.graphView.SaveGraphToDisk());
			AddButton("Show In Project", () => EditorGUIUtility.PingObject(graphView.graph));
		}
	}
}
