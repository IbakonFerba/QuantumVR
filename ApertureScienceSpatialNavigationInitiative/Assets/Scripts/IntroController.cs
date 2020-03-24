using System.Collections;
using System.Collections.Generic;
using FK.Utility;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class IntroController : MonoBehaviour 
{
	// ######################## STRUCTS & CLASSES ######################## //
	[System.Serializable]
	public struct Element
	{
		public Transform Object;
		public float StartTime;
		public float EndTime;
	}
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private Element[] _elements;
	[SerializeField] private AudioSource _audioSource;
	[SerializeField] private float _animationDuration;
	[SerializeField] private float _offscreenPos;

	
	// ######################## PUBLIC VARS ######################## //
	
	
	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //


	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init() 
	{
		foreach (Element element in _elements)
		{
			element.Object.transform.localPosition = new Vector3(_offscreenPos, element.Object.transform.localPosition.y, element.Object.transform.localPosition.z);
		}
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	private void Start () {
		Init();
		Play();
	}
		
	private void Update () {
		
	}
	
	
	// ######################## FUNCTIONALITY ######################## //
	private void Play()
	{
		_audioSource.Play();
		StartCoroutine(PlaySequence());
	}
	
	
	// ######################## COROUTINES ######################## //
	private IEnumerator PlaySequence()
	{
		int activeElementIndex = 0;
		Element activeElement = _elements[activeElementIndex];
		bool activeElementStarted = false;
		while (_audioSource.isPlaying)
		{
			if (_audioSource.time >= activeElement.StartTime && !activeElementStarted)
			{
				activeElementStarted = true;
				activeElement.Object.Interpolate(new Vector3(0, activeElement.Object.localPosition.y, activeElement.Object.localPosition.z), _animationDuration,
					Space.Self, Easing.OutQuintic);
			}

			if (_audioSource.time >= activeElement.EndTime)
			{
				activeElement.Object.Interpolate(new Vector3(-_offscreenPos, activeElement.Object.localPosition.y, activeElement.Object.localPosition.z), _animationDuration,
					Space.Self, Easing.OutQuintic);
				
				if(activeElementIndex >= _elements.Length-1)
					break;
				
				activeElement = _elements[++activeElementIndex];
				activeElementStarted = false;
			}
			yield return null;
		}
	}
	
	
	// ######################## UTILITIES ######################## //
	
}
