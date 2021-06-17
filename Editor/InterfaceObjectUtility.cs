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

		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, InterfaceObjectArguments args)
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

			EditorGUI.BeginChangeCheck();
			var prevValue = property.objectReferenceValue;

			var prevEnabledState = GUI.enabled;
			if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled && !CanAssign(DragAndDrop.objectReferences, args))
				GUI.enabled = false;

			EditorGUI.ObjectField(position, property, args.ObjectType, label);

			GUI.enabled = prevEnabledState;

			if (EditorGUI.EndChangeCheck())
			{
				var newVal = property.objectReferenceValue;
				if (!(newVal == null || CanAssign(newVal, args)))
					property.objectReferenceValue = prevValue;
			}

			var controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;
			if (Event.current.type == EventType.Repaint)
			{
				var displayString = $"({ObjectNames.NicifyVariableName(args.InterfaceType.Name)})";
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
					EditorApplication.delayCall += () => OpenDelayed(property, args);
				}
			}
		}

		private static void OpenDelayed(SerializedProperty property, InterfaceObjectArguments args)
		{
			var win = EditorWindow.focusedWindow;
			win.Close();

			var derivedTypes = TypeCache.GetTypesDerivedFrom(args.InterfaceType);
			var sb = new StringBuilder();
			foreach (var type in derivedTypes)
			{
				if (args.ObjectType.IsAssignableFrom(type))
					sb.Append("t:" + type.FullName + " ");
			}

			var filter = new ObjectSelectorFilter(sb.ToString(), obj => CanAssign(obj, args));
			ObjectSelectorWindow.Show(property, obj => { property.objectReferenceValue = obj; property.serializedObject.ApplyModifiedProperties(); }, (obj, success) => { if (success) property.objectReferenceValue = obj; }, filter);
			ObjectSelectorWindow.Instance.position = win.position;
			var content = new GUIContent($"Select {args.ObjectType.Name} ({args.InterfaceType.Name})");
			ObjectSelectorWindow.Instance.titleContent = content;
			_isOpeningQueued = false;
		}

		private static bool CanAssign(UnityEngine.Object[] objects, InterfaceObjectArguments args) => objects.All(obj => CanAssign(obj, args));

		private static bool CanAssign(UnityEngine.Object obj, InterfaceObjectArguments args) => args.InterfaceType.IsAssignableFrom(obj.GetType()) && args.ObjectType.IsAssignableFrom(obj.GetType());
	}

	public struct InterfaceObjectArguments
	{
		public Type ObjectType;
		public Type InterfaceType;

		public InterfaceObjectArguments(Type objectType, Type interfaceType)
		{
			Debug.Assert(typeof(UnityEngine.Object).IsAssignableFrom(objectType), $"{nameof(objectType)} needs to be of Type {typeof(UnityEngine.Object)}.");
			Debug.Assert(interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface.");
			ObjectType = objectType;
			InterfaceType = interfaceType;
		}
	}
}