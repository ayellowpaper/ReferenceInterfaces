using System.Linq.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Zelude.Editor
{
	public static class InterfaceObjectUtility
	{
		private const string _fieldName = "_underlyingValue";

		private static GUIStyle _style;
		private static bool _isOpeningQueued = false;

		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, Type objectType, Type interfaceType)
		{
			if (_style == null)
			{
				_style = new GUIStyle(EditorStyles.label);
				var objectFieldStyle = EditorStyles.objectField;
				_style.font = objectFieldStyle.font;
				_style.fontSize = objectFieldStyle.fontSize;
				_style.fontStyle = objectFieldStyle.fontStyle;
				_style.alignment = TextAnchor.MiddleRight;
			}

			Debug.Assert(typeof(UnityEngine.Object).IsAssignableFrom(objectType), $"{nameof(objectType)} needs to be of Type {typeof(UnityEngine.Object)}.", property.serializedObject.targetObject);
			Debug.Assert(interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface.", property.serializedObject.targetObject);

			EditorGUI.BeginChangeCheck();
			var prevValue = property.objectReferenceValue;

			var prevEnabledState = GUI.enabled;
			if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled && !CanAssign(DragAndDrop.objectReferences, objectType, interfaceType))
				GUI.enabled = false;

			EditorGUI.ObjectField(position, property, objectType, label);

			GUI.enabled = prevEnabledState;

			if (EditorGUI.EndChangeCheck())
			{
				var newVal = property.objectReferenceValue;
				if (!(newVal == null || CanAssign(newVal, objectType, interfaceType)))
					property.objectReferenceValue = prevValue;
			}

			var controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;
			if (Event.current.type == EventType.Repaint)
			{
				var displayString = $"({ObjectNames.NicifyVariableName(interfaceType.Name)})";
				var interfaceLabelPosition = position;
				interfaceLabelPosition.width -= 22;
				_style.Draw(interfaceLabelPosition, new GUIContent(displayString), controlID, DragAndDrop.activeControlID == controlID, position.Contains(Event.current.mousePosition));
			}
			var currentObjectPickerID = EditorGUIUtility.GetObjectPickerControlID();
			if (controlID == currentObjectPickerID && _isOpeningQueued == false)
			{
				if (EditorWindow.focusedWindow != null)
				{
					_isOpeningQueued = true;
					EditorApplication.delayCall += () => OpenDelayed(property, objectType, interfaceType);
				}
			}
		}

		private static void OpenDelayed(SerializedProperty property, Type objectType, Type interfaceType)
		{
			var win = EditorWindow.focusedWindow;
			win.Close();

			var derivedTypes = TypeCache.GetTypesDerivedFrom(interfaceType);
			var sb = new StringBuilder();
			foreach (var type in derivedTypes)
			{
				if (objectType.IsAssignableFrom(type))
					sb.Append("t:" + type.FullName + " ");
			}

			var filter = new ObjectSelectorFilter(sb.ToString(), obj => CanAssign(obj, interfaceType, objectType));
			ObjectSelectorWindow.Show(property, obj => { property.objectReferenceValue = obj; property.serializedObject.ApplyModifiedProperties(); }, (obj, success) => { if (success) property.objectReferenceValue = obj; }, filter);
			ObjectSelectorWindow.Instance.position = win.position;
			var content = new GUIContent($"Select {objectType.Name} ({interfaceType.Name})");
			ObjectSelectorWindow.Instance.titleContent = content;
			_isOpeningQueued = false;
		}

		private static bool CanAssign(UnityEngine.Object[] objects, Type objectType, Type interfaceType) => objects.All(obj => CanAssign(obj, objectType, interfaceType));

		private static bool CanAssign(UnityEngine.Object obj, Type objectType, Type interfaceType) => interfaceType.IsAssignableFrom(obj.GetType()) && objectType.IsAssignableFrom(obj.GetType());
	}
}