using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;


namespace FK.QuantumVR.Editor
{
	/// <summary>
	/// <para>Class Info</para>
	///
	/// v1.0 mm/20yy
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	[CustomEditor(typeof(FK.QuantumVR.SpatialCellMap))]
	public class SpatialCellMapInspector : UnityEditor.Editor
	{
		// ######################## STRUCTS & CLASSES ######################## //


		// ######################## ENUMS & DELEGATES ######################## //


		// ######################## PROPERTIES ######################## //


		// ######################## EXPOSED VARS ######################## //


		// ######################## PUBLIC VARS ######################## //


		// ######################## PROTECTED VARS ######################## //


		// ######################## PRIVATE VARS ######################## //
		private VisualTreeAsset _nodeListElementAsset;
		private VisualTreeAsset _nodeConnectionListElementAsset;
		private VisualTreeAsset _connectionListElementAsset;

		private TemplateContainer _visualTree;

		private string _nodeAddGuid;
		private string _connectionAddGuid;

		private VisualElement _nodeContainer;
		private VisualElement _connectionContainer;


		// ######################## UNITY EVENT FUNCTIONS ######################## //
		public override VisualElement CreateInspectorGUI()
		{
			VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Inspectors/SpatialCellMap/spatial_cell_map_inspector.uxml");
			_nodeListElementAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Inspectors/SpatialCellMap/node_inspector.uxml");
			_nodeConnectionListElementAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Inspectors/SpatialCellMap/node_connections_inspector.uxml");
			_connectionListElementAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FK/QuantumVR/Editor/Inspectors/SpatialCellMap/connection_inspector.uxml");
			
			_visualTree = visualTree.CloneTree();
			_visualTree.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/FK/QuantumVR/Editor/Inspectors/SpatialCellMap/spatial_cell_map_inspector_styles.uss"));

			_nodeContainer = _visualTree.Q<VisualElement>("node-list");
			_connectionContainer = _visualTree.Q<VisualElement>("connection-list");

			_visualTree.Q<TextField>("node-guid-field").RegisterValueChangedCallback(newValue => _nodeAddGuid = newValue.newValue);
			_visualTree.Q<TextField>("connection-guid-field").RegisterValueChangedCallback(newValue => _connectionAddGuid = newValue.newValue);

			_visualTree.Q<Button>("add-node-button").clickable.clicked += AddNode;
			_visualTree.Q<Button>("add-connection-button").clickable.clicked += AddConnection;
			
			LoadLists();
			return _visualTree;
		}
		
		


		// ######################## INITS ######################## //
		private void LoadLists()
		{
			SerializedProperty nodes = serializedObject.FindProperty("Nodes");
			SerializedProperty nodeKeys = nodes.FindPropertyRelative("_keys");
			SerializedProperty nodevalues = nodes.FindPropertyRelative("_values");
			
			
			SerializedProperty connections = serializedObject.FindProperty("Connections");
			SerializedProperty connectionKeys = connections.FindPropertyRelative("_keys");
			SerializedProperty connectionvalues = connections.FindPropertyRelative("_values");
			
			for (int i = 0; i < nodeKeys.arraySize; ++i)
			{
				AddNodeToListDisplay(nodevalues.GetArrayElementAtIndex(i), nodeKeys.GetArrayElementAtIndex(i));
			}
			
			for (int i = 0; i < connectionKeys.arraySize; ++i)
			{
				AddConnectionToListDisplay(connectionvalues.GetArrayElementAtIndex(i), connectionKeys.GetArrayElementAtIndex(i));
			}
		}

		// ######################## FUNCTIONALITY ######################## //
		private void AddNode()
		{
			SerializedProperty nodes = serializedObject.FindProperty("Nodes");
			AddDictionaryElement(nodes, _nodeAddGuid, "CellGuid", out SerializedProperty value, out SerializedProperty key);
			AddNodeToListDisplay(value, key);
		}
		
		private void AddConnection()
		{
			SerializedProperty connections = serializedObject.FindProperty("Connections");
			AddDictionaryElement(connections, _connectionAddGuid, "PortalGuid", out SerializedProperty value, out SerializedProperty key);
			AddConnectionToListDisplay(value,key);
		}

		private SerializedProperty AddDictionaryElement(SerializedProperty dictionary, string guid, string guidPropertyName, out SerializedProperty value, out SerializedProperty key)
		{
			SerializedProperty keys = dictionary.FindPropertyRelative("_keys");
			SerializedProperty values = dictionary.FindPropertyRelative("_values");
			keys.arraySize += 1;
			values.arraySize += 1;

			value = values.GetArrayElementAtIndex(keys.arraySize - 1);
			key = keys.GetArrayElementAtIndex(keys.arraySize - 1);
			key.FindPropertyRelative("_serializedGuid").stringValue = guid;
			value.FindPropertyRelative(guidPropertyName).FindPropertyRelative("_serializedGuid").stringValue = guid;
			serializedObject.ApplyModifiedProperties();

			
			return value;
		}

		private void AddNodeToListDisplay(SerializedProperty nodeData, SerializedProperty nodeKey)
		{
			TemplateContainer nodeVT = _nodeListElementAsset.CloneTree();
			nodeVT.Q<PropertyField>("cell-guid-field").bindingPath = nodeData.FindPropertyRelative("CellGuid").propertyPath;
			nodeVT.Q<Button>("delete-button").clickable.clicked += () =>
			{
				nodeVT.Unbind();
				_nodeContainer.Remove(nodeVT);
				nodeData.DeleteCommand();
				nodeKey.DeleteCommand();
				serializedObject.ApplyModifiedProperties();
			};

			VisualElement connectionsList = nodeVT.Q<VisualElement>("connections-list");
			nodeVT.Q<Button>("add-connection-button").clickable.clicked += () => AddConnectionToNode(nodeData, connectionsList);

			SerializedProperty connections = nodeData.FindPropertyRelative("Connections");
			for (int i = 0; i < connections.arraySize; ++i)
			{
				AddConnectionToNodeConnectionDisplay(connections.GetArrayElementAtIndex(i), connectionsList);
			}
			nodeVT.Bind(serializedObject);
			_nodeContainer.Add(nodeVT);
		}

		private void AddConnectionToNode(SerializedProperty nodeData,  VisualElement connectionsParent)
		{
			SerializedProperty connections = nodeData.FindPropertyRelative("Connections");
			connections.arraySize += 1;
			SerializedProperty connection = connections.GetArrayElementAtIndex(connections.arraySize - 1);
			connection.FindPropertyRelative("_serializedGuid").stringValue = Guid.Empty.ToString();
			AddConnectionToNodeConnectionDisplay(connection, connectionsParent);
			serializedObject.ApplyModifiedProperties();
		}

		private void AddConnectionToNodeConnectionDisplay(SerializedProperty connectionData, VisualElement parent)
		{
			TemplateContainer connectionVT = _nodeConnectionListElementAsset.CloneTree();
			connectionVT.Q<PropertyField>("connection-guid-field").bindingPath = connectionData.propertyPath;
			connectionVT.Q<Button>("delete-button").clickable.clicked += () =>
			{
				connectionVT.Unbind();
				parent.Remove(connectionVT);
				connectionData.DeleteCommand();
				serializedObject.ApplyModifiedProperties();
			};
			connectionVT.Bind(serializedObject);
			parent.Add(connectionVT);
		}
		
		private void AddConnectionToListDisplay(SerializedProperty connectionData, SerializedProperty connectionKey)
		{
			TemplateContainer connectionVT = _connectionListElementAsset.CloneTree();
			connectionVT.Q<PropertyField>("portal-guid-field").bindingPath = connectionData.FindPropertyRelative("PortalGuid").propertyPath;
			connectionVT.Q<PropertyField>("cell-guid-field").bindingPath = connectionData.FindPropertyRelative("TargetCellGuid").propertyPath;
			
			connectionVT.Q<Button>("delete-button").clickable.clicked += () =>
			{
				connectionVT.Unbind();
				_connectionContainer.Remove(connectionVT);
				connectionData.DeleteCommand();
				connectionKey.DeleteCommand();
				serializedObject.ApplyModifiedProperties();
			};
			
			connectionVT.Bind(serializedObject);
			_connectionContainer.Add(connectionVT);
		}

		// ######################## COROUTINES ######################## //


		// ######################## UTILITIES ######################## //

	}
}
