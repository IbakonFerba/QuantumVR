using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FK.QuantumVR;
using FK.Utility;
using FK.Utility.Binary;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class TestObject : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //

	
	// ######################## PUBLIC VARS ######################## //
	public bool canChange = false;
	public SerializableGuid ChangeGuid;


	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private Renderer _renderer;
	private Material[] _originalMaterials;
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Awake () {
		Init();
	}
		
	private void Update () {
		if(Input.GetKeyDown(KeyCode.X) && canChange)
			SpatialCellManager.Instance.ChangeActiveCell(ChangeGuid.Value);
	}
	
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
//		_renderer = GetComponent<Renderer>();
//		_originalMaterials = new Material[_renderer.sharedMaterials.Length];
//		for (int i = 0; i < _originalMaterials.Length; ++i)
//		{
//			_originalMaterials[i] = _renderer.sharedMaterials[i];
//		}
//		transform.parent.GetComponent<SpatialCell>().OnUpdateLayermaskEvent += (layermask, isActiveCell) => { MaterialManager.SetCompressedSpaceObjectMaterials(GetComponent<Renderer>(), layermask, isActiveCell, _originalMaterials);};
	}
	
	
	
	// ######################## FUNCTIONALITY ######################## //
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
