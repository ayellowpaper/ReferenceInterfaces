using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.Samples
{
	public class InteractableTest : MonoBehaviour
	{
		// Arrays
		[RequireInterface(typeof(IInteractable))]
		public MonoBehaviour[] ReferenceWithAttributeArray;

		public InterfaceReference<IInteractable>[] ReferenceArray;

		// Lists
		[RequireInterface(typeof(IInteractable))]
		public List<Object> ReferenceWithAttributeList;

		public List<InterfaceReference<IInteractable>> ReferenceList;

		// Fields
		public InterfaceReference<IInteractable, ScriptableObject> ReferenceRestrictedToScriptableObject;
		public InterfaceReference<IInteractable, MonoBehaviour> ReferenceRestrictedToMonoBehaviour;

		[RequireInterface(typeof(IInteractable))]
		public ScriptableObject AttributeRestrictedToScriptableObject;
		[RequireInterface(typeof(IInteractable))]
		public MonoBehaviour AttributeRestrictedToMonoBehaviour;
	}
}


