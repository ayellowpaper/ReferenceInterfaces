using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zelude
{
	[System.Serializable]
	public class InterfaceObject<TInterface, UObject> where UObject : Object where TInterface : class
	{
		[SerializeField]
		[HideInInspector]
		private UObject _underlyingValue;

		public Object UnderlyingValue => _underlyingValue;
		public TInterface Value => _underlyingValue as TInterface;

		public InterfaceObject() { }
		public InterfaceObject(UObject target) => _underlyingValue = target;
		public InterfaceObject(TInterface @interface) => _underlyingValue = @interface as UObject;

		public static implicit operator TInterface(InterfaceObject<TInterface, UObject> obj) => obj.Value;
	}

	[System.Serializable]
	public class InterfaceObject<TInterface> : InterfaceObject<TInterface, Object> where TInterface : class
	{
	}
}