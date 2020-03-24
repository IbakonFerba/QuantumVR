using System;
using System.Collections;
using System.Collections.Generic;
using FK.MessageManager;
using FK.QuantumVR;
using FK.QuantumVR.Objects;
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
public class ColorButton : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	public int ColorIndex => _colorIndex;


	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private Material _colorMaterial;
	[SerializeField] private Material _buttonColorMaterial;
	[SerializeField] private Transform _button;
	[SerializeField] private float _animationDuration = 1.0f;
	[SerializeField] private float _animationStrength = 0.1f;
	[SerializeField] private AudioSource _pressedAudioSource;
	[SerializeField] private AudioClip _positiveClip;
	[SerializeField] private AudioClip _negativeClip;

	
	// ######################## PUBLIC VARS ######################## //
	
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private Renderer _mainRenderer;
	private Renderer _buttonRenderer;
	private CompressedSpaceObject _mainCompressedSpaceObject;
	private CompressedSpaceObject _buttonCompressedSpaceObject;
	private MaterialPropertyBlock _propertyBlock;

	private int _colorIndex;

	private bool _wasPressed;
	
	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		_colorIndex = GameController.Instance.GetRandomUnusedColorIndex();
		
		_mainRenderer = GetComponent<Renderer>();
		_buttonRenderer = _button.GetComponent<Renderer>();
		_mainCompressedSpaceObject = GetComponent<CompressedSpaceObject>();
		_buttonCompressedSpaceObject = _button.GetComponent<CompressedSpaceObject>();

		_propertyBlock = new MaterialPropertyBlock();
		_propertyBlock.SetColor("_Color", GameController.Instance.Colors[_colorIndex]);

		UpdateMaterials();
		
		SpatialCellManager.Instance.PostLayermaskUpdate += UpdateMaterials;
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Awake () {
		Init();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_wasPressed || !other.CompareTag("Player"))
			return;

		MessageManager.Post((int)MessageTypes.BUTTON_PRESSED, _colorIndex);

		_button.Interpolate(_button.position - _button.forward * _animationStrength, _animationDuration*0.5f, Space.World,
			() => _button.Interpolate(_button.position + _button.forward * _animationStrength, _animationDuration*0.5f, Space.World));
		
		if (GameController.Instance.LockColors.Search(_colorIndex) <= -1)
		{
			_pressedAudioSource.PlayOneShot(_negativeClip);
			return;
		}
		
		_pressedAudioSource.PlayOneShot(_positiveClip);
		_wasPressed = true;
		UpdateMaterials();

		
		
	}


	// ######################## FUNCTIONALITY ######################## //
	private void UpdateMaterials()
    {
        if (!gameObject.activeInHierarchy)
            return;

		if (_wasPressed)
			SetMainColor();

		SetButtonColor();
	}
	
	private void SetButtonColor()
	{
		_buttonRenderer.SetPropertyBlock(null);
		Material[] copies = _buttonCompressedSpaceObject.GetCurrentInstancesOfMaterial(_buttonColorMaterial);

		for (int i = 0; i < copies.Length; ++i)
		{
			int materialIndex = _buttonRenderer.sharedMaterials.Search(copies[i]);
			_buttonRenderer.SetPropertyBlock(_propertyBlock, materialIndex);
		}
	}

	private void SetMainColor()
	{
		_mainRenderer.SetPropertyBlock(null);
		Material[] copies = _mainCompressedSpaceObject.GetCurrentInstancesOfMaterial(_colorMaterial);

		for (int i = 0; i < copies.Length; ++i)
		{
			int materialIndex = _mainRenderer.sharedMaterials.Search(copies[i]);
			_mainRenderer.SetPropertyBlock(_propertyBlock, materialIndex);
		}
	}
	
	// ######################## COROUTINES ######################## //
	
	
	// ######################## UTILITIES ######################## //
	
}
