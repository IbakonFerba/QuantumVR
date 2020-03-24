using System.Collections;
using System.Collections.Generic;
using FK.QuantumVR;
using FK.QuantumVR.Objects;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class Grabbable : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //

	
	// ######################## PUBLIC VARS ######################## //
	public bool CanBeGrabbed;
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private Rigidbody _rigidbody;
	private CompressedSpaceEntity _compressedSpaceEntity;
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_compressedSpaceEntity = GetComponent<CompressedSpaceEntity>();
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
	}
		
	private void Update () {
		
	}
	
	
	// ######################## FUNCTIONALITY ######################## //
	public void Grab(Transform grabbingHand)
	{
		if(!CanBeGrabbed)
			return;
		
		_rigidbody.isKinematic = true;
		transform.parent = grabbingHand;
		_compressedSpaceEntity.IsPlayerPart = true;
	}

	public void Release()
	{
		_rigidbody.isKinematic = false;
		_compressedSpaceEntity.IsPlayerPart = false;
		transform.parent = _compressedSpaceEntity.SpatialCell.transform;
		
	}
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
