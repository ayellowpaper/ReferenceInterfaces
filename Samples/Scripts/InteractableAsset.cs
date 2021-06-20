using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.Samples
{
	public class InteractableAsset : ScriptableObject, IInteractable
	{
		public void Interact()
		{
			Debug.Log($"Interacted with asset: {this.name}");
		}
	}
}
