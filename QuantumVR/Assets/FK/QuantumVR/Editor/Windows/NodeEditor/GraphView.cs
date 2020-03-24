using System;
using System.Collections.Generic;
using FK.QuantumVR.Editor.Tools;
using FK.UIElements;
using FK.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ColorUtility = FK.Utility.ColorUtility;

[assembly: UxmlNamespacePrefix("FK.QuantumVR.Editor.UIElements", "qvr")]

namespace FK.QuantumVR.Editor.UIElements
{
    /// <summary>
    /// <para>Graph View for a QuantumVR spatial cell map</para>
    ///
    /// v0.93 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class GraphView : VisualElement
    {
        // ######################## STRUCTS & CLASSES ######################## //

        #region STRUCTS & CLASSES

        public new class UxmlFactory : UxmlFactory<GraphView>
        {
        }

        #endregion


        // ######################## EVENTS ######################## //
        public event Action OnStartNodeSelectionModeExit;

        // ######################## PROPERTIES ######################## //

        #region PROPERTIES

        public Viewport Viewport => _viewport;
        public bool HasData => _graph != null;

        public bool StartNodeSelectionModeActive => _startNodeSelectionModeActive;

        /// <summary>
        /// True if there is a floating connection, which means that we are in connection creation mode
        /// </summary>
        private bool CreatingConnection => _floatingConnection.visible;

        #endregion


        // ######################## PRIVATE VARS ######################## //

        #region PRIVATE VARS

        /// <summary>
        /// The graph we are editing
        /// </summary>
        private SpatialCellMap _graph;

        private readonly Viewport _viewport;

        /// <summary>
        /// Visual element that is used to mark the start node
        /// </summary>
        private readonly VisualElement _startNodeMarker;


        /// <summary>
        /// Parent element for all connections
        /// </summary>
        private readonly VisualElement _connectionContainer;

        /// <summary>
        /// All visual representations of connections
        /// </summary>
        private readonly List<Connection> _connections = new List<Connection>();


        /// <summary>
        /// Parent element for all nodes
        /// </summary>
        private readonly VisualElement _nodeContainer;

        /// <summary>
        /// All visual representations of nodes
        /// </summary>
        private readonly Dictionary<Guid, NodeView> _nodeViews = new Dictionary<Guid, NodeView>();


        /// <summary>
        /// The spatial cell that is currently in edit mode in the scene
        /// </summary>
        private SpatialCellMap.NodeData _editingCell;


        #region SELECTION

        private readonly List<NodeView> _selectedNodes = new List<NodeView>();


        #region BOX SELECTION

        /// <summary>
        /// The element that is used to drag the selection box
        /// </summary>
        private readonly VisualElement _selectionHelper;

        private readonly VisualElement _selectionBox;


        /// <summary>
        /// The drag manipulator manipulating the _selectionHelper object
        /// </summary>
        private readonly DragManipulator _boxSelectionDragManipulator;


        /// <summary>
        /// Local point where the box selection was started
        /// </summary>
        private Vector2 _boxSelectionStart;

        /// <summary>
        /// Current local end point of the box selection
        /// </summary>
        private Vector2 _boxSelectionEnd;

        #endregion

        #endregion


        #region CONNECTION CREATION

        private readonly Connection _floatingConnection;
        private SpatialCellMap.NodeData _floatingConnectionStartNode;


        #region BROKEN CONNECTION

        /// <summary>
        /// The connection that was broken. This will stick around until the floating connection establishes a new connection or the connection mode is exited
        /// </summary>
        private Connection _oldBrokenConnection;

        /// <summary>
        /// The portal guid of the half connection that was broken
        /// </summary>
        private Guid _brokenConnectionGuid;

        /// <summary>
        /// The portal guid of the half connection that was kept in place
        /// </summary>
        private Guid _keptConnectionGuid;

        #endregion

        #endregion


        #region FLAGS

        private bool _startNodeSelectionModeActive;

        /// <summary>
        /// If the floating connection is a new connection, this is FALSE. If it is an existing connection that was broken, this is TRUE
        /// </summary>
        private bool _floatingConnectionWasBroken;

        #endregion

        #endregion


        // ######################## INITS ######################## //

        #region CONSTRUCTORS

        public GraphView()
        {
            // load styles
            styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
            styleSheets.Add(Resources.Load<StyleSheet>("GraphView_styles"));
            AddToClassList(UssClasses.FULL);
            AddToClassList("GraphView");

            // we need the graph view to be focusable so we can get keyboard events
            focusable = true;

            // add viewport
            _viewport = new Viewport {name = "viewport"};
            _viewport.AddToClassList("viewport");
            hierarchy.Add(_viewport);

            // add connection container
            _connectionContainer = new VisualElement {name = "connections"};
            _viewport.Add(_connectionContainer);

            // add node container
            _nodeContainer = new VisualElement {name = "nodes"};
            _viewport.Add(_nodeContainer);

            // add start node marker
            _startNodeMarker = new VisualElement {name = "startNodeMarker"};
            _startNodeMarker.AddToClassList("start-node-marker");

            // add selection helper
            _selectionHelper = new VisualElement {name = "selectionHelper"};
            hierarchy.Add(_selectionHelper);

            // add selection box
            _selectionBox = new VisualElement {name = "selectionBox"};
            _selectionBox.AddToClassList("selection-box");
            _selectionBox.visible = false;
            hierarchy.Add(_selectionBox);

            // add floating connection
            // this is the connection that is drawn when a connection is created or retargeted
            _floatingConnection = new Connection {name = "floatingConnection"};
            _floatingConnection.AddToClassList("floating-connection");
            _connectionContainer.Add(_floatingConnection);
            _floatingConnection.visible = false;

            // add a context menu in the form of a manipulator
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Create Spatial Cell", CreateCellContextMenuAction, GetCreateCellContextMenuActionStatus);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Make Start Cell", SetStartCellFromContextMenuAction, GetSetStartCellFromContextMenuActionStatus);
                evt.menu.AppendAction("Delete", DeleteContextMenuAction, GetDeleteContextMenuActionStatus);
            }));

            // add the selection drag manipulator
            _boxSelectionDragManipulator = new DragManipulator(new ManipulatorActivationFilter {button = MouseButton.LeftMouse}, false, _selectionHelper, OnBoxSelectionUpdate, OnBoxSelectionStart,
                OnBoxSelectionEnd);
            this.AddManipulator(_boxSelectionDragManipulator);


            // when an undo is performed we need to reload the graph because the undo could have changed it
            Undo.undoRedoPerformed += OnRedo;

            // register callbacks to the graph view itself
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            // register callbacks on child elements
            _viewport.OnUpdated += OnViewportUpdate;
        }

        #endregion

        #region INITS

        /// <summary>
        /// Unloads any loaded data and loads the provided graph
        /// </summary>
        /// <param name="graph"></param>
        public void LoadGraph(SpatialCellMap graph)
        {
            ClearWorkspace();

            _graph = graph;

            // load the nodes first because they need to be present when setting up the connections
            foreach (KeyValuePair<SerializableGuid, SpatialCellMap.NodeData> pair in _graph.Nodes)
            {
                LoadNodeVisuals(pair.Value);
            }

            QuantumVRLogger.EditorLogVerbose($"Loaded {_graph.Nodes.Count} Nodes");

            foreach (KeyValuePair<SerializableGuid, SpatialCellMap.ConnectionData> pair in _graph.Connections)
            {
                LoadConnectionVisuals(pair.Value);
            }

            QuantumVRLogger.EditorLogVerbose($"Loaded {_graph.Connections.Count / 2} Connections");

            // load viewport
            _viewport.SetPositionAndZoom(_graph.ViewportPosition, _graph.ViewportZoomOrigin, _graph.ViewportZoomLevel);

            // if there are nodes in the graph, check if a start node is set and set one if not. The load the start node marker
            if (_graph.Nodes.Count > 0)
            {
                SetStartCellIfNoneSet();
                LoadStartNodeMarker();
            }

            if (_editingCell != null)
            {
                if (_nodeViews.ContainsKey(_editingCell.CellGuid.Value))
                {
                    RequestEditNodeInScene(_editingCell);
                }
            }

            MarkDirtyRepaint();

            QuantumVRLogger.EditorLogVerbose($"Loaded {_graph.name}");
        }

        private void ReloadGraph()
        {
            if (!HasData)
            {
                QuantumVRLogger.EditorLogWarning("Cannot reload Spatial Cell Map, there is none set!");
                return;
            }

            LoadGraph(_graph);
        }

        public void UnloadGraph()
        {
            if (HasData)
                QuantumVRLogger.EditorLogVerbose($"Unloading {_graph.name}...");

            ClearWorkspace();
            _graph = null;
        }

        #endregion


        // ######################## FUNCTIONALITY ######################## //

        #region FUNCTIONALITY

        #region EVENT_HANDLING

        private void OnRedo()
        {
            if (_editingCell != null)
            {
                if (!_graph.Nodes.ContainsKey(_editingCell.CellGuid))
                {
                    CellVisibilityManager.StopIsolation();
                    _editingCell = null;
                }
            }

            ReloadGraph();
        }
        
        /// <summary>
        /// Serializes Viewport position and zoom values whenever they change
        /// </summary>
        private void OnViewportUpdate()
        {
            if (!HasData)
                return;

            _graph.ViewportPosition = _viewport.Position;
            _graph.ViewportZoomOrigin = _viewport.ZoomOrigin;
            _graph.ViewportZoomLevel = _viewport.ZoomValue;
            EditorUtility.SetDirty(_graph);
        }

        #region INPUT

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    if (CreatingConnection)
                        ExitConnectionMode(true);
                    else if (_startNodeSelectionModeActive)
                        ExitStartNodeSelectionMode();
                    break;
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (CreatingConnection)
                        ExitConnectionMode(true);
                    else if (_startNodeSelectionModeActive)
                        ExitStartNodeSelectionMode();
                    else if (_selectedNodes.Count > 0)
                        DeleteSelectedNodes();
                    break;
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            // if the mouse was leftclicked and the event reaches us, the player clicked into an empty part of the viewport. Clear the selection
            if (evt.button == 0)
                ClearNodeSelection();
        }

        #endregion

        #region NODES

        private void OnNodeClicked(NodeView nodeView)
        {
            // only work in start node selection mode
            if (!_startNodeSelectionModeActive)
                return;

            // the clicked node should be the new start node
            SetStartCell(nodeView.Guid);
            ExitStartNodeSelectionMode();
        }

        /// <summary>
        /// Called when a node connection button is pressed. This may either start a new connection or finish the floating connection
        /// </summary>
        /// <param name="nodeView"></param>
        private void OnNodeConnectionButton(NodeView nodeView)
        {
            Guid nodeGuid = nodeView.Guid;
            SpatialCellMap.NodeData nodeData = _graph.Nodes[nodeGuid];

            if (!CreatingConnection) // if we are not create a connection at the moment, start a new one
            {
                EnterConnectionMode(nodeGuid);
                _floatingConnection.UpdateFloatingConnection(_nodeContainer.LocalToWorld(nodeView.layout.center));
            }
            else if (nodeGuid != _floatingConnectionStartNode.CellGuid) // if the clicked node is not the same as the one the floating direction starts at, we might be able to create the connection
            {
                // check if the connection is valid or not
                if (ValidateConnection(_floatingConnectionStartNode, nodeData, out string message))
                {
                    // if we are dealing with a broken connection it means we are working on retargeting an existing one, else we just create a new one
                    if (_floatingConnectionWasBroken)
                        RetargetBrokenConnection(nodeData);
                    else
                        AddConnection(_floatingConnectionStartNode, nodeData);
                }
                else
                {
                    // if the invalid connection was a broken one, just exit the connection mode and restore the old connection. Else tell the user whats wrong and stay in connection mode
                    if (_floatingConnectionWasBroken)
                        ExitConnectionMode(true);
                    else
                        QuantumVRLogger.EditorLogWarning(message);
                }
            }
        }

        /// <summary>
        /// Called whenever a node is selected or deselected
        /// </summary>
        /// <param name="nodeView">The node that was selected</param>
        /// <param name="selected">Was the node selected or deselected?</param>
        /// <param name="multiSelection">Was the selection event part of a multi selection?</param>
        private void OnNodeSelectedStateChanged(NodeView nodeView, bool selected, bool multiSelection)
        {
            // deal with a multi selection
            if (multiSelection)
            {
                if (selected)
                {
                    // add to selection
                    _selectedNodes.Add(nodeView);
                    nodeView.OnDragStart += OnSelectedNodeDragStart;
                    nodeView.OnDragEnd += OnSelectedNodeDragEnd;

                    QuantumVRLogger.EditorLogVerbose($"Added node {_graph.Nodes[nodeView.Guid].Name} to selection.");
                }
                else
                {
                    // remove from selection
                    _selectedNodes.Remove(nodeView);
                    nodeView.OnDragStart -= OnSelectedNodeDragStart;
                    nodeView.OnDragEnd -= OnSelectedNodeDragEnd;

                    QuantumVRLogger.EditorLogVerbose($"Removed node {_graph.Nodes[nodeView.Guid].Name} from selection.");
                }

                return;
            }

            // if we reach this we are not dealing with a multi selection

            // we might be dealing with the last node of a multi selection so we make sure to remove these events if the node was deselected
            if (!selected)
            {
                nodeView.OnDragStart -= OnSelectedNodeDragStart;
                nodeView.OnDragEnd -= OnSelectedNodeDragEnd;
                return;
            }

            // deselect all other nodes
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                if (pair.Value == nodeView)
                    continue;

                pair.Value.SetSelected(false, false);
                pair.Value.OnDragStart -= OnSelectedNodeDragStart;
                pair.Value.OnDragEnd -= OnSelectedNodeDragEnd;
            }

            // set the node as the only selected node
            _selectedNodes.Clear();
            _selectedNodes.Add(nodeView);
        }

        /// <summary>
        /// Called when the user starts to drag a selected node in a multi selection 
        /// </summary>
        /// <param name="nodeView"></param>
        private void OnSelectedNodeDragStart(NodeView nodeView)
        {
            // this node now controls the movment of the mutli selection, so register to the drag event
            nodeView.OnDrag += OnSelectedNodeDragUpdate;
        }

        /// <summary>
        /// Called when the user stops to drag a selected node in a multi selection 
        /// </summary>
        /// <param name="nodeView"></param>
        private void OnSelectedNodeDragEnd(NodeView nodeView)
        {
            nodeView.OnDrag -= OnSelectedNodeDragUpdate;
        }

        /// <summary>
        /// Called when the controllinf node of a multi selection is dragged. All other nodes should follow
        /// </summary>
        /// <param name="nodeView"></param>
        /// <param name="delta"></param>
        private void OnSelectedNodeDragUpdate(NodeView nodeView, Vector2 delta)
        {
            // move all selected nodes by delta
            for (int i = 0; i < _selectedNodes.Count; ++i)
            {
                // don't move the controlling node
                if (_selectedNodes[i] == nodeView)
                    continue;

                _selectedNodes[i].Move(delta);
            }
        }

        /// <summary>
        /// Called when in node edit mode and any node color changes
        /// </summary>
        /// <param name="nodeView"></param>
        /// <param name="color"></param>
        private void OnNodeColorChanged(NodeView nodeView, Color color)
        {
            UpdatePortalHighlight();
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// Starts displaying an ongoing connection retarget. The data is not touched until the retarget is finished
        /// </summary>
        /// <param name="connection">Connection that is retargeted</param>
        /// <param name="fixedNode">Node that the connection will stay connected with</param>
        /// <param name="detachedNode">Node that the connection is detached from</param>
        /// <param name="fixedGuid">Guid of the connection portal that will be kept</param>
        /// <param name="brokenGuid">Guid of the connection portal that will be deleted</param>
        /// <param name="worldMousPos">World mouse position of the click event that started this</param>
        private void StartRetargetConnection(Connection connection, NodeView fixedNode, NodeView detachedNode, Guid fixedGuid, Guid brokenGuid, Vector2 worldMousPos)
        {
            _floatingConnectionWasBroken = true;
            _brokenConnectionGuid = brokenGuid;
            _keptConnectionGuid = fixedGuid;

            // remove the visual representation of the connection, it will be regenerated from scratch once the connection is not floating anymore
            _connectionContainer.Remove(connection);
            _connections.Remove(connection);
            _oldBrokenConnection = connection;

            // start a floating connection between the node that is still connected and the mouse
            EnterConnectionMode(fixedNode.Guid);
            _floatingConnection.UpdateFloatingConnection(worldMousPos);

            QuantumVRLogger.EditorLogVerbose($"Starting retargeting of Connection {fixedGuid}");
        }


        /// <summary>
        /// Used to draw the floating connection to the mouse position
        /// </summary>
        /// <param name="evt"></param>
        private void UpdateFloatingConnection(MouseMoveEvent evt)
        {
            _floatingConnection.UpdateFloatingConnection(evt.mousePosition);
        }

        #endregion

        #region SELECTION

        /// <summary>
        /// Called by the drag manipulator of the box selection when it is activated
        /// </summary>
        /// <param name="evt"></param>
        private void OnBoxSelectionStart(MouseDownEvent evt)
        {
            // set the selection helpers position to the mouse position
            _selectionHelper.style.left = evt.localMousePosition.x;
            _selectionHelper.style.top = evt.localMousePosition.y;

            // the start position of the manipulator is relative to its target. Because we moved the target to the start postion, we now need to make it (0,0)
            _boxSelectionDragManipulator.StartPosition = Vector2.zero;

            // init box selection points
            _boxSelectionStart = evt.localMousePosition;
            _boxSelectionEnd = evt.localMousePosition;

            // make the selection box visible and reset it
            _selectionBox.visible = true;
            _selectionBox.style.left = evt.localMousePosition.x;
            _selectionBox.style.top = evt.localMousePosition.y;
            _selectionBox.style.width = 0;
            _selectionBox.style.height = 0;

            QuantumVRLogger.EditorLogVerbose("Started Box Selection");
        }

        /// <summary>
        /// Called by the drag manipulator of the box selection when it is deactivated
        /// </summary>
        /// <param name="evt"></param>
        private void OnBoxSelectionEnd(MouseUpEvent evt)
        {
            _selectionBox.visible = false;

            QuantumVRLogger.EditorLogVerbose("Finished Box Selection");
        }

        /// <summary>
        /// Called by the drag manipulator of the box selection when it updates
        /// </summary>
        /// <param name="delta"></param>
        private void OnBoxSelectionUpdate(Vector2 delta)
        {
            _boxSelectionEnd += delta;

            // get the bounds of the selection
            Vector2 min = Vector2.Min(_boxSelectionStart, _boxSelectionEnd);
            Vector2 max = Vector2.Max(_boxSelectionStart, _boxSelectionEnd);

            // updated selection box
            _selectionBox.style.left = min.x;
            _selectionBox.style.top = min.y;
            _selectionBox.style.width = max.x - min.x;
            _selectionBox.style.height = max.y - min.y;

            // go through all nodes and check whether they are contained in the box. If yes select them, if no make sure they are deselected
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                pair.Value.SetSelected(_selectionBox.worldBound.Overlaps(pair.Value.worldBound), true);
            }
        }

        #endregion

        #region CONTEXT_MENU

        private void CreateCellContextMenuAction(DropdownMenuAction dropdownMenuAction)
        {
            AddNode(dropdownMenuAction.eventInfo.mousePosition);
        }

        private DropdownMenuAction.Status GetCreateCellContextMenuActionStatus(DropdownMenuAction dropdownMenuAction)
        {
            // disable the action if there is no data loaded
            return HasData ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }


        private void SetStartCellFromContextMenuAction(DropdownMenuAction dropdownMenuAction)
        {
            NodeView node = GetNodeUnderPosition(dropdownMenuAction.eventInfo.mousePosition);
            if (node != null)
                SetStartCell(node.Guid);
        }

        private DropdownMenuAction.Status GetSetStartCellFromContextMenuActionStatus(DropdownMenuAction dropdownMenuAction)
        {
            // only enable the action when there is data and there is a node under the mouse cursor
            return HasData && GetNodeUnderPosition(dropdownMenuAction.eventInfo.mousePosition) != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }


        private void DeleteContextMenuAction(DropdownMenuAction dropdownMenuAction)
        {
            Connection connection = GetConnectionUnderPosition(dropdownMenuAction.eventInfo.mousePosition);
            if (connection != null)
            {
                DeleteConnection(connection);
                return;
            }

            // if no connection was the target, delete all selected nodes
            DeleteSelectedNodes();
        }

        private DropdownMenuAction.Status GetDeleteContextMenuActionStatus(DropdownMenuAction dropdownMenuAction)
        {
            // only enable the action of there is data loaded and there is either a node or a connection under the cursor
            return HasData && (GetNodeUnderPosition(dropdownMenuAction.eventInfo.mousePosition) != null || GetConnectionUnderPosition(dropdownMenuAction.eventInfo.mousePosition) != null)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        #endregion

        #endregion

        #region DATA MANIPUALTION

        #region NODES

        /// <summary>
        /// Creates a new node im the data and loads it
        /// </summary>
        /// <param name="worldPosition"></param>
        public void AddNode(Vector2 worldPosition)
        {
            Vector2 position = _nodeContainer.WorldToLocal(worldPosition);

            // create a new data object and add it to the graph
            SpatialCellMap.NodeData data = new SpatialCellMap.NodeData(position, ColorUtility.HSL(UnityEngine.Random.Range(0, 361), 100, 79));
            data.Name = $"SpatialCell{_graph.Nodes.Count}";

            SetGraphDirtyWithUndo("Add Spatial Cell");
            _graph.Nodes.Add(data.CellGuid, data);


            // create the cell in the scene
            SpatialCellCreationUtility.CreateSpatialCell(data.CellGuid.Value, data.Name);


            NodeView nodeView = LoadNodeVisuals(data);

            // make sure there is a start cell
            SetStartCellIfNoneSet();

            nodeView.SetSelected(true);

            Undo.SetCurrentGroupName("Add Spatial Cell");

            RequestEditNodeInScene(data);
        }

        /// <summary>
        /// Unloads the visuals of a node and deletes in from the data
        /// </summary>
        /// <param name="nodeData"></param>
        public void DeleteNode(SpatialCellMap.NodeData nodeData)
        {
            if (_editingCell == nodeData)
                StopEditingNodeInScene(nodeData);

            bool wasStartCell = _graph.StartCellGuid == nodeData.CellGuid;

            // first we need to delete all connections to the node
            List<Connection> connectionsToRemove = new List<Connection>();
            for (int i = 0; i < nodeData.Connections.Count; ++i) // go through all connections of the node
            {
                // find the connection in the graph display
                for (int j = 0; j < _connections.Count; ++j)
                {
                    if (!_connections[j].IsGuid(nodeData.Connections[i].Value))
                        continue;

                    connectionsToRemove.Add(_connections[j]);
                    break;
                }
            }

            // delete the collected connections
            for (int i = 0; i < connectionsToRemove.Count; ++i)
            {
                DeleteConnection(connectionsToRemove[i]);
            }

            // delete the cell in the scene
            SpatialCellCreationUtility.DeleteSpatialCell(nodeData.CellGuid.Value);

            // delete the node view
            NodeView view = _nodeViews[nodeData.CellGuid.Value];
            _nodeContainer.Remove(view);
            _nodeViews.Remove(nodeData.CellGuid.Value);

            // delete the node in the data
            SetGraphDirtyWithUndo("Delete Spatial Cell");
            _graph.Nodes.Remove(nodeData.CellGuid);

            if (wasStartCell)
            {
                SetGraphDirtyWithUndo("Set Start Cell");
                _graph.StartCellGuid = Guid.Empty;
                SetStartCellIfNoneSet();
            }

            Undo.SetCurrentGroupName("Delete Spatial Cell");
        }

        private void SetStartCell(Guid guid)
        {
            SetGraphDirtyWithUndo("Change Start Cell");
            _graph.StartCellGuid = guid;

            ExitStartNodeSelectionMode();
            LoadStartNodeMarker();

            QuantumVRLogger.EditorLogVerbose($"Made Cell {_graph.Nodes[guid].Name} the Start Cell");
        }

        #endregion

        #region CONNECTIONS

        /// <summary>
        /// Creates a new two way connection between two nodes in the data and loads it
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        private void AddConnection(SpatialCellMap.NodeData node1, SpatialCellMap.NodeData node2)
        {
            SetGraphDirtyWithUndo("Add Connection");

            // we need two new portals because a connection is two way, so generate guids for them
            Guid portal1To2Guid = Guid.NewGuid();
            Guid portal2To1Guid = Guid.NewGuid();

            // we use the first half of the connection to load the visuals, we don't need the second half for that (it is just the same connection reversed)
            LoadConnectionVisuals(AddHalfConnection(node1, node2, portal1To2Guid, portal2To1Guid));

            // create the other direction
            AddHalfConnection(node2, node1, portal2To1Guid, portal1To2Guid);

            // if the connection was added by the user and not by loading the data, we need to exit connection mode
            if (CreatingConnection)
                ExitConnectionMode(false);

            // create the two way portal in the scene
            PortalCreationUtility.CreatePortal(node1.CellGuid.Value, node2.CellGuid.Value, portal1To2Guid, portal2To1Guid);

            Undo.SetCurrentGroupName("Create Cell Connection");

            if (_editingCell != null)
            {
                CellVisibilityManager.StopIsolation();
                CellVisibilityManager.IsolateCell(_editingCell.CellGuid.Value);
                UpdatePortalHighlight();
            }
        }

        /// <summary>
        /// Creates half a connection in the data (only one direction, no reverse)
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        /// <param name="portalGuid">Guid of the portal going from the "fromNode" to the "toNode"</param>
        /// <param name="reversePortalGuid">Guid of the portal going from the "toNode" to the "fromNode"</param>
        /// <returns></returns>
        private SpatialCellMap.ConnectionData AddHalfConnection(SpatialCellMap.NodeData fromNode, SpatialCellMap.NodeData toNode, Guid portalGuid, Guid reversePortalGuid)
        {
            SpatialCellMap.ConnectionData data = new SpatialCellMap.ConnectionData(portalGuid, toNode.CellGuid.Value, fromNode.CellGuid.Value, reversePortalGuid);

            fromNode.Connections.Add(portalGuid);
            _graph.Connections.Add(portalGuid, new SpatialCellMap.ConnectionData(portalGuid, toNode.CellGuid.Value, fromNode.CellGuid.Value, reversePortalGuid));

            return data;
        }

        /// <summary>
        /// Deletes a two way connection in the data and removes its visual representation
        /// </summary>
        /// <param name="connection"></param>
        private void DeleteConnection(Connection connection)
        {
            SetGraphDirtyWithUndo("Remove Connection");

            // one connection has two directions, so we need to delete two halfs of it
            DeleteHalfConnection(_graph.Connections[connection.StartToEndGuid]);
            DeleteHalfConnection(_graph.Connections[connection.EndToStartGuid]);

            // delete the portal in the scene
            PortalCreationUtility.DeletePortal(connection.StartToEndGuid);

            Undo.SetCurrentGroupName("Remove Connection");

            // remove the visuals of the connection
            _connectionContainer.Remove(connection);
            _connections.Remove(connection);
        }

        /// <summary>
        /// Deletes one direction of a connection from the data
        /// </summary>
        /// <param name="data"></param>
        private void DeleteHalfConnection(SpatialCellMap.ConnectionData data)
        {
            _graph.Nodes[data.StartCellGuid].Connections.Remove(data.PortalGuid);
            _graph.Connections.Remove(data.PortalGuid);
        }

        /// <summary>
        /// Updates one half of a broken connection and creates a new one for the other direction in the data, then loads that new connection
        /// </summary>
        /// <param name="newTargetNode"></param>
        private void RetargetBrokenConnection(SpatialCellMap.NodeData newTargetNode)
        {
            Undo.RecordObject(_graph, "Retarget Connection");

            // the broken part of the connection needs to be deleted
            DeleteHalfConnection(_graph.Connections[_brokenConnectionGuid]);

            // set up replacement of the broken part at the new target (we need a new portal for that)
            Guid portalGuid = Guid.NewGuid();
            AddHalfConnection(newTargetNode, _floatingConnectionStartNode, portalGuid, _keptConnectionGuid);

            // update the part of the connection that was not broken
            SpatialCellMap.ConnectionData keptConnectionData = _graph.Connections[_keptConnectionGuid];
            keptConnectionData.TargetCellGuid = newTargetNode.CellGuid;
            keptConnectionData.ReversePortalGuid = portalGuid;

            LoadConnectionVisuals(keptConnectionData);
            ExitConnectionMode(false);

            // retarget the portal in the scene
            PortalCreationUtility.RetargetPortal(keptConnectionData.PortalGuid.Value, keptConnectionData.TargetCellGuid.Value, keptConnectionData.ReversePortalGuid.Value);

            Undo.SetCurrentGroupName("Retarget Connection");

            if (_editingCell != null)
            {
                CellVisibilityManager.StopIsolation();
                CellVisibilityManager.IsolateCell(_editingCell.CellGuid.Value);
                UpdatePortalHighlight();
            }
        }

        #endregion

        #endregion

        #region VISUALS

        /// <summary>
        /// Adds the visuals for the provided node
        /// </summary>
        /// <param name="nodeData"></param>
        private NodeView LoadNodeVisuals(SpatialCellMap.NodeData nodeData)
        {
            NodeView node = new NodeView(nodeData, this);

            // register to events
            node.OnConnectionButtonClicked += OnNodeConnectionButton;
            node.OnNodeClicked += OnNodeClicked;
            node.OnSelectedStateChanged += OnNodeSelectedStateChanged;

            // if a cell is edited in the scene, we need to listen to the color changed event
            if (_editingCell != null)
                node.OnColorChanged += OnNodeColorChanged;

            _nodeContainer.Add(node);
            _nodeViews.Add(node.Guid, node);

            return node;
        }

        /// <summary>
        /// Displays the start node marker at the start node
        /// </summary>
        private void LoadStartNodeMarker()
        {
            NodeView startNode = _nodeViews[_graph.StartCellGuid.Value];
            _startNodeMarker.RemoveFromHierarchy();
            startNode.Add(_startNodeMarker);
            _startNodeMarker.BringToFront();
        }

        /// <summary>
        /// Adds the visuals for the provided Connection
        /// </summary>
        /// <param name="connectionData"></param>
        private void LoadConnectionVisuals(SpatialCellMap.ConnectionData connectionData)
        {
            // if we already loaded the reverse direction of the requested connection we do not load this one
            for (int i = 0; i < _connections.Count; ++i)
            {
                if (_connections[i].IsGuid(connectionData.PortalGuid.Value))
                    return;
            }

            Connection connection = new Connection();

            _connections.Add(connection);
            _connectionContainer.Add(connection);

            connection.AttatchStart(_nodeViews[connectionData.StartCellGuid.Value], connectionData.PortalGuid.Value);
            connection.AttatchEnd(_nodeViews[connectionData.TargetCellGuid.Value], connectionData.ReversePortalGuid.Value);

            // registert to events
            connection.OnDelete += DeleteConnection;
            connection.OnBreak += StartRetargetConnection;
        }

        /// <summary>
        /// Removes all nodes and connections from the viewport
        /// </summary>
        private void ClearWorkspace()
        {
            // make sure we are not in conenction mode anymore
            if (CreatingConnection)
                ExitConnectionMode(true);

            // make sure we are not in start node selection mode anymore
            if (_startNodeSelectionModeActive)
                ExitStartNodeSelectionMode();

            // remove connections
            for (int i = 0; i < _connections.Count; ++i)
            {
                _connectionContainer.Remove(_connections[i]);
            }

            _connections.Clear();

            // remove nodes
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                _nodeContainer.Remove(pair.Value);
            }

            _nodeViews.Clear();
            _selectedNodes.Clear();
        }

        #endregion

        #region INTERACTION

        /// <summary>
        /// Starts a new floating connection
        /// </summary>
        /// <param name="nodeGuid"></param>
        private void EnterConnectionMode(Guid nodeGuid)
        {
            NodeView nodeView = _nodeViews[nodeGuid];

            // attatch the floating connection to the node that was clicked
            _floatingConnection.visible = true;
            _floatingConnection.BringToFront();
            _floatingConnection.AttatchStart(nodeView, Guid.Empty);

            _floatingConnectionStartNode = _graph.Nodes[nodeGuid];

            // register to mouse move event so the floating connection is updated every time the mouse is moved
            RegisterCallback<MouseMoveEvent>(UpdateFloatingConnection);


            // disable interactions
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                pair.Value.SetContentInteraction(false);
            }

            for (int i = 0; i < _connections.Count; ++i)
            {
                _connections[i].SetDeleteInteractable(false);
                _connections[i].SetBreakInteractable(false);
            }

            QuantumVRLogger.EditorLogVerbose($"Started new Connection from {_floatingConnectionStartNode.Name}");
            MarkDirtyRepaint();
        }

        private void ExitConnectionMode(bool failed)
        {
            // if we were retargeting a connection and it failed, reenable the old connection that was broken
            if (_floatingConnectionWasBroken && failed)
            {
                QuantumVRLogger.EditorLogVerbose("Aborting Connection Retarget...");
                _connections.Add(_oldBrokenConnection);
                _connectionContainer.Add(_oldBrokenConnection);
            }

            // reset values
            _floatingConnectionWasBroken = false;
            _floatingConnectionStartNode = null;
            _floatingConnection.visible = false;

            UnregisterCallback<MouseMoveEvent>(UpdateFloatingConnection);

            // enable interactions
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                pair.Value.SetContentInteraction(true);
            }

            for (int i = 0; i < _connections.Count; ++i)
            {
                _connections[i].SetDeleteInteractable(true);
                _connections[i].SetBreakInteractable(true);
            }

            QuantumVRLogger.EditorLogVerbose("Exited Connection Creation");

            MarkDirtyRepaint();
        }

        public void EnterStartNodeSelectionMode()
        {
            // make sure we are not in connection mode
            if (CreatingConnection)
                ExitConnectionMode(true);

            // disable interactions
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                pair.Value.SetConnectionInteraction(false);
                pair.Value.SetContentInteraction(false);
            }

            for (int i = 0; i < _connections.Count; ++i)
            {
                _connections[i].SetDeleteInteractable(false);
                _connections[i].SetBreakInteractable(false);
            }

            _startNodeSelectionModeActive = true;

            QuantumVRLogger.EditorLogVerbose("Entered Start Selection Mode");
        }

        public void ExitStartNodeSelectionMode()
        {
            // enable interactions
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                pair.Value.SetConnectionInteraction(true);
                pair.Value.SetContentInteraction(true);
            }

            for (int i = 0; i < _connections.Count; ++i)
            {
                _connections[i].SetDeleteInteractable(true);
                _connections[i].SetBreakInteractable(true);
            }

            _startNodeSelectionModeActive = false;
            OnStartNodeSelectionModeExit?.Invoke();

            QuantumVRLogger.EditorLogVerbose("Exited Start Selection Mode");
        }

        private void DeleteSelectedNodes()
        {
            QuantumVRLogger.EditorLogVerbose($"Deleting {_selectedNodes.Count} Nodes...");
            for (int i = 0; i < _selectedNodes.Count; ++i)
            {
                DeleteNode(_graph.Nodes[_selectedNodes[i].Guid]);
            }

            _selectedNodes.Clear();
        }

        /// <summary>
        /// Deselects all selected nodes
        /// </summary>
        private void ClearNodeSelection()
        {
            for (int i = 0; i < _selectedNodes.Count; ++i)
            {
                _selectedNodes[i].SetSelected(false, false, false);
            }

            _selectedNodes.Clear();

            QuantumVRLogger.EditorLogVerbose("Cleared Node Selection");
        }

        #endregion

        #region SCENE_MANIPULATION

        /// <summary>
        /// Called by nodes that want to be edited in the scene
        /// </summary>
        /// <param name="nodeData"></param>
        public void RequestEditNodeInScene(SpatialCellMap.NodeData nodeData)
        {
            CellVisibilityManager.StopIsolation();

            // if another cell was edited, stop its edit mode
            if (_editingCell != null && _editingCell != nodeData)
            {
                _nodeViews[_editingCell.CellGuid.Value].EditingInScene = false;
                QuantumVRLogger.EditorLogVerbose($"Stopped Editing of {nodeData.Name}");
            }

            // isolate cell of the node in the scene
            CellVisibilityManager.IsolateCell(nodeData.CellGuid.Value);

            NodeView nodeView = _nodeViews[nodeData.CellGuid.Value];
            nodeView.EditingInScene = true;

            // register to color changed event of all other nodes so portal highlighting can be updated
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                if (pair.Value == nodeView)
                    continue;

                pair.Value.OnColorChanged += OnNodeColorChanged;
            }

            _editingCell = nodeData;
        }

        /// <summary>
        /// Called by nodes that want to stop being edited in the scene
        /// </summary>
        /// <param name="nodeData"></param>
        public void StopEditingNodeInScene(SpatialCellMap.NodeData nodeData)
        {
            // if that was not the cell that is edited, do nothing
            if (_editingCell != nodeData)
            {
                QuantumVRLogger.EditorLogWarning($"Tried to stop editing {nodeData.Name} even though it is not in edit mode currently!");
                return;
            }

            CellVisibilityManager.StopIsolation();
            NodeView nodeView = _nodeViews[nodeData.CellGuid.Value];
            nodeView.EditingInScene = false;

            // now we don't need to listen to the color changed event anymore
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                if (pair.Value == nodeView)
                    continue;

                pair.Value.OnColorChanged -= OnNodeColorChanged;
            }

            _editingCell = null;
        }

        private void UpdatePortalHighlight()
        {
            PortalHighlightingManager.ClearHighlight();
            PortalHighlightingManager.HighlightCellPortals(_editingCell.CellGuid.Value);
        }

        #endregion

        #endregion


        // ######################## UTILITIES ######################## //

        #region UTILITIES

        /// <summary>
        /// Returns TRUE only if the provided two nodes can be connected
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="message">A message containing more information on the validation</param>
        /// <returns></returns>
        private bool ValidateConnection(SpatialCellMap.NodeData node1, SpatialCellMap.NodeData node2, out string message)
        {
            for (int i = 0; i < node1.Connections.Count; ++i)
            {
                if (_graph.Connections[node1.Connections[i]].TargetCellGuid == node2.CellGuid)
                {
                    message = "These Cells are already connected!";
                    return false;
                }
            }

            message = "Connection valid";
            return true;
        }

        private NodeView GetNodeUnderPosition(Vector2 worldPos)
        {
            foreach (KeyValuePair<Guid, NodeView> pair in _nodeViews)
            {
                NodeView nodeView = pair.Value;
                if (nodeView.ContainsPoint(nodeView.WorldToLocal(worldPos)))
                    return nodeView;
            }

            return null;
        }

        private Connection GetConnectionUnderPosition(Vector2 worldPos)
        {
            for (int i = 0; i < _connections.Count; ++i)
            {
                if (_connections[i].ContainsPoint(_connections[i].WorldToLocal(worldPos)))
                    return _connections[i];
            }

            return null;
        }

        private void SetStartCellIfNoneSet()
        {
            // if the start cell guid is not an empty guid, we are fine, do nothing
            if (_graph.StartCellGuid.Value != Guid.Empty)
                return;

            QuantumVRLogger.EditorLog("There is no start cell set in the Spatial Cell Map of this Scene, for now a start cell will be set for you");

            // get the first node in the dictionary
            Dictionary<SerializableGuid, SpatialCellMap.NodeData>.Enumerator enumerator = _graph.Nodes.GetEnumerator();
            enumerator.MoveNext();

            // set that as start cell
            SetGraphDirty();
            SetStartCell(enumerator.Current.Key.Value);

            // clean up
            enumerator.Dispose();
        }

        public void SetGraphDirtyWithUndo(string message)
        {
            Undo.RegisterCompleteObjectUndo(_graph, message);
            EditorUtility.SetDirty(_graph);
        }

        public void SetGraphDirty()
        {
            EditorUtility.SetDirty(_graph);
        }

        #endregion
    }
}