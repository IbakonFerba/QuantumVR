using FK.QuantumVR.Editor.Windows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor
{
	/// <summary>
	/// <para>Custom Inspector for SpatialCellManager</para>
	///
	/// v1.0 12/2019
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	[CustomEditor(typeof(SpatialCellManager))]
	public class SpatialCellManagerInspector : UnityEditor.Editor
	{
		// ######################## UNITY EVENT FUNCTIONS ######################## //
		public override VisualElement CreateInspectorGUI()
		{
			// load uxml
			VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("SpatialCellManagerInspector_layout");
			TemplateContainer visualTree = visualTreeAsset.CloneTree();

			// load styles
			visualTree.styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
			visualTree.styleSheets.Add(Resources.Load<StyleSheet>("QuantumVRInspectorsCommon_styles"));

			visualTree.Bind(serializedObject);

			// register button event
			Button openMapEditor = visualTree.Q<Button>("openMapEditor");
			openMapEditor.clickable.clicked += SpatialCellMapEditor.OpenEditor;

			return visualTree;
		}
	}
}