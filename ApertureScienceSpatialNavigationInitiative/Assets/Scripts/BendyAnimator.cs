using System.Collections;
using System.Collections.Generic;
using FK.Utility.MathExtensions;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class BendyAnimator : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private Transform _arm;
	[SerializeField] private float _minRot;
	[SerializeField] private float _maxRot;
	[SerializeField] private float _speed = 1;
	
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
		_arm.localRotation = Quaternion.AngleAxis(Mathf.Sin(Time.time*_speed).Remap(-1,1,_minRot,_maxRot), Vector3.forward);
	}
	
	
	// ######################## FUNCTIONALITY ######################## //
	
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
