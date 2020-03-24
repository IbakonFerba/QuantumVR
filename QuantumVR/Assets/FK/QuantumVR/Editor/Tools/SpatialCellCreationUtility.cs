using System;
using UnityEditor;
using UnityEngine;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>Functionality needed for creating a spatial cell in the scene</para>
    ///
    /// v0.9 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class SpatialCellCreationUtility
    {
        // ######################## FUNCTIONALITY ######################## //
        public static void CreateSpatialCell(Guid guid, string name)
        {
            GameObject cellGameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(cellGameObject, "Create Spatial Cell");
            SpatialCell spatialCell = cellGameObject.AddComponent<SpatialCell>();

            SerializedObject spatialCellSerializedObject = new SerializedObject(spatialCell);
            spatialCellSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue = guid.ToString();
            spatialCellSerializedObject.ApplyModifiedProperties();

            CompressedSpaceEntityManager.CheckSpatialCell(cellGameObject);

            Selection.activeGameObject = cellGameObject;
            SceneView.FrameLastActiveSceneView();
            
            QuantumVRLogger.EditorLogVerbose($"Created Spatial Cell \"{name}\"", cellGameObject);
        }

        public static void DeleteSpatialCell(Guid guid)
        {
            SpatialCell cell = CompressedSpaceEntityManager.GetSpatialCell(guid);
            if (cell == null)
            {
                QuantumVRLogger.EditorLogWarning("Could not delete non existent Cell!");
                return;
            }

            QuantumVRLogger.EditorLogVerbose($"Deleting Spatial Cell \"{cell.name}\"...");
            
            CompressedSpaceEntityManager.RemoveSpatialCell(cell);
            Undo.DestroyObjectImmediate(cell.gameObject);
        }

        public static void RenameSpatialCell(Guid guid, string newName)
        {
            SpatialCell cell = CompressedSpaceEntityManager.GetSpatialCell(guid);
            if (cell == null)
            {
                QuantumVRLogger.EditorLogWarning("Could not rename non existent Cell!");
                return;
            }

            QuantumVRLogger.EditorLogVerbose($"Renaming Spatial Cell \"{cell.name}\" to \"{newName}\"...");

            Undo.RecordObject(cell, "Rename Cell");
            cell.gameObject.name = newName;
        }
    }
}