using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

namespace Zelude.Editor
{
	[CustomPropertyDrawer(typeof(InterfaceObject<>))]
	[CustomPropertyDrawer(typeof(InterfaceObject<,>))]
	public class ReferenceInterfaceObjectDrawer : PropertyDrawer
	{
		const string _fieldName = "_underlyingValue";

		private static bool _queuedOpening = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var prop = property.FindPropertyRelative(_fieldName);
			GetObjectAndInterfaceType(out var objectType, out var interfaceType);
			EditorGUI.ObjectField(position, prop, interfaceType, label);
			var controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;
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

			var filter = new ObjectSelectorFilter(sb.ToString(), obj => interfaceType.IsAssignableFrom(obj.GetType()) && objectType.IsAssignableFrom(obj.GetType()));
			ObjectSelectorWindow.Show(property, obj => { property.objectReferenceValue = obj; property.serializedObject.ApplyModifiedProperties(); }, (obj, success) => { if (success) property.objectReferenceValue = obj; }, filter);
			ObjectSelectorWindow.Instance.position = win.position;
			ObjectSelectorWindow.Instance.titleContent = win.titleContent;
			_queuedOpening = false;
		}

		private void GetObjectAndInterfaceType(out Type objectType, out Type interfaceType)
		{
			var fieldType = fieldInfo.FieldType;
			var genericType = fieldType.GetGenericTypeDefinition();
			if (genericType == typeof(InterfaceObject<,>))
			{
				var types = fieldType.GetGenericArguments();
				objectType = types[0];
				interfaceType = types[1];
				return;
			}
			objectType = typeof(UnityEngine.Object);
			interfaceType = fieldType.GetGenericArguments()[0];
		}
	}
}
