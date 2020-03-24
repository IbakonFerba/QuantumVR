using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <para>Class Info</para>
///
/// v1.0 mm/20yy
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public class OverlayToggle : MonoBehaviour
{
    // ######################## EXPOSED VARS ######################## //

    #region EXPOSED VARS

    [SerializeField] private KeyCode _toggleKey = KeyCode.O;

    #endregion


    // ######################## PRIVATE VARS ######################## //

    #region PRIVATE VARS

    private GameObject _child;

    #endregion


    // ######################## UNITY EVENT FUNCTIONS ######################## //

    #region UNITY EVENT FUNCTIONS

    private void Start()
    {
        _child = transform.GetChild(0).gameObject;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            _child.SetActive(!_child.activeSelf);
    }

    #endregion
}