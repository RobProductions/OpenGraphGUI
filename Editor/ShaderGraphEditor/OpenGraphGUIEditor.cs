using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// This is a globally accessible "wrapper" class that helps
/// to shorten the OpenGraphGUI namespace for final use. 
/// With this inheritance setup, you simply need to type in "RPOpenGraphGUI"
/// to the "Custom Editor GUI" field in ShaderGraph rather than the
/// lengthy name RobProductions.OpenGraphGUI.Editor.OpenGraphGUIEditor.
/// </summary>
public class RPOpenGraphGUI : RobProductions.OpenGraphGUI.Editor.OpenGraphGUIEditor
{
	//Leave empty to inherit all default methods + properties

	public RPOpenGraphGUI()
	{
		renderExtensions = new Dictionary<string, System.Action<MaterialEditor, MaterialProperty>>();
		renderExtensions.Add("CustomRender", RenderTest);
	}

	void RenderTest(MaterialEditor editor, MaterialProperty v)
	{
		//Your custom handler GUI here ->
		editor.DefaultShaderProperty(v, "Proof that it renders extension");
	}
}

namespace RobProductions.OpenGraphGUI.Editor
{
	/// <summary>
	/// This is the main Shader Graph custom GUI that renders the material properties.
	/// Inherits from ShaderGUI, which is what the Editor uses to draw the inspector. 
	/// </summary>
	public class OpenGraphGUIEditor : ShaderGUI
	{
		/// <summary>
		/// Width of the default property value editor box.
		/// </summary>
		const float rightValueBoxSize = 100f;
		/// <summary>
		/// Width of the texture field on default texture property renders.
		/// </summary>
		const float texFieldBoxSize = 65f;
		/// <summary>
		/// The amount of tab spacing used when a property is dependent visible.
		/// </summary>
		const float dependentVisibleTabSpace = 12f;
		/// <summary>
		/// The amount of vertical padding after a dependent visible property is shown.
		/// </summary>
		const float dependentVisibleVerticalSpace = 5f;
		/// <summary>
		/// Amount of tab spacing for single line texture property.
		/// </summary>
		const float singleLineTexTabSpace = 3f;

		const string labelPrefix = "*";
		const string singleLineTexPrefix = "%";
		const string dependentVisibleTextPrefix = "^";
		const string linkedPropertyPrefix = "&";

		private MaterialEditor matEditor;
		private Dictionary<string, MaterialProperty> currentLinkedProperties = new Dictionary<string, MaterialProperty>();

		protected Dictionary<string, System.Action<MaterialEditor, MaterialProperty>> renderExtensions = null;

		public OpenGraphGUIEditor()
		{
			renderExtensions = null;
		}

		//BASE GUI STRUCTURE

		/// <summary>
		/// Main GUI function that will render this panel.
		/// </summary>
		/// <param name="materialEditor"></param>
		/// <param name="properties"></param>
		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			matEditor = materialEditor;
			Material material = materialEditor.target as Material;
			

			//Set the width of the left side to be current size minus some constant pixel value
			//This seems to be how the default ShaderGUI for ShaderGraphs is handled
			EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - rightValueBoxSize;

			RenderPropertiesList(properties);

			RenderBottomOptions();
		}

		void RenderPropertiesList(MaterialProperty[] properties)
		{
			bool lastWasPopulated = true;

			for (int i = 0; i < properties.Length; i++)
			{
				var thisProp = properties[i];

				if (thisProp.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
				{
					//Don't draw this property since it is meant to be hidden
					continue;
				}

				//First check if this property has a custom extension
				if(renderExtensions != null && renderExtensions.ContainsKey(thisProp.displayName))
				{
					//Invoke the custom render function passed into the dictionary
					renderExtensions[thisProp.displayName].Invoke(matEditor, thisProp);
				}
				else
				{
					var propName = thisProp.displayName;

					//Check the initial type of the property
					if(propName.StartsWith(labelPrefix))
					{
						//This is a label type, so show a bold header instead of the property

						//Trim the label prefix
						propName = propName.Substring(labelPrefix.Length);

						RenderLabelProperty(propName);
					}
					else if (propName.StartsWith(dependentVisibleTextPrefix))
					{
						//It is dependent, so we will conditionally render it

						//Trim the dependent visible prefix
						propName = propName.Substring(dependentVisibleTextPrefix.Length);

						if (lastWasPopulated)
						{
							RenderDependentVisibleProperty(thisProp, propName, i);
						}
						else
						{
							//Don't render this property
						}
					}
					else
					{
						//It's not dependent, so update populated state based on this
						if (thisProp.type == MaterialProperty.PropType.Texture)
						{
							var tex = thisProp.textureValue;
							lastWasPopulated = tex != null;
						}
						else
						{
							lastWasPopulated = true;
						}
						//Render as an "OpenGraphGUI" property; i.e. it will draw
						RenderVisibleProperty(thisProp, propName, i);
					}
				}
			}
		}

		/// <summary>
		/// Render the bottom emission and instancing fields that are present
		/// on default ShaderGraph GUIs.
		/// </summary>
		void RenderBottomOptions()
		{
			matEditor.RenderQueueField();

			matEditor.EnableInstancingField();
			matEditor.DoubleSidedGIField();
			matEditor.EmissionEnabledProperty();
		}

		//PROPERTY RENDERING

		void RenderDependentVisibleProperty(MaterialProperty v, string labelName, int index)
		{
			//Shift over by a small amount to show the dependency
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.Space(dependentVisibleTabSpace);
			//Adjust label width to compensate
			SetUtilityLabelWidth(dependentVisibleTabSpace);
			//Render the property like we would any other
			RenderVisibleProperty(v, labelName, index);
			//Reset the label width
			SetUtilityLabelWidth();

			EditorGUILayout.EndHorizontal();
			//Add extra padding since the horizontal seems to bring them closer
			EditorGUILayout.Space(dependentVisibleVerticalSpace);
		}

		/// <summary>
		/// Render a property which could be one of the custom OpenGraphGUI types.
		/// Otherwise, render the default property view.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="labelName"></param>
		/// <param name="index"></param>
		void RenderVisibleProperty(MaterialProperty v, string labelName, int index)
		{

			if(labelName.StartsWith(singleLineTexPrefix))
			{
				//This is a single line texture type

				//Trim the single line tex prefix
				labelName = labelName.Substring(singleLineTexPrefix.Length);

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(singleLineTexTabSpace);
				matEditor.TexturePropertySingleLine(new GUIContent(labelName), v);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(2f);
			}
			else
			{
				RenderDefaultPropertyView(v, labelName);
			}
		}

		/// <summary>
		/// Render the default editor code for this property.
		/// </summary>
		/// <param name="v"></param>
		void RenderDefaultPropertyView(MaterialProperty v, string customName = "")
		{
			string finalName = (customName == "") ? v.displayName : customName;

			switch(v.type)
			{
				case MaterialProperty.PropType.Texture:
					//Seems that default texture property rendering is more
					//thin than what we get in the ShaderGraph default GUI.
					//This step makes it more wide to match that look.
					var lastFieldWidth = EditorGUIUtility.fieldWidth;
					SetUtilityFieldWidth(texFieldBoxSize);
					matEditor.TextureProperty(v, v.displayName);
					SetUtilityFieldWidth(lastFieldWidth);
					break;
				default:
					matEditor.DefaultShaderProperty(v, finalName);
					break;
			}
			

		}

		/// <summary>
		/// Render a bold label in this space.
		/// </summary>
		/// <param name="propName"></param>
		void RenderLabelProperty(string propName)
		{
			EditorGUILayout.LabelField(propName, EditorStyles.boldLabel);
		}

		//EDITOR GUI

		void SetUtilityLabelWidth(float offset = 0f)
		{
			EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - rightValueBoxSize - offset;
		}

		void SetUtilityFieldWidth(float size)
		{
			EditorGUIUtility.fieldWidth = size;
		}

		//UTILITY

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
