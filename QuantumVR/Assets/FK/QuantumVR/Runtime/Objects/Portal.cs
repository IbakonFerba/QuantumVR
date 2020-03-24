using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FK.Utility;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace FK.QuantumVR.Objects
{
    /// <summary>
    /// <para>Controls a QuantumVR portal that objects can pass through</para>
    ///
    /// v0.9 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    #if UNITY_EDITOR
    [ExecuteInEditMode]
    #endif
    [AddComponentMenu("")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class Portal : CompressedSpaceEntity
    {
        // ######################## PROPERTIES ######################## //
        public System.Guid Guid => _guid.Value;

        /// <summary>
        /// The renderers world to local matrix that can be used for shaders
        /// </summary>
        public Matrix4x4 WorldToLocalMatrix => _renderer.worldToLocalMatrix;


        // ######################## EXPOSED VARS ######################## //
        [SerializeField] private SerializableGuid _guid;

        /// <summary>
        /// The portal that is the reverse of this one (it leads from the target cell into this cell and is at the exact place of this portal)
        /// </summary>
        [SerializeField] private Portal _reversePortal;


        // ######################## PRIVATE VARS ######################## //
        private Renderer _renderer;
        private Material _orignalStencilMaterial;
        private Material _originalSurfaceMaterial;
        private Material _originalViewMaterial;
        private SerializableGuid _targetCellGuid;


        /// <summary>
        /// A oriented bounding box that encapsulates the portal and is used to determine whether something is going through or not
        /// </summary>
        private OBB _obb;

        /// <summary>
        /// The camera that is currently going through the portal. Is null if non is close enough
        /// </summary>
        private QuantumVRCamera _trackedCamera;

        private QuantumVRCamera _cameraInReach;

        /// <summary>
        /// All objects that are currently going through the portal
        /// </summary>
        private readonly List<CompressedSpaceEntity> _trackedObjects = new List<CompressedSpaceEntity>();

        /// <summary>
        /// All tracked colliders
        /// </summary>
        private readonly List<Collider> _containedTrackedObjectColliders = new List<Collider>();

        /// <summary>
        /// The last frame positions of all objects currently going through the portal
        /// </summary>
        private readonly List<Vector3> _trackedObjectPrevRelativePositions = new List<Vector3>();


        // ######################## INITS ######################## //
        ///<summary>
        /// Does the Init for this Behaviour
        ///</summary>
        private void Init()
        {
            _targetCellGuid = _reversePortal._spatialCellGuid;
            _renderer = GetComponent<Renderer>();

            // find the portals materials
            foreach (Material material in _renderer.sharedMaterials)
            {
                if (material.shader.name.Equals("Hidden/QuantumVR/PortalStencil"))
                    _orignalStencilMaterial = new Material(material);
                else if (material.shader.name.Equals("Hidden/QuantumVR/PortalSurface"))
                    _originalSurfaceMaterial = new Material(material);
                else if (material.shader.name.Equals("Hidden/QuantumVR/PortalView"))
                    _originalViewMaterial = new Material(material);

                if (_originalSurfaceMaterial && _orignalStencilMaterial && _originalViewMaterial)
                    break;
            }

            SpatialCellManagerInstance.RegisterPortal(this);

            // calculate the obb using the collider of the portal
            Vector3 colliderSize = InitCollider();
            Vector3 scale = transform.lossyScale;
            _obb = new OBB(transform.position, new Vector3(scale.x * colliderSize.x, scale.y * colliderSize.y, Camera.main.nearClipPlane + 0.1f * 2), transform.rotation);
        }

        /// <summary>
        /// Sets up the collider of the portal and returns its size
        /// </summary>
        /// <returns></returns>
        private Vector3 InitCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            Vector3 size = new Vector3(1, 1, SettingsLoader.Settings[SettingsLoader.PORTAL_COLLIDER_SIZE_KEY].FloatValue);
            col.size = size;
            col.center = new Vector3(0, 0, -size.z * 0.5f);

            return size;
        }


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        protected override void Awake()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            base.Awake();
            Init();
        }

        #region UNITY_EDITOR

#if UNITY_EDITOR
        protected override void Reset()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            InitCollider();
        }
#endif

        #endregion

        private void Update()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                UpdateReversePortal();
                return;
            }
#endif
            // we don't need to do anything if there are no objects that are tracked
            if (_trackedObjects.Count == 0)
                return;

            // handle all objects currently going through the portal
            for (int i = 0; i < _trackedObjects.Count; ++i)
            {
                CompressedSpaceEntity trackedObject = _trackedObjects[i];
                Transform trackedObjectTransform = trackedObject.transform;

                // get position relative to the portal
                Vector3 currentRelativePos = transform.InverseTransformPoint(trackedObjectTransform.position);

                // if the object is on the side of the portal where the cell is it leads to and it wasn't before, the camera went through the portal
                if (!Single.IsNaN(_trackedObjectPrevRelativePositions[i].z) && _trackedObjectPrevRelativePositions[i].z <= 0.0f && currentRelativePos.z > 0.0f &&
                    _obb.Contains(trackedObjectTransform.position))
                {
                    // immediately enter the reverse portal, otherwise the object would only be on the other side for one frame and not on this (this would be a visible one frame flicker)
                    trackedObject.ExitPortal(this);
                    trackedObject.EnterPortal(_reversePortal, _reversePortal._targetCellGuid.Value, true);

                    // make object change its cell
                    trackedObject.SetSpatialCell(_targetCellGuid.Value, true, true, true);
                    break;
                }


                // if the object is on the back side of the portal and is currently handled as being inside the portal, we need to disable that (if an object is behind a portal it cannot go through it), else we need to enable it
                if ((currentRelativePos.z > 0.0f) && trackedObject.IsInsidePortal)
                {
                    trackedObject.ExitPortal(this);
                }
                else if (currentRelativePos.z <= 0.0f && !trackedObject.IsInsidePortal)
                {
                    trackedObject.EnterPortal(this, _targetCellGuid.Value);
                }


                _trackedObjectPrevRelativePositions[i] = currentRelativePos;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            
            if (other.isTrigger)
                return;

            CompressedSpaceEntity compressedSpaceEntity;

            // if the main camera collider enters the portal trigger, we need to track it, it might be going through
            if (other.gameObject.CompareTag("MainCamera"))
            {
                _cameraInReach = other.GetComponent<QuantumVRCamera>();
                if (_cameraInReach)
                {
                    //_trackedCamera.TogglePortalTransition(true, SpatialCellManagerInstance.GetPortalTargetLayerIndex(Guid, 0), this);
                    StartCoroutine(TrackCamera());
                }
            }
            else if ((compressedSpaceEntity = other.GetComponent<CompressedSpaceEntity>()) != null) // if the object is a compressedSpaceEntity, it might be able to go through the portal, chack that
            {
                // portals and static entities cannot go through portals
                if (compressedSpaceEntity is Portal || compressedSpaceEntity.Static)
                    return;

                _containedTrackedObjectColliders.Add(other);

                EnterPortal(compressedSpaceEntity);
            }
        }

        private void OnTriggerExit(Collider other)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            
            if (other.isTrigger)
                return;

            CompressedSpaceEntity compressedSpaceEntity;

            // if we are tracking a camera and the main camera is exiting the trigger, we need to stop tracking it
            if (other.gameObject.CompareTag("MainCamera") && _cameraInReach)
                StopCameraTracking();
            else if ((compressedSpaceEntity = other.GetComponent<CompressedSpaceEntity>()) != null) // if the object is a compressedSpaceEntity we might be tracking it and should maybe stop
            {
                _containedTrackedObjectColliders.Remove(other);

                // if the object is not tracked itself, we need to check whether it is a child of a tracked object to remove that object if this was the last tracked child. This also takes care of multiple colliders on one object
                if (!_trackedObjects.Contains(other.GetComponent<CompressedSpaceEntity>()))
                {
                    foreach (CompressedSpaceEntity trackedObject in _trackedObjects)
                    {
                        // if the exiting collider is not a child of this tracked object, check the next one
                        if (!other.transform.IsChildOf(trackedObject.transform))
                            continue;

                        // if this collider is a child of a tracked object, check if there are any other children. If yes, this object can't be removed
                        if (_containedTrackedObjectColliders.Any(col => col.transform.IsChildOf(trackedObject.transform)))
                            continue;

                        RemoveTrackedObject(trackedObject);
                        return;
                    }

                    return;
                }

                // if this tracked object has any tracked children, don't remove it
                if (_containedTrackedObjectColliders.Any(col => col.transform.IsChildOf(compressedSpaceEntity.transform)))
                    return;

                RemoveTrackedObject(compressedSpaceEntity);
            }
        }


        // ######################## FUNCTIONALITY ######################## //
        protected override void OnLayerMaskChanged(byte spatialLayermask, bool isInActiveCell)
        {
            MaterialManager.SetPortalMaterials(_renderer, Guid, spatialLayermask, isInActiveCell, _orignalStencilMaterial, _originalSurfaceMaterial, _originalViewMaterial);
        }

        public void EnterPortal(CompressedSpaceEntity compressedSpaceEntity)
        {
            // only track the object if it is not a child of an already tracked object. If it is the parent of a tracked object remove that object and add this one instead
            for (int i = _trackedObjects.Count - 1; i >= 0; --i)
            {
                CompressedSpaceEntity trackedObject = _trackedObjects[i];
                if (compressedSpaceEntity.transform.IsChildOf(trackedObject.transform))
                    return;

                if (trackedObject.transform.IsChildOf(compressedSpaceEntity.transform))
                {
                    _trackedObjects.RemoveAt(i);
                }
            }

            // track the object
            _trackedObjects.Add(compressedSpaceEntity);
            _trackedObjectPrevRelativePositions.Add(new Vector3(Single.NaN, Single.NaN, Single.NaN));
        }

        private void RemoveTrackedObject(CompressedSpaceEntity trackedObject)
        {
            // stop portal traversal
            trackedObject.ExitPortal(this);
            int index = _trackedObjects.IndexOf(trackedObject);
            _trackedObjectPrevRelativePositions.RemoveAt(index);
            _trackedObjects.RemoveAt(index);
        }


        // ######################## COROUTINES ######################## //
        /// <summary>
        /// Tracks a camera that might go through the portal and triggers the transition if it does go through
        /// </summary>
        /// <returns></returns>
        private IEnumerator TrackCamera()
        {
            QuantumVRLogger.Log($"Camera {_cameraInReach.name} entered Portal {this.name}", this);
            
            Transform cameraTransform = _cameraInReach.transform;
            Vector3 prevRelativePos = transform.InverseTransformPoint(cameraTransform.position);
            bool transitionPaused = false;

            while (_cameraInReach)
            {
                // get position and relative to the portal
                Vector3 currentRelativePos = transform.InverseTransformPoint(cameraTransform.position);

                if (_cameraInReach.TraversingPortal != this)
                    _trackedCamera = null;

                if (_trackedCamera == null)
                {
                    // if the camera is on the side of the portal where the cell is it leads to and the velocity is going towards that world, the camera went through the portal
                    if (currentRelativePos.z <= 0.0f)
                    {
                        _trackedCamera = _cameraInReach;
                        _trackedCamera.TogglePortalTransition(true,
                            SpatialCellManagerInstance.GetPortalTargetLayerIndex(Guid, 0), this);
                    }
                }
                else
                {
                    // if the camera is on the side of the portal where the cell is it leads to and the velocity is going towards that world, the camera went through the portal
                    if (prevRelativePos.z < 0.0f && currentRelativePos.z >= 0.0f && _obb.Contains(cameraTransform.position))
                    {
                        SpatialCellManagerInstance.ChangeActiveCell(_targetCellGuid.Value);
                        StopCameraTracking();
                        break;
                    }
                    else if ((currentRelativePos.z >= 0.0f || !_obb.Contains(cameraTransform.position)) && !transitionPaused)
                    {
                        transitionPaused = true;
                        _trackedCamera.PausePortalTransition(true);
                    }
                    else if (currentRelativePos.z < 0.0f && transitionPaused && _obb.Contains(cameraTransform.position))
                    {
                        transitionPaused = false;
                        _trackedCamera.PausePortalTransition(false);
                    }
                }


                prevRelativePos = currentRelativePos;
                yield return new WaitForEndOfFrame();
            }
        }


        // ######################## UTILITIES ######################## //
        private void StopCameraTracking()
        {
            QuantumVRLogger.Log($"Camera {_cameraInReach.name} exited Portal {this.name}", this);
            _cameraInReach = null;

            if (_trackedCamera == null)
                return;

            if (_trackedCamera.TraversingPortal)
                _trackedCamera?.TogglePortalTransition(false);
            _trackedCamera = null;
        }

        #region UNITY_EDITOR

        #if UNITY_EDITOR
        private void UpdateReversePortal()
        {
            if(!Selection.gameObjects.Contains(gameObject))
                return;

            Transform reversePortalTransform = _reversePortal.transform;
            Transform thisTransform = transform;
            
            reversePortalTransform.position = thisTransform.position;
            reversePortalTransform.SetLossyScale(thisTransform.lossyScale);
            reversePortalTransform.rotation = thisTransform.rotation*Quaternion.AngleAxis(180, Vector3.up);
        }
        #endif

        #endregion
    }
}