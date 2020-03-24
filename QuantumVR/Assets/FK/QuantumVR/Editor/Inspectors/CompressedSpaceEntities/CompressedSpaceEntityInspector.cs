using System;
using FK.QuantumVR.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FK.QuantumVR.Editor
{
    /// <summary>
    /// <para>Base inspector for compressed space entities</para>
    ///
    /// v1.2 01/2020
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CompressedSpaceEntity), true)]
    public class CompressedSpaceEntityInspector : UnityEditor.Editor
    {
        // ######################## PRIVATE VARS ######################## //

        #region PRIVATE VARS

        private TextElement _guidDisplay;
        private VisualElement _noColliderWarningContainer;
        private VisualElement _noColliderWarning;
        private TextElement _noColliderWarningText;

        #region CONSTANTS

        private const string NO_COLLIDER_WARNING_SINGLE =
            "This Object is not QuantumVR static but does not have a collider. This means that it will behave as if it were QuantumVR static and will not be able to pass through Portals. Consider giving it a collider!";

        private const string NO_COLLIDER_WARNING_MULTI =
            "One or more of the Objects edited is not QuantumVR static but does not have a collider. This means that it will behave as if it were QuantumVR static and will not be able to pass through Portals. Consider giving it a collider!";

        #endregion

        #endregion


        // ######################## UNITY EVENT FUNCTIONS ######################## //

        #region UNITY EVENT FUNCTIONS

        public override VisualElement CreateInspectorGUI()
        {
            // load uxml
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("CompressedSpaceEntityInspector_layout");
            VisualElement visualTree = visualTreeAsset.CloneTree();

            // load styles
            visualTree.styleSheets.Add(Resources.Load<StyleSheet>("CompressedSpaceEntityInspector_styles"));
            visualTree.styleSheets.Add(Resources.Load<StyleSheet>("Styles"));
            visualTree.styleSheets.Add(Resources.Load<StyleSheet>("QuantumVRInspectorsCommon_styles"));

            visualTree.Bind(serializedObject);

            // get guid display
            _guidDisplay = visualTree.Q<TextElement>("guidDisplay");
            _noColliderWarningContainer = visualTree.Q<VisualElement>("no-collider-warning-container");
            _noColliderWarning = visualTree.Q<VisualElement>("no-collider-warning");
            _noColliderWarningText = _noColliderWarning.Q<TextElement>();
            _noColliderWarning.RemoveFromHierarchy();

            PropertyField staticField = visualTree.Q<PropertyField>("staticField");
            staticField.RegisterCallback<ChangeEvent<bool>>(OnStaticChanged);

            PropertyField isPlayerPartField = visualTree.Q<PropertyField>("isPlayerPartField");
            isPlayerPartField.RegisterCallback<ChangeEvent<bool>>(OnIsPlayerPartChanged);


            return visualTree;
        }

        private void OnEnable()
        {
            EditorApplication.update += Udpate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Udpate;
        }

        protected virtual void Udpate()
        {
            if (serializedObject.targetObject == null)
                return;
            serializedObject.Update();

            // handle display of no collider waring
            if (_noColliderWarning != null)
            {
                SerializedProperty isStatic = serializedObject.FindProperty("Static");
                
                // we only need to check the collider if the entity is static
                if (!isStatic.boolValue)
                {
                    // check if any of the edited entites has no collider
                    bool hasCollider = true;
                    for (int i = 0; i < serializedObject.targetObjects.Length; ++i)
                    {
                        CompressedSpaceEntity entity = serializedObject.targetObjects[i] as CompressedSpaceEntity;
                        hasCollider = entity.gameObject.GetComponent<Collider>() != null;
                        if (!hasCollider)
                            break;
                    }

                    // if there is no collider display the message, else don't display it
                    if (!hasCollider && _noColliderWarning.parent == null)
                    {
                        _noColliderWarningContainer.Add(_noColliderWarning);
                        _noColliderWarningText.text = serializedObject.targetObjects.Length > 1 ? NO_COLLIDER_WARNING_MULTI : NO_COLLIDER_WARNING_SINGLE;
                    }
                    else if(hasCollider && _noColliderWarning.parent != null)
                    {
                        _noColliderWarning.RemoveFromHierarchy();
                    }
                }
                else if (_noColliderWarning.parent != null)
                    _noColliderWarning.RemoveFromHierarchy();
            }

            if (_guidDisplay == null)
                return;


            SerializedProperty cellGuid = serializedObject.FindProperty("_spatialCellGuid").FindPropertyRelative("_serializedGuid");
            _guidDisplay.text = !cellGuid.hasMultipleDifferentValues ? cellGuid.stringValue : "-";
        }

        #endregion

        // ######################## FUNCTIONALITY ######################## //

        #region FUNCTIONALITY

        private void OnStaticChanged(ChangeEvent<bool> evt)
        {
            ChangeChildrenProperty("Static", evt.newValue);
        }

        private void OnIsPlayerPartChanged(ChangeEvent<bool> evt)
        {
            ChangeChildrenProperty("IsPlayerPart", evt.newValue);
        }

        private void ChangeChildrenProperty(string propertyName, bool newValue)
        {
            for (int i = 0; i < serializedObject.targetObjects.Length; ++i)
            {
                CompressedSpaceEntity entity = serializedObject.targetObjects[i] as CompressedSpaceEntity;
                CompressedSpaceEntity[] childEntities = entity.GetComponentsInChildren<CompressedSpaceEntity>(true);
                if (childEntities.Length <= 1)
                    continue;

                if (!EditorUtility.DisplayDialog($"Change QuantumVR {propertyName}", $"Do you want to change the QuantumVR {propertyName} state for all the child objects as well?",
                    "Yes, change children", "No, this object only"))
                    continue;

                for (int j = 0; j < childEntities.Length; ++j)
                {
                    if (childEntities[j] is Portal || childEntities[j] == entity)
                        continue;

                    SerializedObject childEntitySerializedObject = new SerializedObject(childEntities[j]);
                    childEntitySerializedObject.FindProperty(propertyName).boolValue = newValue;
                    childEntitySerializedObject.ApplyModifiedProperties();
                }
            }
        }

        #endregion
    }
}