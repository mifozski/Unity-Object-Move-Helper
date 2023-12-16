using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MysticEggs
{
	class SimpleTreeViewWindow : EditorWindow
	{
		[SerializeField] TreeViewState m_TreeViewState;

		FolderSearcherView m_FolderSearchView;

		const string SEARCH_FIELD_ID = "SearchField";

		public string searchString = "";

		void OnEnable()
		{
			if (m_TreeViewState == null)
			{
				m_TreeViewState = new TreeViewState();
			}

			m_FolderSearchView = new FolderSearcherView(m_TreeViewState);

			m_FolderSearchView.OnSelectedFolder += HandleSelectedFolder;
		}

		private void HandleSelectedFolder(string folderPath)
		{
			var selectedAssets = Selection.assetGUIDs;

			Debug.Log($"Moving {selectedAssets.Length} object(s) to " + folderPath);

			foreach (var asset in selectedAssets)
			{
				var path = AssetDatabase.GUIDToAssetPath(asset);
				var assetName = Path.GetFileName(path);
				var error = AssetDatabase.MoveAsset(path, $"Assets/{folderPath}/{assetName}");
				if (string.IsNullOrEmpty(error))
				{
					Debug.LogError("Error while moving asset " + assetName + ": " + error);
				}
			}
			AssetDatabase.SaveAssets();
		}

		void OnGUI()
		{
			GUI.SetNextControlName(SEARCH_FIELD_ID);
			var searchInputRect = new Rect(10, 10, 200, 20);
			searchString = GUI.TextField(new Rect(10, 10, 200, 20), searchString, 25);

			// Draw Plcaeholder
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

			if (_inputNeedsFocus)
			{
				_inputNeedsFocus = false;
				GUI.FocusControl(SEARCH_FIELD_ID);
			}

			var update = GUI.Button(new Rect(250, 10, 100, 20), "Move");
			if (update)
			{
				m_FolderSearchView.Reload();
			}

			m_FolderSearchView.searchString = searchString;

			m_FolderSearchView.OnGUI(new Rect(10, 20 + 20, position.width, position.height));
		}

		[MenuItem("Assets/Move to... %m", false)]
		static void ShowMoveToWindow()
		{
			var window = GetWindow<SimpleTreeViewWindow>();
			window.titleContent = new GUIContent("Move to folder");
			window.Show();
		}

		bool _inputNeedsFocus = true;
	}

	public class FolderSearcherView : TreeView
	{
		public Action<string> OnSelectedFolder;
		public FolderSearcherView(TreeViewState state) : base(state)
		{
			Reload();

			rowHeight = 20;
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

			var allItems = new List<TreeViewItem>();

			_dirPathsPerId.Clear();
			AddSubdirectioriesToTree(Application.dataPath, allItems, "", 0);

			SetupParentsAndChildrenFromDepths(root, allItems);

			return root;
		}

		void AddSubdirectioriesToTree(string parentDir, List<TreeViewItem> allItems, string path, int depth)
		{
			string[] dirs = Directory.GetDirectories(parentDir);

			foreach (var dirPath in dirs)
			{
				string fullPath = Path.GetFullPath(dirPath).TrimEnd(Path.DirectorySeparatorChar);
				string dir = Path.GetFileName(fullPath);

				var id = dirPath.GetHashCode();
				var newPath = path + (string.IsNullOrEmpty(path) ? "" : "/") + dir;

				_dirPathsPerId.Add(id, newPath);


				allItems.Add(new DirItem(
					id,
					depth,
					dir,
					newPath
				));

				AddSubdirectioriesToTree(dirPath, allItems, newPath, depth + 1);
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			foreach (var id in selectedIds)
			{
				Debug.Log(_dirPathsPerId[id]);
			}
			base.SelectionChanged(selectedIds);
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			var item = _dirPathsPerId[args.item.id];
			if (item == null)
			{
				return;
			}

			var iconRect = args.rowRect;
			iconRect.width = 16f;

			var labelRect = args.rowRect;
			var indent = iconRect.x + iconRect.width;
			labelRect.x += indent;
			labelRect.width -= indent;

			var labelStyle = GUI.skin.label;
			labelStyle.fontSize = 15;
			EditorGUI.LabelField(labelRect, item, labelStyle);
		}

		protected override void DoubleClickedItem(int id)
		{
			OnSelectedFolder(_dirPathsPerId[id]);
		}

		static Dictionary<int, string> _dirPathsPerId = new Dictionary<int, string>();
	}

	internal class DirItem : TreeViewItem
	{
		public string FullPath { get; private set; }

		public DirItem(int id, int depth, string displayName, string fullPath) : base(id, depth, displayName)
		{
			FullPath = fullPath;
		}
	}
}