using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FK.QuantumVR
{
    /// <summary>
    /// <para>QuantumVR Logger</para>
    ///
    /// v2.0 12/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class QuantumVRLogger
    {
        // ######################## PRIVATE VARS ######################## //
        private const string QUANTUM_VR_RUNTIME_TAG = "[QuantumVR]";
#if UNITY_EDITOR
        private const string QUANTUM_VR_EDITOR_TAG = "[QuantumVR Editor]";
#endif
        private static readonly ILogger _logger = new Logger(Debug.unityLogger.logHandler);

        private static bool _verbose;
        private static bool _initializing;

        
        // ######################## INITS ######################## //
        static QuantumVRLogger()
        {
            Init();
        }

        public static void Init()
        {
            if(_initializing)
                return;
            
            _initializing = true;
            _verbose = false;

            try
            {
                if (SettingsLoader.Settings.HasField(SettingsLoader.LOG_LEVEL_KEY))
                {

                    switch (SettingsLoader.Settings[SettingsLoader.LOG_LEVEL_KEY].IntValue)
                    {
                        case 0:
                            _verbose = true;
                            _logger.filterLogType = LogType.Log;
                            break;
                        case 1:
                            _logger.filterLogType = LogType.Log;
                            break;
                        case 2:
                            _logger.filterLogType = LogType.Warning;
                            break;
                        case 3:
                            _logger.filterLogType = LogType.Assert;
                            break;
                        case 4:
                            _logger.filterLogType = LogType.Exception;
                            break;
                    }
                }
                else
                {
                    _logger.filterLogType = LogType.Log;
                }
            }
            catch (Exception e)
            {
                _logger.filterLogType = LogType.Log;
            }
            

            _initializing = false;
        }
        
        // ######################## FUNCTIONALITY ######################## //
        public static void LogVerbose(string message)
        {
            if(!_verbose)
                return;
            _logger.Log(QUANTUM_VR_RUNTIME_TAG, message);
        }
        
        public static void LogVerbose(string message, Object context)
        {
            if(!_verbose)
                return;
            _logger.Log(QUANTUM_VR_RUNTIME_TAG, message, context);
        }
        
        public static void Log(string message)
        {
            _logger.Log(QUANTUM_VR_RUNTIME_TAG, message);
        }

        public static void Log(string message, Object context)
        {
            _logger.Log(QUANTUM_VR_RUNTIME_TAG, message, context);
        }

        public static void LogWarning(string message)
        {
            _logger.LogWarning(QUANTUM_VR_RUNTIME_TAG, message);
        }
        
        public static void LogWarning(string message, Object context)
        {
            _logger.LogWarning(QUANTUM_VR_RUNTIME_TAG, message, context);
        }

        public static void LogError(string message)
        {
            _logger.LogError(QUANTUM_VR_RUNTIME_TAG, message);
        }
        
        public static void LogError(string message, Object context)
        {
            _logger.LogError(QUANTUM_VR_RUNTIME_TAG, message, context);
        }
        
        public static void LogException(System.Exception exception)
        {
            _logger.LogException(exception);
        }
        
        public static void LogException(System.Exception exception, Object context)
        {
            _logger.LogException(exception, context);
        }

#if UNITY_EDITOR
        public static void EditorLogVerbose(string message)
        {
            if(!_verbose)
                return;
            _logger.Log(QUANTUM_VR_EDITOR_TAG, message);
        }
        
        public static void EditorLogVerbose(string message, Object context)
        {
            if(!_verbose)
                return;
            _logger.Log(QUANTUM_VR_EDITOR_TAG, message, context);
        }
        
        public static void EditorLog(string message)
        {
            _logger.Log(QUANTUM_VR_EDITOR_TAG, message);
        }

        public static void EditorLog(string message, Object context)
        {
            _logger.Log(QUANTUM_VR_EDITOR_TAG, message, context);
        }

        public static void EditorLogWarning(string message)
        {
            _logger.LogWarning(QUANTUM_VR_EDITOR_TAG, message);
        }
        
        public static void EditorLogWarning(string message, Object context)
        {
            _logger.LogWarning(QUANTUM_VR_EDITOR_TAG, message, context);
        }

        public static void EditorLogError(string message)
        {
            _logger.LogError(QUANTUM_VR_EDITOR_TAG, message);
        }
        
        public static void EditorLogError(string message, Object context)
        {
            _logger.LogError(QUANTUM_VR_EDITOR_TAG, message, context);
        }
#endif
    }
}