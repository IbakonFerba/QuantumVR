using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class SelectScene : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	#region STRUCTS & CLASSES
	
	#endregion
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	#region ENUMS & DELEGATES
	#endregion
	
	
	// ######################## EVENTS ######################## //
	#region EVENTS
	
	#endregion
	
	
	// ######################## PROPERTIES ######################## //
	#region PROPERTIES
	
	#endregion
	
	
	// ######################## EXPOSED VARS ######################## //
	#region EXPOSED VARS

	[SerializeField] private GameObject _sceneActivatorObject;
	[SerializeField] private Color _selectedColor = Color.cyan;
	[SerializeField] private bool _selectedAtStart = false;
	#endregion
	
	
	// ######################## PUBLIC VARS ######################## //
	#region PUBLIC VARS
	
	#endregion
	
	
	// ######################## PROTECTED VARS ######################## //
	#region PROTECTED VARS
	
	#endregion
	
	
	// ######################## PRIVATE VARS ######################## //
	#region PRIVATE VARS

	private Image _image;

	private static GameObject _activeButton;
	#endregion
	
	
	// ######################## INITS ######################## //
	#region CONSTRUCTORS
	
	#endregion

	#region INITS
	
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		Button button = GetComponent<Button>();
		button.onClick.AddListener(() => SetSceneActivatorActive(true));

		_image = button.image;

		SetSceneActivatorActive(_selectedAtStart);
	}
	
	#endregion
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	#region UNITY EVENT FUNCTIONS
	
	private void Start () {
		Init();
	}
		
	private void Update () {
		if(_activeButton != gameObject && _sceneActivatorObject.activeSelf)
			SetSceneActivatorActive(false);
	}
	
	#endregion
	
	
	// ######################## FUNCTIONALITY ######################## //
	#region FUNCTIONALITY

	private void SetSceneActivatorActive(bool active)
	{
		_sceneActivatorObject.SetActive(active);
		_image.color = active ? _selectedColor : Color.white;

		if (active)
			_activeButton = gameObject;
	}
	#endregion
	
	
	// ######################## COROUTINES ######################## //
	#region COROUTINES 
	
	#endregion
	
	
	// ######################## UTILITIES ######################## //
	#region UTILITIES
	
	#endregion		
}
