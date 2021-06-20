using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.Linq;
using AYellowpaper;

namespace AYellowpaper.Editor
{
	[CustomPropertyDrawer(typeof(InterfaceReference<>))]
	[CustomPropertyDrawer(typeof(InterfaceReference<,>))]
	public class InterfaceReferenceDawer : PropertyDrawer
	{
		private const string _fieldName = "_underlyingValue";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var prop = property.FindPropertyRelative(_fieldName);
			InterfaceReferenceUtility.OnGUI(position, prop, label, GetArguments(fieldInfo));
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var prop = property.FindPropertyRelative(_fieldName);
			return InterfaceReferenceUtility.GetPropertyHeight(prop, label, GetArguments(fieldInfo));
		}

		private static void GetObjectAndInterfaceType(Type fieldType, out Type objectType, out Type interfaceType)
		{
			var genericType = fieldType.GetGenericTypeDefinition();
			if (genericType == typeof(InterfaceReference<,>))
			{
				var types = fieldType.GetGenericArguments();
				interfaceType = types[0];
				objectType = types[1];
				return;
			}
			objectType = typeof(UnityEngine.Object);
			interfaceType = fieldType.GetGenericArguments()[0];
		}

		private static InterfaceObjectArguments GetArguments(FieldInfo fieldInfo)
		{
			GetObjectAndInterfaceType(fieldInfo.FieldType, out var objectType, out var interfaceType);
			return new InterfaceObjectArguments(objectType, interfaceType);
		}
	}
}
