using System;
using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEditor.XRTools.Utils;
using UnityEngine;

namespace UnityEditor.MARS.Build
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    class BuildValidatorSettings : EditorScriptableSettings<BuildValidatorSettings>
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("Errors from Build Validator Rules will not cause the build to fail.")]
        bool m_IgnoreBuildErrors;
#pragma warning restore 649

        internal bool ignoreBuildErrors => m_IgnoreBuildErrors;
    }
}
