using UnityEngine;
using UnityEditor;
using Zelude;

namespace Zelude.Editor
{
	[CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
	public class RequireInterfaceAttributeDrawer : PropertyDrawer
	{
		private RequireInterfaceAttribute _requireInterfaceAttribute => (RequireInterfaceAttribute)attribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InterfaceObjectUtility.OnGUI(position, property, label, fieldInfo.FieldType, _requireInterfaceAttribute.InterfaceType);
		}
	}
}