using System;
using FK.Utility;
using FK.Utility.Binary;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using FK.Utility.VersionControl;
using UnityEditor;

#endif

namespace FK.QuantumVR.Objects
{
    /// <summary>
    /// <para>Base for all types of objects that use QuantumVR compressed Space</para>
    ///
    /// v1.6 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public abstract class CompressedSpaceEntity : MonoBehaviour
    {
        // ######################## PROPERTIES ######################## //
        public SpatialCell SpatialCell => _spatialCell;
        public bool IsInsidePortal => _insidePortal;

        /// <summary>
        /// If the Entity is currently going through a portal, this is the portal it is going through
        /// </summary>
        public Portal TraversingPortal => _traversingPortal;


        // ######################## EXPOSED VARS ######################## //
        [SerializeField] protected SerializableGuid _spatialCellGuid;


        // ######################## PUBLIC VARS ######################## //
        /// <summary>
        /// If true, the entity cannot go throuhg portals. Portals can't go through portals regardless
        /// </summary>
        public bool Static;

        /// <summary>
        /// Is this object part of the player?
        /// </summary>
        public bool IsPlayerPart;


        // ######################## PROTECTED VARS ######################## //
        protected SpatialCellManager SpatialCellManagerInstance;
        protected int OriginalUnityLayer;


        // ######################## PRIVATE VARS ######################## //
        private SpatialCell _spatialCell;

        /// <summary>
        /// backing for TraversingPortal
        /// </summary>
        private Portal _traversingPortal;

        /// <summary>
        /// backing for IsInsidePortal
        /// </summary>
        private bool _insidePortal;


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        protected virtual void Awake()
        {
            Init();
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            if (!(this is CompressedSpaceObject))
                return;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;

            for (int i = 0; i < meshRenderer.sharedMaterials.Length; ++i)
            {
                Material sharedMaterial = meshRenderer.sharedMaterials[i];
                if (sharedMaterial == null)
                    continue;


                Shader quantumVrStandardShader = Shader.Find("QuantumVR/Standard");
                if (sharedMaterial.shader == quantumVrStandardShader || sharedMaterial.shader == Shader.Find("QuantumVR/Unlit") || sharedMaterial.shader == Shader.Find("QuantumVR/Transparent"))
                    continue;

                if ((sharedMaterial.hideFlags & HideFlags.NotEditable) != 0)
                {
                    QuantumVRLogger.EditorLogWarning(
                        $"Material {sharedMaterial.name} of Compressed Space Object {name} cannot be converted to a QuantumVR material because it is not editable, you should change it to another Material!",
                        this);
                    continue;
                }

                if (!EditorUtility.DisplayDialog("Possibly Incompatible Material detected",
                    $"The Material {sharedMaterial.name} does not use a QuantumVR shader and thus can lead to rendering glitches on Compressed Space Object {name}. Should the Material be converted to use a QuantumVR Shader?",
                    "Yes", "No"))
                    continue;

                VersionControlUtils.CheckoutAsset(AssetDatabase.GetAssetPath(sharedMaterial));
                sharedMaterial.shader = quantumVrStandardShader;
            }
        }
#endif


        // ######################## INITS ######################## //
        ///<summary>
        /// Does the Init for this Behaviour
        ///</summary>
        private void Init()
        {
            OriginalUnityLayer = gameObject.layer;
            SpatialCellManagerInstance = SpatialCellManager.Instance;
            if (IsPlayerPart)
            {
                _spatialCellGuid = SpatialCellManagerInstance.StartCell;
                QuantumVRLogger.Log($"Set Spatial Cell of {this.name} to the start cell specified in the Spatial Cell Map because it is a player Part", this);
            }

            SetSpatialCell(_spatialCellGuid.Value, false, false, false);
        }


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Called by the parent spatial cell when its spatialLayermask updates
        /// </summary>
        /// <param name="spatialLayermask"></param>
        /// <param name="isInActiveCell"></param>
        private void OnOnUpdateLayermaskEvent(byte spatialLayermask, bool isInActiveCell)
        {
            SetUnityLayer(spatialLayermask, isInActiveCell);
            OnLayerMaskChanged(spatialLayermask, isInActiveCell);
        }

        /// <summary>
        /// Called when the spatialLayermask of object updated
        /// </summary>
        /// <param name="spatialLayermask"></param>
        /// <param name="isInActiveCell"></param>
        protected abstract void OnLayerMaskChanged(byte spatialLayermask, bool isInActiveCell);

        private void SetUnityLayer(byte layermask, bool isActiveCell)
        {
            if (isActiveCell)
            {
                gameObject.layer = OriginalUnityLayer;
                return;
            }

            int layerIndex = layermask.GetLeastSignificantBitPosition();
            if (layerIndex > -1)
            {
                gameObject.SetActive(true);
                gameObject.layer = SpatialCellManagerInstance.UnityLayersBySpatialLayer[layerIndex];
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the spatial cell this entity belongs to as well as the ones of deep children if desired
        /// </summary>
        /// <param name="newSpatialCellGuid">Guid of the new spatial cell</param>
        /// <param name="isRecursiveChangeRoot">Is this the root object for a recursive change that also changes deep children? Only if this is true the object will be reparented</param>
        /// <param name="setChildren">If true, all children and deep children will also change their spatial cell</param>
        /// <param name="doUpdate">Should the object be updated after the change?</param>
        internal void SetSpatialCell(Guid newSpatialCellGuid, bool isRecursiveChangeRoot, bool setChildren, bool doUpdate = true)
        {
            // if we already have a spatial cell, unsubscribe from the update event
            if (_spatialCell)
                _spatialCell.OnUpdateLayermaskEvent -= OnOnUpdateLayermaskEvent;

            // set new spatial cell and update object
            _spatialCellGuid = newSpatialCellGuid;
            _spatialCell = SpatialCellManagerInstance.GetCellObject(newSpatialCellGuid);
            _spatialCell.OnUpdateLayermaskEvent += OnOnUpdateLayermaskEvent;

            // only reparent if this is the root of the change
            if (isRecursiveChangeRoot && !IsPlayerPart)
                transform.parent = _spatialCell.transform;

            // update all children if desired
            if (setChildren)
            {
                foreach (Transform child in transform)
                {
                    child.GetComponent<CompressedSpaceEntity>()?.SetSpatialCell(newSpatialCellGuid, false, true, doUpdate);
                }
            }

            if (doUpdate)
                OnOnUpdateLayermaskEvent(SpatialCellManagerInstance.GetCellSpatialLayermask(newSpatialCellGuid), SpatialCellManagerInstance.IsActiveCell(newSpatialCellGuid));

            QuantumVRLogger.LogVerbose($"{this.name} Changed to spatial cell {newSpatialCellGuid}", this);
        }

        /// <summary>
        /// Sets the spatial cell this entity belongs to as well as the ones of deep children if desired
        /// </summary>
        /// <param name="newSpatialCellGuid">Guid of the new spatial cell</param>
        /// <param name="setChildren">If true, all children and deep children will also change their spatial cell</param>
        /// <param name="doUpdate">Should the object be updated after the change?</param>
        public void ChangeSpatialCell(Guid newSpatialCellGuid, bool setChildren, bool doUpdate = true)
        {
            if (this is Portal)
            {
                QuantumVRLogger.LogError("Portals cannot be moved to another Spatial Cell at runtime!", this);
                return;
            }

            SetSpatialCell(newSpatialCellGuid, true, setChildren, doUpdate);
        }

        /// <summary>
        /// Enables any special behavior needed when going through a portal
        /// </summary>
        /// <param name="portal"></param>
        /// <param name="targetCell"></param>
        /// <param name="doUpdate"></param>
        public virtual void EnterPortal(Portal portal, Guid targetCell, bool doUpdate = true)
        {
            if (_traversingPortal != null)
                return;

            _insidePortal = true;
            _traversingPortal = portal;

            StartCoroutine(CheckParenting());

            QuantumVRLogger.Log($"{this.name} entered Portal {portal.name}", this);
        }

        /// <summary>
        /// Disables any special behavior needed when going through a portal
        /// </summary>
        /// <param name="portal"></param>
        public virtual void ExitPortal(Portal portal)
        {
            if (_traversingPortal != portal)
                return;

            _insidePortal = false;
            _traversingPortal = null;

            QuantumVRLogger.Log($"{this.name} exited Portal {portal.name}", this);
        }


        // ######################## COROUTINES ######################## //
        /// <summary>
        /// Checks if any deep parent changed and updates portal tracking accordingly
        /// </summary>
        private IEnumerator CheckParenting()
        {
            List<Transform> originalParents = new List<Transform>();
            GetParents(transform, originalParents);
            while (IsInsidePortal)
            {
                if (ParentChanged(transform, originalParents, 0))
                {
                    _traversingPortal.EnterPortal(this);
                    break;
                }

                yield return null;
            }
        }


        // ######################## UTILITIES ######################## //
        public bool IsInActiveCell()
        {
            return SpatialCellManagerInstance.IsActiveCell(_spatialCellGuid.Value);
        }

        /// <summary>
        /// Checks if any deep parent changed
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="originalParents"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        private static bool ParentChanged(Transform trans, List<Transform> originalParents, int counter)
        {
            Transform parent = trans.parent;

            if (!parent && counter >= originalParents.Count)
                return false;

            if (parent && counter >= originalParents.Count)
                return true;

            if (parent != originalParents[counter])
                return true;

            return ParentChanged(trans.parent, originalParents, ++counter);
        }

        private static void GetParents(Transform trans, List<Transform> originalParents)
        {
            if (!trans.parent)
                return;

            originalParents.Add(trans.parent);
            GetParents(trans.parent, originalParents);
        }
    }
}