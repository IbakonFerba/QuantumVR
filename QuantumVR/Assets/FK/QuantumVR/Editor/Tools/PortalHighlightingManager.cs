using System;
using System.Collections.Generic;
using FK.QuantumVR.Objects;
using FK.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>Manages setting highlight materials on portals</para>
    ///
    /// v0.9 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [InitializeOnLoad]
    public static class PortalHighlightingManager
    {
        // ######################## PRIVATE VARS ######################## //
        private static Dictionary<Guid, GameObject> _openScenePortals;

        private static Texture _ditherTexture;

        private static readonly Shader _highlight_shader = Shader.Find("Unlit/Highlight");
        private static readonly int _dither_texture_property = Shader.PropertyToID("_DitherTexture");
        private static readonly int _color = Shader.PropertyToID("_Color");


        // ######################## INITS ######################## //
        static PortalHighlightingManager()
        {
            EditorApplication.playModeStateChanged += OnPlaymodeStatChange;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Init();
        }

        private static void Init()
        {
            _ditherTexture = AssetDatabase.LoadAssetAtPath<Texture>("Assets/FK/QuantumVR/Textures/HighlightDither.png");

            _openScenePortals = new Dictionary<Guid, GameObject>();

            EditorSceneManager.sceneOpened += OnSceneOpened;
            PortalCreationUtility.OnPortalCreated += AddPortal;
            PortalCreationUtility.OnBeforePortalRemoved += RemovePortal;

            Undo.undoRedoPerformed += FindPortals;

            FindPortals();
            ClearHighlight();
        }

        private static void FindPortals()
        {
            _openScenePortals.Clear();

            Portal[] portals = UnityEngine.Object.FindObjectsOfType<Portal>();
            for (int i = 0; i < portals.Length; ++i)
            {
                SerializedObject portalSerializedObject = new SerializedObject(portals[i]);
                AddPortal(new Guid(portalSerializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid").stringValue),
                    new Guid(portalSerializedObject.FindProperty("_guid").FindPropertyRelative("_serializedGuid").stringValue), portals[i]);
            }
            
            QuantumVRLogger.EditorLogVerbose($"Found {portals.Length/2} two-way Portals in open Scene!");
        }

        // ######################## UNITY EVENT FUNCTIONS ######################## //
        private static void OnPlaymodeStatChange(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    Init();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    ClearHighlight();
                    break;
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            FindPortals();
            ClearHighlight();
        }

        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Adds a Highlight material in the requested color to the provided portal
        /// </summary>
        /// <param name="portal"></param>
        /// <param name="targetCellColor"></param>
        public static void HighlightPortal(Guid portal, Color targetCellColor)
        {
            if (!_openScenePortals.ContainsKey(portal))
                return;

            SerializedProperty materials = GetPortalMaterialsProperty(_openScenePortals[portal]);
            materials.arraySize += 1;
            materials.GetArrayElementAtIndex(materials.arraySize - 1).objectReferenceValue = GetHighlightMaterial(targetCellColor);
            materials.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Highlights all portals from a cell
        /// </summary>
        /// <param name="spatialCellGuid"></param>
        public static void HighlightCellPortals(Guid spatialCellGuid)
        {
            SpatialCellMap.NodeData node = SceneMapDataManager.SpatialCellMap.Nodes[spatialCellGuid];
            QuantumVRLogger.EditorLogVerbose($"Highlighting Portals of Spatial Cell \"{node.Name}\"...");
            List<SerializableGuid> portals = node.Connections;
            for (int i = 0; i < portals.Count; ++i)
            {
                HighlightPortal(portals[i].Value, SceneMapDataManager.SpatialCellMap.Nodes[SceneMapDataManager.SpatialCellMap.Connections[portals[i]].TargetCellGuid].Color);
            }
        }

        /// <summary>
        /// Removes all highlight materials from portals in the scene
        /// </summary>
        public static void ClearHighlight()
        {
            QuantumVRLogger.EditorLogVerbose("Clearing Portal Highlighting...");
            foreach (KeyValuePair<Guid, GameObject> portal in _openScenePortals)
            {
                SerializedProperty materials = GetPortalMaterialsProperty(portal.Value);
                for (int j = 0; j < materials.arraySize; ++j)
                {
                    // check for the highlight material
                    Material material = (Material) materials.GetArrayElementAtIndex(j).objectReferenceValue;
                    if (!material?.shader.name.Equals("Unlit/Highlight") ?? false)
                        continue;

                    // remove and delete the material. The array element has to be removed two times, the first time only removes the value, the second time removes the actual element
                    UnityEngine.Object.DestroyImmediate(material);
                    materials.DeleteArrayElementAtIndex(j);
                    materials.DeleteArrayElementAtIndex(j);
                    break;
                }

                materials.serializedObject.ApplyModifiedProperties();
            }
        }


        // ######################## UTILITIES ######################## //
        private static void AddPortal(Guid parentCellGuid, Guid portalGuid, Portal portal)
        {
            if (_openScenePortals.ContainsKey(portalGuid))
            {
                _openScenePortals[portalGuid] = portal.gameObject;
                return;
            }

            _openScenePortals.Add(portalGuid, portal.gameObject);
        }

        private static void RemovePortal(Guid parentCellGuid, Guid portalGuid, Portal portal)
        {
            if (!_openScenePortals.ContainsKey(portalGuid))
                return;

            _openScenePortals.Remove(portalGuid);
        }

        /// <summary>
        /// Returns the materials serialized property of the Mesh Renderer of the Portal
        /// </summary>
        /// <param name="portal"></param>
        /// <returns></returns>
        private static SerializedProperty GetPortalMaterialsProperty(GameObject portal)
        {
            MeshRenderer portalRenderer = portal.GetComponent<MeshRenderer>();
            SerializedObject portalRendererSerializedObject = new SerializedObject(portalRenderer);
            return portalRendererSerializedObject.FindProperty("m_Materials");
        }

        /// <summary>
        /// Creates a new Highlight Material
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static Material GetHighlightMaterial(Color color)
        {
            Material material = new Material(_highlight_shader);
            material.SetTexture(_dither_texture_property, _ditherTexture);
            material.SetColor(_color, color);
            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }
    }
}