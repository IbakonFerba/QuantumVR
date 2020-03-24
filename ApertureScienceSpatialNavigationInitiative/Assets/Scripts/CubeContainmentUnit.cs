using System;
using System.Collections;
using System.Collections.Generic;
using FK.MessageManager;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class CubeContainmentUnit : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private Transform _cubeSnapPoint;
	[SerializeField] private AudioSource _audioSource;

	
	// ######################## PUBLIC VARS ######################## //
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init() 
	{
		
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
	}
		
	private void Update () {
		
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Cube"))
			return;

		Grabbable grabbable = other.GetComponent<Grabbable>();
		grabbable.CanBeGrabbed = false;
		grabbable.GetComponent<Rigidbody>().isKinematic = true;
		grabbable.transform.parent = _cubeSnapPoint;
		grabbable.transform.position = _cubeSnapPoint.position;
		grabbable.transform.rotation = _cubeSnapPoint.rotation;
		
		_audioSource.Play();
		MessageManager.Post((int)MessageTypes.CUBE_DELIVERED);
	}


	// ######################## FUNCTIONALITY ######################## //
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
