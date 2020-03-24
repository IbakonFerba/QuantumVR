using FK.JSON;
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
    public const string ACTIVE_LAYER_PORTAL_SURFACE_QUEUE = "ActiveLayerPortalSurfaceQueue";
    public const string PORTAL_COLLIDER_SIZE = "PortalColliderSize";
    public const string DEPTH_CLEAR_QUEUE = "DepthClearQueue";

    public static readonly JSONObject Settings;


    // ######################## INITS ######################## //
    static SettingsLoader()
    {
        Settings = JSONObject.LoadFromFile($"{Application.streamingAssetsPath}{SETTINGS_PATH_IN_STREAMING_ASSETS}");

#if UNITY_EDITOR
        Debug.Log("Loaded QuantumVR settings");
#endif
    }
}