using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.Tools.CollectionIcon
{
	[Serializable]
	public struct CollectionIconPartVariation
	{
		public string Name;
		public Texture2D Texture;
		public Color BaseTintColor;
		public Color PatternTintColor0;
		public Color PatternTintColor1;
		[Range(0f, 1f)]
		public float hueVariation;
	}

	[Serializable]
	public struct CollectionIconPart
	{
		public string PartName;
		public List<CollectionIconPartVariation> Variations;
	}
}
