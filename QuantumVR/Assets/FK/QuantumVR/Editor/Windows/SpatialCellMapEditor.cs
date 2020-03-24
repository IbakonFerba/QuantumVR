using System;
using FK.QuantumVR.Editor.Tools;
using FK.QuantumVR.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


namespace FK.QuantumVR.Editor.Windows
{
    /// <summary>
    /// <para>An editor window for editing spatial cell maps in a node editor</para>
    ///
    /// v0.93 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class SpatialCellMapEditor : EditorWindow
    {
        // ######################## PRIVATE VARS ######################## //

        #region PRIVATE VARS

        #region CONSTANTS

        private const string SELECT_START_CELL_BUTTON_TEXT_IDLE = "Select Start Cell";
        private const string SELECT_START_CELL_BUTTON_TEXT_SELECTING = "Exit Start Cell Selection";

        #endregion

        private GraphView _graphView;

        private ToolbarToggle _selectStartCellButton;

        #endregion


        // ######################## INITS ######################## //
        private void InitInteraction()
        {
            ToolbarButton sceneSetupButton = rootVisualElement.Q<ToolbarButton>("sceneSetupButton");
            sceneSetupButton.clickable.clicked += SceneSetupUtility.SetupScene;

            ToolbarButton createSpatialCellButton = rootVisualElement.Q<ToolbarButton>("createSpatialCellButton");
            createSpatialCellButton.clickable.clicked += CreateSpatialCell;

            _selectStartCellButton = rootVisualElement.Q<ToolbarToggle>("selectStartCellButton");
            _selectStartCellButton.text = SELECT_START_CELL_BUTTON_TEXT_IDLE;
            _selectStartCellButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    EnterStartCellSelection();
                else
                    _graphView.ExitStartNodeSelectionMode();
            });


            Toolbar toolbar = rootVisualElement.Q<Toolbar>();
            toolbar.focusable = true;
            toolbar.RegisterCallback<KeyDownEvent>(OnKeyDown);

            _graphView.OnStartNodeSelectionModeExit += OnExitStartCellSelection;
        }


        // ######################## UNITY EVENT FUNCTIONS ######################## //

        #region UNITY EVENT FUNCTIONS

        [MenuItem("Tools/QuantumVR/Spatial Cell Map Editor")]
        public static void OpenEditor()
        {
            SpatialCellMapEditor wnd = GetWindow<SpatialCellMapEditor>();
        }

        public void OnEnable()
        {
            // Import UXML
            VisualTreeAsset visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Windows/Resources/SpatialCellMapEditor_layout.uxml");
            visualTreeAsset.CloneTree(rootVisualElement);

            // load styles
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("SpatialCellMapEditor_styles"));

            _graphView = rootVisualElement.Q<GraphView>();

            InitInteraction();

            // register callbacks
            SceneMapDataManager.OnSceneMapChanged += OnSceneMapChanged;

            // make sure the scene is set up for QuantumVR and open the scene map
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                SceneSetupUtility.CheckScene();
            OpenSceneMap();
        }

        private void OnDisable()
        {
            // remove callbacks
            SceneMapDataManager.OnSceneMapChanged -= OnSceneMapChanged;
        }

        #endregion


        // ######################## FUNCTIONALITY ######################## //

        #region FUNCTIONALITY

        #region EVENT HANDLING

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && _graphView.StartNodeSelectionModeActive)
                _graphView.ExitStartNodeSelectionMode();
        }

        private void OnSceneMapChanged(SpatialCellMap spatialCellMap)
        {
            // if the new map is null and there is still data loaded, unload it, else load the map
            if (spatialCellMap == null && _graphView.HasData)
                _graphView.UnloadGraph();
            else
                LoadMap(spatialCellMap);
        }

        private void OnExitStartCellSelection()
        {
            _selectStartCellButton.text = SELECT_START_CELL_BUTTON_TEXT_IDLE;
            _selectStartCellButton.value = false;
        }

        #endregion

        private void LoadMap(SpatialCellMap spatialCellMap)
        {
            _graphView.LoadGraph(spatialCellMap);
            titleContent.text = spatialCellMap.name;
        }

        private void OpenSceneMap()
        {
            // dont load anything if in play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += OpenSceneMap;

                return;
            }

            // if there is no scene map, do nothing
            if (!SceneMapDataManager.SpatialCellMap)
            {
                QuantumVRLogger.EditorLog("There is no Spatial Cell Map to load in this scene!");
                return;
            }

            LoadMap(SceneMapDataManager.SpatialCellMap);
        }

        private void CreateSpatialCell()
        {
            // do nothing if no data is loaded
            if (!_graphView.HasData)
            {
                QuantumVRLogger.EditorLogWarning("Cannot create a Spatial Cell with not Spatial Cell Map Loaded! Make sure the open scene is set up properly for QuantumVR!");
                return;
            }

            _graphView.AddNode(_graphView.Viewport.LocalToWorld(_graphView.Viewport.layout.center));
        }

        private void EnterStartCellSelection()
        {
            // do nothing if no data is loaded
            if (!_graphView.HasData)
            {
                QuantumVRLogger.EditorLogWarning("Cannot select a Start Cell with not Spatial Cell Map Loaded! Make sure the open scene is set up properly for QuantumVR!");
                return;
            }

            _graphView.EnterStartNodeSelectionMode();
            _selectStartCellButton.text = SELECT_START_CELL_BUTTON_TEXT_SELECTING;
        }

        #endregion
    }
}