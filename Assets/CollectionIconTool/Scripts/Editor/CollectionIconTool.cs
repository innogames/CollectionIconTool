using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tools.CollectionIcon
{
	/// <summary>
	/// Editor tool to create collection icon materials and names
	/// </summary>

	[HelpURL("https://tech.innogames.com")]
	public class CollectionIconTool : EditorWindow
	{
		public static CollectionIconTool CurrentTool;

		private const string _shaderName = "CollectionIcon";
		private const int _layerCount = 3;
		private List<Material> _iconMaterials;
		private int _previewSize = 100;
		private Vector2 _scrollPosition;
		private int _selectedMaterial;
		private GUIStyle _gridListTextStyle;
		private CollectionIconPartSet _partSet;
		private bool _showAll = true;

		private static readonly int _outlineColorId = Shader.PropertyToID("_OutlineColor");
		private static readonly int _texture0Id = Shader.PropertyToID("_Texture0");

		private bool CanExportIcons => _partSet != null && _partSet.MaterialExportPath != null;
		private int IconCount => _iconMaterials.Count;
		private int FilteredIconCount
		{
			get
			{
				int count = 0;
				for (var i = 0; i < _iconMaterials.Count; i++)
				{
					if (_partSet.IgnoredVariations[i])
					{
						continue;
					}

					count++;
				}

				return count;
			}
		}

		[MenuItem("Window/Collection Icon Tool", false, 0)]
		private static void Init()
		{
			GetWindow<CollectionIconTool>("Collection Icon Tool", true);
		}

		private void OnEnable()
		{
			CurrentTool = this;
		}

		private void OnDestroy()
		{
			CurrentTool = null;
		}

		private void OnGUI()
		{
			GUILayout.Space(4);

			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			_partSet =
				EditorGUILayout.ObjectField("Icon Part Set", _partSet, typeof(CollectionIconPartSet)) as
					CollectionIconPartSet;
			if (EditorGUI.EndChangeCheck())
			{
				CreateMaterials();
			}

			GUI.enabled = CanExportIcons;
			if (GUILayout.Button("Export", EditorStyles.miniButton, GUILayout.MaxWidth(70)))
			{
				ExportIcons();
			}

			if (GUILayout.Button("Delete Assets", EditorStyles.miniButton, GUILayout.MaxWidth(70)))
			{
				DeleteAssets();
			}

			GUI.enabled = true;

			CollectionIconUtils.DrawHelpIcon(this);

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(4);

			if (_partSet == null)
			{
				_iconMaterials = null;
			}

			CollectionIconUtils.DrawHorizontalLine();

			if (_iconMaterials == null)
			{
				EditorGUILayout.HelpBox("Please specify a part set.", MessageType.Error);
				return;
			}

			ValidateIgnoredVariations();

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			if (_gridListTextStyle == null)
			{
				_gridListTextStyle = new GUIStyle(EditorStyles.label);
				_gridListTextStyle.alignment = TextAnchor.MiddleCenter;
			}

			int xCount = 0;
			int yPos = 0;
			int iconsPerLine = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / _previewSize);
			iconsPerLine = Mathf.NextPowerOfTwo(iconsPerLine / 2 + 1);
			int labelWidth = _previewSize - ( EditorStyles.label.margin.left + EditorStyles.label.margin.right );

			EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(_previewSize * iconsPerLine));

			for (var i = 0; i < _iconMaterials.Count; i++)
			{
				if (!_showAll && _partSet.IgnoredVariations[i])
				{
					continue;
				}

				EditorGUILayout.BeginVertical();

				var rect = GUILayoutUtility.GetRect(_previewSize, _previewSize, GUILayout.Width(_previewSize));

				var selectionRect = rect;
				selectionRect.yMax += 20;

				if (Event.current.type == EventType.MouseDown)
				{
					if (selectionRect.Contains(Event.current.mousePosition))
					{
						_partSet.IgnoredVariations[i] = !_partSet.IgnoredVariations[i];
						Event.current.Use();
						EditorUtility.SetDirty(_partSet);
					}
				}


				var texture = _iconMaterials[i].GetTexture(_texture0Id) ?? Texture2D.blackTexture;
				GUI.color = _partSet.IgnoredVariations[i] ? Color.gray : Color.white;
				EditorGUI.DrawPreviewTexture(rect, texture, _iconMaterials[i]);
				EditorGUILayout.LabelField(_iconMaterials[i].name, _gridListTextStyle, GUILayout.Width(labelWidth));

				EditorGUILayout.EndVertical();

				xCount++;
				if (xCount >= iconsPerLine)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Mathf.Min(_iconMaterials.Count - i,
						_previewSize * iconsPerLine)));
					xCount = 0;
				}
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();
			GUI.color = Color.white;

			CollectionIconUtils.DrawHorizontalLine();

			EditorGUILayout.BeginHorizontal();
			int filteredIconCount = FilteredIconCount;
			EditorGUILayout.LabelField(
				$"{filteredIconCount} Icons ({IconCount - filteredIconCount} ignored) from {GetUsedTextureCount()} Textures.");

			_showAll = EditorGUILayout.ToggleLeft("Show All", _showAll, GUILayout.MaxWidth(100));

			_previewSize = (int)GUILayout.HorizontalSlider(_previewSize, 32f, 256f, GUILayout.MaxWidth(64));

			EditorGUILayout.EndHorizontal();
		}

		private int GetUsedTextureCount()
		{
			var usedTextures = new HashSet<Texture>();

			for (var i = 0; i < _iconMaterials.Count; i++)
			{
				if (_partSet.IgnoredVariations[i])
				{
					continue;
				}

				var material = _iconMaterials[i];

				for (int j = 0; j < _layerCount; j++)
				{
					var texture = material.GetTexture($"_Texture{j}");
					if (texture == null)
					{
						continue;
					}

					usedTextures.Add(texture);
				}
			}

			return usedTextures.Count;
		}

		private void ExportIcons()
		{
			var filteredIconMaterials = new List<Material>();
			for (var i = 0; i < _iconMaterials.Count; i++)
			{
				if (_partSet.IgnoredVariations[i])
				{
					continue;
				}

				filteredIconMaterials.Add(_iconMaterials[i]);
			}

			string materialsRootAssetPath = AssetDatabase.GetAssetPath(_partSet.MaterialExportPath);
			CollectionIconUtils.ExportMaterials(_partSet.name, materialsRootAssetPath, filteredIconMaterials);
			if (_partSet.LocaExportPath != null)
			{
				CollectionIconUtils.ExportLoca(_partSet.name, GetLocaFilePath(), filteredIconMaterials);
			}
		}

		private string GetLocaFilePath()
		{
			string locaFileName = _partSet.name + ".jsont";
			string locaFilePath = CollectionIconUtils.GetSystemPath(_partSet.LocaExportPath) + "/" + locaFileName;
			return locaFilePath;
		}

		private void DeleteAssets()
		{
			if (!EditorUtility.DisplayDialog("Delete Assets",
				"This will delete all materials and loca entries created from this part set. Continue?", "Ok",
				"Cancel"))
			{
				return;
			}

			File.Delete(GetLocaFilePath());
			string materialsRootAssetPath = AssetDatabase.GetAssetPath(_partSet.MaterialExportPath);
			string[] guids = AssetDatabase.FindAssets(_partSet.name, new[] {materialsRootAssetPath});
			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				AssetDatabase.DeleteAsset(assetPath);
			}
		}

		private void CreateMaterials()
		{
			_iconMaterials = new List<Material>();
			if (_partSet == null)
			{
				return;
			}

			var shader = Shader.Find(_shaderName);
			if (shader == null)
			{
				Debug.LogError($"Cannot find shader {_shaderName}");
				return;
			}

			if (_partSet.Parts.Count < 1 || _partSet.Parts[0].Variations.Count == 0)
			{
				return;
			}

			for (int c0 = 0; c0 < _partSet.Parts[0].Variations.Count; c0++)
			{
				for (int c1 = 0; c1 < _partSet.Parts[1].Variations.Count; c1++)
				{
					for (int c2 = 0; c2 < _partSet.Parts[2].Variations.Count; c2++)
					{
						var material = new Material(shader);
						material.EnableKeyword("EDITOR_CLIP");

						string name = _partSet.NamePattern;
						name = name.Replace("{" + _partSet.Parts[0].PartName + "}",
							_partSet.Parts[0].Variations[c0].Name);
						name = name.Replace("{" + _partSet.Parts[1].PartName + "}",
							_partSet.Parts[1].Variations[c1].Name);
						name = name.Replace("{" + _partSet.Parts[2].PartName + "}",
							_partSet.Parts[2].Variations[c2].Name);
						material.name = CollectionIconUtils.Capitalize(name);

						SetMaterialTextureVariation(material, 0, c0);
						SetMaterialTextureVariation(material, 1, c1);
						SetMaterialTextureVariation(material, 2, c2);

						SetMaterialColorVariation(material, 0, c0);
						SetMaterialColorVariation(material, 1, c1);
						SetMaterialColorVariation(material, 2, c2);

						material.SetColor(_outlineColorId, _partSet.OutlineColor);

						Random.InitState(_iconMaterials.Count);
						float scale = Mathf.Lerp(1f, Random.Range(1f, 2f), _partSet.ScaleVariation);
						float offset = -( scale - 1f ) * 0.5f;

						Vector2 scaleVector = Vector2.one * scale;
						Vector2 offsetVector = Vector2.one * offset;

						if (Random.value < _partSet.FlipVariation)
						{
							scaleVector.x = -scaleVector.x;
							offsetVector.x = 1f - offsetVector.x;
						}

						material.SetTextureScale(_texture0Id, scaleVector);
						material.SetTextureOffset(_texture0Id, offsetVector);

						_iconMaterials.Add(material);
					}
				}
			}

			ValidateIgnoredVariations();
		}

		private void ValidateIgnoredVariations()
		{
			if (_partSet.IgnoredVariations == null)
			{
				_partSet.IgnoredVariations = new List<bool>();
			}

			if (_partSet.IgnoredVariations.Count >= _iconMaterials.Count)
			{
				return;
			}

			for (int i = _partSet.IgnoredVariations.Count; i < _iconMaterials.Count; i++)
			{
				_partSet.IgnoredVariations.Add(false);
			}
		}

		private void SetMaterialTextureVariation(Material material, int layerIndex, int variationIndex)
		{
			var texture = _partSet.Parts[layerIndex].Variations[variationIndex].Texture;
			material.SetTexture($"_Texture{layerIndex}", texture);
		}

		private void SetMaterialColorVariation(Material material, int layerIndex, int variationIndex)
		{
			var variation = _partSet.Parts[layerIndex].Variations[variationIndex];
			float hueShift = Random.Range(-0.5f * variation.hueVariation, 0.5f * variation.hueVariation);

			material.SetColor($"_Color{layerIndex}", CollectionIconUtils.ShiftHue(variation.BaseTintColor, hueShift));
			material.SetColor($"_Color{layerIndex}Pattern0",
				CollectionIconUtils.ShiftHue(variation.PatternTintColor0, hueShift));
			material.SetColor($"_Color{layerIndex}Pattern1",
				CollectionIconUtils.ShiftHue(variation.PatternTintColor1, hueShift));
		}

		public static void UpdateIcons()
		{
			if (CurrentTool == null)
			{
				return;
			}

			CurrentTool.CreateMaterials();
			CurrentTool.Repaint();
		}
	}
}