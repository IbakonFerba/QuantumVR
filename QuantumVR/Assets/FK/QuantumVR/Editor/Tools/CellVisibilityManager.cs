using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FK.QuantumVR.Editor.Tools
{
	/// <summary>
	/// <para>Manages hiding all scenes but one that can then be edited</para>
	///
	/// v0.91 12/2019
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	[InitializeOnLoad]
	public static class CellVisibilityManager
	{
		// ######################## INITS ######################## //
		static CellVisibilityManager()
		{
			EditorSceneManager.sceneClosing += OnSceneClosing;
			
			SceneVisibilityManager.instance.ExitIsolation();
		}

		
		// ######################## UNITY EVENT FUNCTIONS ######################## //
		private static void OnSceneClosing(Scene scene, bool removingScene)
		{
			StopIsolation();
		}


		// ######################## FUNCTIONALITY ######################## //
		public static void IsolateCell(Guid spatialCellGuid)
		{
			// get the cell and make sure it exists
			SpatialCell cell = CompressedSpaceEntityManager.GetSpatialCell(spatialCellGuid);
			if(cell == null)
				return;
			
			// isolate cell
			SceneVisibilityManager.instance.Isolate(cell.gameObject, true);
			if (!(Selection.activeGameObject != null && Selection.activeGameObject.transform.IsChildOf(cell.gameObject.transform)))
			{
				Selection.activeGameObject = cell.gameObject;
				SceneView.FrameLastActiveSceneView();
			}

			// highlight all portals of the cell
			PortalHighlightingManager.ClearHighlight();
			PortalHighlightingManager.HighlightCellPortals(spatialCellGuid);
			
			QuantumVRLogger.EditorLogVerbose($"Isolated Cell \"{cell.gameObject.name}\"", cell);
		}

		public static void StopIsolation()
		{
			SceneVisibilityManager.instance.ExitIsolation();
			PortalHighlightingManager.ClearHighlight();
			
			QuantumVRLogger.EditorLogVerbose("Cell Isolation Stopped");
		}
	}
}
