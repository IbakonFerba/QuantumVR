using FK.QuantumVR.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor
{
	/// <summary>
	/// <para>Custom Inspector for Portals</para>
	///
	/// v1.1 12/2019
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Portal))]
	public class PortalInspector : CompressedSpaceEntityInspector
	{
		// ######################## PRIVATE VARS ######################## //
		private TextElement _guidDisplay;
		private Button _selectReversePortalButton;
		
		// ######################## UNITY EVENT FUNCTIONS ######################## //
		#region UNITY EVENT FUNCTIONS

		public override VisualElement CreateInspectorGUI()
		{
			VisualElement visualTree = base.CreateInspectorGUI();
			VisualElement root = visualTree.Q("root");
			
			// load uxml
			VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("PortalInspector_layout");
			visualTreeAsset.CloneTree(root);
			
			// disable not needed fields
			root.Q<PropertyField>("staticField").RemoveFromHierarchy();
			root.Q<PropertyField>("isPlayerPartField").RemoveFromHierarchy();
			
			// set up guid display
			_guidDisplay = visualTree.Q<TextElement>("portalGuidDisplay");
			

			// setup select reverse portal Button display
			_selectReversePortalButton = root.Q<Button>("selectReversePortalButton");
			_selectReversePortalButton.clickable.clicked += OnSelectReversePortalButton;

			return visualTree;
		}
		
		#endregion


		// ######################## FUNCTIONALITY ######################## //

		#region FUNCTIONALITY
		private void OnSelectReversePortalButton()
		{
			serializedObject.Update();
			
			Selection.activeObject = serializedObject.FindProperty("_reversePortal").objectReferenceValue;
			SceneView.FrameLastActiveSceneView();
		}

		protected override void Udpate()
		{
			base.Udpate();

			if (_selectReversePortalButton != null)
			{
				if (serializedObject.targetObjects.Length > 1 && _selectReversePortalButton.visible)
				{
					_selectReversePortalButton.visible = false;
				}
				else if (serializedObject.targetObjects.Length == 1 && !_selectReversePortalButton.visible)
				{
					_selectReversePortalButton.visible = true;
				}
			}

			if (_guidDisplay != null)
			{

				SerializedProperty guid = serializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid");
				_guidDisplay.text = !guid.hasMultipleDifferentValues ? guid.stringValue : "-";
			}
		}

		#endregion
	}
}
