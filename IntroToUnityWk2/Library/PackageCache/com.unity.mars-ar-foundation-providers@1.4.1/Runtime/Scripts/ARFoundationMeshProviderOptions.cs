using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.MARS.Providers.ARFoundation
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    class ARFoundationMeshProviderOptions : ScriptableSettings<ARFoundationMeshProviderOptions>
    {
        [SerializeField]
        [Tooltip("Specifies the amount of tessellation to perform on the generated mesh (on supported platforms).\nZero is the least tesselation, one the most.")]
        [Range(0f, 1f)]
        float m_MeshDensity;

        internal float MeshDensity { get => m_MeshDensity;  set { m_MeshDensity = value; } }
    }
}
