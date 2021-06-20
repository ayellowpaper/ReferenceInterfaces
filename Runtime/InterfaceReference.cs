using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper
{
	/// <summary>
	/// Serializes a UnityEngine.Object with the given interface. Adds a nice decorator in the inspector as well and a custom object selector.
	/// </summary>
	/// <typeparam name="TInterface">The interface.</typeparam>
	/// <typeparam name="UObject">The UnityEngine.Object.</typeparam>
	[System.Serializable]
	public class InterfaceReference<TInterface, UObject> where UObject : Object where TInterface : class
	{
		[SerializeField]
		[HideInInspector]
		private UObject _underlyingValue;

		/// <summary>
		/// Get the interface, if the UnderlyingValue is not null and implements the given interface.
		/// </summary>
		public TInterface Value
		{
			get
			{
				if (_underlyingValue == null)
					return null;
				var @interface = _underlyingValue as TInterface;
				Debug.Assert(@interface != null, $"{_underlyingValue} needs to implement interface {nameof(TInterface)}.");
				return @interface;
			}
			set
			{
				if (value == null)
					_underlyingValue = null;
				else
				{
					var newValue = value as UObject;
					UnityEngine.Debug.Assert(newValue != null, $"{value} needs to be of type {typeof(UObject)}.");
					_underlyingValue = newValue;
				}
			}
		}
		/// <summary>
		/// Get the actual UnityEngine.Object that gets serialized.
		/// </summary>
		public UObject UnderlyingValue
		{
			get => _underlyingValue;
			set => _underlyingValue = value;
		}

		public InterfaceReference() { }
		public InterfaceReference(UObject target) => _underlyingValue = target;
		public InterfaceReference(TInterface @interface) => _underlyingValue = @interface as UObject;

		public static implicit operator TInterface(InterfaceReference<TInterface, UObject> obj) => obj.Value;
	}

	/// <summary>
	/// Serializes a UnityEngine.Object with the given interface. Adds a nice decorator in the inspector as well and a custom object selector.
	/// </summary>
	/// <typeparam name="TInterface">The interface.</typeparam>
	[System.Serializable]
	public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class
	{
	}
}