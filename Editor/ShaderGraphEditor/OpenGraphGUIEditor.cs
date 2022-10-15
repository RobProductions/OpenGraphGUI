using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RobProductions.OpenGraphGUI.Editor
{
	public class OpenGraphGUIEditor : ShaderGUI
	{

		MaterialEditor matEditor;

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			matEditor = materialEditor;
			Material material = materialEditor.target as Material;

			// Use default labelWidth
			//EditorGUIUtility.labelWidth = 0f;

			for(int i = 0; i < properties.Length; i++)
			{
				var thisProp = properties[i];

				if(thisProp.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
				{
					continue;
				}
				RenderDefaultPropertyView(thisProp);
			}


			matEditor.RenderQueueField();

			matEditor.EnableInstancingField();
			matEditor.DoubleSidedGIField();
		}

		void RenderDefaultPropertyView(MaterialProperty v)
		{
			switch(v.type)
			{
				case MaterialProperty.PropType.Texture:
					matEditor.TextureProperty(v, v.displayName);
					break;
				case MaterialProperty.PropType.Float:
					matEditor.FloatProperty(v, v.displayName);
					break;
				default:
					GraphLog("Couldn't find material property type to render - " + v.displayName);
					break;
			}
		}

		/// <summary>
		/// Log a warning or error with OpenGraphGUI.
		/// </summary>
		/// <param name="v"></param>
		void GraphLog(string v)
		{
			Debug.Log("OpenGraphGUI: " + v);
		}
	}
}
