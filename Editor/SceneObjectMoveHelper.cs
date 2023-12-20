using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.IMGUI.Controls;

namespace MysticEggs
{
	public static class SceneObjectMoveHelper
	{	
		[MenuItem("GameObject/Move Helper/Move Out of Parent", false, 0)]
		static void MoveOutOfParent()
		{
			var gos = Selection.gameObjects;

			bool changed = false;
			foreach (var go in gos)
			{
				if (go.transform.parent)
				{
					changed = true;

					if (go.transform.parent.parent)
					{
						go.transform.parent = go.transform.parent.parent;
					}
					else
					{
						go.transform.parent = null;
					}
				}
			}

			if (changed)
			{
				EditorSceneManager.MarkSceneDirty(gos[0].scene);
			}
		}
	}

	class HierarchySearchWindow : EditorWindow
	{
		[SerializeField] TreeViewState _treeViewState;

		public string searchString = "";

		const string SEARCH_FIELD_ID = "SearchField";

		void OnEnable()
		{
			if (_treeViewState == null)
			{
				_treeViewState = new TreeViewState();
			}

			_objectSearchView = new SceneObjectSearcherView(_treeViewState, selectedObjects);

			_objectSearchView.OnSelectedObject += HandleSelectedNewParent;
		}

		private void HandleSelectedNewParent(Transform targetParent)
		{
			Debug.Log($"Moving {selectedObjects.Length} object(s) to " + targetParent.name);

			foreach (var obj in selectedObjects)
			{
				obj.transform.parent = targetParent;
			}

			Close();
		}

		void OnGUI()
		{
			if (Event.current.keyCode == KeyCode.Escape)
			{
				Close();
			}

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
					_objectSearchView.SetFocus();
				}
			}

			_objectSearchView.searchString = searchString;

			_objectSearchView.OnGUI(new Rect(10, 20 + 20, position.width, position.height));

			var move = GUI.Button(new Rect(250, 10, 100, 20), "Move");

			if (move || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
			{
				var selectedTrasnform = _objectSearchView.GetSelectedTransform();
				if (selectedTrasnform)
				{
					HandleSelectedNewParent(selectedTrasnform);
				}
			}

			var moveToSceneRoot = GUI.Button(new Rect(370, 10, 150, 20), "Move to Scene Root");
			if (moveToSceneRoot)
			{
				foreach (var obj in selectedObjects)
				{
					obj.transform.parent = null;
				}

				Close();
			}
		}

		[MenuItem("GameObject/Move Helper/Move to... %#m", false, 0)]
		static void MoveTo()
		{
			selectedObjects = Selection.gameObjects;

			var window = GetWindow<HierarchySearchWindow>();
			window.titleContent = new GUIContent("Move to object");
			window.Show();
		}

		bool _inputNeedsFocus = true;

		static GameObject[] selectedObjects;

		SceneObjectSearcherView _objectSearchView;
	}

	public class SceneObjectSearcherView : TreeView
	{
		public Action<Transform> OnSelectedObject;

		public SceneObjectSearcherView(TreeViewState state, GameObject[] objectsToSkip) : base(state)
		{
			_objectsToSkip = objectsToSkip.ToDictionary(obj => obj.transform, (obj) => true);

			Reload();

			rowHeight = 20;
		}

		public Transform GetSelectedTransform()
		{
			var selectedItems = GetSelection();
			if (selectedItems.Count > 0)
			{
				return _transformsPerId[selectedItems.First()];
			}
			return null;
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

			var allItems = new List<TreeViewItem>();

			_transformsPerId.Clear();

			var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

			foreach (var rootObj in rootObjects)
			{
				AddChildObjectsToTree(rootObj.transform, allItems, "", 0);
			}

			SetupParentsAndChildrenFromDepths(root, allItems);

			return root;
		}

		void AddChildObjectsToTree(Transform obj, List<TreeViewItem> allItems, string path, int depth)
		{
			if (_objectsToSkip.ContainsKey(obj))
			{
				return;
			}

			var newPath = path + (string.IsNullOrEmpty(path) ? "" : "/") + obj.name;
			var id = obj.GetInstanceID().GetHashCode();
			allItems.Add(new GameObjectItem(
				id,
				depth,
				newPath,
				newPath
			));

			_transformsPerId.Add(id, obj);

			foreach (Transform child in obj)
			{
				AddChildObjectsToTree(child, allItems, newPath, depth + 1);
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

		protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
		{
			return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return false;
		}

		protected override void DoubleClickedItem(int id)
		{
			OnSelectedObject(_transformsPerId[id]);
		}

		static Dictionary<int, Transform> _transformsPerId = new Dictionary<int, Transform>();

		Dictionary<Transform, bool> _objectsToSkip;
	}

	internal class GameObjectItem : TreeViewItem
	{
		public string FullPath { get; private set; }

		public GameObjectItem(int id, int depth, string displayName, string fullPath) : base(id, depth, displayName)
		{
			FullPath = fullPath;
		}
	}
}