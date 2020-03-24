using FK.QuantumVR.Objects;
using FK.Utility.VersionControl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FK.QuantumVR.Editor.Tools
{
    /// <summary>
    /// <para>Functionality for automatically settig up a scene for QuantumVR</para>
    ///
    /// v1.3 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSetupUtility
    {
        // ######################## PRIVATE VARS ######################## //
        private const string MAIN_CAMERA_TAG = "MainCamera";
        private const string SKYBOX_CAMERA_NAME = "SkyboxCamera";
        private const string PORTAL_TRANSITION_QUAD_NAME = "QuantumVR_PortalTransitionQuad";
        private const string CLEAR_DEPTH_QUAD_NAME = "QuantumVR_ClearDepthQuad";


        // ######################## INITS ######################## //
        static SceneSetupUtility()
        {
            // if the project is not set up, ask the user to perform the automatic setup
            if (!SettingsManager.ProjectIsSetUp)
            {
                if (EditorUtility.DisplayDialog("Set up Project for QuantumVR?",
                    "QuantumVR could not find a settings file. This likely means that the project is not yet set up for QuantumVR. Would you like to run the automatic setup now?", "Yes", "No"))
                {
                    SettingsManager.SetupProject();
                    CheckScene();
                }
            }

            // make sure we get notified when a new scene is opened and check the already openend scene
            EditorSceneManager.sceneOpened += (scene, mode) => CheckScene();
            EditorApplication.delayCall += CheckScene;
        }


        // ######################## FUNCTIONALITY ######################## //
        /// <summary>
        /// Checks whether the scene contains any compressed space entities. If yes, it checks if the scene is set up correctly and runs the setup if not
        /// </summary>
        public static void CheckScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            
            if(!SettingsManager.ProjectIsSetUp)
                SettingsManager.SetupProject();
            
            // do nothing if automatic setup is disabled
            if (!SettingsManager.Settings[SettingsLoader.DO_SCENE_AUTO_SETUP_KEY].BoolValue)
            {
                QuantumVRLogger.EditorLogVerbose("Auto Scene Setup is disabled");
                return;
            }

            // if there are no QuantumVR objects, do nothing
            QuantumVRCamera camera = Object.FindObjectOfType<QuantumVRCamera>();
            SpatialCellManager spatialCellManager = Object.FindObjectOfType<SpatialCellManager>();
            if (Object.FindObjectOfType<CompressedSpaceEntity>() == null && camera == null && spatialCellManager == null)
            {
                QuantumVRLogger.EditorLogVerbose("Scene contains no QuantumVR objects, no need to run the QuantumVR Setup.");
                return;
            }

            // if there is a QuantumVR camera and a spatial cell manager, there is no need to run the setup
            if (camera != null && spatialCellManager != null)
            {
                QuantumVRLogger.EditorLogVerbose("Found a Quantum VR Camera and and a Spatial Cell Manager, it seems like the Scene is set up correctly for QuantumVR.");
                return;
            }
            
            AskSetupScene();
        }

        /// <summary>
        /// Asks the user if he wants to setup the scene for QuantumVR
        /// </summary>
        public static void AskSetupScene()
        {
            // ask the user first if he wants to run the setup, if not return
            if (!EditorUtility.DisplayDialog("QuantumVR Scene Setup",
                "QuantumVR has detected that this scene contains QuantumVR objects but is not properly set up for use with QuantumVR. Do you want to run the setup? (If you don't do the setup, QuantumVR won't work in this scene)",
                "Yes", "No"))
                return;

            SetupScene();
        }

        /// <summary>
        /// Sets up the scene for use with quantumVR, adding all needed objects
        /// </summary>
        [MenuItem("Tools/QuantumVR/Set up Scene")]
        public static void SetupScene()
        {
            QuantumVRCamera camera = Object.FindObjectOfType<QuantumVRCamera>();
            SpatialCellManager spatialCellManager = Object.FindObjectOfType<SpatialCellManager>();

            SetUpCamera(camera);

            // if there is no spatial cell manager, set it up
            if (spatialCellManager == null)
                SetUpSpatialCellManager(spatialCellManager);
            
            QuantumVRLogger.EditorLog("Set up Scene");
        }

        /// <summary>
        /// Creates and sets up the needed cameras
        /// </summary>
        /// <param name="camera"></param>
        private static void SetUpCamera(QuantumVRCamera camera)
        {
            // if there is no quantumVR camera, create one on an existing camera, or create a new camera if there is none
            if (camera == null)
            {
                GameObject cameraObject = GameObject.FindWithTag(MAIN_CAMERA_TAG);
                if (cameraObject == null)
                {
                    cameraObject = Object.FindObjectOfType<Camera>()?.gameObject;
                    if(cameraObject != null)
                        QuantumVRLogger.EditorLogWarning("Found a Camera but no camera tagged as Main Camera. This Camera will become the Main Camera now!", cameraObject);
                }

                if (cameraObject == null)
                {
                    cameraObject = new GameObject("Main Camera");
                    QuantumVRLogger.EditorLogVerbose("Could not find a Main Camera, creating a new one...", cameraObject);
                }

                cameraObject.tag = MAIN_CAMERA_TAG;
                camera = cameraObject.AddComponent<QuantumVRCamera>();
                QuantumVRLogger.EditorLogVerbose("Created QuantumVR Camera", camera);
            }

            // setup component values of the camera component
            Camera cameraComponent = camera.GetComponent<Camera>();
            SerializedObject cameraComponentSerializedObject = new SerializedObject(cameraComponent);
            cameraComponentSerializedObject.FindProperty("m_ClearFlags").enumValueIndex = 2;
            cameraComponentSerializedObject.ApplyModifiedProperties();


            // find and set up the quad used for rendering portal transitions. If there is non, create a new one
            GameObject portalTransitionQuad = camera.transform.Find(PORTAL_TRANSITION_QUAD_NAME)?.gameObject;
            if (portalTransitionQuad == null)
                portalTransitionQuad = PortalCreationUtility.CreatePortalQuad(PORTAL_TRANSITION_QUAD_NAME, camera.transform);
            else
                QuantumVRLogger.EditorLogVerbose("Found Existing Portal Transitioning Quad!", portalTransitionQuad);
            SerializedObject portalTransitionQuadRendererSerializedObject = new SerializedObject(portalTransitionQuad.GetComponent<MeshRenderer>());
            SerializedProperty portalTransitionQuadMaterials = portalTransitionQuadRendererSerializedObject.FindProperty("m_Materials");
            portalTransitionQuadMaterials.arraySize = 3;
            portalTransitionQuadMaterials.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_CameraDepth.mat");
            portalTransitionQuadMaterials.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_CameraPortalStencil.mat");
            portalTransitionQuadMaterials.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_CameraView.mat");
            portalTransitionQuadRendererSerializedObject.ApplyModifiedProperties();

            // find and set up the quad used for clearing the depth buffer. If there is non, create a new one
            GameObject clearDepthQuad = camera.transform.Find(CLEAR_DEPTH_QUAD_NAME)?.gameObject;
            if (clearDepthQuad == null)
                clearDepthQuad = PortalCreationUtility.CreatePortalQuad(CLEAR_DEPTH_QUAD_NAME, camera.transform);
            else
                QuantumVRLogger.EditorLogVerbose("Found Existing Clear Depth Quad!", portalTransitionQuad);
            SerializedObject clearDepthQuadRendererSerializedObject = new SerializedObject(clearDepthQuad.GetComponent<MeshRenderer>());
            SerializedProperty clearDepthQuadMaterials = clearDepthQuadRendererSerializedObject.FindProperty("m_Materials");
            clearDepthQuadMaterials.arraySize = 1;
            clearDepthQuadMaterials.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/FK/QuantumVR/Materials/Mat_ClearDepth.mat");
            clearDepthQuadRendererSerializedObject.ApplyModifiedProperties();

            // set up component values of the quantumVR camera
            SerializedObject quantumVRCameraSerializedObject = new SerializedObject(camera);
            quantumVRCameraSerializedObject.FindProperty("_portalTransitionQuad").objectReferenceValue = portalTransitionQuad;
            quantumVRCameraSerializedObject.FindProperty("_depthClearQuad").objectReferenceValue = clearDepthQuad.transform;
            quantumVRCameraSerializedObject.ApplyModifiedProperties();
            
            QuantumVRLogger.EditorLogVerbose("Finished QuantumVR Camera Setup!", camera);

            // attempt to get the skybox camera. If there is none, ask wether it should be created
            GameObject skyboxCameraObject = GameObject.Find(SKYBOX_CAMERA_NAME);
            if (skyboxCameraObject == null)
            {
                if (EditorUtility.DisplayDialog("Create Skybox Camera?",
                    "If you want to render the skybox with QuantumVR, a seperate Camera has to be set up. Do you want to do that?", "Yes", "No"))
                    skyboxCameraObject = new GameObject(SKYBOX_CAMERA_NAME);
            }
            else
            {
                QuantumVRLogger.EditorLogVerbose("Found Exisiting QuantumVR Skybox Camera!", skyboxCameraObject);
            }

            // if there is a skybox camerea, set it up correctly
            if (skyboxCameraObject)
            {
                Camera skyboxCamera = skyboxCameraObject.GetComponent<Camera>();
                if (skyboxCamera == null)
                    skyboxCamera = skyboxCameraObject.AddComponent<Camera>();

                SerializedObject skyboxCameraSerializedObject = new SerializedObject(skyboxCamera);
                skyboxCameraSerializedObject.FindProperty("m_CullingMask").intValue = 0;
                skyboxCameraSerializedObject.FindProperty("m_Depth").floatValue = cameraComponent.depth - 1;
                skyboxCameraSerializedObject.ApplyModifiedProperties();
                
                QuantumVRLogger.EditorLogVerbose("Finished QuantumVR Skybox Camera Setup!", skyboxCameraObject);
            }
        }

        /// <summary>
        /// Creates a spatial cell manager and a map to go with it
        /// </summary>
        /// <param name="spatialCellManager"></param>
        private static void SetUpSpatialCellManager(SpatialCellManager spatialCellManager)
        {
            // create the manager
            GameObject spatialCellManagerObject = new GameObject("SpatialCellManager");
            spatialCellManager = spatialCellManagerObject.AddComponent<SpatialCellManager>();

            // create a new spatial cell map
            SpatialCellMap spatialCellMap = ScriptableObject.CreateInstance<SpatialCellMap>();

            // get the target folder from the settings and create the directory if it does not yet exist
            string targetFolderPath = SettingsManager.Settings[SettingsLoader.DEFAULT_SPATIAL_CELL_MAP_LOCATION_KEY].StringValue;
            if (!AssetDatabase.IsValidFolder(targetFolderPath))
            {
                QuantumVRLogger.EditorLogVerbose("Creating QuantumVR Spatial Cell Map Directory...");
                System.IO.Directory.CreateDirectory($"{Application.dataPath.Substring(0, Application.dataPath.IndexOf("Assets"))}/{targetFolderPath}");
            }

            // create the asset target path and save the asset
            string assetPath = $"{targetFolderPath}/{EditorSceneManager.GetActiveScene().name}_map.asset";
            AssetDatabase.CreateAsset(spatialCellMap, assetPath);
            VersionControlUtils.AddToVersionControl(assetPath);

            // reference the asset in the manager
            SerializedObject spatialCellManagerSerializedObject = new SerializedObject(spatialCellManager);
            spatialCellManagerSerializedObject.FindProperty("_spatialCellMapData").objectReferenceValue = spatialCellMap;
            spatialCellManagerSerializedObject.ApplyModifiedProperties();
            
            QuantumVRLogger.EditorLogVerbose("Created Spatial Cell Manager!", spatialCellManager);
            QuantumVRLogger.EditorLogVerbose("Created new Spatial Cell Map!", spatialCellMap);
        }
    }
}