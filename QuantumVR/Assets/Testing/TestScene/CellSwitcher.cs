using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FK.QuantumVR;
using FK.Utility;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class CellSwitcher : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	[System.Serializable]
	public struct CellSwitchData
	{
		public string Key;
		public SerializableGuid Cell;
	}
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //

	
	// ######################## PUBLIC VARS ######################## //
	public CellSwitchData[] SwitchKeys;
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
	}
		
	private void Update () {
		foreach (CellSwitchData cellSwitchData in SwitchKeys)
		{
			if (Input.GetKeyDown(cellSwitchData.Key))
			{
				SpatialCellManager.Instance.ChangeActiveCell(cellSwitchData.Cell.Value);
				return;
			}
		}


	}
	
	
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		
	}



	// ######################## FUNCTIONALITY ######################## //


	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
