using System;
using FK.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor.UIElements
{
    /// <summary>
    /// <para>A Connection between two nodes of the QuantumVR Node Editor</para>
    ///
    /// v0.2 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public class Connection : VisualElement
    {
        // ######################## ENUMS & DELEGATES ######################## //

        #region ENUMS & DELEGATES

        /// <summary>
        /// Delegate for the event when a break button is pressed
        /// </summary>
        /// <param name="connection">The connection on which the button was pressed</param>
        /// <param name="fixedNode">The node the connection is still conencted to</param>
        /// <param name="detachedNode">The node the connection was detached from</param>
        /// <param name="fixedGuid">The guid of the one way connection (portal) that stays</param>
        /// <param name="detachedGuid">The guid of the one way connection (portal) that was detached</param>
        public delegate void BreakConnectionDelegate(Connection connection, NodeView fixedNode, NodeView detachedNode, Guid fixedGuid, Guid detachedGuid, Vector2 mouseWorldPos);

        #endregion


        // ######################## EVENTS ######################## //

        #region EVENTS

        /// <summary>
        /// Called when the delete button is pressed
        /// </summary>
        public event Action<Connection> OnDelete;

        /// <summary>
        /// Called when one of the break buttons is pressed
        /// </summary>
        public event BreakConnectionDelegate OnBreak;

        #endregion


        // ######################## PROPERTIES ######################## //

        #region PROPERTIES

        public NodeView StartNode => _startElement as NodeView;
        public NodeView EndNode => _endElement as NodeView;

        #endregion


        // ######################## PUBLIC VARS ######################## //

        #region PUBLIC VARS

        /// <summary>
        /// The guid of the connection from the start node to the end node
        /// </summary>
        public Guid StartToEndGuid;

        /// <summary>
        /// The guid of the connection from the end node to the start node
        /// </summary>
        public Guid EndToStartGuid;

        #endregion


        // ######################## PRIVATE VARS ######################## //

        #region PRIVATE VARS

        #region CONSTANTS

        private const string CUSTOM_BUTTON_CLASS = "custom-button";
        private const string DETACH_BUTTON_CLASS = "connection-detach-button";
        private const string DELETE_BUTTON_CLASS = "connection-delete-button";
        private const string SMALL_BUTTON_CLASS = "square-button-small";
        private const string BIG_BUTTON_CLASS = "square-button-big";

        private const string FLOATING_CONNECTION_HELPER_NAME = "floatingEndTarget";

        #endregion
        
        #region USS_PROPERTIES

        private static readonly CustomStyleProperty<int> _line_width_property = new CustomStyleProperty<int>("--line-width");
        private static readonly CustomStyleProperty<Color> _line_add_color_property = new CustomStyleProperty<Color>("--line-add-color");
        private static readonly CustomStyleProperty<float> _line_opacity_property = new CustomStyleProperty<float>("--line-opacity");
        private static readonly CustomStyleProperty<float> _line_soft_edge_fraction = new CustomStyleProperty<float>("--line-soft-edge-fraction");


        #endregion

        private VisualElement _startElement;
        private VisualElement _endElement;

        #region CHILD_ELEMENTS

        private readonly Line _line;

        private readonly Button _deleteButton;
        private readonly Button _detatchStartButton;
        private readonly Button _detatchEndButton;

        #endregion

        #endregion


        // ######################## INITS ######################## //

        #region CONSTRUCTORS

        public Connection()
        {
            // load styles
            styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
            AddToClassList(UssClasses.FLOAT);
            AddToClassList("connection");

            // create the line
            _line = new Line();
            _line.AddToClassList("connection-line");
            Add(_line);

            // add detatch start button
            _detatchStartButton = new Button {name = "detatchStart"};
            _detatchStartButton.AddToClassList(UssClasses.FLOAT);
            _detatchStartButton.AddToClassList(CUSTOM_BUTTON_CLASS);
            _detatchStartButton.AddToClassList(DETACH_BUTTON_CLASS);
            _detatchStartButton.AddToClassList(SMALL_BUTTON_CLASS);
            Add(_detatchStartButton);

            // add detatch end button
            _detatchEndButton = new Button {name = "detatchEnd"};
            _detatchEndButton.AddToClassList(UssClasses.FLOAT);
            _detatchEndButton.AddToClassList(CUSTOM_BUTTON_CLASS);
            _detatchEndButton.AddToClassList(DETACH_BUTTON_CLASS);
            _detatchEndButton.AddToClassList(SMALL_BUTTON_CLASS);
            Add(_detatchEndButton);

            // add delete button
            _deleteButton = new Button {name = "delete"};
            _deleteButton.AddToClassList(UssClasses.FLOAT);
            _deleteButton.AddToClassList(CUSTOM_BUTTON_CLASS);
            _deleteButton.AddToClassList(DELETE_BUTTON_CLASS);
            _deleteButton.AddToClassList(BIG_BUTTON_CLASS);
            Add(_deleteButton);

            // register callbacks
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            _detatchStartButton.clickable.clicked += () =>
                OnBreak?.Invoke(this, EndNode, StartNode, EndToStartGuid, StartToEndGuid, _detatchStartButton.LocalToWorld(_detatchStartButton.clickable.lastMousePosition));
            _detatchEndButton.clickable.clicked += () =>
                OnBreak?.Invoke(this, StartNode, EndNode, StartToEndGuid, EndToStartGuid, _detatchEndButton.LocalToWorld(_detatchEndButton.clickable.lastMousePosition));
            _deleteButton.clickable.clicked += () => OnDelete?.Invoke(this);

            // we need to update our display once the buttons geometry changed the first time due to how Visual elements set themselves up. We will only react once to this event
            _deleteButton.RegisterCallback<GeometryChangedEvent>(InitialUpdate);
        }

        #endregion


        #region INITS

        /// <summary>
        /// Called one time after the buttons finished setting up their geometry
        /// </summary>
        /// <param name="evt"></param>
        void InitialUpdate(GeometryChangedEvent evt)
        {
            // immediately unregister
            ((VisualElement) evt.target).UnregisterCallback<GeometryChangedEvent>(InitialUpdate);

            // update only if we are connected to something
            if (_startElement != null)
                Update();
        }

        #endregion


        // ######################## FUNCTIONALITY ######################## //

        #region FUNCTIONALITY

        #region EVENT HANDLING

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle styles = evt.customStyle;

            if (styles.TryGetValue(_line_width_property, out int value))
                _line.Width = value;
            if (styles.TryGetValue(_line_add_color_property, out Color tintColor))
                _line.AddColor = tintColor;
            if (styles.TryGetValue(_line_opacity_property, out float opacity))
                _line.Opacity = opacity;
            if (styles.TryGetValue(_line_soft_edge_fraction, out float softEdgeFraction))
                _line.SoftEdgeFraction = softEdgeFraction;
        }
        
        /// <summary>
        /// Called when the attachment points change their transform
        /// </summary>
        /// <param name="evt"></param>
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            VisualElement target = evt.target as VisualElement;
            if (target == null)
                return;

            // only update if the target is either our start or end attachment
            if (target == _startElement || target == _endElement)
                Update();
        }

        private void OnNodeColorChanged(NodeView node, Color newColor)
        {
            if (node == StartNode)
                _line.StartColor = newColor;
            else if (node == EndNode)
                _line.EndColor = newColor;
        }

        #endregion

        /// <summary>
        /// Updates the visuals of the connection
        /// </summary>
        private void Update()
        {
            _line.Start = GetTargetIntersectionPoint(_startElement, _endElement);
            _line.End = GetTargetIntersectionPoint(_endElement, _startElement);

            PositionButtons();
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Attatches the start of this connection to the provided Node
        /// </summary>
        /// <param name="target"></param>
        /// <param name="startToEndGuid"></param>
        public void AttatchStart(NodeView target, Guid startToEndGuid)
        {
            // make sure any old callbacks are removed
            if (_startElement != null)
            {
                _startElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                StartNode.OnColorChanged -= OnNodeColorChanged;
            }

            if (_endElement != null)
            {
                _endElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                if (EndNode != null)
                {
                    EndNode.OnColorChanged -= OnNodeColorChanged;
                }
            }

            _startElement = target;

            // add a temporary end element for floating connections if there isn't already one
            if (_endElement?.name != FLOATING_CONNECTION_HELPER_NAME)
            {
                _endElement = new VisualElement() {name = FLOATING_CONNECTION_HELPER_NAME};
                hierarchy.Add(_endElement);
            }

            _endElement.RegisterCallback<GeometryChangedEvent>(InitialUpdate);

            // draw the line with only one color
            _line.StartColor = target.Color;
            _line.EndColor = target.Color;

            StartToEndGuid = startToEndGuid;
            EndToStartGuid = Guid.Empty;

            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            target.OnColorChanged += OnNodeColorChanged;

            // as long as the connection is floating we don't want the buttons to be visible
            SetButtonsVisible(false);
            Update();
        }


        /// <summary>
        /// Attatches the end of this connection to the provided Node
        /// </summary>
        /// <param name="target"></param>
        /// <param name="endToStartGuid"></param>
        public void AttatchEnd(NodeView target, Guid endToStartGuid)
        {
            // we don't need the helper end element anymore
            if (_endElement?.name == FLOATING_CONNECTION_HELPER_NAME)
                hierarchy.Remove(_endElement);

            _endElement = target;

            _line.EndColor = target.Color;
            EndToStartGuid = endToStartGuid;

            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            target.OnColorChanged += OnNodeColorChanged;

            // now we want the buttons to be visible
            SetButtonsVisible(true);
            Update();
        }

        /// <summary>
        /// Updates the end point of the connection to the provided world space position
        /// </summary>
        /// <param name="worldPosEndPoint"></param>
        public void UpdateFloatingConnection(Vector2 worldPosEndPoint)
        {
            Vector2 endPoint = this.WorldToLocal(worldPosEndPoint);
            _endElement.style.left = endPoint.x;
            _endElement.style.top = endPoint.y;
            Update();
        }

        /// <summary>
        /// Sets the interactable state of the delete button
        /// </summary>
        /// <param name="interactable"></param>
        public void SetDeleteInteractable(bool interactable)
        {
            _deleteButton.SetEnabled(interactable);
        }

        /// <summary>
        /// Sets the interactable state of the break buttons
        /// </summary>
        /// <param name="interactable"></param>
        public void SetBreakInteractable(bool interactable)
        {
            _detatchStartButton.SetEnabled(interactable);
            _detatchEndButton.SetEnabled(interactable);
        }

        /// <summary>
        /// Returns true if either of the buttons or the line contains the provided point in local space
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return _line.ContainsPoint(this.ChangeCoordinatesTo(_line, localPoint)) || _deleteButton.ContainsPoint(this.ChangeCoordinatesTo(_deleteButton, localPoint)) ||
                   _detatchStartButton.ContainsPoint(this.ChangeCoordinatesTo(_detatchStartButton, localPoint)) ||
                   _detatchEndButton.ContainsPoint(this.ChangeCoordinatesTo(_detatchEndButton, localPoint));
        }

        #endregion


        // ######################## UTILITIES ######################## //

        #region UTILITES

        /// <summary>
        /// Returns a point at a percentage position along the line seen from the start point (0 is the start point and 1 is the end point)
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 GetPointAlongLine(float position)
        {
            return Vector2.Lerp(_line.Start, _line.End, position);
        }

        /// <summary>
        /// Sets the center position of a visual element at a percentage position along the line
        /// </summary>
        /// <param name="element"></param>
        /// <param name="position"></param>
        private void SetPositionAlongLine(VisualElement element, float position)
        {
            Vector2 positionAlongLine = GetPointAlongLine(position);
            element.style.left = positionAlongLine.x - element.layout.width * 0.5f;
            element.style.top = positionAlongLine.y - element.layout.height * 0.5f;
        }

        /// <summary>
        /// Returns the point of intersection between the line and one of its attachments
        /// </summary>
        /// <param name="target">The attachment point that the intersection point should be calculated for</param>
        /// <param name="otherElement">The other attachment point</param>
        /// <returns></returns>
        private Vector2 GetTargetIntersectionPoint(VisualElement target, VisualElement otherElement)
        {
            Vector2 point = new Vector2();
            Vector2 start = target.layout.center;
            Vector2 end = otherElement.layout.center;

            float slope = (start.y - end.y) / (start.x - end.x);
            float rectHeight = target.layout.height;
            float rectWidth = target.layout.width;

            if (-rectHeight * 0.5f <= slope * rectWidth * 0.5f && slope * rectWidth * 0.5f <= rectHeight * 0.5f)
            {
                if (start.x > end.x)
                {
                    point.x = start.x - rectWidth * 0.5f;
                    point.y = start.y - slope * rectWidth * 0.5f;
                }
                else if (start.x < end.x)
                {
                    point.x = start.x + rectWidth * 0.5f;
                    point.y = start.y + slope * rectWidth * 0.5f;
                }
            }
            else if (-rectWidth * 0.5f <= (rectHeight * 0.5f) / slope && (rectHeight * 0.5f) / slope <= rectWidth * 0.5f)
            {
                if (start.y > end.y)
                {
                    point.y = start.y - rectHeight * 0.5f;
                    point.x = start.x - (rectHeight * 0.5f) / slope;
                }
                else if (start.y < end.y)
                {
                    point.y = start.y + rectHeight * 0.5f;
                    point.x = start.x + (rectHeight * 0.5f) / slope;
                }
            }

            return point;
        }

        private void SetButtonsVisible(bool setVisible)
        {
            _deleteButton.visible = setVisible;
            _detatchStartButton.visible = setVisible;
            _detatchEndButton.visible = setVisible;
        }

        private void PositionButtons()
        {
            SetPositionAlongLine(_deleteButton, 0.5f);
            SetPositionAlongLine(_detatchStartButton, 0.2f);
            SetPositionAlongLine(_detatchEndButton, 0.8f);
        }

        /// <summary>
        /// Returns true if either the start Guid or the End Guid equals the provided guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool IsGuid(Guid guid)
        {
            return StartToEndGuid == guid || EndToStartGuid == guid;
        }

        #endregion
    }
}