using System;
using UnityEngine;
using FK.Utility;

namespace FK.QuantumVR
{
    /// <summary>
    /// <para>Scene representation of a spatial cell</para>
    ///
    /// v1.0 11/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [AddComponentMenu("")]
    public class SpatialCell : MonoBehaviour
    {
        // ######################## DELEGATES ######################## //
        public delegate void UpdateLayermaskDelegate(byte spatialLayermask, bool isInActiveCell);


        // ######################## PROPERTIES ######################## //
        public Guid Guid => _guid.Value;


        // ######################## PUBLIC VARS ######################## //
        /// <summary>
        /// Called when the child objects should update their layermasks
        /// </summary>
        public event UpdateLayermaskDelegate OnUpdateLayermaskEvent;


        // ######################## EXPOSED VARS ######################## //
        [SerializeField] private SerializableGuid _guid;


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        private void Awake()
        {
            SpatialCellManager.Instance.RegisterCellObject(this);
            SpatialCellManager.Instance.UpdateLayermask += OnUpdateLayermask;
        }

        private void OnDestroy()
        {
            if (SpatialCellManager.Instance)
                SpatialCellManager.Instance.UpdateLayermask -= OnUpdateLayermask;
        }

        // ######################## FUNCTIONALITY ######################## //
        private void OnUpdateLayermask(Guid cellGuid, byte layermask, bool isActiveCell)
        {
            if(!gameObject.activeSelf)
                return;

            if (cellGuid == Guid)
                OnUpdateLayermaskEvent?.Invoke(layermask, isActiveCell);
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}