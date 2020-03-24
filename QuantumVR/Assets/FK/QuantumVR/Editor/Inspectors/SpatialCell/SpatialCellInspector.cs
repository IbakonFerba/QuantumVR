using FK.QuantumVR.Editor.Windows;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor
{
    /// <summary>
    /// <para>Custom Inspector for spatial cells</para>
    ///
    /// v1.0 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [CustomEditor(typeof(SpatialCell))]
    public class SpatialCellInspector : UnityEditor.Editor
    {
        // ######################## PRIVATE VARS ######################## //
        private TextElement _guidDisplay;


        // ######################## UNITY EVENT FUNCTIONS ######################## //

        #region UNITY EVENT FUNCTIONS

        public override VisualElement CreateInspectorGUI()
        {
            // load uxml
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("SpatialCellInspector_layout");
            TemplateContainer visualTree = visualTreeAsset.CloneTree();

            // load styles
            visualTree.styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
            visualTree.styleSheets.Add(Resources.Load<StyleSheet>("QuantumVRInspectorsCommon_styles"));

            // get guid display
            _guidDisplay = visualTree.Q<TextElement>("guidDisplay");

            // register button event
            Button openMapEditor = visualTree.Q<Button>("openMapEditor");
            openMapEditor.clickable.clicked += SpatialCellMapEditor.OpenEditor;

            OnValidate();

            return visualTree;
        }

        public void OnValidate()
        {
            serializedObject.Update();

            if (_guidDisplay == null)
                return;
            _guidDisplay.text = serializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue;
        }

        #endregion
    }
}