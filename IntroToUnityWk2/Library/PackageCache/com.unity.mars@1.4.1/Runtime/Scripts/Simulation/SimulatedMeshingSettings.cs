using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.MARS.Simulation
{
    /// <summary>
    /// Settings for simulated mesh providers
    /// </summary>
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class SimulatedMeshingSettings : ScriptableSettings<SimulatedMeshingSettings>
    {
        [SerializeField]
        [Tooltip("Sets the format of the index buffer data for simulated meshes")]
        IndexFormat m_IndexFormat;

        [SerializeField]
        [Tooltip("If enabled, simulated meshes will not include outward-facing exterior faces from environment meshes " +
            "that are rendered with the 'MARS/Room X-Ray' shader. The excluded faces are determined by the interior bounds defined by " +
            "the environment's XRay Region.")]
        bool m_ExcludeExteriorXRayGeometry = true;

        /// <summary>
        /// Format of the index buffer data for simulated meshes
        /// </summary>
        public IndexFormat IndexFormat
        {
            get => m_IndexFormat;
            set => m_IndexFormat = value;
        }

        /// <summary>
        /// If true, simulated meshes will not include outward-facing exterior faces from environment meshes that are rendered
        /// with the 'MARS/Room X-Ray' shader. The excluded faces are determined by the interior bounds defined by the environment's XRay Region.
        /// </summary>
        public bool ExcludeExteriorXRayGeometry
        {
            get => m_ExcludeExteriorXRayGeometry;
            set => m_ExcludeExteriorXRayGeometry = value;
        }
    }
}
