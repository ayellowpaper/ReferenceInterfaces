using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.Linq;

namespace Zelude.Editor
{
	[CustomPropertyDrawer(typeof(InterfaceObject<>))]
	[CustomPropertyDrawer(typeof(InterfaceObject<,>))]
	public class ReferenceInterfaceObjectDrawer : PropertyDrawer
	{
		private const string _fieldName = "_underlyingValue";

		private static GUIStyle _style;
		private static bool _queuedOpening = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
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

			var prop = property.FindPropertyRelative(_fieldName);
			GetObjectAndInterfaceType(out var objectType, out var interfaceType);

			EditorGUI.BeginChangeCheck();
			var prevValue = prop.objectReferenceValue;

			var prevEnabledState = GUI.enabled;
			if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled && !CanAssign(DragAndDrop.objectReferences, objectType, interfaceType))
				GUI.enabled = false;

			EditorGUI.ObjectField(position, prop, objectType, label);

			GUI.enabled = prevEnabledState;

			if (EditorGUI.EndChangeCheck())
			{
				var newVal = prop.objectReferenceValue;
				if (!(newVal == null || CanAssign(newVal, objectType, interfaceType)))
					prop.objectReferenceValue = prevValue;
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
			if (controlID == currentObjectPickerID && _queuedOpening == false)
			{
				if (EditorWindow.focusedWindow != null)
				{
					_queuedOpening = true;
					EditorApplication.delayCall += () => OpenDelayed(prop, objectType, interfaceType);
				}
			}
		}

		private void OpenDelayed(SerializedProperty property, Type objectType, Type interfaceType)
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
			win.titleContent.text = win.titleContent.text + $" ({interfaceType.Name})";
			ObjectSelectorWindow.Instance.titleContent = win.titleContent;
			_queuedOpening = false;
		}

		private bool CanAssign(UnityEngine.Object[] objects, Type objectType, Type interfaceType) => objects.All(obj => CanAssign(obj, objectType, interfaceType));

		private bool CanAssign(UnityEngine.Object obj, Type objectType, Type interfaceType) => interfaceType.IsAssignableFrom(obj.GetType()) && objectType.IsAssignableFrom(obj.GetType());

		private void GetObjectAndInterfaceType(out Type objectType, out Type interfaceType)
		{
			var fieldType = fieldInfo.FieldType;
			var genericType = fieldType.GetGenericTypeDefinition();
			if (genericType == typeof(InterfaceObject<,>))
			{
				var types = fieldType.GetGenericArguments();
				interfaceType = types[0];
				objectType = types[1];
				return;
			}
			objectType = typeof(UnityEngine.Object);
			interfaceType = fieldType.GetGenericArguments()[0];
		}
	}
}
