using System;
using System.Collections.Generic;
using FK.QuantumVR.Objects;
using FK.Utility.VersionControl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>A static editor class that manages setting up the correct relations between spatial cells and compressed space entities</para>
    ///
    /// v1.6 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [InitializeOnLoad]
    public static class CompressedSpaceEntityManager
    {
        // ######################## PRIVATE VARS ######################## //
        private static List<SpatialCell> _openSceneSpatialCells;

        // ######################## INITS ######################## //
        static CompressedSpaceEntityManager()
        {
            EditorApplication.playModeStateChanged += OnPlaymodeStatChange;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Init();
        }

        private static void Init()
        {
            _openSceneSpatialCells = new List<SpatialCell>();

            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            // TODO: can be removed when hierarchy changed event works properly
            EditorSceneManager.sceneSaving += ValidateObjects;


            Undo.undoRedoPerformed += FindSpatialCells;
            FindSpatialCells();
        }

        private static void FindSpatialCells()
        {
            _openSceneSpatialCells.Clear();
            _openSceneSpatialCells.AddRange(Object.FindObjectsOfType<SpatialCell>());
            
            QuantumVRLogger.EditorLogVerbose($"Found {_openSceneSpatialCells.Count} Spatial Cells in open Scene!");
        }


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        private static void OnPlaymodeStatChange(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
                Init();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            FindSpatialCells();
        }

        private static void OnHierarchyChanged()
        {
            // go through all selected objects, these are the ones that could have changed
            for (int i = 0; i < Selection.gameObjects.Length; ++i)
            {
                GameObject obj = Selection.gameObjects[i];

                CheckObject(obj);
            }
        }


        // ######################## FUNCTIONALITY ######################## //
        private static void CheckObject(GameObject obj)
        {
            foreach (Transform child in obj.transform)
            {
                CheckObject(child.gameObject);
            }
            
            // if the object is a spatial cell we don't need to do the following checks
            if (CheckSpatialCell(obj))
                return;

            // check if the object is a child of a spatial cell now, if yes we need to update it
            for (int j = 0; j < _openSceneSpatialCells.Count; ++j)
            {
                SpatialCell spatialCell = _openSceneSpatialCells[j];
                if (!obj.transform.IsChildOf(spatialCell.transform))
                    continue;

                ValidateCompressedSpaceEntity(obj, spatialCell);
                return;
            }

            // if we reach this, the object is not child of a spatial cell. If it has a compressed space object as parent however, we should add one anyways, it could be a player part
            CompressedSpaceEntity parent = obj.GetComponentInParent<CompressedSpaceEntity>();
            if(parent)
                ValidateCompressedSpaceEntity(obj, null);
        }
        /// <summary>
        /// Tests if the object is a spatial cell. If yes and it is not tracked yet, it will be added to the list of tracked spatial cells. If the cell is a child of another cell, that parenting is broken because it is invalid
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool CheckSpatialCell(GameObject obj)
        {
            SpatialCell spatialCell = obj.GetComponent<SpatialCell>();
            if (!spatialCell)
                return false;

            if (!_openSceneSpatialCells.Contains(spatialCell))
            {
                _openSceneSpatialCells.Add(spatialCell);
                
                QuantumVRLogger.EditorLogVerbose($"Found new Spatial Cell \"{spatialCell.gameObject.name}\"", spatialCell);
            }

            ValidateCellParenting(obj);

            return true;
        }

        private static void MakeCompressedSpaceEntity(GameObject obj, SpatialCell parentCell)
        {
            CompressedSpaceEntity compressedSpaceEntity = null;
            if (obj.GetComponent<Renderer>() || obj.GetComponent<Collider>())
            {
                compressedSpaceEntity = obj.AddComponent<CompressedSpaceObject>();
                
                QuantumVRLogger.EditorLogVerbose($"Made {obj.gameObject.name} a Compressed Space Object", compressedSpaceEntity);
            }
            else if (obj.GetComponent<Light>())
            {
                compressedSpaceEntity = obj.AddComponent<CompressedSpaceLight>();
                
                QuantumVRLogger.EditorLogVerbose($"Made {obj.gameObject.name} a Compressed Space Light", compressedSpaceEntity);
            }

            if (compressedSpaceEntity == null)
                return;

            UpdateCompressedSpaceEntity(compressedSpaceEntity, parentCell);
        }

        private static void UpdateCompressedSpaceEntity(CompressedSpaceEntity entity, SpatialCell parentCell)
        {
            SerializedObject parentCellSerializedObject = parentCell ? new SerializedObject(parentCell) : null;
            SerializedObject entitySerializedObject = new SerializedObject(entity);

            SerializedProperty cellGuid = parentCellSerializedObject?.FindProperty("_guid").FindPropertyRelative("_serializedGuid") ?? null;
            SerializedProperty entityCellGuid = entitySerializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid");

            bool dataChanged = false;
            if (!cellGuid?.stringValue.Equals(entityCellGuid.stringValue) ?? false)
            {
                entityCellGuid.stringValue = cellGuid.stringValue;

                dataChanged = true;
                QuantumVRLogger.EditorLogVerbose($"Updated Spatial Cell of {entity.gameObject.name}", entity);
            }

            SerializedProperty isStatic = entitySerializedObject.FindProperty("Static");
            if (!isStatic.boolValue && entity.gameObject.isStatic)
            {
                isStatic.boolValue = entity.gameObject.isStatic;
                
                dataChanged = true;
                QuantumVRLogger.EditorLogVerbose($"Made {entity.gameObject.name} QuantumVR static", entity);
            }

            if (dataChanged)
                entitySerializedObject.ApplyModifiedProperties();
        }

        #region VALIDATION

        /// <summary>
        /// Makes sure a spatial cell is not a child of another spatial cell
        /// </summary>
        /// <param name="obj"></param>
        private static void ValidateCellParenting(GameObject obj)
        {
            if (!obj.transform.parent)
                return;

            SpatialCell parentCell = obj.transform.parent.GetComponentInParent<SpatialCell>();
            if (parentCell)
            {
                obj.transform.parent = parentCell.transform.parent;
                
                QuantumVRLogger.EditorLogWarning("Spatial Cells are not allowed to be children of other Spatial Cells! QuantumVR reparented the Game Object to fix that issue", obj);
            }
        }

        /// <summary>
        /// If the object has a component that needs to interact with compressed space and it is not yet a compressed space entity, it will be made one. Its cell guid is updated to the correct one as well
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="spatialCell"></param>
        private static void ValidateCompressedSpaceEntity(GameObject obj, SpatialCell spatialCell)
        {
            CompressedSpaceEntity compressedSpaceEntity = obj.GetComponent<CompressedSpaceEntity>();
            if (!compressedSpaceEntity)
                MakeCompressedSpaceEntity(obj, spatialCell);
            else
                UpdateCompressedSpaceEntity(compressedSpaceEntity, spatialCell);
        }

        // TODO: can be removed when hierarchy changed event works properly
        /// <summary>
        /// Makes sure all Compressed Space Entities and Cells are set up correctly when the scene is saved
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="path"></param>
        private static void ValidateObjects(Scene scene, string path)
        {
            for (int i = 0; i < _openSceneSpatialCells.Count; ++i)
            {
                SpatialCell cell = _openSceneSpatialCells[i];
                ValidateCellParenting(cell.gameObject);
                ValidateChildren(cell.transform, cell);
            }
        }

        private static void ValidateChildren(Transform parent, SpatialCell parentSpatialCell)
        {
            foreach (Transform child in parent.transform)
            {
                ValidateCompressedSpaceEntity(child.gameObject, parentSpatialCell);
                ValidateChildren(child, parentSpatialCell);
            }
        }

        #endregion

        // ######################## GETTER ######################## //
        public static SpatialCell GetSpatialCell(System.Guid guid)
        {
            for (int i = 0; i < _openSceneSpatialCells.Count; ++i)
            {
                SerializedObject cellSerializedObject = new SerializedObject(_openSceneSpatialCells[i]);

                SerializedProperty cellGuidProperty = cellSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid");
                System.Guid cellGuid = System.Guid.Parse(cellGuidProperty.stringValue);
                if (cellGuid == guid)
                    return _openSceneSpatialCells[i];
            }

            QuantumVRLogger.EditorLogError($"There is no Spatial Cell with the Guid {guid.ToString()}");
            return null;
        }

        public static void RemoveSpatialCell(SpatialCell cell)
        {
            _openSceneSpatialCells.Remove(cell);
        }
    }
}