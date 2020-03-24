using System;
using FK.QuantumVR.Objects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>All the functionality needed for creating the actual portals in the scene</para>
    ///
    /// v0.9 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class PortalCreationUtility
    {
        // ######################## DELEGATES ######################## //
        public delegate void OnPortalCreatedDelegate(Guid parentSpatialCellGuid, Guid portalGuid, Portal portal);

        public delegate void OnPortalRemovedDelegate(Guid parentSpatialCellGuid, Guid portalGuid, Portal portal);

        // ######################## CALLBACKS ######################## //
        public static event OnPortalCreatedDelegate OnPortalCreated;
        public static event OnPortalRemovedDelegate OnBeforePortalRemoved;


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Creates a portal along with its reverse portal between the provided cells
        /// </summary>
        /// <param name="spatialCell1Guid"></param>
        /// <param name="spatialCell2Guid"></param>
        /// <param name="portal1To2Guid"></param>
        /// <param name="portal2To1Guid"></param>
        public static void CreatePortal(Guid spatialCell1Guid, Guid spatialCell2Guid, Guid portal1To2Guid, Guid portal2To1Guid)
        {
            QuantumVRLogger.EditorLogVerbose("Creating new two way portal...");
            
            SpatialCell spatialCell1 = CompressedSpaceEntityManager.GetSpatialCell(spatialCell1Guid);
            SpatialCell spatialCell2 = CompressedSpaceEntityManager.GetSpatialCell(spatialCell2Guid);

            SerializedObject portal1To2 = SetUpPortal(portal1To2Guid, spatialCell1, spatialCell1Guid, spatialCell2, false);
            SerializedObject portal2To1 = SetUpPortal(portal2To1Guid, spatialCell2, spatialCell2Guid, spatialCell1, true);

            portal1To2.FindProperty("_reversePortal").objectReferenceValue = portal2To1.targetObject;
            portal2To1.FindProperty("_reversePortal").objectReferenceValue = portal1To2.targetObject;

            portal1To2.ApplyModifiedProperties();
            portal2To1.ApplyModifiedProperties();

            // focus on the new portal
            Selection.activeObject = portal1To2.targetObject;
            SceneView.FrameLastActiveSceneView();
        }

        /// <summary>
        /// Create and set up a single (half) portal
        /// </summary>
        /// <param name="portalGuid"></param>
        /// <param name="parentSpatialCell"></param>
        /// <param name="parentCellGuid"></param>
        /// <param name="targetSpatialCell"></param>
        /// <param name="isReverse"></param>
        /// <returns></returns>
        private static SerializedObject SetUpPortal(Guid portalGuid, SpatialCell parentSpatialCell, Guid parentCellGuid, SpatialCell targetSpatialCell, bool isReverse)
        {
            GameObject portalObject = CreatePortalQuad($"Portal_{parentSpatialCell.name}_to_{targetSpatialCell.name}", null);
            Undo.RegisterCreatedObjectUndo(portalObject, "Create Portal");

            // set up dimensions and positioning of the portal
            Transform portalTransform = portalObject.transform;
            if (isReverse)
                portalTransform.Rotate(Vector3.up, 180);
            portalTransform.SetParent(parentSpatialCell.transform, true);

            // set materials
            SerializedObject portalRendererSerializedObject = new SerializedObject(portalObject.GetComponent<MeshRenderer>());
            SerializedProperty portalMaterials = portalRendererSerializedObject.FindProperty("m_Materials");
            portalMaterials.arraySize = 3;
            portalMaterials.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_PortalSurface.mat");
            portalMaterials.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_PortalStencil.mat");
            portalMaterials.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_PortalView.mat");
            portalRendererSerializedObject.ApplyModifiedProperties();


            Portal portal = portalObject.AddComponent<Portal>();

            // setup portal
            SerializedObject portalSerializedObject = new SerializedObject(portal);
            portalSerializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid").stringValue = parentCellGuid.ToString();
            portalSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue = portalGuid.ToString();

            OnPortalCreated?.Invoke(parentCellGuid, portalGuid, portal);

            QuantumVRLogger.EditorLogVerbose($"Created new Portal {portalObject.name}!", portalObject);
            
            return portalSerializedObject;
        }

        /// <summary>
        /// Deletes the reverse of the requested portal and creates a new one in the new target cell
        /// </summary>
        /// <param name="portalGuid"></param>
        /// <param name="newTargetSpatialCellGuid"></param>
        /// <param name="newReversePortalGuid"></param>
        public static void RetargetPortal(Guid portalGuid, Guid newTargetSpatialCellGuid, Guid newReversePortalGuid)
        {
            SerializedObject portalSerializedObject = FindPortal(portalGuid);

            // focus on the portal
            GameObject portalObject = ((Portal) portalSerializedObject.targetObject).gameObject;
            Selection.activeGameObject = portalObject;
            SceneView.FrameLastActiveSceneView();

            // get the reverse portal and delete it
            SerializedProperty reversePortalProperty = portalSerializedObject.FindProperty("_reversePortal");
            Portal oldReversePortal = reversePortalProperty.objectReferenceValue as Portal;


            SpatialCell parentCell = CompressedSpaceEntityManager.GetSpatialCell(new Guid(portalSerializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid").stringValue));
            SpatialCell targetCell = CompressedSpaceEntityManager.GetSpatialCell(newTargetSpatialCellGuid);


            Undo.RecordObject(portalObject, "Retarget Portal");

            // rename the portal
            portalObject.name = $"Portal_{parentCell.name}_to_{targetCell.name}";

            // create a new portal
            SerializedObject reversePortalSerializedObject = SetUpPortal(newReversePortalGuid, targetCell, newTargetSpatialCellGuid, parentCell, false);

            Undo.RecordObject(portalSerializedObject.targetObject, "Retarget Portal");

            // set up reverse references
            reversePortalProperty.objectReferenceValue = reversePortalSerializedObject.targetObject;
            reversePortalSerializedObject.FindProperty("_reversePortal").objectReferenceValue = portalSerializedObject.targetObject;

            portalSerializedObject.ApplyModifiedProperties();
            reversePortalSerializedObject.ApplyModifiedProperties();

            // delete the old reverse portal last so undo works properly
            if (oldReversePortal != null)
            {
                PostPortalDeleteEvent(oldReversePortal);
                GameObject reversePortalGameObject = oldReversePortal.gameObject;
                Undo.DestroyObjectImmediate(oldReversePortal);
                Undo.DestroyObjectImmediate(reversePortalGameObject);
            }
            
            QuantumVRLogger.EditorLogVerbose($"Retargeted Portal {portalObject.name} to {targetCell.gameObject.name}");
        }

        /// <summary>
        /// Deletes a portal and its reverese portal
        /// </summary>
        /// <param name="portalGuid"></param>
        public static void DeletePortal(Guid portalGuid)
        {
            SerializedObject portalSerializedObject = FindPortal(portalGuid);

            // get the game objects
            Portal portal = (Portal) portalSerializedObject.targetObject;
            GameObject portalObject = portal.gameObject;
            Portal reversePortal = (Portal) portalSerializedObject.FindProperty("_reversePortal").objectReferenceValue;
            GameObject reversePortalObject = reversePortal.gameObject;

            PostPortalDeleteEvent(portal);
            PostPortalDeleteEvent(reversePortal);

            Undo.DestroyObjectImmediate(portal);
            Undo.DestroyObjectImmediate(reversePortal);
            Undo.DestroyObjectImmediate(reversePortalObject);
            Undo.DestroyObjectImmediate(portalObject);

            // because we did not edit serialized objects here, we need to set the scene dirty manually
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            QuantumVRLogger.EditorLogVerbose("Deleted two way portal!");
        }

        // ######################## UTILITIES ######################## //
        /// <summary>
        /// Creates a quad without collider that does not cast and receive shadows and does not interact with probes
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static GameObject CreatePortalQuad(string name, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            if (parent)
                quad.transform.SetParent(parent, false);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

            MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
            SerializedObject meshRendererSerializedObject = new SerializedObject(meshRenderer);
            meshRendererSerializedObject.FindProperty("m_CastShadows").enumValueIndex = 0;
            meshRendererSerializedObject.FindProperty("m_ReceiveShadows").boolValue = false;
            meshRendererSerializedObject.FindProperty("m_LightProbeUsage").intValue = 0;
            meshRendererSerializedObject.FindProperty("m_ReflectionProbeUsage").intValue = 0;
            meshRendererSerializedObject.FindProperty("m_DynamicOccludee").boolValue = false;

            meshRendererSerializedObject.ApplyModifiedProperties();
            return quad;
        }

        /// <summary>
        /// Finds a portal in the open scene and returns the SerializedObject of the Portal Component
        /// </summary>
        /// <param name="portalGuid"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private static SerializedObject FindPortal(Guid portalGuid)
        {
            Portal[] portals = UnityEngine.Object.FindObjectsOfType<Portal>();
            string guidString = portalGuid.ToString();

            // go through all portals
            SerializedObject portalSerializedObject = null;
            for (int i = 0; i < portals.Length; ++i)
            {
                portalSerializedObject = new SerializedObject(portals[i]);
                if (portalSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue == guidString)
                    break;
            }

            // if we do not have a portal at this point, something is wrong
            if (portalSerializedObject == null)
                throw new NullReferenceException("[QuantumVR]: Could not find the specified portal!");

            return portalSerializedObject;
        }

        private static void PostPortalDeleteEvent(Portal portal)
        {
            SerializedObject oldReversePortalSerializedObject = new SerializedObject(portal);
            OnBeforePortalRemoved?.Invoke(new Guid(oldReversePortalSerializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid").stringValue),
                new Guid(oldReversePortalSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue), portal);
        }
    }
}