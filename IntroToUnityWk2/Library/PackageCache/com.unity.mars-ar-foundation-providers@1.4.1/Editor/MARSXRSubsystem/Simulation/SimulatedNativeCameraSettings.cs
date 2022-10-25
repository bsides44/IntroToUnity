using UnityEditor.XRTools.Utils;
using UnityEngine;

#if MODULE_URP_ENABLED
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.MARS.XRSubsystem
{
    class SimulatedNativeCameraSettings : EditorScriptableSettings<SimulatedNativeCameraSettings>
    {
        [SerializeField]
#if MODULE_URP_ENABLED
        ForwardRendererData m_CompositeForwardRendererData;
#else
        ScriptableObject m_CompositeForwardRendererData;
#endif

        internal ScriptableObject compositeForwardRendererData => m_CompositeForwardRendererData;
    }
}
