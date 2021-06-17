using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Zelude.Editor
{
	public class Tab : Toggle
	{
		public Tab(string text) : base()
		{
			base.text = text;
			RemoveFromClassList(Toggle.ussClassName);
			AddToClassList(ussClassName);
		}
	}
}