using System.Xml.Linq;
using System.Data.Common;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEngine;

namespace Zelude.Editor
{
	internal class ObjectSelectorWindow : EditorWindow
	{
		public class ItemInfo
		{
			public Texture Icon;
			public int? InstanceID;
			public string Label;
		}

		public static ObjectSelectorWindow Instance { get; private set; }

		private Action<Object> _selectionChangedCallback;
		private Action<Object, bool> _selectorClosedCallback;
		private ObjectSelectorFilter _filter;
		private SerializedProperty _editingProperty;
		private List<ItemInfo> _allItems;
		private List<ItemInfo> _filteredItems;
		private ItemInfo _currentItem;
		private string _searchText;
		private bool _userCanceled = true;
		private bool _showSceneObjects = true;
		private int _undoGroup;
		private ToolbarSearchField _searchbox;
		private ListView _listView;
		private Label _detailsLabel;
		private Tab _sceneTab;
		private Tab _assetsTab;

		private static ItemInfo _nullItem = new ItemInfo() { InstanceID = null, Label = "None" };

		public bool initialized { get; private set; } = false;

		public string SearchText
		{
			get => _searchText;
			set
			{
				_searchText = value;
				FilterItems();
			}
		}

		public static void Show(SerializedProperty property, Action<Object> onSelectionChanged, Action<Object, bool> onSelectorClosed, ObjectSelectorFilter filter)
		{
			if (Instance == null)
				Instance = ScriptableObject.CreateInstance<ObjectSelectorWindow>();
			Instance._editingProperty = property;
			Instance._selectionChangedCallback = onSelectionChanged;
			Instance._selectorClosedCallback = onSelectorClosed;
			Instance._filter = filter;
			Instance.Init();
			Instance.ShowAuxWindow();
			//Instance.Show();
		}

		private void Init()
		{
			InitData();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.zelude.objectwithinterface/Assets/USS/ObjectSelectorWindow.uss");
			rootVisualElement.styleSheets.Add(styleSheet);

			_searchbox = new ToolbarSearchField();
			_searchbox.RegisterValueChangedCallback(SearchFilterChanged);
			rootVisualElement.Add(_searchbox);

			var tabContainer = new VisualElement();
			tabContainer.style.flexDirection = FlexDirection.Row;
			_assetsTab = new Tab("Assets");
			_sceneTab = new Tab("Scene");
			tabContainer.Add(_assetsTab);
			tabContainer.Add(_sceneTab);
			rootVisualElement.Add(tabContainer);

			_listView = new ListView(_filteredItems, 16, MakeItem, BindItem);
			_listView.onSelectionChange += ItemSelectionChanged;
			_listView.onItemsChosen += ItemsChosen;
			rootVisualElement.Add(_listView);

			_detailsLabel = new Label();
			_detailsLabel.AddToClassList("details");
			rootVisualElement.Add(_detailsLabel);

			Tab activeTab = _showSceneObjects ? _sceneTab : _assetsTab;
			activeTab.SetValueWithoutNotify(true);

			var toggleGroup = new ToggleGroup();
			toggleGroup.RegisterToggle(_assetsTab);
			toggleGroup.RegisterToggle(_sceneTab);
			toggleGroup.OnToggleChanged += HandleGroupChanged;

			var targetObject = _editingProperty.objectReferenceValue;
			if (targetObject)
			{
				int instanceID = targetObject.GetInstanceID();
				var index = _filteredItems.FindIndex(x => x.InstanceID == instanceID);
				if (index >= 0)
					_listView.selectedIndex = index;
			}

			FinishInit();
		}

		private void HandleGroupChanged(object sender, Toggle toggle)
		{
			if (_showSceneObjects && toggle == this._sceneTab) return;
			_showSceneObjects = !_showSceneObjects;
			PopulateItems();
			FilterItems();
		}

		private void OnDisable()
		{
			_selectorClosedCallback?.Invoke(GetCurrentObject(), _userCanceled);
			if (_userCanceled)
				Undo.RevertAllDownToGroup(_undoGroup);
			else
				Undo.CollapseUndoOperations(_undoGroup);
			Instance = null;
		}

		private void InitData()
		{
			_undoGroup = Undo.GetCurrentGroup();
			_searchText = "";
			_allItems = new List<ItemInfo>();
			_filteredItems = new List<ItemInfo>();

			_showSceneObjects = true;
			var target = _editingProperty.objectReferenceValue;
			if (target != null)
				_showSceneObjects = !AssetDatabase.Contains(target);

			PopulateItems();
			FilterItems();
		}

		private void PopulateItems()
		{
			_allItems.Clear();
			_filteredItems.Clear();
			if (_showSceneObjects)
				_allItems.AddRange(FetchAllComponents());
			else
				_allItems.AddRange(FetchAllAssets());
			_allItems.Sort((item, other) => item.Label.CompareTo(other.Label));
		}

		private void FinishInit()
		{
			EditorApplication.delayCall += () =>
			{
				_listView.Focus();
				initialized = true;
			};
		}

		public void SetSearchFilter(string query)
		{
			_searchbox.value = query;
		}

		private void SearchFilterChanged(ChangeEvent<string> evt)
		{
			SearchText = evt.newValue;
		}

		private void FilterItems()
		{
			_filteredItems.Clear();
			_filteredItems.Add(_nullItem);
			_filteredItems.AddRange(_allItems.Where(item => string.IsNullOrEmpty(SearchText) || item.Label.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0));

			_listView?.Refresh();
		}

		private void BindItem(VisualElement listItem, int index)
		{
			if (index < 0 || index >= _filteredItems.Count)
				return;

			var label = listItem.Q<Label>();
			if (label != null)
				label.text = _filteredItems[index].Label;
			var image = listItem.Q<Image>();
			image.image = _filteredItems[index].Icon;
		}

		private static VisualElement MakeItem()
		{
			var ve = new VisualElement();
			var image = new Image();
			var label = new Label();
			ve.Add(image);
			ve.Add(label);

			ve.AddToClassList("list-item");
			label.AddToClassList("list-item__text");
			image.AddToClassList("list-item__icon");

			return ve;
		}

		private void ItemSelectionChanged(IEnumerable<object> selectedItems)
		{
			_currentItem = selectedItems.FirstOrDefault() as ItemInfo;
			UpdateDetails();
			_selectionChangedCallback?.Invoke(GetCurrentObject());
		}

		private void ItemsChosen(IEnumerable<object> selectedItems)
		{
			_currentItem = selectedItems.FirstOrDefault() as ItemInfo;
			_userCanceled = false;
			Close();
		}

		private void UpdateDetails()
		{
			if (_currentItem == null)
			{
				_detailsLabel.text = "";
				return;
			}

			if (_currentItem.InstanceID == null)
			{
				_detailsLabel.text = _currentItem.Label;
				return;
			}

			var obj = EditorUtility.InstanceIDToObject((int)_currentItem.InstanceID);
			if (AssetDatabase.Contains(obj))
			{
				_detailsLabel.text = AssetDatabase.GetAssetPath(obj);
			}
			else
			{
				var transform = (obj is GameObject go) ? go.transform : (obj as Component).transform;
				int compIndex = Array.IndexOf(transform.gameObject.GetComponents(typeof(Component)), obj);
				_detailsLabel.text = $"{GetTransformPath(transform)} [{compIndex}]";
			}
		}

		private string GetTransformPath(Transform transform)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(transform.name);
			while (transform.parent != null)
			{
				sb.Insert(0, transform.parent.name + "/");
				transform = transform.parent;
			}
			return sb.ToString();
		}

		private IEnumerable<ItemInfo> FetchAllAssets()
		{
			var property = new HierarchyProperty(HierarchyType.Assets, false);
			property.SetSearchFilter(_filter.AssetSearchFilter, 0);

			while (property.Next(null))
			{
				yield return new ItemInfo { Icon = property.icon, InstanceID = property.instanceID, Label = property.name };
			}
			yield break;
		}

		private IEnumerable<ItemInfo> FetchAllComponents()
		{
			var property = new HierarchyProperty(HierarchyType.GameObjects, false);

			while (property.Next(null))
			{
				var go = property.pptrValue as GameObject;
				if (go == null) continue;

				if (CheckFilter(go))
					yield return new ItemInfo { Icon = property.icon, InstanceID = property.instanceID, Label = property.name };

				foreach (var comp in go.GetComponents(typeof(Component)))
				{
					if (!CheckFilter(comp)) continue;
					yield return new ItemInfo { Icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image, InstanceID = comp.GetInstanceID(), Label = property.name };
				}
			}
		}

		private bool CheckFilter(UnityEngine.Object obj)
		{
			var matchFilterConstraint = _filter.SceneFilterCallback?.Invoke(obj);
			return (!matchFilterConstraint.HasValue || matchFilterConstraint.Value);
		}

		private Object GetCurrentObject()
		{
			if (_currentItem == null || _currentItem.InstanceID == null) return null;
			return EditorUtility.InstanceIDToObject((int)_currentItem.InstanceID);
		}
	}

	public class ObjectSelectorFilter
	{
		public string AssetSearchFilter;
		public Func<Object, bool> SceneFilterCallback;

		public ObjectSelectorFilter() : this("", x => true) { }

		public ObjectSelectorFilter(string assetSearchFilter, Func<Object, bool> sceneFilterCallback)
		{
			AssetSearchFilter = assetSearchFilter;
			SceneFilterCallback = sceneFilterCallback;
		}
	}
}