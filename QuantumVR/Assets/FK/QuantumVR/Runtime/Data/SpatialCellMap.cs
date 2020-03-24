using System;
using System.Collections;
using System.Collections.Generic;
using FK.Utility;
using UnityEngine;


namespace FK.QuantumVR
{
    /// <summary>
    /// <para>A serializable Map of the spatial cells of a Compressed space level</para>
    ///
    /// v1.0 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [CreateAssetMenu(fileName = "SpatialCellMap", menuName = "QuantumVR/Spatial Cell Map", order = 0)]
    public class SpatialCellMap : ScriptableObject
    {
        // ######################## STRUCTS & CLASSES ######################## //
        /// <summary>
        /// Editor and Serializable Data for spatial cell nodes
        /// </summary>
        [System.Serializable]
        public class NodeData
        {
            /// <summary>
            /// Guid of the Spatial Cell this Node represents
            /// </summary>
            public SerializableGuid CellGuid;

            /// <summary>
            /// Guids of all portals leading out of this cell
            /// </summary>
            public List<SerializableGuid> Connections;

            #region EDITOR_DATA

#if UNITY_EDITOR
            public Color Color;
            public string Name;
            public Vector2 Position;
#endif

            #endregion

#if UNITY_EDITOR
            public NodeData(Vector2 position, Color color)
            {
                CellGuid = Guid.NewGuid();
                Connections = new List<SerializableGuid>();

                Color = color;
                Name = "NewCell";
                Position = position;
            }
#endif
        }

        /// <summary>
        /// Serializable Data for connections between spatial cells. THis represents only one way of the connection, just like a portal
        /// </summary>
        [System.Serializable]
        public class ConnectionData
        {
            /// <summary>
            /// The portal that this connection represents
            /// </summary>
            public SerializableGuid PortalGuid;

            /// <summary>
            /// Guid of the cell the connection leads to
            /// </summary>
            public SerializableGuid TargetCellGuid;

            /// <summary>
            /// Guid of the cell the connection comes from
            /// </summary>
            public SerializableGuid StartCellGuid;

            /// <summary>
            /// Guid of the portal leading back
            /// </summary>
            public SerializableGuid ReversePortalGuid;

            public ConnectionData(Guid portalGuid, Guid targetCellGuid, Guid startCellGuid, Guid reversePortalGuid)
            {
                PortalGuid = portalGuid;
                TargetCellGuid = targetCellGuid;
                StartCellGuid = startCellGuid;
                ReversePortalGuid = reversePortalGuid;
            }
        }

        /// <summary>
        /// Non generic serializable dictionary for nodes (because the Unity serializer does not yet support generic serialization)
        /// </summary>
        [System.Serializable]
        public class NodeDictionary : SerializableDictionary<SerializableGuid, NodeData>
        {
        }

        /// <summary>
        /// Non generic serializable dictionary for connections (because the Unity serializer does not yet support generic serialization)
        /// </summary>
        [System.Serializable]
        public class ConnectionDictionary : SerializableDictionary<SerializableGuid, ConnectionData>
        {
        }


        // ######################## EXPOSED VARS ######################## //
        public NodeDictionary Nodes = new NodeDictionary();
        public ConnectionDictionary Connections = new ConnectionDictionary();

        /// <summary>
        /// Guid of the Active Cell at startup
        /// </summary>
        public SerializableGuid StartCellGuid;

        #region EDITOR_DATA

#if UNITY_EDITOR
        public Vector2 ViewportPosition = Vector2.zero;
        public Vector2 ViewportZoomOrigin = Vector2.zero;
        public float ViewportZoomLevel = 1;
#endif

        #endregion
    }


    /// <summary>
    /// <para>The runtime version of the Map of the spatial cells of a Compressed space level</para>
    ///
    /// v1.1 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class RuntimeSpatialCellMap : IEnumerable, IEnumerator
    {
        // ######################## STRUCTS & CLASSES ######################## //
        public class Node
        {
            public readonly Guid CellGuid;
            public SpatialCell CellObject;
            public readonly List<Node> AdjacentCells;

            /// <summary>
            /// Portals leading out of this cell. Key is the target cell, value is the portal
            /// </summary>
            public readonly Dictionary<Guid, Guid> PortalsLeadingOut;

            public byte SpatialLayerMask;
            public bool IsActiveCell;

            public Node(Guid guid)
            {
                CellGuid = guid;
                AdjacentCells = new List<Node>();
                PortalsLeadingOut = new Dictionary<Guid, Guid>();
            }
        }


        // ######################## PUBLIC VARS ######################## //
        public object Current => ((KeyValuePair<Guid, Node>) _cellDictionaryEnumerator.Current).Value;


        // ######################## PRIVATE VARS ######################## //
        private readonly SpatialCellMap _serializedData;
        private readonly Dictionary<Guid, Node> _cells;

        private IEnumerator _cellDictionaryEnumerator;


        // ######################## INITS ######################## //
        public RuntimeSpatialCellMap(SpatialCellMap serializedData, bool autoParse = false)
        {
            _serializedData = serializedData;
            _cells = new Dictionary<Guid, Node>();

            if (autoParse)
                Parse();
        }


        // ######################## Operators ######################## //
        public Node this[Guid guid] => _cells[guid];


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Parses the serialized data into its runtime format
        /// </summary>
        public void Parse()
        {
            QuantumVRLogger.LogVerbose("Parsing Spatial Cell Map");
            
            // first create all the nodes
            foreach (KeyValuePair<SerializableGuid, SpatialCellMap.NodeData> dataNode in _serializedData.Nodes)
            {
                Guid cellGuid = dataNode.Key.Value;
                _cells.Add(cellGuid, new Node(cellGuid));
            }

            // now set up connections between the nodes
            foreach (KeyValuePair<SerializableGuid, SpatialCellMap.NodeData> dataNode in _serializedData.Nodes)
            {
                Node runtimeNode = _cells[dataNode.Key.Value];

                foreach (SerializableGuid connectionGuid in dataNode.Value.Connections)
                {
                    SpatialCellMap.ConnectionData connection = _serializedData.Connections[connectionGuid];
                    Node adjacentNode = _cells[connection.TargetCellGuid.Value];
                    runtimeNode.AdjacentCells.Add(adjacentNode);
                    runtimeNode.PortalsLeadingOut.Add(adjacentNode.CellGuid, connection.PortalGuid.Value);
                }
            }
        }

        /// <summary>
        /// Sets the layermask of each cell to 0
        /// </summary>
        public void ClearSpatialLayers()
        {
#if UNITY_EDITOR
            QuantumVRLogger.LogVerbose("Clearing Spatial Layers of all cells");
#endif
            
            foreach (KeyValuePair<Guid, Node> pair in _cells)
            {
                pair.Value.SpatialLayerMask = 0;
                pair.Value.IsActiveCell = false;
            }
        }


        // ######################## IEnumerable ######################## //
        public IEnumerator GetEnumerator()
        {
            _cellDictionaryEnumerator = _cells.GetEnumerator();
            return (IEnumerator) this;
        }


        // ######################## IEnumerator ######################## //
        public bool MoveNext()
        {
            return _cellDictionaryEnumerator.MoveNext();
        }

        public void Reset()
        {
            _cellDictionaryEnumerator.Reset();
        }
    }
}