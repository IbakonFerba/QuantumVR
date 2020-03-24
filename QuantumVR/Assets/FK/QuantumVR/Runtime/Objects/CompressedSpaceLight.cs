using FK.QuantumVR.Objects;
using UnityEngine;

/// <summary>
/// <para>A light shining only inside its spatial cell</para>
///
/// v0.1 10/2019
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
[AddComponentMenu("QuantumVR/Light")]
[RequireComponent(typeof(Light))]
public class CompressedSpaceLight : CompressedSpaceEntity
{
    // ######################## PRIVATE VARS ######################## //
    private Light _light;
    private int _originalCullingMask;


    // ######################## UNITY EVENT FUNCTIONS ######################## //
    protected override void Awake()
    {
        base.Awake();
        Init();
    }


    // ######################## INITS ######################## //
    ///<summary>
    /// Does the Init for this Behaviour
    ///</summary>
    private void Init()
    {
        _light = GetComponent<Light>();

        // remove compressed space layers from default culling mask
        _originalCullingMask = _light.cullingMask;
        for (int i = 0; i < SettingsLoader.Settings[SettingsLoader.MAX_SPATIAL_LAYERS_KEY].IntValue-1; ++i)
        {
            _originalCullingMask &= ~(1 << SpatialCellManagerInstance.UnityLayersBySpatialLayer[i]);
        }

        _light.cullingMask = _originalCullingMask;
    }


    // ######################## FUNCTIONALITY ######################## //
    protected override void OnLayerMaskChanged(byte spatialLayermask, bool isInActiveCell)
    {
        int cullingMask = isInActiveCell ? _originalCullingMask : 0;

        // add all spatial layers that are on the layermask to the culling mask
        for (int i = 0; i < 8; ++i)
        {
            if (((spatialLayermask >> i) & 1) != 0)
                cullingMask |= (1 << SpatialCellManagerInstance.UnityLayersBySpatialLayer[i]);
            if ((1 << (i + 1)) > spatialLayermask)
                break;
        }

        _light.cullingMask = cullingMask;
    }
}