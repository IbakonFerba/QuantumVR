using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FK.JSON;
using UnityEditor;
using FK.Utility.VersionControl;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>Class for managing and setting up the QuantumVR settings in the Editor</para>
    ///
    /// v0.5 10/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class SettingsManager
    {
        // ######################## PROPERTIES ######################## //
        /// <summary>
        /// Loads the settings if they are not loaded yet or creates a new settings object if there is no file to load
        /// </summary>
        public static JSONObject Settings
        {
            get
            {
                if (_settings == null || _settings.IsNull)
                {
                    try
                    {
                        _settings = JSONObject.LoadFromFile($"{Application.streamingAssetsPath}{SettingsLoader.SETTINGS_PATH_IN_STREAMING_ASSETS}");
                    }
                    catch (FileNotFoundException)
                    {
                        _settings = new JSONObject();
                    }
                }

                return _settings;
            }
        }

        public static bool ProjectIsSetUp => Settings.ObjectType != JSONObject.Type.NULL;


        // ######################## PRIVATE VARS ######################## //
        /// <summary>
        /// Backing for Settings
        /// </summary>
        private static JSONObject _settings;

        /// <summary>
        /// name prefix for the QuantumVR unity layers
        /// </summary>
        private const string UNITY_LAYER_NAME = "CompressedSpaceLayer";


        // ######################## FUNCTIONALITY ######################## //
        public static void SetupProject()
        {
            SetUpDefaultSettings(true);
            QuantumVRLogger.EditorLog("Set up Project!");
        }
        
        /// <summary>
        /// Creates the default settings, setting up unity layers and the collision matrix
        /// </summary>
        public static void SetUpDefaultSettings(bool writeToFile)
        {
            Settings.SetField(SettingsLoader.PORTAL_COLLIDER_SIZE_KEY, 0.1f);
            Settings.SetField(SettingsLoader.DO_SCENE_AUTO_SETUP_KEY, true);
            Settings.SetField(SettingsLoader.DEFAULT_SPATIAL_CELL_MAP_LOCATION_KEY, "Assets/QuantumVR Maps");
            Settings.SetField(SettingsLoader.LOG_LEVEL_KEY, 1);

            SetSpatialLayerCount(9, false);
            GenerateUnityLayers(false);
            GenerateRenderQueueRanges(0, 1999, false);
            if (writeToFile)
                WriteSettingsFileToDisk();
            
            QuantumVRLogger.EditorLogVerbose("Set all Settings to Default!");
        }

        /// <summary>
        /// Sets the spatial layer count in the settings file
        /// </summary>
        /// <param name="maxSpatialLayers">Amount of spatial layers that should be rendered. Clamped between 1 and 8</param>
        /// <param name="writeFile">If true, the settings file will be written to the disk</param>
        public static void SetSpatialLayerCount(int maxSpatialLayers = 9, bool writeFile = true)
        {
            Settings.SetField(SettingsLoader.MAX_SPATIAL_LAYERS_KEY, Mathf.Clamp(maxSpatialLayers, 1, 9));

            QuantumVRLogger.EditorLogVerbose("Updated Spatial Layer count");
            if (writeFile)
                WriteSettingsFileToDisk();
        }

        /// <summary>
        /// Generates the render queue ranges for each spatial layer inside the provided range
        /// </summary>
        /// <param name="rangeMin">Minimum queue value to be used by QuantumVR</param>
        /// <param name="rangeMax">Maximum queueu value to be used by QuantumVR</param>
        /// <param name="writeFile">If true, the settings file will be written to the disk</param>
        public static void GenerateRenderQueueRanges(int rangeMin = 0, int rangeMax = 1999, bool writeFile = true)
        {
            JSONObject renderQueueRanges = new JSONObject(JSONObject.Type.ARRAY);

            --rangeMax;
            // the complete range we can use for geometry
            int rangeDelta = rangeMax - (rangeMin + (Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1) * 2);
            int rangePerLayer = Mathf.FloorToInt(rangeDelta / (Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1));

            // set up the queue values for each spatial layer
            for (int i = 0; i < Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1; ++i)
            {
                JSONObject range = new JSONObject(JSONObject.Type.OBJECT);

                // portals are rendered from closest layer to farthest layer, so we use the earliest queue value for the closest layer
                JSONObject portalsRange = new JSONObject(JSONObject.Type.ARRAY);
                portalsRange.Add(rangeMin + i);
                portalsRange.Add(rangeMax - rangePerLayer * (i + 1) + 1);
                portalsRange.Add(rangeMin + (Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1) + (Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 2 - i));
                range[SettingsLoader.PORTAL_RENDER_QUEUE_RANGES_KEY] = portalsRange;

                // objects are rendererd from farthest layer to closest layer, so we use the latest queue value for the closest layer
                JSONObject objectsRange = new JSONObject(JSONObject.Type.ARRAY);
                objectsRange.Add(rangeMax - rangePerLayer * (i + 1) + 2);
                objectsRange.Add(rangeMax - rangePerLayer * i);

                range[SettingsLoader.OBJECTS_RENDER_QUEUE_RANGES_KEY] = objectsRange;
                renderQueueRanges.Add(range);
            }

            Settings.SetField(SettingsLoader.ACTIVE_LAYER_PORTAL_SURFACE_QUEUE_KEY, rangeMax + 1);
            Settings.SetField(SettingsLoader.DEPTH_CLEAR_QUEUE_KEY, rangeMin + (Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - 1) * 2);

            // save to settings
            Settings[SettingsLoader.RENDER_QUEUE_RANGES_KEY] = renderQueueRanges;

            QuantumVRLogger.EditorLogVerbose("Updated Render Queue Ranges");
            if (writeFile)
                WriteSettingsFileToDisk();
        }

        /// <summary>
        /// Generates the needed unity layers and sets up the collision matrix so each of these layers only collides with itself
        /// </summary>
        /// <param name="writeFile">If true, the settings file will be written to the disk</param>
        public static void GenerateUnityLayers(bool writeFile = true)
        {
            // load the Tag manager where layer names are stored
            VersionControlUtils.CheckoutAsset("ProjectSettings/TagManager.asset");
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            SerializedProperty layers = tagManager.FindProperty("layers");

            // intermediate storage for the created layer names
            List<string> layerNames = new List<string>();

            // start from the back so the QuantumVR layers are out of the way of the devs. We don't start at layer 31 because that is used internally by the unity editor
            int layerIndex = 30;

            // now we attempt to create as many layers as there are spatial layers
            for (int i = 1; i < Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue; ++i)
            {
                // first we need to find an unused layer. We check the layer at the current index and if its name is empty or if it already is a QuantumVR layer, we can use it. Else we need to check the next one
                bool noLayersLeft = false;
                string currentLayerName = layers.GetArrayElementAtIndex(layerIndex).stringValue;
                while (currentLayerName != string.Empty && !currentLayerName.StartsWith(UNITY_LAYER_NAME))
                {
                    --layerIndex;

                    if (layerIndex <= 8)
                    {
                        noLayersLeft = true;
                        break;
                    }

                    currentLayerName = layers.GetArrayElementAtIndex(layerIndex).stringValue;
                }

                // if there are no empty layers left, notify the user and set the actual layer count in the settings
                if (noLayersLeft)
                {
                    EditorUtility.DisplayDialog("Not enough empty layers!",
                        $"There are not enough unused layers (need {Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue - i} more)! Please free some or reduce the number of spatial layers in the settings",
                        "Close");

                    QuantumVRLogger.EditorLogWarning($"Reducing number of spatial layers to number of available Unity Layers ({i})");
                    SetSpatialLayerCount(i, false);
                    break;
                }

                // add the layer
                layerNames.Add($"{UNITY_LAYER_NAME}{i}");
                layers.GetArrayElementAtIndex(layerIndex--).stringValue = layerNames[i - 1];
                
                QuantumVRLogger.EditorLogVerbose($"Created Unity Layer \"{layerNames[i-1]}\"");
            }

            // save
            Settings.SetField(SettingsLoader.UNITY_LAYERS_KEY, layerNames.ToArray());
            tagManager.ApplyModifiedProperties();

            SetUpCollisionMatrix();

            QuantumVRLogger.EditorLogVerbose("Updated Unity Layers");
            if (writeFile)
                WriteSettingsFileToDisk();
        }

        private static void SetUpCollisionMatrix()
        {
            // load the dynamics manager where the collision matrix is stored
            VersionControlUtils.CheckoutAsset("ProjectSettings/DynamicsManager.asset");
            SerializedObject dynamicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset"));
            SerializedProperty layerCollisionMatrix = dynamicsManager.FindProperty("m_LayerCollisionMatrix");

            // go through all QuantumVR layers
            JSONObject layers = Settings[SettingsLoader.UNITY_LAYERS_KEY];
            for (int i = 0; i < layers.Count; ++i)
            {
                int layerIndex = LayerMask.NameToLayer(layers[i].StringValue);

                // for each QuantumVR layer we need to make sure that no other layer collides with it.
                // To achieve that, we set the bit of its layermask to 0 for each other layer and the collision mask for itself we set to only collide with itself
                for (int j = 0; j < layerCollisionMatrix.arraySize; ++j)
                {
                    layerCollisionMatrix.GetArrayElementAtIndex(j).longValue = j == layerIndex ? 1 << layerIndex : layerCollisionMatrix.GetArrayElementAtIndex(j).longValue & ~(1 << layerIndex);
                }
            }

            // save
            dynamicsManager.ApplyModifiedProperties();
            
            QuantumVRLogger.EditorLogVerbose("Udpated Collision Matrix");
        }


        // ######################## UTILITIES ######################## //
        public static void WriteSettingsFileToDisk()
        {
            // chekout the asset. If it could not be checked out it might have to be added
            bool add = !VersionControlUtils.CheckoutAsset($"Assets/StreamingAssets/{SettingsLoader.SETTINGS_PATH_IN_STREAMING_ASSETS}");

            Settings.SaveToFile($"{Application.streamingAssetsPath}{SettingsLoader.SETTINGS_PATH_IN_STREAMING_ASSETS}");
            AssetDatabase.Refresh();

            if (add)
                VersionControlUtils.AddToVersionControl($"Assets/StreamingAssets/{SettingsLoader.SETTINGS_PATH_IN_STREAMING_ASSETS}");

            QuantumVRLogger.EditorLog("Saved Settings");
            SettingsLoader.LoadSettings();
        }
    }
}