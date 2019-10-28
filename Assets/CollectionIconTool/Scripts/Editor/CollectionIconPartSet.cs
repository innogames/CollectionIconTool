using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Editor.Tools.CollectionIcon
{
	[CreateAssetMenu(fileName = "new CollectionIconPartSet", menuName = "Collection Icon Part Set", order = 0)]
	public class CollectionIconPartSet : ScriptableObject
	{
		public DefaultAsset MaterialExportPath;
		public DefaultAsset LocaExportPath;
		public string NamePattern;
		[Range(0f, 1f)]
		public float ScaleVariation;
		[Range(0f, 1f)]
		public float FlipVariation;
		public Color OutlineColor = Color.white;
		[FormerlySerializedAs("Rules")]
		public List<CollectionIconPart> Parts = new List<CollectionIconPart>(4);
		public List<bool> IgnoredVariations;
	}
}
