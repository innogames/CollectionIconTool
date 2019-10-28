using Game.Editor.Tools;
using Game.Editor.Tools.CollectionIcon;
using UnityEditor;

namespace Game.Editor.CustomEditors
{
	[CustomEditor(typeof(CollectionIconPartSet))]
	public class CollectionIconPartSetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck())
			{
				UpdateCollectionIconTool();
			}
		}

		private void UpdateCollectionIconTool()
		{
			CollectionIconTool.UpdateIcons();
		}
	}
}
