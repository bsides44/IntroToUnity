using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global

namespace UnityEditor.MARS.Build
{
    [Obsolete]
    [MovedFrom("Unity.MARS.Build")]
    public abstract class ElectiveExtensions : IPreprocessBuildWithReport
    {
        [Obsolete] public int callbackOrder => 0;

        [Obsolete] public abstract void OnPreprocessBuild(BuildReport report);

        [Obsolete] public static string RunReport(SupportedConfiguration config)
        {
            return string.Empty;
        }

        [Obsolete]
        public class SupportedConfiguration
        {
            [Obsolete] public string FriendlyName { get; }
            [Obsolete] public BuildTarget[] BuildTargets { get; }
            [Obsolete] public PackageItem[] Dependencies { get; }
            [Obsolete] public DefineSymbolItem[] DefineSymbols { get; }
            [Obsolete] public UnityEngine.Rendering.GraphicsDeviceType[] SupportedGraphicsAPIs { get; }
            [Obsolete] public string[] SupportedEditorVersions { get; }
            [Obsolete] public ReportOutputLevels ReportOutputLevel { get; set; }
            [Obsolete] public bool SupportedGraphicsAPIsIsOrdered { get; }

            [Obsolete] public SupportedConfiguration(string friendlyName = "", BuildTarget[] buildTargets = null,
                PackageItem[] dependencies = null, DefineSymbolItem[] defineSymbols = null,
                UnityEngine.Rendering.GraphicsDeviceType[] supportedGraphicsApis = null,
                string[] supportedEditorVersions = null,
                ReportOutputLevels reportOutputLevel = ReportOutputLevels.WarnOnIssue, bool supportedGraphicsApIsIsOrdered = false)
            {
            }

            [Obsolete]
            public enum ReportOutputLevels
            {
                ReportDisabled,
                DebugLogOnly,
                WarnOnIssue,
                ErrorOnIssue
            }
        }

        [Obsolete]
        public class PackageItem
        {
            [Obsolete] public string Name { get; }
            [Obsolete] public string Version { get; }
            [Obsolete] public bool Required { get; }
            [Obsolete] public PackageItem(string name, string version, bool required = true) { }
        }

        [Obsolete]
        public class DefineSymbolItem
        {
            [Obsolete] public string Name { get; }
            [Obsolete] public bool Required { get; }
            [Obsolete] public bool Present { get; }

            [Obsolete] public DefineSymbolItem(string name, bool required, bool present = false) { }
        }
    }
}
