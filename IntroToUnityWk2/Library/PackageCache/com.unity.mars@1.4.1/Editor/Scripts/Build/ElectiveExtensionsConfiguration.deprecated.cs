using System;
using UnityEditor.Build.Reporting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Build
{
    [Obsolete]
    public class ElectiveExtensionsConfigurationWsa : ElectiveExtensions
    {
        [Obsolete]
        public override void OnPreprocessBuild(BuildReport report) { }

        [Obsolete]
        public static readonly SupportedConfiguration ConfigWsa = new SupportedConfiguration();
    }

    [Obsolete]
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationAndroid : ElectiveExtensions
    {
        [Obsolete]
        public override void OnPreprocessBuild(BuildReport report) { }

        [Obsolete]
        public static readonly SupportedConfiguration ConfigAndroid = new SupportedConfiguration();
    }

    [Obsolete]
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationIOS : ElectiveExtensions
    {
        [Obsolete]
        public override void OnPreprocessBuild(BuildReport report) { }

        [Obsolete]
        public static readonly SupportedConfiguration ConfigIOS = new SupportedConfiguration();
    }

    [Obsolete]
    [MovedFrom("Unity.MARS.Build")]
    public class ElectiveExtensionsConfigurationLumin : ElectiveExtensions
    {
        [Obsolete]
        public override void OnPreprocessBuild(BuildReport report) { }

        [Obsolete]
        public static readonly SupportedConfiguration ConfigLumin = new SupportedConfiguration();
    }
}
