using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEditor.XRTools.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Simulation.Rendering
{
    /// <summary>
    /// Settings options for the <c>CompositeRenderModule</c>
    /// </summary>
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    [MovedFrom("Unity.MARS")]
    public class CompositeRenderModuleOptions : EditorScriptableSettings<CompositeRenderModuleOptions>
    {
        [SerializeField]
        [Tooltip("Use a simplified version of composite rendering with greater compatibility to different rendering setups.")]
        bool m_UseFallbackCompositeRendering;

        [SerializeField]
        [Tooltip("Use a camera stack for composite rendering of game view and other views that support camera stacking in Fallback composite rendering.")]
        bool m_UseCameraStackInFallback;

#pragma warning disable 649
        [SerializeField]
        bool m_IncludeUniversalRenderPipeline;
#pragma warning restore 649

        /// <summary>
        /// Use a simplified version of composite rendering with greater compatibility to different rendering setups.
        /// </summary>
        public bool UseFallbackCompositeRendering
        {
            get
            {
                if (GraphicsSettings.currentRenderPipeline != null)
                    return true;

                return m_UseFallbackCompositeRendering;
            }
            set { m_UseFallbackCompositeRendering = value; }
        }

        /// <summary>
        /// Use a camera stack for composite rendering of game view and other views that support camera stacking in Fallback composite rendering.
        /// </summary>
        internal bool UseCameraStackInFallback
        {
            get { return m_UseFallbackCompositeRendering && m_UseCameraStackInFallback; }
            set { m_UseCameraStackInFallback = value; }
        }

        internal bool CheckShadersNeedToRecompile()
        {
            var needsRecompile = false;
            var serializedObject = new SerializedObject(this);

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
            const bool includeUrp = true;
#else
            const bool includeUrp = false;
#endif

            if (m_IncludeUniversalRenderPipeline != includeUrp)
            {
                serializedObject.FindProperty(nameof(m_IncludeUniversalRenderPipeline)).boolValue = includeUrp;
                needsRecompile = true;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return needsRecompile;
        }
    }
}
