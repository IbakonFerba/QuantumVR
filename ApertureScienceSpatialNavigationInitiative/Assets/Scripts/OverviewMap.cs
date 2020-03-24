using System;
using System.Collections;
using System.Collections.Generic;
using FK.QuantumVR;
using FK.Utility;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class OverviewMap : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	[System.Serializable]
	public struct Room
	{
		public SerializableGuid RoomGuid;
		public RectTransform MapRepresentation;
		public ColorButton Button;
		public Image ButtonRepresentation;
	}
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private Transform _playerCamera;
	[SerializeField] private RectTransform _playerMarker;

	[SerializeField] private float _realRoomSize = 3.7f;
	
	[SerializeField] private Room[] _rooms;


	// ######################## PUBLIC VARS ######################## //
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private Dictionary<Guid, RectTransform> _roomRepresentations = new Dictionary<Guid, RectTransform>();

	private RectTransform _currentRoom;
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		SpatialCellManager.Instance.PostLayermaskUpdate += OnRoomChange;

		foreach (Room room in _rooms)
		{
			_roomRepresentations.Add(room.RoomGuid.Value, room.MapRepresentation);
			if (!room.Button)
			{
				room.ButtonRepresentation.gameObject.SetActive(false);
				continue;
			}

			room.ButtonRepresentation.color = GameController.Instance.Colors[room.Button.ColorIndex];
		}
		
		OnRoomChange();
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
	}
		
	private void Update () {
		_playerMarker.anchoredPosition = new Vector2(_currentRoom.sizeDelta.x * (_playerCamera.position.x/_realRoomSize), _currentRoom.sizeDelta.y * (_playerCamera.position.z/_realRoomSize));
		_playerMarker.localRotation = Quaternion.Euler(0,0,-_playerCamera.rotation.eulerAngles.y);
	}
	
	
	// ######################## FUNCTIONALITY ######################## //
	private void OnRoomChange()
	{
		_currentRoom = _roomRepresentations[SpatialCellManager.Instance.ActiveCell];
		_playerMarker.SetParent(_currentRoom);
	}
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
