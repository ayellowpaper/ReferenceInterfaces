using System.Linq.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AYellowpaper.Editor
{
	internal static class InterfaceReferenceUtility
	{
		private const string _fieldName = "_underlyingValue";
		private const float _helpBoxHeight = 24;

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

			var prevValue = property.objectReferenceValue;
			position.height = EditorGUIUtility.singleLineHeight;
			var prevColor = GUI.backgroundColor;
			// change visuals if the assigned value doesn't implement the interface (e.g. after removing the interface from the target)
			if (IsAssignedAndHasWrongInterface(prevValue, args))
			{
				var helpBoxPosition = position;
				helpBoxPosition.y += position.height;
				helpBoxPosition.height = _helpBoxHeight;
				EditorGUI.HelpBox(helpBoxPosition, $"Object {prevValue.name} needs to implement the required interface {args.InterfaceType}.", MessageType.Error);
				GUI.backgroundColor = Color.red;
			}

			// disable of not assignable
			var prevEnabledState = GUI.enabled;
			if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled && !CanAssign(DragAndDrop.objectReferences, args, true))
				GUI.enabled = false;

			EditorGUI.BeginChangeCheck();
			EditorGUI.ObjectField(position, property, args.ObjectType, label);
			if (EditorGUI.EndChangeCheck())
			{
				// assign the value from the GameObject if it's dragged in, or reset if the value isn't assignable
				var newVal = GetComponentInGameObjectOrDefault(property.objectReferenceValue, args);
				if (newVal != null && !CanAssign(newVal, args))
					property.objectReferenceValue = prevValue;
				property.objectReferenceValue = newVal;
			}

			GUI.color = prevColor;
			GUI.enabled = prevEnabledState;

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

		public static float GetPropertyHeight(SerializedProperty property, GUIContent label, InterfaceObjectArguments args)
		{
			if (IsAssignedAndHasWrongInterface(property.objectReferenceValue, args))
				return EditorGUIUtility.singleLineHeight + _helpBoxHeight;
			return EditorGUIUtility.singleLineHeight;
		}

		public static bool IsAsset(Type type)
		{
			return !(type == typeof(GameObject) || type == typeof(Component));
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
			// this makes sure we don't find anything if there's no type supplied
			if (sb.Length == 0)
				sb.Append("t:");

			var filter = new ObjectSelectorFilter(sb.ToString(), obj => CanAssign(obj, args));
			ObjectSelectorWindow.Show(property, obj => { property.objectReferenceValue = obj; property.serializedObject.ApplyModifiedProperties(); }, (obj, success) => { if (success) property.objectReferenceValue = obj; }, filter);
			ObjectSelectorWindow.Instance.position = win.position;
			var content = new GUIContent($"Select {args.ObjectType.Name} ({args.InterfaceType.Name})");
			ObjectSelectorWindow.Instance.titleContent = content;
			_isOpeningQueued = false;
		}

		private static UnityEngine.Object GetComponentInGameObjectOrDefault(UnityEngine.Object obj, InterfaceObjectArguments args)
		{
			if (obj is GameObject go && go.TryGetComponent(args.InterfaceType, out var comp))
				return comp;
			return obj;
		}

		private static bool IsAssignedAndHasWrongInterface(UnityEngine.Object obj, InterfaceObjectArguments args) => obj != null && !args.InterfaceType.IsAssignableFrom(obj.GetType());

		private static bool CanAssign(UnityEngine.Object[] objects, InterfaceObjectArguments args, bool lookIntoGameObject = false) => objects.All(obj => CanAssign(obj, args, lookIntoGameObject));

		private static bool CanAssign(UnityEngine.Object obj, InterfaceObjectArguments args, bool lookIntoGameObject = false)
		{
			if (lookIntoGameObject)
				obj = GetComponentInGameObjectOrDefault(obj, args);
			return args.InterfaceType.IsAssignableFrom(obj.GetType()) && args.ObjectType.IsAssignableFrom(obj.GetType());
		}
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