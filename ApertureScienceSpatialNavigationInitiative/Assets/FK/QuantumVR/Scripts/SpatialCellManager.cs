using System;
using System.Collections.Generic;
using FK.JSON;
using FK.QuantumVR.Objects;
using UnityEngine;
using FK.Utility;


namespace FK.QuantumVR
{
    /// <summary>
    /// <para>Manages the spatial cells of a compressed space scene</para>
    ///
    /// v1.0 10/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class SpatialCellManager : Singleton<SpatialCellManager>
    {
        // ######################## ENUMS & DELEGATES ######################## //
        public delegate void UpdateLayermaskDelegate(Guid cellGuid, byte layermask, bool isActiveCell);


        // ######################## PROPERTIES ######################## //
        public Guid ActiveCell => _activeCell;


        // ######################## EXPOSED VARS ######################## //
        [SerializeField] private SpatialCellMap _spatialCellMapData;


        // ######################## PUBLIC VARS ######################## //
        /// <summary>
        /// Called after the spatial layers where changed and the Spatial cells should change their layermasks
        /// </summary>
        public event UpdateLayermaskDelegate UpdateLayermask;

        /// <summary>
        /// Called after UpdateLayermask
        /// </summary>
        public event Action PostLayermaskUpdate;

        /// <summary>
        /// Unity layers by spatial layer index-1 (spatial layer 0 has the original unity layers of the respective objects)
        /// </summary>
        [HideInInspector] public int[] UnityLayersBySpatialLayer;


        // ######################## PRIVATE VARS ######################## //
        private JSONObject _settings;
        private RuntimeSpatialCellMap _spatialCellMap;

        private readonly Dictionary<Guid, Portal> _portals = new Dictionary<Guid, Portal>();

        /// <summary>
        /// The index of a Portal Guid in this corresponds to the spatial layer the portal leads into (and thus should render). Only one portal can lead into a spatial layer (spatial layer != spatial cell !!!)
        /// </summary>
        private Guid[] _portalsPerTargetLayer;

        /// <summary>
        /// The index of a Portal Guid in this corresponds to the spatial layer the portal is deactivated in because the layer is rendered through its counterpart.
        /// </summary>
        private Guid[] _disabledPortalsPerTargetLayer;

        private Guid _activeCell;
        private readonly List<Guid> _visibleCells = new List<Guid>();

        /// <summary>
        /// Node Queue for the Cell layer calculation algorythm
        /// </summary>
        private readonly Queue<RuntimeSpatialCellMap.Node> _nodeQueue = new Queue<RuntimeSpatialCellMap.Node>();

        /// <summary>
        /// Queue for the Cell layer calculation algorythm containing the nodes that was looking at the node at the same place in _nodeQueue
        /// </summary>
        private readonly Queue<RuntimeSpatialCellMap.Node> _comingFromQueue = new Queue<RuntimeSpatialCellMap.Node>();


        // ######################## UNITY EVENT FUNCTIONS ######################## //
        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        private void Start()
        {
            // setup spatial layers
            ChangeActiveCell(_spatialCellMapData.StartCellGuid.Value);
        }


        // ######################## INITS ######################## //
        ///<summary>
        /// Does the Init for this Behaviour
        ///</summary>
        private void Init()
        {
            // load data
            _settings = SettingsLoader.Settings;
            _spatialCellMap = new RuntimeSpatialCellMap(_spatialCellMapData, true);

            // setup arrays
            int spatialLayerCount = _settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue;
            _portalsPerTargetLayer = new Guid[spatialLayerCount];
            _disabledPortalsPerTargetLayer = new Guid[_portalsPerTargetLayer.Length];
            
            // get unity layers
            UnityLayersBySpatialLayer = new int[spatialLayerCount-1];
            JSONObject unityLayerNames = _settings[SettingsLoader.UNITY_LAYERS_KEY];
            for (int i = 0; i < UnityLayersBySpatialLayer.Length; ++i)
            {
                UnityLayersBySpatialLayer[i] = LayerMask.NameToLayer(unityLayerNames[i].StringValue);
            }
        }


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Calculates the spatial layers of the spatial cells using an adapted Breadth First Search starting from the active cell
        /// </summary>
        private void CalculateCellLayers()
        {
            // clear old data
            _spatialCellMap.ClearSpatialLayers();
            _visibleCells.Clear();
            Array.Clear(_portalsPerTargetLayer, 0, _portalsPerTargetLayer.Length);
            Array.Clear(_disabledPortalsPerTargetLayer, 0, _disabledPortalsPerTargetLayer.Length);
            MaterialManager.MarkAllMaterialsUnused();

            // start at active cell
            _spatialCellMap[_activeCell].IsActiveCell = true;
            _nodeQueue.Enqueue(_spatialCellMap[_activeCell]);
            _comingFromQueue.Enqueue(null);

            // calculate spatial layers
            byte nextLayer = 0;
            int totalLayers = 0;
            do
            {
                // get the node we are loocking at from the queue
                RuntimeSpatialCellMap.Node currentNode = _nodeQueue.Dequeue();
                RuntimeSpatialCellMap.Node comingFromNode = _comingFromQueue.Dequeue();

                // add the cell to the visible cells. As we can visit cells multiple times, it might already be in the list, we want to have it in there only once
                if (!_visibleCells.Contains(currentNode.CellGuid))
                {
                    _visibleCells.Add(currentNode.CellGuid);
                    currentNode.CellObject.SetActive(true);
                }

                // add the current layer to the layermask of the node
                currentNode.SpatialLayerMask |= nextLayer;

                // if there is a node we are coming from, we need to find the portal that represents that connection and store the direction we came through
                if (comingFromNode != null)
                {
                    _portalsPerTargetLayer[totalLayers] = comingFromNode.PortalsLeadingOut[currentNode.CellGuid];
                    _disabledPortalsPerTargetLayer[totalLayers] = currentNode.PortalsLeadingOut[comingFromNode.CellGuid];
                }
                else
                {
                    _portalsPerTargetLayer[totalLayers] = Guid.Empty;
                    _disabledPortalsPerTargetLayer[totalLayers] =Guid.Empty;
                }

                // now add all adjacent cells exept the one we came from to the queue
                for (int i = 0; i < currentNode.AdjacentCells.Count; ++i)
                {
                    if (currentNode.AdjacentCells[i] == comingFromNode)
                        continue;

                    _nodeQueue.Enqueue(currentNode.AdjacentCells[i]);
                    _comingFromQueue.Enqueue(currentNode);
                }

                // create bitmask for next layer
                if (nextLayer == 0)
                    nextLayer = 1;
                else
                    nextLayer <<= 1;

                ++totalLayers;

                // stop once we reached our max layers, no nodes are left in the queue or there are no more bits left in the bitmask
            } while (totalLayers < _settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue && nextLayer > 0 && _nodeQueue.Count > 0);


            // post update event for each cell and cull invisible cells
            foreach (RuntimeSpatialCellMap.Node cell in _spatialCellMap)
            {
                if (cell.SpatialLayerMask == 0 && !cell.IsActiveCell)
                {
                    if (cell.CellObject.gameObject.activeSelf)
                        cell.CellObject.SetActive(false);
                }
                else
                    UpdateLayermask?.Invoke(cell.CellGuid, cell.SpatialLayerMask, cell.IsActiveCell);
            }

            // clean up
            _nodeQueue.Clear();
            _comingFromQueue.Clear();
            MaterialManager.DisposeUnusedMaterials();
            PostLayermaskUpdate?.Invoke();
        }

        /// <summary>
        /// Sets the active cell and recalculates spatial layers
        /// </summary>
        /// <param name="newActiveCell"></param>
        public void ChangeActiveCell(Guid newActiveCell)
        {
            _activeCell = newActiveCell;
            CalculateCellLayers();
        }

        public bool IsActiveCell(Guid cellGuid)
        {
            return cellGuid == _activeCell;
        }

        public bool IsInActiveCell(GameObject obj)
        {
            CompressedSpaceEntity compressedSpaceEntity = obj.GetComponent<CompressedSpaceEntity>();
            return compressedSpaceEntity != null && compressedSpaceEntity.IsInActiveCell();
        }


        // ######################## SETTER ######################## //
        public void RegisterCellObject(SpatialCell cell)
        {
            _spatialCellMap[cell.Guid].CellObject = cell;
        }

        public void RegisterPortal(Portal portal)
        {
            if (_portals.ContainsKey(portal.Guid))
                return;

            _portals.Add(portal.Guid, portal);
        }


        // ######################## GETTER ######################## //
        public SpatialCell GetCellObject(Guid cellGuid)
        {
            return _spatialCellMap[cellGuid].CellObject;
        }

        public byte GetCellSpatialLayermask(Guid cellGuid)
        {
            return _spatialCellMap[cellGuid].SpatialLayerMask;
        }

        /// <summary>
        /// Returns the the portal leading into a spatial layer. Will be null if there is no portal leading into it
        /// </summary>
        /// <param name="spatialLayerIndex"></param>
        /// <returns></returns>
        public Portal GetPortalLeadingToSpatialLayer(int spatialLayerIndex)
        {
            return spatialLayerIndex <= 0 ? null : _portals[_portalsPerTargetLayer[spatialLayerIndex]];
        }

        public bool PortalDisabledOnLayer(Guid portalGuid, int portalLayer)
        {
            return _disabledPortalsPerTargetLayer[portalLayer] == portalGuid;
        }

        /// <summary>
        /// Returns the index of the spatial layer the portal lead into and should render. -1 if it should not render a spatial layer
        /// </summary>
        /// <param name="portalGuid"></param>
        /// <param name="portalSpatialLayerIndex">The spatial layer the portal is on itself</param>
        /// <returns></returns>
        public int GetPortalTargetLayerIndex(Guid portalGuid, int portalSpatialLayerIndex)
        {
            for (int i = portalSpatialLayerIndex; i < _portalsPerTargetLayer.Length; ++i)
            {
                if (_portalsPerTargetLayer[i] == portalGuid)
                    return i;
            }

            return -1;
        }

        public Portal GetPortal(Guid portalGuid)
        {
            return _portals[portalGuid];
        }
    }
}