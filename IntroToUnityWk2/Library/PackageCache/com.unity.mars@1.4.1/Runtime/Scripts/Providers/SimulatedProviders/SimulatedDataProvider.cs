using Unity.MARS.MARSUtils;
using UnityEngine;

namespace Unity.MARS.Providers.Synthetic
{
    /// <summary>
    /// Base class for a behavior that uses the simulated environment to provide data
    /// </summary>
    abstract class SimulatedDataProvider : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            EditorOnlyEvents.onEnvironmentSetup += OnEnvironmentSetup;

            if (Application.isPlaying)
                return;

            // In edit mode simulation the environment is set up before providers are enabled
            OnEnvironmentSetup();
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            EditorOnlyEvents.onEnvironmentSetup -= OnEnvironmentSetup;
#endif
        }

        void OnEnvironmentSetup()
        {
#if UNITY_EDITOR
            var getEnvRoot = EditorOnlyDelegates.GetSimulatedEnvironmentRoot;
            if (getEnvRoot != null)
                OnEnvironmentReady(getEnvRoot());
#endif
        }

        /// <summary>
        /// Called as soon as the simulated environment is set up. Use this for initialization that depends on
        /// the environment.
        /// </summary>
        /// <param name="environmentRoot">The root game object of the simulated environment hierarchy</param>
        protected virtual void OnEnvironmentReady(GameObject environmentRoot) { }
    }
}
