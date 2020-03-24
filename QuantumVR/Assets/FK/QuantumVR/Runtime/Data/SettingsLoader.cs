using System;
using FK.JSON;
using FK.QuantumVR;
using UnityEngine;

/// <summary>
/// <para>Static class that loads the settings once at application startup</para>
///
/// v2.2 10/2019
/// Written by Fabian Kober
/// fabian-kober@gmx.net
/// </summary>
public static class SettingsLoader
{
    // ######################## PUBLIC VARS ######################## //
    /// <summary>
    /// Path to the settings relative to the StreamingAssets folder
    /// </summary>
    public const string SETTINGS_PATH_IN_STREAMING_ASSETS = "/QuantumVR/settings.json";

    public const string MAX_SPATIAL_LAYERS_KEY = "MaxSpatialLayers";
    public const string UNITY_LAYERS_KEY = "UnityLayers";
    public const string RENDER_QUEUE_RANGES_KEY = "RenderQueueRanges";
    public const string PORTAL_RENDER_QUEUE_RANGES_KEY = "Portals";
    public const string OBJECTS_RENDER_QUEUE_RANGES_KEY = "Objects";
    public const string ACTIVE_LAYER_PORTAL_SURFACE_QUEUE_KEY = "ActiveLayerPortalSurfaceQueue";
    public const string PORTAL_COLLIDER_SIZE_KEY = "PortalColliderSize";
    public const string DEPTH_CLEAR_QUEUE_KEY = "DepthClearQueue";
    public const string DO_SCENE_AUTO_SETUP_KEY = "DoSceneAutoSetup";
    public const string DEFAULT_SPATIAL_CELL_MAP_LOCATION_KEY = "DefaultSpatialCellMapLocation";
    public const string LOG_LEVEL_KEY = "LogLevel";

    public static JSONObject Settings;


    // ######################## INITS ######################## //
    static SettingsLoader()
    {
        LoadSettings();
    }

    public static void LoadSettings()
    {
        try
        {
            Settings = JSONObject.LoadFromFile($"{Application.streamingAssetsPath}{SETTINGS_PATH_IN_STREAMING_ASSETS}");
        }
        catch (Exception e)
        {
            Settings = null;
        }

        QuantumVRLogger.Init();
        
        if (Settings == null)
            QuantumVRLogger.LogError("Could not load Settings!");
        else
            QuantumVRLogger.Log("Loaded settings");
    }
}