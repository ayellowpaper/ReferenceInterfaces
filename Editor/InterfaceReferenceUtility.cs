using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AYellowpaper.Editor
{
    internal static class InterfaceReferenceUtility
    {
        private const float _helpBoxHeight = 24;

        private static GUIStyle _normalInterfaceLabelStyle;
        private static bool _isOpeningQueued = false;

        public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, InterfaceObjectArguments args)
        {
            InitializeStyleIfNeeded();

            var prevValue = property.objectReferenceValue;
            position.height = EditorGUIUtility.singleLineHeight;
            var prevColor = GUI.backgroundColor;
            // change visuals if the assigned value doesn't implement the interface (e.g. after removing the interface from the target)
            if (IsAssignedAndHasWrongInterface(prevValue, args))
            {
                ShowWrongInterfaceErrorBox(position, prevValue, args);
                GUI.backgroundColor = Color.red;
            }

            // disable if not assignable from drag and drop
            var prevEnabledState = GUI.enabled;
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled && !CanAssign(DragAndDrop.objectReferences, args, true))
                GUI.enabled = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(position, property, args.ObjectType, label);
            if (EditorGUI.EndChangeCheck())
            {
                // assign the value from the GameObject if it's dragged in, or reset if the value isn't assignable
                var newVal = GetClosestAssignableComponent(property.objectReferenceValue, args);
                if (newVal != null && !CanAssign(newVal, args))
                    property.objectReferenceValue = prevValue;
                property.objectReferenceValue = newVal;
            }

            GUI.backgroundColor = prevColor;
            GUI.enabled = prevEnabledState;

            var controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;
            bool isHovering = position.Contains(Event.current.mousePosition);
            DrawInterfaceNameLabel(position, prevValue == null || isHovering ? $"({args.InterfaceType.Name})" : "*", controlID);
            ReplaceObjectPickerForControl(property, args, controlID);
        }

        private static void ShowWrongInterfaceErrorBox(Rect position, UnityEngine.Object prevValue, InterfaceObjectArguments args)
        {
            var helpBoxPosition = position;
            helpBoxPosition.y += position.height;
            helpBoxPosition.height = _helpBoxHeight;
            EditorGUI.HelpBox(helpBoxPosition, $"Object {prevValue.name} needs to implement the required interface {args.InterfaceType}.", MessageType.Error);
        }

        private static void ReplaceObjectPickerForControl(SerializedProperty property, InterfaceObjectArguments args, int controlID)
        {
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

        private static void DrawInterfaceNameLabel(Rect position, string displayString, int controlID)
        {
            if (Event.current.type == EventType.Repaint)
            {
                const int additionalLeftWidth = 3;
                const int verticalIndent = 1;
                var content = EditorGUIUtility.TrTextContent(displayString);
                var size = _normalInterfaceLabelStyle.CalcSize(content);
                var interfaceLabelPosition = position;
                interfaceLabelPosition.width = size.x + additionalLeftWidth;
                interfaceLabelPosition.x += position.width - interfaceLabelPosition.width - 18;
                interfaceLabelPosition.height = interfaceLabelPosition.height - verticalIndent * 2;
                interfaceLabelPosition.y += verticalIndent;
                _normalInterfaceLabelStyle.Draw(interfaceLabelPosition, EditorGUIUtility.TrTextContent(displayString), controlID, DragAndDrop.activeControlID == controlID, false);
            }
        }

        private static void InitializeStyleIfNeeded()
        {
            if (_normalInterfaceLabelStyle != null)
                return;

            _normalInterfaceLabelStyle = new GUIStyle(EditorStyles.label);
            var objectFieldStyle = EditorStyles.objectField;
            _normalInterfaceLabelStyle.font = objectFieldStyle.font;
            _normalInterfaceLabelStyle.fontSize = objectFieldStyle.fontSize;
            _normalInterfaceLabelStyle.fontStyle = objectFieldStyle.fontStyle;
            _normalInterfaceLabelStyle.alignment = TextAnchor.MiddleRight;
            _normalInterfaceLabelStyle.padding = new RectOffset(0, 2, 0, 0);
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(40/255f, 40/255f, 40/255f));
            texture.Apply();
            _normalInterfaceLabelStyle.normal.background = texture;
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

        /// <summary>
        /// Gets itself if assignable, otherwise will get the root gameobject if it belongs to one, and return the first possible component
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static UnityEngine.Object GetClosestAssignableComponent(UnityEngine.Object obj, InterfaceObjectArguments args)
        {
            if (CanAssign(obj, args))
                return obj;
            if (args.ObjectType.IsSubclassOf(typeof(Component)))
            {
                if (obj is GameObject go && TryFindSuitableComponent(go, args, out Component foundComponent))
                    return foundComponent;
                if (obj is Component comp && TryFindSuitableComponent(comp.gameObject, args, out foundComponent))
                    return foundComponent;
            }
            return null;
        }

        private static bool TryFindSuitableComponent(GameObject go, InterfaceObjectArguments args, out Component component)
        {
            foreach (var comp in go.GetComponents(args.ObjectType))
            {
                if (CanAssign(comp, args))
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        private static bool IsAssignedAndHasWrongInterface(UnityEngine.Object obj, InterfaceObjectArguments args) => obj != null && !args.InterfaceType.IsAssignableFrom(obj.GetType());

        private static bool CanAssign(UnityEngine.Object[] objects, InterfaceObjectArguments args, bool lookIntoGameObject = false) => objects.All(obj => CanAssign(obj, args, lookIntoGameObject));

        private static bool CanAssign(UnityEngine.Object obj, InterfaceObjectArguments args, bool lookIntoGameObject = false)
        {
            // We should never pass null, but this catches cases where scripts are broken (deleted/not compiled but still on the GameObject)
            if (obj == null)
                return false;

            if (args.InterfaceType.IsAssignableFrom(obj.GetType()) && args.ObjectType.IsAssignableFrom(obj.GetType()))
                return true;
            if (lookIntoGameObject)
                return CanAssign(GetClosestAssignableComponent(obj, args), args);
            return false;
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