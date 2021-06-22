Everything is in the namespace AYellowpaper.
Use the attribute RequireInterface on any serializable field of type UnityEngine.Object or of any type inheriting UnityEngine.Object.
Alternatively use the InterfaceReference class, which is serializable and doesn't require the attribute. This allows you to access the Value property which automatically casts to the interface.