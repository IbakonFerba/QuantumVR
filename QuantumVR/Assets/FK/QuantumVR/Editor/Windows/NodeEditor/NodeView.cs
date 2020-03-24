using System;
using FK.QuantumVR.Editor.Tools;
using FK.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor.UIElements
{
    /// <summary>
    /// <para>The visual representation of a node for the QuantumVR node editor</para>
    ///
    /// v0.92 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class NodeView : VisualElement
    {
        // ######################## ENUMS & DELEGATES ######################## //

        #region ENUMS & DELEGATES

        public delegate void OnSelectedStateChangedDelegate(NodeView nodeView, bool selected, bool multiSelection);

        public delegate void OnDragDelegate(NodeView nodeView, Vector2 delta);

        #endregion


        // ######################## EVENTS ######################## //

        #region EVENTS

        public event Action<NodeView> OnNodeClicked;
        public event Action<NodeView> OnConnectionButtonClicked;

        /// <summary>
        /// Called whenever the node is selected or deselected
        /// </summary>
        public event OnSelectedStateChangedDelegate OnSelectedStateChanged;

        /// <summary>
        /// Called when a drag action is started
        /// </summary>
        public event Action<NodeView> OnDragStart;

        /// <summary>
        /// Called when a drag action ends
        /// </summary>
        public event Action<NodeView> OnDragEnd;

        /// <summary>
        /// Called each time a drag action updates
        /// </summary>
        public event OnDragDelegate OnDrag;

        public event Action<NodeView, Color> OnColorChanged;

        #endregion


        // ######################## PROPERTIES ######################## //

        #region PROPERTIES

        public override VisualElement contentContainer => _contentContainer;

        public Guid Guid => _data.CellGuid.Value;
        public Color Color => _data.Color;

        /// <summary>
        /// Is the Cell this node represents currently opened in edit mode in the scene?
        /// </summary>
        public bool EditingInScene
        {
            get => _editingInScene;
            set
            {
                _editingInScene = value;

                // change the button text
                _editButton.text = _editingInScene ? EDIT_BUTTON_TEXT_EDITING : EDIT_BUTTON_TEXT_NOT_EDITING;

                // if we are editing, we want this and only this node to be selected. It is not important for the logic, but it makes the visuals clearer
                if (_editingInScene)
                    Isolate();
            }
        }

        #endregion


        // ######################## PRIVATE VARS ######################## //

        #region PRIVATE VARS

        #region CONSTANTS

        private const string EDIT_BUTTON_TEXT_EDITING = "Stop Editing";
        private const string EDIT_BUTTON_TEXT_NOT_EDITING = "Edit in Scene";
        private const string SELECTED_CLASS = "selected";

        #endregion

        private readonly SpatialCellMap.NodeData _data;

        /// <summary>
        /// The graph view this node belongs to
        /// </summary>
        private readonly GraphView _graphView;


        private VisualElement _contentContainer;

        /// <summary>
        /// The element that is activated to show that the node is selected
        /// </summary>
        private VisualElement _selectionHighlight;

        /// <summary>
        /// This button is the access point for creating connections from or to this node
        /// </summary>
        private Button _connectionButton;

        #region CONTENT

        private TextField _nameField;
        private ColorButton _colorButton;
        private Button _editButton;

        private Color _contentBackgroundColor;

        #endregion

        #region FLAGS

        /// <summary>
        /// Is the Cell this node represents currently opened in edit mode in the scene?
        /// </summary>
        private bool _editingInScene;

        private bool _selected;

        #endregion

        #endregion


        // ######################## INITS ######################## //

        #region CONSTRUCTORS

        public NodeView(SpatialCellMap.NodeData data, GraphView graphView)
        {
            Init();

            _data = data;
            _graphView = graphView;

            LoadData();
        }

        #endregion

        #region INITS

        private void Init()
        {
            // load style sheeds
            styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
            styleSheets.Add(Resources.Load<StyleSheet>("NodeView_styles"));

            // set classes
            AddToClassList(UssClasses.FLOAT);
            AddToClassList("NodeView");

            // create the connection button
            _connectionButton = new Button {name = "connectionButton"};
            _connectionButton.AddToClassList(UssClasses.ROUNDED);
            _connectionButton.AddToClassList("connection-button");
            hierarchy.Add(_connectionButton);

            // create the selection highlight element
            _selectionHighlight = new VisualElement {name = "selectionHighlight"};
            _selectionHighlight.AddToClassList(UssClasses.ROUNDED);
            _selectionHighlight.AddToClassList("selection-highlight");
            hierarchy.Add(_selectionHighlight);
            _selectionHighlight.visible = false;

            // create the content container
            _contentContainer = new VisualElement {name = "content"};
            _contentContainer.AddToClassList(UssClasses.ROUNDED);
            _contentContainer.AddToClassList("node-content");
            hierarchy.Add(_contentContainer);

            // load the node content from xml
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("NodeViewContent_layout");
            visualTreeAsset.CloneTree(_contentContainer);

            // get the node content elements
            _nameField = _contentContainer.Q<TextField>("cellName");
            _colorButton = _contentContainer.Q<ColorButton>("colorButton");
            Button deleteButton = _contentContainer.Q<Button>("deleteButton");
            _editButton = _contentContainer.Q<Button>("editButton");

            // add manipulators
            this.AddManipulator(new DragManipulator(new ManipulatorActivationFilter {button = MouseButton.LeftMouse}, false, this, OnDragged, OnDragStarted, OnDragEnded));

            // add callbacks
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            _connectionButton.clickable.clicked += () => OnConnectionButtonClicked?.Invoke(this);

            // add content element callbacks
            _nameField.RegisterValueChangedCallback(evt =>
            {
                _graphView.SetGraphDirtyWithUndo("Change Cell Name");
                _data.Name = evt.newValue;

                SpatialCellCreationUtility.RenameSpatialCell(_data.CellGuid.Value, _data.Name);
            });
            _colorButton.RegisterValueChangedCallback(evt =>
            {
                _graphView.SetGraphDirtyWithUndo("Change Node Color");
                _data.Color = evt.newValue;
                _data.Color.a = 1;
                UpdateColor();
            });
            deleteButton.clickable.clicked += () => _graphView.DeleteNode(_data);
            _editButton.clickable.clicked += OnEditButtonPressed;

            // initialize edit mode by setting it to false
            EditingInScene = false;
        }

        private void LoadData()
        {
            style.left = _data.Position.x;
            style.top = _data.Position.y;

            _nameField.value = _data.Name;
            _colorButton.value = _data.Color;

            UpdateColor();
        }

        #endregion


        // ######################## FUNCTIONALITY ######################## //

        #region FUNCTIONALITY

        #region EVENT_HANDLING

        private void OnMouseDown(MouseDownEvent evt)
        {
            BringToFront();
            if (evt.shiftKey && _selected)
                SetSelected(false, evt.shiftKey);
            else
                SetSelected(true, evt.shiftKey);
            OnNodeClicked?.Invoke(this);
            evt.StopPropagation();
        }

        private void OnDragStarted(MouseDownEvent evt)
        {
            OnDragStart?.Invoke(this);
        }

        private void OnDragEnded(MouseUpEvent evt)
        {
            OnDragEnd?.Invoke(this);
        }

        private void OnDragged(Vector2 delta)
        {
            _data.Position = layout.position;
            _graphView.SetGraphDirty();

            OnDrag?.Invoke(this, delta);
        }

        private void OnEditButtonPressed()
        {
            if (_editingInScene)
                _graphView.StopEditingNodeInScene(_data);
            else
                _graphView.RequestEditNodeInScene(_data);
        }

        #endregion

        /// <summary>
        /// Sets whether the content of the node is interactable or not
        /// </summary>
        /// <param name="interactable"></param>
        public void SetContentInteraction(bool interactable)
        {
            _contentContainer.SetEnabled(interactable);
        }

        /// <summary>
        /// Sets whether the connection button of the node is interactable or not
        /// </summary>
        /// <param name="interactable"></param>
        public void SetConnectionInteraction(bool interactable)
        {
            _connectionButton.SetEnabled(interactable);
        }

        /// <summary>
        /// Sets the selection stat of the node.
        ///
        /// <para>Returns TRUE if the state changed and FALSE if it was already in the requested state</para>
        /// </summary>
        /// <param name="selected">Should the node be selected?</param>
        /// <param name="multiSelection">Is the call part of a multi selection? The node itself does not use this value, but it is passed into the OnSelectionStateChanged Event</param>
        /// <param name="sendEvent">Should the OnSelectionStateChanged event be sent?</param>
        /// <returns></returns>
        public bool SetSelected(bool selected, bool multiSelection = false, bool sendEvent = true)
        {
            // if we already are in the requested state do nothing
            if (selected == _selected)
                return false;
            
            QuantumVRLogger.EditorLogVerbose(selected ? $"Selected {_data.Name}" : $"Deselected {_data.Name}");

            _selected = selected;
            _selectionHighlight.visible = selected;

            // set classes
            if (_selected)
            {
                AddToClassList(SELECTED_CLASS);
                _contentContainer.AddToClassList(SELECTED_CLASS);
                _connectionButton.AddToClassList(SELECTED_CLASS);
                
                _contentBackgroundColor = _contentContainer.resolvedStyle.backgroundColor;
                _contentBackgroundColor.a = 1;
                SetSelectedContentColor();
            }
            else
            {
                RemoveFromClassList(SELECTED_CLASS);
                _contentContainer.RemoveFromClassList(SELECTED_CLASS);
                _connectionButton.RemoveFromClassList(SELECTED_CLASS);
                _contentContainer.style.backgroundColor = new StyleColor(StyleKeyword.Null);
            }

            // send event
            if (sendEvent)
                OnSelectedStateChanged?.Invoke(this, _selected, multiSelection);

            return true;
        }

        /// <summary>
        /// Selects this cell using a single selection
        /// </summary>
        public void Isolate()
        {
            // if the cell was already selected, we need to manually send the event as it will not be sent by SetSelected if the State doesn't change
            if (!SetSelected(true))
                OnSelectedStateChanged?.Invoke(this, _selected, false);
        }

        /// <summary>
        /// Moves the node by delta
        /// </summary>
        /// <param name="delta"></param>
        public void Move(Vector2 delta)
        {
            style.top = layout.yMin + delta.y;
            style.left = layout.xMin + delta.x;

            OnDragged(delta);
        }

        #endregion


        // ######################## UTILITIES ######################## //

        #region UTILITIES

        private void UpdateColor()
        {
            _connectionButton.style.backgroundColor = _data.Color;
            
            if(_selected)
                SetSelectedContentColor();
                
            OnColorChanged?.Invoke(this, _data.Color);
        }

        private void SetSelectedContentColor()
        {
            _contentContainer.style.backgroundColor = Color.Lerp(_contentBackgroundColor, _data.Color, 0.4f);
        }

        #endregion
    }
}