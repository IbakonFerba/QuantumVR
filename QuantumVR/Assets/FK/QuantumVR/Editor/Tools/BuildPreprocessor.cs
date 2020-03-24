using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace FK.QuantumVR.Editor.Tools
{
	/// <summary>
	/// <para>Does Pre Build operations needed for Quantum VR</para>
	///
	/// v0.1 11/2019
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	public class BuildPreprocessor : IPreprocessBuildWithReport
	{
		// ######################## PUBLIC VARS ######################## //
		public int callbackOrder => 0;

		
		// ######################## UNITY EVENT FUNCTIONS ######################## //
		public void OnPreprocessBuild(BuildReport report)
		{
			CellVisibilityManager.StopIsolation();
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		}
	}
}
