using System.Collections.Generic;
using FK.QuantumVR.Editor.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace FK.QuantumVR.Editor.Windows
{
    /// <summary>
    /// <para>Editor Window for editing QuantumVR settings</para>
    ///
    /// v1.2 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class SettingsEditor : EditorWindow
    {
        // ######################## PRIVATE VARS ######################## //

        private const string TITLE = "QuantumVR Settings";

        #region TOOLBAR

        private ToolbarButton _saveButton;

        #endregion

        #region RENDERING

        private SliderInt _portalNumberSlider;
        private IntegerField _portalNumberValue;

        private MinMaxSlider _renderQueueRangeSlider;
        private IntegerField _renderQueueRangeMinValue;
        private IntegerField _renderQueueRangeMaxValue;

        #endregion


        #region INTERACTION

        private FloatField _portalCollisionSize;

        #endregion

        #region LOGGING

        private PopupField<string> _logLevel;

        #endregion

        #region EDITOR

        private Toggle _automaticSceneSetup;
        private Button _spatialCellMapLocationButton;

        private string _spatialCellMapPath;

        #endregion


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        [MenuItem("Tools/QuantumVR/Settings")]
        public static void ShowWindow()
        {
            SettingsEditor wnd = GetWindow<SettingsEditor>();
            wnd.titleContent = new GUIContent(TITLE);
        }

        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            VisualTreeAsset visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Windows/Resources/SettingsEditor_layout.uxml");
            VisualElement visualTree = visualTreeAsset.CloneTree();
            root.Add(visualTree);

            _logLevel = new PopupField<string>(new List<string>(new string[] {"Verbose", "Logs", "Warnings", "Errors", "Fatal"}), 1);
            root.Q("logLevel").Add(_logLevel);

            // initialize window
            LinkLogic();
            LoadSettings();
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e == null)
                return;

            if (e.type == EventType.KeyUp && e.keyCode == KeyCode.S && e.control)
                SaveSettings();
        }

        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Links the Logic to the Visual Elements
        /// </summary>
        private void LinkLogic()
        {
            // toolbar
            _saveButton = rootVisualElement.Q<ToolbarButton>("save_button");
            _saveButton.clickable.clicked += SaveSettings;
            rootVisualElement.Q<ToolbarButton>("reset_button").clickable.clicked += ResetSettings;

            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.actionKey) SaveSettings();
            });

            // number of poertals
            VisualElement numberOfPortalsRoot = rootVisualElement.Q("number_of_portals");
            _portalNumberSlider = numberOfPortalsRoot.Q<SliderInt>("slider");
            _portalNumberValue = numberOfPortalsRoot.Q<IntegerField>("value_field");

            _portalNumberSlider.RegisterValueChangedCallback(changeEvent => { _portalNumberValue.value = (changeEvent.newValue); });
            _portalNumberValue.RegisterValueChangedCallback(changeEvent =>
            {
                _portalNumberSlider.value = changeEvent.newValue;
                SetDataDirty();
            });

            // render queue range
            VisualElement renderQueueRangeRoot = rootVisualElement.Q("queue_range");
            _renderQueueRangeSlider = renderQueueRangeRoot.Q<MinMaxSlider>("min_max_slider");
            _renderQueueRangeMinValue = renderQueueRangeRoot.Q<IntegerField>("min_value_field");
            _renderQueueRangeMaxValue = renderQueueRangeRoot.Q<IntegerField>("max_value_field");

            _renderQueueRangeSlider.RegisterValueChangedCallback(changeEvent =>
            {
                Vector2Int newValues = new Vector2Int(Mathf.RoundToInt(changeEvent.newValue.x), Mathf.RoundToInt(changeEvent.newValue.y));
                _renderQueueRangeMinValue.SetValueWithoutNotify(newValues.x);
                _renderQueueRangeMaxValue.SetValueWithoutNotify(newValues.y);
                _renderQueueRangeSlider.value = newValues;
                SetDataDirty();
            });
            _renderQueueRangeMinValue.RegisterValueChangedCallback(changedEvent =>
            {
                Vector2Int newValues = new Vector2Int(changedEvent.newValue, (int) _renderQueueRangeSlider.value.y);
                if (newValues.x > newValues.y)
                    newValues.x = newValues.y;
                _renderQueueRangeSlider.value = newValues;
                _renderQueueRangeMinValue.SetValueWithoutNotify(newValues.x);
                SetDataDirty();
            });
            _renderQueueRangeMaxValue.RegisterValueChangedCallback(changedEvent =>
            {
                Vector2Int newValues = new Vector2Int((int) _renderQueueRangeSlider.value.x, changedEvent.newValue);
                if (newValues.x > newValues.y)
                    newValues.x = newValues.y;
                _renderQueueRangeSlider.value = newValues;
                _renderQueueRangeMinValue.SetValueWithoutNotify(newValues.x);
                SetDataDirty();
            });

            // portal collision size
            VisualElement portalColliderRoot = rootVisualElement.Q("portal_collider");
            _portalCollisionSize = portalColliderRoot.Q<FloatField>("value_field");
            _portalCollisionSize.RegisterValueChangedCallback(changeEvent => SetDataDirty());

            _logLevel.RegisterValueChangedCallback(changeEvent => SetDataDirty());

            // automatic scene setup
            _automaticSceneSetup = rootVisualElement.Q<Toggle>("auto_scene_setup_toggle");
            _automaticSceneSetup.RegisterValueChangedCallback(changeEvent => SetDataDirty());

            // spatial cell map location
            VisualElement spatialCellMapLocationRoot = rootVisualElement.Q("default_spatial_cell_map_location");
            _spatialCellMapLocationButton = spatialCellMapLocationRoot.Q<Button>("path_button");
            _spatialCellMapLocationButton.clickable.clicked += () =>
            {
                _spatialCellMapPath = ValidatePath(EditorUtility.OpenFolderPanel("Select a location", Application.dataPath, ""), _spatialCellMapPath);
                _spatialCellMapLocationButton.text = _spatialCellMapPath;
                SetDataDirty();
            };
        }

        private string ValidatePath(string path, string oldValidPath)
        {
            if (!path.StartsWith(Application.dataPath))
            {
                QuantumVRLogger.EditorLogError($"The path {path} does not lead to a directory relative to this Project!");
                return oldValidPath;
            }

            return $"Assets{path.Replace(Application.dataPath, "")}";
        }

        /// <summary>
        /// Restores default settings and writes them to the disk
        /// </summary>
        private void ResetSettings()
        {
            SettingsManager.SetUpDefaultSettings(true);
            LoadSettings();
        }

        /// <summary>
        /// Loads the settings and sets up the UI to display the right values
        /// </summary>
        private void LoadSettings()
        {
            if (!SettingsManager.ProjectIsSetUp)
                SettingsManager.SetupProject();

            _portalNumberValue.SetValueWithoutNotify(SettingsManager.Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1);
            _portalNumberSlider.value = _portalNumberValue.value;

            _renderQueueRangeMinValue.SetValueWithoutNotify(SettingsManager.Settings[SettingsLoader.RENDER_QUEUE_RANGES_KEY][0][SettingsLoader.PORTAL_RENDER_QUEUE_RANGES_KEY][0].IntValue);
            _renderQueueRangeMaxValue.SetValueWithoutNotify(SettingsManager.Settings[SettingsLoader.ACTIVE_LAYER_PORTAL_SURFACE_QUEUE_KEY].IntValue);
            _renderQueueRangeSlider.SetValueWithoutNotify(new Vector2Int(_renderQueueRangeMinValue.value, _renderQueueRangeMaxValue.value));

            _portalCollisionSize.SetValueWithoutNotify(SettingsManager.Settings[SettingsLoader.PORTAL_COLLIDER_SIZE_KEY].FloatValue);

            _logLevel.index = SettingsManager.Settings[SettingsLoader.LOG_LEVEL_KEY].IntValue;

            _automaticSceneSetup.SetValueWithoutNotify(SettingsManager.Settings[SettingsLoader.DO_SCENE_AUTO_SETUP_KEY].BoolValue);
            _spatialCellMapPath = SettingsManager.Settings[SettingsLoader.DEFAULT_SPATIAL_CELL_MAP_LOCATION_KEY].StringValue;
            _spatialCellMapLocationButton.text = _spatialCellMapPath;

            SetDataDirty(false);
        }

        /// <summary>
        /// Writes the new settings to the file and sets up everything accordingly
        /// </summary>
        private void SaveSettings()
        {
            SettingsManager.SetSpatialLayerCount(_portalNumberSlider.value + 1, false);
            SettingsManager.GenerateUnityLayers(false);
            SettingsManager.GenerateRenderQueueRanges(_renderQueueRangeMinValue.value, _renderQueueRangeMaxValue.value, false);
            SettingsManager.Settings[SettingsLoader.PORTAL_COLLIDER_SIZE_KEY].FloatValue = _portalCollisionSize.value;
            SettingsManager.Settings[SettingsLoader.LOG_LEVEL_KEY].IntValue = _logLevel.index;
            SettingsManager.Settings[SettingsLoader.DO_SCENE_AUTO_SETUP_KEY].BoolValue = _automaticSceneSetup.value;
            SettingsManager.Settings[SettingsLoader.DEFAULT_SPATIAL_CELL_MAP_LOCATION_KEY].StringValue = _spatialCellMapPath;
            SettingsManager.WriteSettingsFileToDisk();
            SetDataDirty(false);
        }

        /// <summary>
        /// Makes the window display that there is unsaved data
        /// </summary>
        /// <param name="dirty"></param>
        private void SetDataDirty(bool dirty = true)
        {
            _saveButton.EnableInClassList("save-button-dirty", dirty);
            titleContent.text = dirty ? $"{TITLE}*" : TITLE;

            if (dirty)
                QuantumVRLogger.EditorLogVerbose("Settings have unsaved Changes!");
        }
    }
}