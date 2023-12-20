using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace MysticEggs
{
	static public class Utils {
		static public string DrawSearchInput(string searchString, string inputId)
		{
			GUI.SetNextControlName(inputId);
			var searchInputRect = new Rect(10, 10, 200, 20);
			var nextSearchString = GUI.TextField(searchInputRect, searchString, 25);
			
			if (string.IsNullOrEmpty(searchString)) {
				GUIStyle style = new GUIStyle
				{
					alignment = TextAnchor.UpperLeft,
					padding = new RectOffset(3, 0, 2, 0),
					fontStyle = FontStyle.Italic,
					normal =
					{
						textColor = Color.grey
					}
				};
				EditorGUI.LabelField(searchInputRect, "Search Folders", style);
			}

			return nextSearchString;
		}

		static public void DrawTreeItem(Rect itemRect, string text)
		{
			var iconRect = itemRect;
			iconRect.width = 16f;

			var labelRect = itemRect;
			var indent = iconRect.x + iconRect.width;
			labelRect.x += indent;
			labelRect.width -= indent;

			var labelStyle = GUI.skin.label;
			labelStyle.fontSize = 15;
			EditorGUI.LabelField(labelRect, text, labelStyle);
		}
	}
}