using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.MARS.Providers.HoloLens
{
    /// <summary>
    /// Mesh provider options for HoloLens.
    /// </summary>
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    class HoloLensMeshProviderOptions : ScriptableSettings<HoloLensMeshProviderOptions>
    {
        /// <summary>
        /// Choices for mesh level of detail
        /// </summary>
        public enum MeshLevelOfDetail
        {
            // Values map to Microsoft.MixedReality.SceneUnderstanding.SceneMeshLevelOfDetail
            Low = 0,
            Medium = 1,
            High = 2,
            Maximum = 255,
        }

        /// <summary>
        /// Radius in meters to query for scene understanding data
        /// </summary>
        [SerializeField]
        [Tooltip("Radius to process in meters.")]
        float m_BoundingRadius = 10f;

        /// <summary>
        /// Refresh interval in seconds.  Setting this to &lt; 0 prevents refreshes
        /// </summary>
        [SerializeField]
        [Tooltip("Refresh interval in seconds.  Negative values will disable refresh.")]
        float m_RefreshInterval = -1f;

        /// <summary>
        /// The level of detail to request for scene understanding
        /// </summary>
        [SerializeField]
        [Tooltip("Mesh level of detail.")]
        MeshLevelOfDetail m_LevelOfDetail = MeshLevelOfDetail.High;

        /// <summary>
        /// Ask Scene Understanding for processed and classified meshes.  Turning off returns the raw mesh
        /// </summary>
        [SerializeField]
        [Tooltip("Scene Understanding produces classified, watertight meshes.")]
        bool m_UseSceneUnderstanding = true;

        internal float BoundingRadius { get => m_BoundingRadius; set { m_BoundingRadius = value; } }
        internal float RefreshInterval { get => m_RefreshInterval; set { m_RefreshInterval = value; } }

        internal MeshLevelOfDetail LevelOfDetail { get => m_LevelOfDetail; set { m_LevelOfDetail = value; } }

        internal bool UseSceneUnderstanding { get => m_UseSceneUnderstanding; set { m_UseSceneUnderstanding = value; } }
    }
}
