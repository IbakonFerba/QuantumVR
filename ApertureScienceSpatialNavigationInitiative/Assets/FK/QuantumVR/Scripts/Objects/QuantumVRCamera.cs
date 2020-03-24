using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace FK.QuantumVR.Objects
{
    /// <summary>
    /// <para>Contains extra functionality for a QuantumVR Camera</para>
    ///
    /// v1.2 11/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class QuantumVRCamera : MonoBehaviour
    {
        // ######################## PROPERTIES ######################## //
        /// <summary>
        /// World space speed of the camera
        /// </summary>
        public Vector3 Velocity => _cameraComponent.velocity;
        public float NearClipPlane => _cameraComponent.nearClipPlane;
        public Portal TraversingPortal => _traversingPortal;


        // ######################## EXPOSED VARS ######################## //
        /// <summary>
        /// A Quad that is rendered when going through a portal that does some stencil and depth magic so the near clip plane clipping the portal does not break the immersion
        /// </summary>
        [SerializeField] private GameObject _portalTransitionQuad;
        [SerializeField] private Transform _depthClearQuad;

        [SerializeField] private float _transitionQuadNearPlaneOffset = 0.01f;


        // ######################## PRIVATE VARS ######################## //
        private Material _portalTransitionStencilMaterial;
        private Material _portalTransitionDepthMaterial;
        private Material _portalTransitionViewMaterial;

        private Camera _cameraComponent;

        private Portal _traversingPortal;


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            gameObject.tag = "MainCamera";
        }


        // ######################## INITS ######################## //
        ///<summary>
        /// Does the Init for this Behaviour
        ///</summary>
        private void Init()
        {
            _cameraComponent = GetComponent<Camera>();
            _cameraComponent.opaqueSortMode = OpaqueSortMode.FrontToBack;

            // get the transition quad materials
            Renderer quadRenderer = _portalTransitionQuad.GetComponent<Renderer>();
            foreach (Material material in quadRenderer.sharedMaterials)
            {
                if (material.shader.name.Equals("Hidden/QuantumVR/CameraPortalStencil"))
                    _portalTransitionStencilMaterial = material;
                else if (material.shader.name.Equals("Hidden/QuantumVR/CameraDepth"))
                    _portalTransitionDepthMaterial = material;
                else if (material.shader.name.Equals("Hidden/QuantumVR/CameraView"))
                    _portalTransitionViewMaterial = material;

                if (_portalTransitionDepthMaterial && _portalTransitionStencilMaterial && _portalTransitionViewMaterial)
                    break;
            }

            quadRenderer = _depthClearQuad.GetComponent<Renderer>();
            quadRenderer.sharedMaterial.renderQueue = SettingsLoader.Settings[SettingsLoader.DEPTH_CLEAR_QUEUE].IntValue;

            // the stencil material should always render first
            _portalTransitionStencilMaterial.renderQueue = 0;
            _portalTransitionDepthMaterial.renderQueue = MaterialManager.PortalSurfaceActiveLayerQueue;
            _portalTransitionViewMaterial.renderQueue = MaterialManager.PortalViewQueue[0];

            // position the transition quad so that it is just barely beyond the near clip plane and disable it
            _portalTransitionQuad.transform.localPosition = Vector3.forward * (_cameraComponent.nearClipPlane + _transitionQuadNearPlaneOffset);
            _portalTransitionQuad.SetActive(false);

            _depthClearQuad.localPosition = _portalTransitionQuad.transform.localPosition;

            // set the collider so it protrudes a bit beyond the near clip plane
            SphereCollider col = GetComponent<SphereCollider>();
            col.radius = _cameraComponent.nearClipPlane + 0.1f;
        }


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Toggles portal transition rendering
        /// </summary>
        /// <param name="enable">Render portal transition?</param>
        /// <param name="targetLayer">Spatial layer the portal is rendering</param>
        /// <param name="portal">The portal that is currently traversed</param>
        public void TogglePortalTransition(bool enable, int targetLayer = 0, Portal portal = null)
        {
            _portalTransitionQuad.SetActive(enable);

            _traversingPortal = portal;
            if (!enable)
                return;

            // set portal clipping matrix
            Matrix4x4 worldToPortal = portal.transform.worldToLocalMatrix;
            _portalTransitionStencilMaterial.SetMatrix(MaterialManager.ShaderWorldToPortalMatrixProperty, worldToPortal);
            _portalTransitionDepthMaterial.SetMatrix(MaterialManager.ShaderWorldToPortalMatrixProperty, worldToPortal);
            _portalTransitionViewMaterial.SetMatrix(MaterialManager.ShaderWorldToPortalMatrixProperty, worldToPortal);

            // set stencil masking and depth queue
            _portalTransitionStencilMaterial.SetInt(MaterialManager.ShaderWriteBitProperty, 1 << (targetLayer - 1));
            _portalTransitionViewMaterial.SetInt(MaterialManager.ShaderWriteBitProperty, 1 << (targetLayer - 1));
        }

        public void PausePortalTransition(bool pause)
        {
            _portalTransitionQuad.SetActive(!pause);
        }
    }
}