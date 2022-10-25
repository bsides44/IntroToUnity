using System.Collections.Generic;
using UnityEngine;
using Unity.MARS.MARSUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MARS
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    class GeneratedPlanesRoot : MonoBehaviour
    {
        public const string PlanesRootName = "Generated Planes";

        static readonly List<Renderer> k_Renderers = new List<Renderer>();

        void Awake()
        {
            DisablePlanesScenePicking();
        }

        public void DisablePlanesScenePicking()
        {
#if UNITY_EDITOR
            // Disabling picking does not work in simulation scene since it is a preview scene, so disable
            // renderers if in the simulated environment scene
            var disableRenderers = false;
            var getSimEnvironmentScene = EditorOnlyDelegates.GetSimulatedEnvironmentScene;
            if (getSimEnvironmentScene != null)
                disableRenderers = gameObject.scene == getSimEnvironmentScene();

            if (disableRenderers)
            {
                GetComponentsInChildren(k_Renderers);
                foreach (var rend in k_Renderers)
                {
                    rend.enabled = false;
                }
            }
            else
            {
                SceneVisibilityManager.instance.DisablePicking(gameObject, true);
            }
#endif
        }
    }
}
