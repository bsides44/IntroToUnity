#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.MARS.Settings;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEditor.MARS;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

#if XRMANAGEMENT_3_2_OR_NEWER
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
#endif

namespace Unity.MARS.XRSubsystem
{
    class MarsXRSubsystemModule : IModuleDependency<MARSSceneModule>, IModuleBehaviorCallbacks
    {
        readonly List<IMarsXRSubscriber> m_MarsXRSubscribers = new List<IMarsXRSubscriber>();

        MeshSubscriber m_MeshSubscriber;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<IMarsXRSubsystem> k_MarsXRSubsystems = new List<IMarsXRSubsystem>();
        static readonly List<XRMeshSubsystem> k_XRMeshSubsystems = new List<XRMeshSubsystem>();
        static readonly List<object> k_SubscriberObjects = new List<object>();

        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
#if XRMANAGEMENT_3_2_OR_NEWER
            // Ensures build target is loaded into `XRGeneralSettings`, this can fail if 'XRGeneralSettingsPerBuildTarget'
            // is not loaded during an editor session causing no subsystems to be loaded in `SubsystemLifecycleManager`.
            // This is only an issue for editor subsystems.
            if (XRGeneralSettings.Instance == null)
            {
                XRGeneralSettings.Instance = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }

            PackageVersionWatcher.packageUpdated += EnableMarsXRSubsystem;
#endif

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

#if XRMANAGEMENT_3_2_OR_NEWER
        static void EnableMarsXRSubsystem()
        {
            var buildTargetGroup = BuildTargetGroup.Standalone;
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (generalSettings == null)
            {
                var settingsManagerType = typeof(XRGeneralSettingsPerBuildTarget).Assembly.GetType("UnityEditor.XR.Management.XRSettingsManager");
                if (settingsManagerType == null)
                    return;

                var currentSettingsProperty = settingsManagerType.GetProperty("currentSettings", BindingFlags.NonPublic | BindingFlags.Static);
                if (currentSettingsProperty == null)
                    return;

                var getGeneralSettingsMethod = currentSettingsProperty.GetMethod;
                if (getGeneralSettingsMethod == null)
                    return;

                var currentSettings = (XRGeneralSettingsPerBuildTarget)getGeneralSettingsMethod.Invoke(null, null);
                if (currentSettings == null)
                    return;

                generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                currentSettings.SetSettingsForBuildTarget(buildTargetGroup, generalSettings);
                generalSettings.name = $"{buildTargetGroup.ToString()} Settings";
                AssetDatabase.AddObjectToAsset(generalSettings, AssetDatabase.GetAssetOrScenePath(currentSettings));

                var xrManagerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                xrManagerSettings.name = $"{buildTargetGroup.ToString()} Providers";
                AssetDatabase.AddObjectToAsset(xrManagerSettings, AssetDatabase.GetAssetOrScenePath(currentSettings));
                generalSettings.AssignedSettings = xrManagerSettings;
            }

            XRPackageMetadataStore.AssignLoader(generalSettings.Manager, "Unity.MARS.XRSubsystem.MARSXRSubsystemLoader", BuildTargetGroup.Standalone);
        }
#endif

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // List is cleared by GetInstances
            SubsystemManager.GetInstances(k_MarsXRSubsystems);
            var anyMarsSubsystemsRunning = false;
            foreach (var subsystem in k_MarsXRSubsystems)
            {
                if (subsystem.running)
                {
                    anyMarsSubsystemsRunning = true;
                    break;
                }
            }

            if (!anyMarsSubsystemsRunning)
            {
                k_XRMeshSubsystems.Clear();
                SubsystemManager.GetInstances(k_XRMeshSubsystems);
                anyMarsSubsystemsRunning = k_XRMeshSubsystems.Any(x => x.running);
            }

            if (anyMarsSubsystemsRunning)
                MARSSession.EnsureSessionInActiveScene();
        }

        public void ConnectDependency(MARSSceneModule dependency)
        {
            // List is cleared by GetInstances
            SubsystemManager.GetInstances(k_MarsXRSubsystems);
            m_MarsXRSubscribers.Clear();
            k_SubscriberObjects.Clear();
            foreach (var subsystem in k_MarsXRSubsystems)
            {
                // For most SubsystemLifecycleManagers we can guarantee that if they exist in the scene they have started
                // running their subsystems by this point, since they have lower script execution orders than MARSSession.
                // ARRaycastManager is an exception to this - it has a default execution order. So we make an exception for
                // the raycast subsystem here and assume that it will need a provider.
                // Also for Subsystems before version 3 the image marker subsystem does not report as running.
#if ARSUBSYSTEMS_3_OR_NEWER
                if (subsystem.running || subsystem is RaycastSubsystem)
#else
                if (subsystem.running || subsystem is RaycastSubsystem || subsystem is ImageMarkerSubsystem)
#endif
                {
                    var subscriber = subsystem.FunctionalitySubscriber;
                    m_MarsXRSubscribers.Add(subscriber);
                    k_SubscriberObjects.Add(subscriber);
                }
            }

            k_XRMeshSubsystems.Clear();
            SubsystemManager.GetInstances(k_XRMeshSubsystems);
            var meshSubsystem = k_XRMeshSubsystems.FirstOrDefault(x => x.running);
            if (meshSubsystem != null)
            {
                m_MeshSubscriber = new MeshSubscriber(meshSubsystem);
                m_MarsXRSubscribers.Add(m_MeshSubscriber);
                k_SubscriberObjects.Add(m_MeshSubscriber);
            }

            if (k_SubscriberObjects.Count > 0)
                dependency.AddRuntimeSceneObjects(k_SubscriberObjects);
        }

        public void LoadModule() { }

        public void UnloadModule() { }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable()
        {
            foreach (var subscriber in m_MarsXRSubscribers)
            {
                subscriber.SubscribeToEvents();
            }
        }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate() { }

        public void OnBehaviorDisable()
        {
            foreach (var subscriber in m_MarsXRSubscribers)
            {
                subscriber?.UnsubscribeFromEvents();
            }

            if (m_MeshSubscriber != null)
            {
                m_MeshSubscriber.Dispose();
                m_MeshSubscriber = null;
            }
        }

        public void OnBehaviorDestroy() { }
    }
}
#endif
