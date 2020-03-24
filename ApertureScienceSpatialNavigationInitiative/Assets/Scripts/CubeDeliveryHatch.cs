using System.Collections;
using System.Collections.Generic;
using FK.MessageManager;
using FK.Utility;
using FK.Utility.ArraysAndLists;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class CubeDeliveryHatch : MonoBehaviour
{
	// ######################## STRUCTS & CLASSES ######################## //


	// ######################## ENUMS & DELEGATES ######################## //


	// ######################## PROPERTIES ######################## //


	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private AudioSource _openAudioSource;
	[SerializeField] private Transform[] _hatchDoors;
	[SerializeField] private float _hatchDoorsTargetRotation = -59.934f;
	[SerializeField] private float _openingDuration = 1.0f;
	[SerializeField] private Grabbable _cube;


	// ######################## PUBLIC VARS ######################## //


	// ######################## PROTECTED VARS ######################## //


	// ######################## PRIVATE VARS ######################## //
	private readonly List<int> _pressedButtons = new List<int>();


	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		MessageManager.AddObserver<int>((int)MessageTypes.BUTTON_PRESSED, OnButtonPressed);

		_cube.CanBeGrabbed = false;
		_cube.GetComponent<Rigidbody>().isKinematic = true;

	}


	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start()
	{
		Init();
	}

	private void Update()
	{

	}


	// ######################## FUNCTIONALITY ######################## //
	private void DeliverCube()
	{
	_openAudioSource.gameObject.SetActive(true);
		_openAudioSource.Play();

		foreach (Transform hatchDoor in _hatchDoors)
		{
			hatchDoor.Interpolate(Quaternion.Euler(0, _hatchDoorsTargetRotation, 0), _openingDuration, Space.Self, Easing.OutCubic);
		}

		_cube.GetComponent<Rigidbody>().isKinematic = false;
		_cube.CanBeGrabbed = true;

	}

	private void OnButtonPressed(int buttonColorIndex)
	{
		if (GameController.Instance.LockColors.Search(buttonColorIndex) <= -1)
			return;

		if (_pressedButtons.Contains(buttonColorIndex))
			return;

		_pressedButtons.Add(buttonColorIndex);

		if (_pressedButtons.Count == GameController.Instance.LockColors.Length)
			DeliverCube();
	}

	// ######################## COROUTINES ######################## //


	// ######################## UTILITIES ######################## //

}
