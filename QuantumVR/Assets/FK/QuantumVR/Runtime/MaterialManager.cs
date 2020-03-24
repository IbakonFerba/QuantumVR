using System;
using System.Collections.Generic;
using System.Linq;
using FK.JSON;
using FK.QuantumVR.Objects;
using UnityEngine;
using UnityEngine.Rendering;
using FK.Utility.MathExtensions;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace FK.QuantumVR
{
    /// <summary>
    /// <para>Manages Materials for compressed space rendering.
    /// Objects and portals can request their materials here and the manager will take care of duplicating them if needed and it will set them up correctly</para>
    ///
    /// v1.5 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class MaterialManager
    {
        // ######################## STRUCTS & CLASSES ######################## //
        /// <summary>
        /// A Collection of materials for a given material asset
        /// </summary>
        private class MaterialCollection
        {
            /// <summary>
            /// The render queue value the material asset originally had
            /// </summary>
            public readonly int OriginalRenderQueue;

            public readonly MaterialData OriginalMaterialData;
            public readonly Dictionary<int, MaterialData> Copies;

            /// <summary>
            /// Copies used by objects going through portals
            /// </summary>
            public readonly List<MaterialData> PortalTraversalCopies;

#if UNITY_EDITOR
            public readonly string OriginalName;
#endif

            public MaterialCollection(Material mat)
            {
                OriginalMaterialData = new MaterialData(mat);
                OriginalRenderQueue = mat.renderQueue;
                Copies = new Dictionary<int, MaterialData>();
                PortalTraversalCopies = new List<MaterialData>();

#if UNITY_EDITOR
                OriginalName = mat.name;
#endif
            }
        }

        private class MaterialData
        {
            public readonly Material Material;

            /// <summary>
            /// Bitmask of the spatial layer of this material. There is only ever 1 bit set at max
            /// </summary>
            public byte SpatialLayerMask;

            /// <summary>
            /// This -1 indicates the set bit of the spatial layer mask. 0 means this is in the base layer and the SpatialLayerMask is 0b00000000
            /// </summary>
            public int SpatialLayerIndex;

            /// <summary>
            /// Is this Material used by any object?
            /// </summary>
            public bool Used;

            /// <summary>
            /// If this material is used for portal traversal, this is the portal it is going through
            /// </summary>
            public CompressedSpaceEntity AssociatedEntity;

            public MaterialData(Material mat)
            {
                Material = mat;
                AssociatedEntity = null;
            }
        }


        // ######################## ENUMS & DELEGATES ######################## //
        private enum MaterialType
        {
            OBJECT,
            PORTAL_STENCIL,
            PORTAL_SURFACE,
            PORTAL_VIEW,
        }


        // ######################## PUBLIC VARS ######################## //
        public static readonly int PortalSurfaceActiveLayerQueue;

        /// <summary>
        /// Portal view queue values by spatial layer - 1 (active layer is handled seperately)
        /// </summary>
        public static readonly int[] PortalViewQueue;

        #region SHADER_PROPERTIES

        public static readonly int ShaderMaskingBitProperty = Shader.PropertyToID("_MaskingBit");
        public static readonly int ShaderWorldToPortalMatrixProperty = Shader.PropertyToID("_WorldToPortal");
        public static readonly int ShaderWorldToPortalTraversingMatrixProperty = Shader.PropertyToID("_WorldToPortalTraversing");
        public static readonly int ShaderWriteBitProperty = Shader.PropertyToID("_WriteBit");
        public static readonly int ShaderDoPortalClippingProperty = Shader.PropertyToID("_DoPortalClipping");

        #endregion


        // ######################## PRIVATE VARS ######################## //
        private static readonly Dictionary<Material, MaterialCollection> _materials = new Dictionary<Material, MaterialCollection>();

        #region SETTINGS

        private static readonly int _spatial_layer_count;

        /// <summary>
        /// Lower bounds of the object queues by spatial layer-1 (layer 0 is excluded because it gets the original queue value)
        /// </summary>
        private static readonly int[] _object_queue_lower_bounds;

        /// <summary>
        /// Upper bounds of the object queues by spatial layer-1 (layer 0 is excluded because it gets the original queue value)
        /// </summary>
        private static readonly int[] _object_queue_upper_bounds;

        /// <summary>
        /// Portal stencil queue values by spatial layer
        /// </summary>
        private static readonly int[] _portal_stencil_queue;

        /// <summary>
        /// Portal surface queue values by spatial layer - 1 (active layer is handled seperately)
        /// </summary>
        private static readonly int[] _portal_surface_queue;

        private static readonly int _render_queue_lower_bound;
        private static readonly int _render_queue_upper_bound;

        #endregion


        // ######################## INITS ######################## //
        static MaterialManager()
        {
            // get all the values we need from the settings and save them in a format that allows faster access
            JSONObject settings = SettingsLoader.Settings;
            _spatial_layer_count = settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue;
            _object_queue_lower_bounds = new int[_spatial_layer_count - 1];
            _object_queue_upper_bounds = new int[_spatial_layer_count - 1];
            _portal_stencil_queue = new int[_spatial_layer_count - 1];
            _portal_surface_queue = new int[_spatial_layer_count - 1];
            PortalViewQueue = new int[_spatial_layer_count - 1];
            for (int i = 0; i < _spatial_layer_count - 1; ++i)
            {
                JSONObject queueRange = settings[SettingsLoader.RENDER_QUEUE_RANGES_KEY][i];
                _object_queue_lower_bounds[i] = queueRange[SettingsLoader.OBJECTS_RENDER_QUEUE_RANGES_KEY][0].IntValue;
                _object_queue_upper_bounds[i] = queueRange[SettingsLoader.OBJECTS_RENDER_QUEUE_RANGES_KEY][1].IntValue;
                _portal_stencil_queue[i] = queueRange[SettingsLoader.PORTAL_RENDER_QUEUE_RANGES_KEY][0].IntValue;
                _portal_surface_queue[i] = queueRange[SettingsLoader.PORTAL_RENDER_QUEUE_RANGES_KEY][1].IntValue;
                PortalViewQueue[i] = queueRange[SettingsLoader.PORTAL_RENDER_QUEUE_RANGES_KEY][2].IntValue;
            }

            PortalSurfaceActiveLayerQueue = settings[SettingsLoader.ACTIVE_LAYER_PORTAL_SURFACE_QUEUE_KEY].IntValue;
            _render_queue_lower_bound = RenderQueueRange.all.lowerBound;
            _render_queue_upper_bound = RenderQueueRange.all.upperBound;

            QuantumVRLogger.LogVerbose("Initialized Material Manager");
#if UNITY_EDITOR
            // in the editor we need to reset a bunch of things when we exit play mode, so we need to listen to that event
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
#endif
        }


        // ######################## FUNCTIONALITY ######################## //
        public static void MarkAllMaterialsUnused()
        {
            foreach (MaterialCollection matCollection in _materials.Select(pair => pair.Value))
            {
                matCollection.OriginalMaterialData.Used = false;

                foreach (KeyValuePair<int, MaterialData> copyPair in matCollection.Copies)
                {
                    copyPair.Value.Used = false;
                }

                foreach (MaterialData materialData in matCollection.PortalTraversalCopies)
                {
                    materialData.Used = false;
                }
            }
        }

        public static void DisposeUnusedMaterials()
        {
#if UNITY_EDITOR
            QuantumVRLogger.LogVerbose("Disposing of unused Materials");
#endif

            List<Material> materialsToRemove = new List<Material>();
            foreach (MaterialCollection matData in _materials.Select(pair => pair.Value))
            {
                // if the original material is unused, we can delete all copies without checking them and remove it from our database
                if (!matData.OriginalMaterialData.Used && matData.PortalTraversalCopies.Count <= 0)
                {
                    ResetMaterialAsset(matData);
                    materialsToRemove.Add(matData.OriginalMaterialData.Material);
                    foreach (KeyValuePair<int, MaterialData> copyPair in matData.Copies)
                    {
                        UnityEngine.Object.Destroy(copyPair.Value.Material);
                    }

                    foreach (MaterialData materialData in matData.PortalTraversalCopies)
                    {
                        UnityEngine.Object.Destroy(materialData.Material);
                    }
                }
                else // if the original material is in use we need to check each submaterial seperately
                {
                    List<int> removedCopies = new List<int>();
                    foreach (KeyValuePair<int, MaterialData> copyPair in matData.Copies.Where(copyPair => !copyPair.Value.Used))
                    {
                        UnityEngine.Object.Destroy(copyPair.Value.Material);
                        removedCopies.Add(copyPair.Key);
                    }

                    for (int i = matData.PortalTraversalCopies.Count - 1; i >= 0; --i)
                    {
                        MaterialData copyData = matData.PortalTraversalCopies[i];
                        if (copyData.Used)
                            continue;

                        UnityEngine.Object.Destroy(copyData.Material);
                        matData.PortalTraversalCopies.RemoveAt(i);
                    }

                    for (int i = 0; i < removedCopies.Count; ++i)
                    {
                        matData.Copies.Remove(removedCopies[i]);
                    }
                }
            }

            for (int i = 0; i < materialsToRemove.Count; ++i)
            {
                _materials.Remove(materialsToRemove[i]);
            }
        }

        /// <summary>
        /// Destroys the materials that where set up for portal traversal for the provided entity
        /// </summary>
        /// <param name="compressedSpaceEntity"></param>
        /// <param name="originalMaterials"></param>
        public static void DisposePortalTraversalMaterials(CompressedSpaceEntity compressedSpaceEntity, IEnumerable<Material> originalMaterials)
        {
#if UNITY_EDITOR
            QuantumVRLogger.LogVerbose($"Disposing of Portal traversal Materials for {compressedSpaceEntity.name}", compressedSpaceEntity);
#endif
            foreach (Material material in originalMaterials)
            {
                // ignore materials the manager does not know about
                if (!_materials.ContainsKey(material))
                    continue;

                MaterialCollection materialCollection = _materials[material];
                for (int i = materialCollection.PortalTraversalCopies.Count - 1; i >= 0; --i)
                {
                    MaterialData materialData = materialCollection.PortalTraversalCopies[i];

                    // only delete for this entity
                    if (materialData.AssociatedEntity != compressedSpaceEntity)
                        continue;

                    UnityEngine.Object.Destroy(materialData.Material);
                    materialCollection.PortalTraversalCopies.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Sets the materials for a Compressed Space object. This automatically duplicates materials if needed and sets up all needed material properties
        /// </summary>
        /// <param name="renderer">The renderer of the object. renderer.sharedMaterials will be overwritten</param>
        /// <param name="compressedSpaceEntity">The entity the renderer belongs to</param>
        /// <param name="spatialLayerMask">The spatial layer mask of the object. If more than 1 bit is set the object will be rendered multiple times</param>
        /// <param name="isInActiveCell">Should be true if the object is on layer 0</param>
        /// <param name="originalMaterials">The Material Assets that where assigned to the object at startup</param>
        /// <param name="isGoingThroughPortal">If true, portal rendering will be performed for this object</param>
        /// <param name="belongsToActiveCell">Only of interest when going through a portal. If true the object belongs to the Active cell and is going through a portal from there</param>
        /// <param name="traversingPortal">The portal the object is going through</param>
        public static void SetCompressedSpaceObjectMaterials(Renderer renderer, CompressedSpaceEntity compressedSpaceEntity, byte spatialLayerMask, bool isInActiveCell,
            IEnumerable<Material> originalMaterials,
            bool isGoingThroughPortal, bool belongsToActiveCell, Portal traversingPortal)
        {
            // we will store the materials for this renderer in this list
            List<Material> rendererMaterials = new List<Material>();

            // for every original material we need to retrieve one ore more materials for the respective spatial layers the object is on
            foreach (Material material in originalMaterials)
            {
                if (!_materials.ContainsKey(material))
                    AddMaterial(material);

                MaterialCollection matCollection = _materials[material];

                // go through all spatial layers
                for (int i = 0; i < _spatial_layer_count; ++i)
                {
                    // layer 0 has to be checked extra because no bit is set in the layermask of that
                    if (i == 0 && isInActiveCell)
                    {
                        rendererMaterials.Add(GetOrCreateMaterialFromCollection(matCollection, i, MaterialType.OBJECT, isGoingThroughPortal, belongsToActiveCell,
                            compressedSpaceEntity, traversingPortal));
                    }
                    else if (((spatialLayerMask >> (i - 1)) & 1) == 1) // when checking the bitmask we need to substract 1 from the layer index because layer 1 is bit 0
                    {
                        rendererMaterials.Add(GetOrCreateMaterialFromCollection(matCollection, i, MaterialType.OBJECT, isGoingThroughPortal, belongsToActiveCell,
                            compressedSpaceEntity, traversingPortal));

                        // if the bitmask of the next layer is a bigger value than the spatial layer mask of the object we are done with this material
                        if ((1 << i) > spatialLayerMask)
                            break;
                    }
                }
            }

            // set the materials we collected
            renderer.sharedMaterials = rendererMaterials.ToArray();
        }

        /// <summary>
        /// Sets the materials for a Portal. This automatically duplicates materials if needed and sets up all needed material properties
        /// </summary>
        /// <param name="renderer">Renderer of the portal that is calling</param>
        /// <param name="portalGuid">Guid of the portal that is calling</param>
        /// <param name="spatialLayerMask">The spatial layer mask of the object. If more than 1 bit is set the object will be rendered multiple times</param>
        /// <param name="isInActiveCell">Should be true if the object is on layer 0</param>
        /// <param name="originalStencilMaterial">Stencil Material Asset the portal had at startup</param>
        /// <param name="originalSurfaceMaterial">Portal Surface Material Asset the portal had at startup</param>
        public static void SetPortalMaterials(Renderer renderer, Guid portalGuid, byte spatialLayerMask, bool isInActiveCell, Material originalStencilMaterial, Material originalSurfaceMaterial,
            Material originalViewMaterial)
        {
            SpatialCellManager spatialCellManager = SpatialCellManager.Instance;

            // we will store the materials for this renderer in this list
            List<Material> rendererMaterials = new List<Material>();

            if (!_materials.ContainsKey(originalStencilMaterial))
                AddMaterial(originalStencilMaterial);

            if (!_materials.ContainsKey(originalSurfaceMaterial))
                AddMaterial(originalSurfaceMaterial);

            if (!_materials.ContainsKey(originalViewMaterial))
                AddMaterial(originalViewMaterial);

            MaterialCollection stencilMaterialCollection = _materials[originalStencilMaterial];
            MaterialCollection surfaceMaterialCollection = _materials[originalSurfaceMaterial];
            MaterialCollection viewMaterialCollection = _materials[originalViewMaterial];

            // go through all spatial layers
            for (int i = 0; i < _spatial_layer_count; ++i)
            {
                // a portal is disabled on a layer if the layer is rendererd through its counterpart leading into the layer. A disabled portal should not be rendered, so we don't set a material here
                if (spatialCellManager.PortalDisabledOnLayer(portalGuid, i))
                    continue;

                int targetLayerIndex = spatialCellManager.GetPortalTargetLayerIndex(portalGuid, i);

                // layer 0 has to be checked extra because no bit is set in the layermask of that
                if (i == 0 && isInActiveCell)
                {
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(stencilMaterialCollection, i, MaterialType.PORTAL_STENCIL, false, false, null, null, targetLayerIndex));
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(surfaceMaterialCollection, i, MaterialType.PORTAL_SURFACE, false, false, null, null));
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(viewMaterialCollection, i, MaterialType.PORTAL_VIEW, false, false, null, null, targetLayerIndex));
                }
                else if (((spatialLayerMask >> (i - 1)) & 1) == 1) // when checking the bitmask we need to substract 1 from the layer index because layer 1 is bit 0
                {
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(stencilMaterialCollection, i, MaterialType.PORTAL_STENCIL, false, false, null, null, targetLayerIndex));
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(surfaceMaterialCollection, i, MaterialType.PORTAL_SURFACE, false, false, null, null));
                    rendererMaterials.Add(GetOrCreateMaterialFromCollection(viewMaterialCollection, i, MaterialType.PORTAL_VIEW, false, false, null, null, targetLayerIndex));

                    // if the bitmask of the next layer is a bigger value than the spatial layer mask of the object we are done with this material
                    if ((1 << i) > spatialLayerMask)
                        break;
                }
            }

            // if there were no materials found, disable rendering for the portal
            if (rendererMaterials.Count == 0)
                renderer.enabled = false;
            else if (!renderer.enabled)
                renderer.enabled = true;

            // set the materials we collected
            renderer.sharedMaterials = rendererMaterials.ToArray();
        }

        /// <summary>
        /// Returns a correctly set up material for the given spatial layer
        /// </summary>
        /// <param name="materialCollection">Material Collection to look for the material</param>
        /// <param name="spatialLayerIndex">The index of the spatial layer a material is needed for (0-8)</param>
        /// <param name="materialType">Type of the material. Portal materials are handled differently</param>
        /// <param name="isGoingThroughPortal">If true, portal rendering will be performed for this object</param>
        /// <param name="belongsToActiveCell">Only of interest when going through a portal. If true the object belongs to the Active cell and is going through a portal from there</param>
        /// <param name="compressedSpaceEntity">Only of interes when going through a portal. The entity the renderer belongs to</param>
        /// <param name="traversingPortal">The portal the object is going through</param>
        /// <param name="portalTargetLayerIndex">Only needed for the PORTAL_STENCIL material type</param>
        /// <returns>A correctly set up material for the given spatial layer</returns>
        private static Material GetOrCreateMaterialFromCollection(MaterialCollection materialCollection, int spatialLayerIndex, MaterialType materialType, bool isGoingThroughPortal,
            bool belongsToActiveCell, CompressedSpaceEntity compressedSpaceEntity,
            Portal traversingPortal, int portalTargetLayerIndex = 0)
        {
            // handle objetcs going through portals seperately
            if (isGoingThroughPortal)
                return CreatePortalTraversingMaterialCopy(materialCollection, spatialLayerIndex, belongsToActiveCell, compressedSpaceEntity, traversingPortal);

            // if the original material is unused, it needs to be set up and can be returned
            if (!materialCollection.OriginalMaterialData.Used)
            {
                SetUpMaterial(materialCollection.OriginalMaterialData, spatialLayerIndex, materialCollection.OriginalRenderQueue, materialType, portalTargetLayerIndex
#if UNITY_EDITOR
                    , materialCollection.OriginalName
#endif
                );
                return materialCollection.OriginalMaterialData.Material;
            }

            // if the original material is used for the same spatial layer we are looking for, we can reuse it
            if (materialCollection.OriginalMaterialData.SpatialLayerIndex == spatialLayerIndex)
            {
                return materialCollection.OriginalMaterialData.Material;
            }

            // if we reach this it means the original material can't be used because it is used on another layer. Check if there is an existing copy for this layer and return it if there is one
            if (materialCollection.Copies.ContainsKey(spatialLayerIndex))
            {
                MaterialData matData = materialCollection.Copies[spatialLayerIndex];
                if (spatialLayerIndex > 0)
                    SetupPortalClipping(spatialLayerIndex, matData.Material);
                matData.Used = true;
                return matData.Material;
            }

            // if this is reached there is no suitable copy of the material yet. Copy the original material and set it up for the requested layer
            MaterialData copiedMaterial = new MaterialData(new Material(materialCollection.OriginalMaterialData.Material));
            materialCollection.Copies.Add(spatialLayerIndex, copiedMaterial);
            SetUpMaterial(copiedMaterial, spatialLayerIndex, materialCollection.OriginalRenderQueue, materialType, portalTargetLayerIndex
#if UNITY_EDITOR
                , materialCollection.OriginalName
#endif
            );
            return copiedMaterial.Material;
        }

        /// <summary>
        /// Creates a copy of the material that owns the provided collections set up for portal traversal
        /// </summary>
        /// <param name="materialCollection">The collection to copy from</param>
        /// <param name="spatialLayerIndex">The index of the spatial layer a material is needed for (0-8)</param>
        /// <param name="belongsToActiveCell">If true the object belongs to the Active cell and is going through a portal from there</param>
        /// <param name="compressedSpaceEntity">The entity the renderer belongs to</param>
        /// <param name="traversingPortal">The portal the object is going through</param>
        /// <returns></returns>
        private static Material CreatePortalTraversingMaterialCopy(MaterialCollection materialCollection, int spatialLayerIndex, bool belongsToActiveCell, CompressedSpaceEntity compressedSpaceEntity,
            Portal traversingPortal)
        {
            MaterialData copy = new MaterialData(new Material(materialCollection.OriginalMaterialData.Material));
            materialCollection.PortalTraversalCopies.Add(copy);

            SetUpMaterial(copy, spatialLayerIndex, materialCollection.OriginalRenderQueue, MaterialType.OBJECT, 0
#if UNITY_EDITOR
                , materialCollection.OriginalName
#endif
            );

#if UNITY_EDITOR
            copy.Material.name += "_portalTraversalCopy";
#endif

            // set up traversing portal clipping
            copy.Material.SetMatrix(ShaderWorldToPortalTraversingMatrixProperty, traversingPortal.WorldToLocalMatrix);
            bool belongsToLayer = spatialLayerIndex == 0
                ? belongsToActiveCell
                : (SpatialCellManager.Instance.GetCellSpatialLayermask(traversingPortal.SpatialCell.Guid) & (1 << (spatialLayerIndex - 1))) != 0;

            copy.Material.SetInt(ShaderDoPortalClippingProperty, belongsToLayer ? 1 : 2);

            copy.AssociatedEntity = compressedSpaceEntity;

#if UNITY_EDITOR
            QuantumVRLogger.LogVerbose($"Created new Portal traversal Material for {compressedSpaceEntity.name}", compressedSpaceEntity);
#endif
            return copy.Material;
        }

        /// <summary>
        /// Sets up Queue and Material values for a material on the provided spatial layer
        /// </summary>
        /// <param name="materialData">The Material to work on</param>
        /// <param name="spatialLayerIndex">The index of the spatial layer the material should be rendered on (0-8)</param>
        /// <param name="originalRenderQueue">Render queue of the material asset this material is derived from at game startup</param>
        /// <param name="materialType">Type of the material. Portal materials are handled differently</param>
        /// <param name="portalTargetLayerIndex">Only needed for the PORTAL_STENCIL material type</param>
        private static void SetUpMaterial(MaterialData materialData, int spatialLayerIndex, int originalRenderQueue, MaterialType materialType, int portalTargetLayerIndex
#if UNITY_EDITOR
            , string originalMaterialName
#endif
        )
        {
            // setup the data
            materialData.SpatialLayerIndex = spatialLayerIndex;
            materialData.SpatialLayerMask = spatialLayerIndex == 0 ? (byte) 0 : (byte) (1 << (spatialLayerIndex - 1));
            materialData.Used = true;

            Material mat = materialData.Material;

            // layer 0 is handled differently because it is not viewed through a portal
            if (spatialLayerIndex == 0)
            {
                if (materialType == MaterialType.OBJECT)
                    mat.renderQueue = originalRenderQueue; // an object on the base layer should render at the regular time
                else if (materialType == MaterialType.PORTAL_SURFACE)
                    mat.renderQueue = PortalSurfaceActiveLayerQueue; // a portal surface on the base layer needs to render just before the regular time
            }
            else
            {
                if (materialType == MaterialType.OBJECT)
                    mat.renderQueue = originalRenderQueue.Remap(_render_queue_lower_bound, _render_queue_upper_bound,
                        _object_queue_lower_bounds[spatialLayerIndex - 1], _object_queue_upper_bounds[spatialLayerIndex - 1]); // remap the queue of the material to the range of the layer it is on
                else if (materialType == MaterialType.PORTAL_SURFACE)
                    mat.renderQueue = _portal_surface_queue[spatialLayerIndex - 1];

                SetupPortalClipping(spatialLayerIndex, mat);
            }

            // set the stencil read mask
            mat.SetInt(ShaderMaskingBitProperty, materialData.SpatialLayerMask);

            // if the material is a portal stencil material, we need to do some more work on it
            if (materialType == MaterialType.PORTAL_STENCIL || materialType == MaterialType.PORTAL_VIEW)
                SetUpPortalStencilMaterial(mat, portalTargetLayerIndex, spatialLayerIndex, materialType);

#if UNITY_EDITOR
            mat.name = materialType == MaterialType.PORTAL_STENCIL
                ? $"{originalMaterialName}_SpatialLayer{spatialLayerIndex}_ToLayer{portalTargetLayerIndex}"
                : $"{originalMaterialName}_SpatialLayer{spatialLayerIndex}";
#endif
        }

        private static void SetupPortalClipping(int spatialLayerIndex, Material material)
        {
            // set the portal clipping matrix
            Portal portal = SpatialCellManager.Instance.GetPortalLeadingToSpatialLayer(spatialLayerIndex);
            material.SetMatrix(ShaderWorldToPortalMatrixProperty, portal.WorldToLocalMatrix);
        }

        private static void SetUpPortalStencilMaterial(Material material, int targetSpatialLayerIndex, int spatialLayerIndex, MaterialType materialType)
        {
            // if the layer this is on is higher than the second to last layer in existens or if its target layer is smaller than 1 (which would not make sense),
            // make the portal not write into the stencil buffer. If it does not write it needs no queue, so we are done.
            if (spatialLayerIndex > _spatial_layer_count - 2 || targetSpatialLayerIndex < 1)
            {
                material.SetInt(ShaderWriteBitProperty, 0);
                return;
            }

            // setup stencil write mask and queue
            byte stencilWriteMask = (byte) (1 << (targetSpatialLayerIndex - 1)); // target layer is never 0
            material.SetInt(ShaderWriteBitProperty, stencilWriteMask);
            material.renderQueue = materialType == MaterialType.PORTAL_STENCIL ? _portal_stencil_queue[spatialLayerIndex] : PortalViewQueue[spatialLayerIndex];
        }

        #region UNITY_EDITOR

#if UNITY_EDITOR
        /// <summary>
        /// Called when the Entering or Exiting Play and Edit Mode and when Pausing.
        /// </summary>
        /// <param name="state"></param>
        private static void OnPlayModeStateChange(PlayModeStateChange state)
        {
            // if the event was not playmode exit, do nothing
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;

            // reset material asset queues and disable stencil masking, as the editor won't clean this up automatically
            foreach (KeyValuePair<Material, MaterialCollection> pair in _materials)
            {
                ResetMaterialAsset(pair.Value);
            }

            // delete unused Materials
            MarkAllMaterialsUnused();
            DisposeUnusedMaterials();

            QuantumVRLogger.EditorLogVerbose("Cleaned up temporary Materials");
        }
#endif

        #endregion


        private static void ResetMaterialAsset(MaterialCollection collection)
        {
            collection.OriginalMaterialData.Material.renderQueue = collection.OriginalRenderQueue;
            collection.OriginalMaterialData.Material.SetInt(ShaderMaskingBitProperty, 0);
#if UNITY_EDITOR
            collection.OriginalMaterialData.Material.name = collection.OriginalName;
#endif
        }

        // ######################## UTILITIES ######################## //
        private static void AddMaterial(Material mat)
        {
            MaterialCollection matCollection = new MaterialCollection(mat);
            _materials.Add(mat, matCollection);

#if UNITY_EDITOR
            QuantumVRLogger.LogVerbose($"Added {mat.name} to managed Materials", mat);
#endif
        }

        public static Material[] GetMaterialInstances(Material mat)
        {
            if (!_materials.ContainsKey(mat))
            {
                QuantumVRLogger.LogWarning($"Material {mat} is not managed by the Material Manager!", mat);
                return new Material[0];
            }

            MaterialCollection materialCollection = _materials[mat];
            Material[] instances = new Material[materialCollection.Copies.Count + materialCollection.PortalTraversalCopies.Count + 1];
            instances[0] = mat;
            for (int i = 0; i < materialCollection.Copies.Count; ++i)
            {
                instances[i + 1] = materialCollection.Copies.ElementAt(i).Value.Material;
            }

            for (int i = 0; i < materialCollection.PortalTraversalCopies.Count; ++i)
            {
                instances[materialCollection.Copies.Count + i + 1] = materialCollection.PortalTraversalCopies[i].Material;
            }

            return instances;
        }
    }
}