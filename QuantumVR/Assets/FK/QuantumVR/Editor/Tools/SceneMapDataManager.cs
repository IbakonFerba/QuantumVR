using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>Manages getting the map data used in the open scene</para>
    ///
    /// v0.9 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [InitializeOnLoad]
    public static class SceneMapDataManager
    {
        // ######################## PROPERTIES ######################## //
        public static SpatialCellMap SpatialCellMap => _spatialCellMap;


        // ######################## PUBLIC VARS ######################## //
        public static event System.Action<SpatialCellMap> OnSceneMapChanged;


        // ######################## PRIVATE VARS ######################## //
        private static SerializedObject _spatialCellManager;
        private static SerializedProperty _spatialCellMapProperty;

        private static SpatialCellMap _spatialCellMap;


        // ######################## INITS ######################## //
        static SceneMapDataManager()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.update += Update;

            FindSpatialCellManager();
        }

        private static void FindSpatialCellManager()
        {
            // dont load anything if in play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += FindSpatialCellManager;

                return;
            }
            
            SpatialCellManager spatialCellManager = Object.FindObjectOfType<SpatialCellManager>();
            if (spatialCellManager == null)
            {
                _spatialCellManager = null;
                return;
            }

            _spatialCellManager = new SerializedObject(spatialCellManager);
            _spatialCellMapProperty = _spatialCellManager.FindProperty("_spatialCellMapData");

            QuantumVRLogger.EditorLogVerbose("Found Spatial Cell Manager in the open Scene!", spatialCellManager);
            Update();
        }


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            FindSpatialCellManager();
        }

        private static void Update()
        {
            if (_spatialCellManager == null || _spatialCellManager.targetObject == null)
                FindSpatialCellManager();

            // if we still don't have a spatial cell manager at this point, there is none in the scene. If we still have a scene map, we need to set it to null
            if (_spatialCellManager == null)
            {
                if (_spatialCellMap != null)
                {
                    OnSceneMapChanged?.Invoke(null);
                    _spatialCellMap = null;
                }

                return;
            }

            _spatialCellManager.Update();

            // if the map in the manager is still the same as the one we are looking it nothing changed, else we need to update the map we are looking at
            SpatialCellMap newMap = _spatialCellMapProperty.objectReferenceValue as SpatialCellMap;
            if (newMap == _spatialCellMap)
                return;

            OnSceneMapChanged?.Invoke(newMap);
            _spatialCellMap = newMap;

            if (newMap != null)
                QuantumVRLogger.EditorLogVerbose("Found a new Spatial Cell Map in the open Scene!", newMap);
            else
                QuantumVRLogger.EditorLogWarning("Open Scene does not have a Spatial Cell Map anymore!");
        }
    }
}