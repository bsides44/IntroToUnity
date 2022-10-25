using Unity.MARS.Data.Recorded;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.MARS.Simulation
{
    class MarsAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Update list of environments and/or recordings if an environment/recording has been imported
            var environmentWasImported = false;
            var sessionRecordingWasImported = false;
            foreach (var assetPath in importedAssets)
            {
                if (sessionRecordingWasImported && environmentWasImported)
                    break;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (!environmentWasImported && assetType == typeof(GameObject))
                {
                    const string environmentLabel = MARSEnvironmentManager.EnvironmentLabel;
                    var labels = AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(assetPath));
                    foreach (var label in labels)
                    {
                        if (label == environmentLabel)
                        {
                            environmentWasImported = true;
                            break;
                        }
                    }
                }

                if (!sessionRecordingWasImported && assetType == typeof(TimelineAsset))
                {
                    var sessionRecordingInfo = AssetDatabase.LoadAssetAtPath<SessionRecordingInfo>(assetPath);
                    if (sessionRecordingInfo != null)
                        sessionRecordingWasImported = true;
                }
            }

            // Delay refreshing environments/recordings to allow for asset import postprocessing
            if (environmentWasImported)
            {
                EditorApplication.delayCall += () =>
                {
                    // MARSEnvironmentManager will refresh environments list on LoadModule, so only refresh if modules are loaded
                    var moduleLoaderCore = ModuleLoaderCore.instance;
                    if (moduleLoaderCore.ModulesAreLoaded)
                        RefreshEnvironments();
                };
            }

            if (sessionRecordingWasImported)
            {
                EditorApplication.delayCall += () =>
                {
                    // SimulationRecordingManager will refresh recordings list on LoadModule, so only refresh if modules are loaded
                    var moduleLoaderCore = ModuleLoaderCore.instance;
                    if (moduleLoaderCore.ModulesAreLoaded)
                        RefreshRecordings();
                };
            }
        }

        static void RefreshEnvironments()
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            environmentManager.UpdateSimulatedEnvironmentCandidates();
            environmentManager.TryRefreshEnvironmentAndRestartSimulation();
        }

        static void RefreshRecordings()
        {
            SimulationRecordingManager.RefreshSessionRecordings();
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            environmentManager.TryRefreshEnvironmentAndRestartSimulation();
        }
    }
}
