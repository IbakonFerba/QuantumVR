using System.Collections;
using System.Collections.Generic;
using FK.MessageManager;
using FK.Utility;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class GameController : Singleton<GameController>
{
	// ######################## STRUCTS & CLASSES ######################## //
	
	
	// ######################## ENUMS & DELEGATES ######################## //
	
	
	// ######################## PROPERTIES ######################## //
	

	// ######################## EXPOSED VARS ######################## //
	[SerializeField] private LayoutGroup _colorMonitorLayoutGroup;
	[SerializeField] private AudioSource _endAnnouncer;

	
	// ######################## PUBLIC VARS ######################## //
	public Color[] Colors;

	public int CombinationSize = 3;
	[HideInInspector] public int[] LockColors;


	// ######################## PROTECTED VARS ######################## //
	
	
	// ######################## PRIVATE VARS ######################## //
	private List<int> _unusedColorIndices;
	


	// ######################## INITS ######################## //
	///<summary>
	/// Does the Init for this Behaviour
	///</summary>
	private void Init()
	{
		SetupUnusedColors();
		
		LockColors = new int[CombinationSize];
		for (int i = 0; i < CombinationSize; ++i)
		{
			LockColors[i] = GetRandomUnusedColorIndex();
		}
		
		SetupUnusedColors();
		
		SetupMonitor();
		
		MessageManager.AddObserver((int)MessageTypes.CUBE_DELIVERED, () => StartCoroutine(EndGame()));
	}
	
	
	// ######################## UNITY EVENT FUNCTIONS ######################## //
	protected override void Awake () {
		base.Awake();
		Init();
	}


	// ######################## FUNCTIONALITY ######################## //
	public int GetRandomUnusedColorIndex()
	{
		int randomIndex = Random.Range(0, _unusedColorIndices.Count);
		int colorIndex = _unusedColorIndices[randomIndex];
		_unusedColorIndices.RemoveAt(randomIndex);
		return colorIndex;
	}

	private void SetupMonitor()
	{
		foreach (int lockColor in LockColors)
		{
			GameObject colorImage = new GameObject($"LockColor_{lockColor}");
			Image image = colorImage.AddComponent<Image>();
			image.color = Colors[lockColor];

			colorImage.transform.parent = _colorMonitorLayoutGroup.transform;
			colorImage.transform.localScale = Vector3.one;
			colorImage.transform.localPosition = Vector3.zero;
			colorImage.transform.rotation = Quaternion.identity;
		}
	}
	
	
	// ######################## COROUTINES ######################## //
	private IEnumerator EndGame()
	{
		_endAnnouncer.Play();
		yield return new WaitWhile(() => _endAnnouncer.isPlaying);
		
		Application.Quit();
	}
	
	
	// ######################## UTILITIES ######################## //
	private void SetupUnusedColors()
	{
		_unusedColorIndices = new List<int>();
		for (int i = 0; i < Colors.Length; ++i)
		{
			Colors[i].a = 1;
			_unusedColorIndices.Add(i);
		}
	}
	
}
