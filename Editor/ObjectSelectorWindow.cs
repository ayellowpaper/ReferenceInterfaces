using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEngine;
using System.Linq;

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
	private Func<Object, bool> _filterCallback;
	private SerializedProperty _editingProperty;
	private List<ItemInfo> _filteredItems;
	private ToolbarSearchField _searchbox;
	private ListView _listView;
	private Label _detailsLabel;
	private string _searchText;
	private ItemInfo _currentItem;
	private bool _canceled = true;
	private int _undoGroup;

	public bool initialized { get; private set; } = false;

	public string searchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
			FilterItems();
		}
	}

	public List<ItemInfo> allItems { get; private set; }

	public static void Show(SerializedProperty property, Action<Object> onSelectionChanged, Action<Object, bool> onSelectorClosed, Func<Object, bool> filterCallback)
	{
		if (Instance == null)
			Instance = ScriptableObject.CreateInstance<ObjectSelectorWindow>();
		Instance._editingProperty = property;
		Instance._selectionChangedCallback = onSelectionChanged;
		Instance._selectorClosedCallback = onSelectorClosed;
		Instance._filterCallback = filterCallback ?? (x => true);
		Instance.Init();
		Instance.ShowAuxWindow();
		// Instance.Show();
	}

	private void Init()
	{
		InitData();

		_searchbox = new ToolbarSearchField();
		_searchbox.RegisterValueChangedCallback(SearchFilterChanged);
		_searchbox.style.flexGrow = 1;
		_searchbox.style.maxHeight = 16;
		_searchbox.style.width = Length.Percent(100f);
		_searchbox.style.marginRight = 4;
		rootVisualElement.Add(_searchbox);

		_listView = new ListView(_filteredItems, 16, MakeItem, BindItem);
		_listView.onSelectionChange += ItemSelectionChanged;
		_listView.onItemsChosen += ItemsChosen;
		_listView.style.flexGrow = 1;
		rootVisualElement.Add(_listView);

		_detailsLabel = new Label();
		_detailsLabel.style.backgroundColor = new StyleColor(new Color(0.165f, 0.165f, 0.165f));
		_detailsLabel.style.borderTopColor = new StyleColor(new Color(0.251f, 0.251f, 0.251f));
		_detailsLabel.style.borderTopWidth = 1;
		_detailsLabel.style.paddingLeft = 20;
		_detailsLabel.style.paddingTop = 4;
		_detailsLabel.style.paddingBottom = 2;
		_detailsLabel.style.color = new StyleColor(new Color(0.667f, 0.667f, 0.667f));
		_detailsLabel.style.fontSize = 12;
		rootVisualElement.Add(_detailsLabel);


		// Initialize selection
		// if (s_Context.currentObject != null)
		// {
		// 	var currentSelectedId = s_Context.currentObject.GetInstanceID();
		// 	var selectedIndex = m_FilteredItems.FindIndex(item => item.instanceId == currentSelectedId);
		// 	if (selectedIndex >= 0)
		// 		m_ListView.selectedIndex = selectedIndex;
		// }

		FinishInit();
	}

	private void OnDisable()
	{
		_selectorClosedCallback?.Invoke(GetCurrentObject(), _canceled);
		if (_canceled)
			Undo.RevertAllDownToGroup(_undoGroup);
		else
			Undo.CollapseUndoOperations(_undoGroup);
		Instance = null;
	}

	private void InitData()
	{
		_undoGroup = Undo.GetCurrentGroup();
		_searchText = "";
		allItems = new List<ItemInfo>();
		_filteredItems = new List<ItemInfo>();

		// if ((s_Context.visibleObjects & VisibleObjects.Assets) == VisibleObjects.Assets)
		// 	allItems.AddRange(FetchAllAssets());
		// if ((s_Context.visibleObjects & VisibleObjects.Scene) == VisibleObjects.Scene)
		// 	allItems.AddRange(FetchAllGameObjects());

		var empty = new ItemInfo() { InstanceID = null, Label = "None" };

		allItems.AddRange(FetchAllComponents());
		allItems.Sort((item, other) => item.Label.CompareTo(other.Label));
		allItems.Insert(0, empty);

		_filteredItems.AddRange(allItems);
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
		searchText = evt.newValue;
	}

	private void FilterItems()
	{
		_filteredItems.Clear();
		_filteredItems.AddRange(allItems.Where(item => string.IsNullOrEmpty(searchText) || item.Label.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) >= 0));

		_listView.Refresh();
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

		ve.style.flexDirection = FlexDirection.Row;
		ve.style.paddingLeft = 17;
		label.style.flexGrow = 1;
		label.style.paddingLeft = 3;
		image.style.flexBasis = 14;

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
		_canceled = false;
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
		var transform = (obj is GameObject go) ? go.transform : (obj as Component).transform;
		int compIndex = Array.IndexOf(transform.gameObject.GetComponents(typeof(Component)), obj);
		_detailsLabel.text = $"{GetTransformPath(transform)} [{compIndex}]";
	}

	private string GetTransformPath(Transform tr)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(tr.name);
		while (tr.parent != null)
		{
			sb.Insert(0, tr.parent.name + "/");
			tr = tr.parent;
		}
		return sb.ToString();
	}

	private IEnumerable<ItemInfo> FetchAllAssets()
	{
		var allPaths = AssetDatabase.GetAllAssetPaths();
		if (allPaths == null)
			yield break;

		var requiredTypes = new List<string>();
		foreach (var path in allPaths)
		{
			var type = AssetDatabase.GetMainAssetTypeAtPath(path);
			var typeName = type.FullName ?? "";
			if (requiredTypes.Any(requiredType => typeName.Contains(requiredType)))
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(path);
				var matchFilterConstraint = _filterCallback?.Invoke(asset);
				if (matchFilterConstraint.HasValue && !matchFilterConstraint.Value)
					continue;
				var instanceId = asset?.GetInstanceID() ?? 0;
				yield return new ItemInfo { InstanceID = instanceId, Label = path };
			}
		}
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
		var matchFilterConstraint = _filterCallback?.Invoke(obj);
		return (!matchFilterConstraint.HasValue || matchFilterConstraint.Value);
	}

	private Object GetCurrentObject()
	{
		if (_currentItem == null || _currentItem.InstanceID == null) return null;
		return EditorUtility.InstanceIDToObject((int)_currentItem.InstanceID);
	}
}