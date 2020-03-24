using System;
using System.Collections;
using System.Collections.Generic;
using FK.QuantumVR.Objects;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class PlayerHand : MonoBehaviour
{
	// ######################## STRUCTS & CLASSES ######################## //


	// ######################## ENUMS & DELEGATES ######################## //


	// ######################## PROPERTIES ######################## //


	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private InputDeviceRole _role;


	// ######################## PUBLIC VARS ######################## //


	// ######################## PROTECTED VARS ######################## //


	// ######################## PRIVATE VARS ######################## //
	private static readonly List<InputDevice> _devices = new List<InputDevice>();

	private bool _inputValue;
	private bool _isPressed;

	private CompressedSpaceEntity _compressedSpaceEntity;

	private readonly List<Grabbable> _objectsInReach = new List<Grabbable>();

	private Grabbable _heldObject;

	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		_compressedSpaceEntity = GetComponent<CompressedSpaceEntity>();
	}


	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start()
	{
		Init();
	}

	private void Update()
	{
		InputDevices.GetDevicesWithRole(_role, _devices);
		if (_devices.Count > 0)
		{
			InputDevice device = _devices[0];
			if (device.TryGetFeatureValue(CommonUsages.gripButton, out _inputValue) && _inputValue)
			{
				if (!_isPressed)
				{
					_isPressed = true;
					TryGrab();
				}
			}
			else if (device.TryGetFeatureValue(CommonUsages.triggerButton, out _inputValue) && _inputValue)
			{
				if (!_isPressed)
				{
					_isPressed = true;
					TryGrab();
				}
			}
			else if (_isPressed)
			{
				_isPressed = false;
				StopGrab();
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		Grabbable grabbable;
		if ((grabbable = other.GetComponent<Grabbable>()) != null)
		{
			if (_objectsInReach.Contains(grabbable))
				return;

			_objectsInReach.Add(grabbable);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Grabbable grabbable;
		if ((grabbable = other.GetComponent<Grabbable>()) != null)
		{
			if (!_objectsInReach.Contains(grabbable))
				return;

			_objectsInReach.Remove(grabbable);
		}
	}


	// ######################## FUNCTIONALITY ######################## //
	private void TryGrab()
	{
		if (_objectsInReach.Count <= 0)
			return;

		if (!_objectsInReach[0].CanBeGrabbed)
			return;

		_objectsInReach[0].Grab(transform);
		_heldObject = _objectsInReach[0];
	}

	private void StopGrab()
	{
		if (_heldObject)
		{
			_heldObject.Release();
		}
	}


	// ######################## COROUTINES ######################## //


	// ######################## UTILITIES ######################## //

}
