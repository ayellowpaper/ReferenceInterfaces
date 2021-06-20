using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.Samples
{
	public class InteractableComponent : MonoBehaviour, IInteractable
	{
		public string DebugText;

		public void Interact()
		{
			Debug.Log($"Interacted with component: {this.name}");
		}
	}
}
