using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class PlayerStartTarget : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private int _sceneIndexToLoad;
	[SerializeField] private float _positionalTolerance = 0.2f;
	[SerializeField] private float _rotationalTolerance = 20f;

	
	// ######################## PUBLIC VARS ######################## //
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private Transform _playerCamera;
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		_playerCamera = Camera.main.transform;
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
	}
		
	private void Update ()
	{
		Vector3 relativeCamPosition = transform.InverseTransformPoint(_playerCamera.position);
		relativeCamPosition.y = 0;

		if(relativeCamPosition.magnitude > _positionalTolerance)
			return;

		Debug.Log("Position OK");
		if(Vector3.Angle(transform.forward, _playerCamera.forward) > _rotationalTolerance)
			return;
		
		SceneManager.LoadScene(_sceneIndexToLoad, LoadSceneMode.Single);
	}
	
	
	// ######################## FUNCTIONALITY ######################## //
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
