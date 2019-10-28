using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Tools.CollectionIcon
{
	public static class CollectionIconUtils
	{
		private static GUIContent _helpIconContent;
		private static GUIStyle _iconButtonStyle;

		public static void ExportLoca(string collectionName, string path, List<Material> iconMaterials)
		{
			var jsonString = new StringBuilder();
			jsonString.AppendLine("{");

			for (var i = 0; i < iconMaterials.Count; i++)
			{
				string key = $"{collectionName}{i}";
				string value = iconMaterials[i].name;
				string comma = i < iconMaterials.Count - 1 ? "," : "";
				jsonString.AppendLine($"  \"{key}\": \"{value}\"{comma}");
			}
			jsonString.AppendLine("}");

			File.WriteAllText(path, jsonString.ToString());

			Debug.Log($"Exported loca to {path}");
		}

		public static void ExportMaterials(string collectionName, string assetRootPath, List<Material> iconMaterials)
		{
			for (var i = 0; i < iconMaterials.Count; i++)
			{
				var material = new Material(iconMaterials[i]);
				material.name = $"{collectionName}{i}";
				material.DisableKeyword("EDITOR_CLIP");

				string assetPath = $"{assetRootPath}/{material.name}.mat";

				UpdateAsset<Material>(assetPath, material);
			}

			DeleteUnusedMaterialAssets(collectionName, assetRootPath, iconMaterials.Count);

			Debug.Log($"Exported materials to {assetRootPath}");
		}

		private static void UpdateAsset<T>(string assetPath, Object asset) where T:Object
		{
			var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (existingAsset == null)
			{
				AssetDatabase.CreateAsset(asset, assetPath);
			}
			else
			{
				EditorUtility.CopySerialized(asset, existingAsset);
			}
		}

		private static void DeleteUnusedMaterialAssets(string collectionName, string assetRootPath, int materialCount)
		{
			var regex = new Regex(@"[a-zA-Z]+(\d+).mat");
			string[] guids = AssetDatabase.FindAssets(collectionName, new[]{assetRootPath});
			for (var i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				Match match = regex.Match(assetPath);
				Group indexGroup = match.Groups[1];
				int index = Int32.Parse(indexGroup.Value);
				if (!match.Success || index >= materialCount)
				{
					Debug.Log($"Deleting unused material asset: {assetPath}");
					AssetDatabase.DeleteAsset(assetPath);
				}
			}
		}

		public static string Capitalize(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return s;
			}

			var builder = new StringBuilder(s);
			var upper = true;
			for (var i = 0; i < s.Length; i++)
			{
				builder[i] = upper ? char.ToUpper(s[i]) : char.ToLower(s[i]);
				upper = char.IsWhiteSpace(s[i]);
			}

			return builder.ToString();
		}

		public static Color ShiftHue(Color color, float shift)
		{
			Color.RGBToHSV(color, out float h, out float s, out float v);
			h = (h + shift + 1f) % 1f;
			var newColor = Color.HSVToRGB(h, s, v);
			newColor.a = color.a;
			return newColor;
		}

		public static void DrawHelpIcon(Object obj)
		{
			if (_helpIconContent == null)
			{
				_helpIconContent = new GUIContent(EditorGUIUtility.IconContent("_Help"));
			}

			if (_iconButtonStyle == null)
			{
				_iconButtonStyle = new GUIStyle("IconButton");
			}

			if (GUILayout.Button(_helpIconContent, _iconButtonStyle, GUILayout.MaxWidth(20)))
			{
				Help.ShowHelpForObject(obj);
			}
		}

		public static void DrawHorizontalLine()
		{
			Rect rect = GUILayoutUtility.GetLastRect();
			rect.yMin = rect.yMax;
			rect.yMax = rect.yMin + 1;
			rect.xMin = 0;
			rect.xMax = EditorGUIUtility.currentViewWidth;
			EditorGUI.DrawRect(rect, Color.black);
		}

		public static string GetSystemPath(DefaultAsset pathAsset)
		{
			string dataPath = Application.dataPath.Replace("/Assets", "/");
			return dataPath + AssetDatabase.GetAssetPath(pathAsset);
		}
	}
}
