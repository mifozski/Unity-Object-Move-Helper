using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MysticEggs
{
	class SimpleTreeViewWindow : EditorWindow
	{
		[SerializeField] TreeViewState m_TreeViewState;

		FolderSearcherView _folderSearchView;

		const string SEARCH_FIELD_ID = "SearchField";

		public string searchString = "";

		void OnEnable()
		{
			if (m_TreeViewState == null)
			{
				m_TreeViewState = new TreeViewState();
			}

			_folderSearchView = new FolderSearcherView(m_TreeViewState);

			_folderSearchView.OnSelectedFolder += HandleSelectedFolder;
		}

		private void HandleSelectedFolder(string folderPath)
		{
			Close();
			
			var selectedAssets = Selection.assetGUIDs;

			Debug.Log($"Moving {selectedAssets.Length} asset(s) to " + folderPath);

			foreach (var asset in selectedAssets)
			{
				var path = AssetDatabase.GUIDToAssetPath(asset);
				var assetName = Path.GetFileName(path);
				var error = AssetDatabase.MoveAsset(path, $"Assets/{folderPath}/{assetName}");
				if (!string.IsNullOrEmpty(error))
				{
					Debug.LogError("Error while moving asset " + assetName + ": " + error);
				}
			}
			AssetDatabase.SaveAssets();
		}

		void OnGUI()
		{
			searchString = Utils.DrawSearchInput(searchString, SEARCH_FIELD_ID);

			if (_inputNeedsFocus || (Event.current.keyCode != KeyCode.None && Event.current.keyCode != KeyCode.Return && Event.current.keyCode != KeyCode.KeypadEnter && Event.current.keyCode != KeyCode.UpArrow && Event.current.keyCode != KeyCode.DownArrow))
			{
				_inputNeedsFocus = false;
				GUI.FocusControl(SEARCH_FIELD_ID);
			}

			if (Event.current.keyCode == KeyCode.DownArrow)
			{
				if (GUI.GetNameOfFocusedControl() == SEARCH_FIELD_ID)
				{
					_folderSearchView.SetFocus();
				}
			}

			_folderSearchView.searchString = searchString;

			_folderSearchView.OnGUI(new Rect(10, 20 + 20, position.width, position.height));

			var move = GUI.Button(new Rect(250, 10, 100, 20), "Move");

			if (move || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
			{
				var selectedItems = _folderSearchView.GetSelection();
				if (selectedItems.Count > 0)
				{
					var selectedFolder = _folderSearchView.GetSelectedFolder();
					if (!string.IsNullOrEmpty(selectedFolder))
					{
						HandleSelectedFolder(selectedFolder);
					}
				}
			}
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
					newPath,
					newPath
				));

				AddSubdirectioriesToTree(dirPath, allItems, newPath, depth + 1);
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			base.SelectionChanged(selectedIds);
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			Utils.DrawTreeItem(args.rowRect, args.item.displayName);
		}

		protected override void DoubleClickedItem(int id)
		{
			OnSelectedFolder(_dirPathsPerId[id]);
		}

		public string GetSelectedFolder()
		{
			var selectedItems = GetSelection();
			if (selectedItems.Count > 0)
			{
				return _dirPathsPerId[selectedItems.First()];
			}
			return null;
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