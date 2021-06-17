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
	public class InterfaceObjectDawer : PropertyDrawer
	{
		private const string _fieldName = "_underlyingValue";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var prop = property.FindPropertyRelative(_fieldName);
			GetObjectAndInterfaceType(fieldInfo.FieldType, out var objectType, out var interfaceType);
			InterfaceObjectArguments args = new InterfaceObjectArguments(objectType, interfaceType);
			InterfaceObjectUtility.OnGUI(position, prop, label, args);
		}

		private static void GetObjectAndInterfaceType(Type fieldType, out Type objectType, out Type interfaceType)
		{
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
