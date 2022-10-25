using System.Linq;

#if INCLUDE_XR_MANAGEMENT
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
#endif // INCLUDE_XR_MANAGEMENT

namespace UnityEditor.MARS.Build
{
    static class MarsValidatorRules
    {
        const string k_CcuDefineSymbol = "UNITY_CCU";

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var globalMarsRules = new []
            {
                new BuildValidationRule()
                {
                    name = "Unity Conditional Compilation Utility (Unity CCU) define symbol set",
                    message = "Unity CCU allows separate packages to define optional dependencies independently",
                    checkPredicate = () =>
                    {
                        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                        var scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup)
                            .Split(';').ToList();

                        return scriptingDefines.Contains(k_CcuDefineSymbol);
                    },
                    fixItMessage = "Open Project Settings > Player Settings under the active build target's 'Other Settings'  " +
                        "add 'UNITY_CCU' separated by a ';' to the existing 'Scripting Define Symbols'.",
                    fixIt = () =>
                    {
                        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                        var scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup)
                            .Split(';').ToList();

                        scriptingDefines.Insert(0, k_CcuDefineSymbol);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";",
                            scriptingDefines.ToArray()));
                    },
                    helpLink = "https://github.com/Unity-Technologies/ConditionalCompilationUtility",
                },
#if INCLUDE_XR_MANAGEMENT
                new BuildValidationRule()
                {
                    name = "MARS Simulation Subsystem enabled",
                    message = "MARS Simulation subsystem is used to fill out subsystems that can be used for XR " +
                        "Simulation in the Editor.",
                    checkPredicate = () =>
                    {
                        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                            BuildTargetGroup.Standalone);

                        if (generalSettings == null)
                            return false;

                        var managerSettings = generalSettings.AssignedSettings;
                        if (managerSettings == null)
                            return false;

#if XR_MANAGEMENT_4_0_OR_NEWER
                        return managerSettings.activeLoaders.Any(loader => loader.name == "MARSXR Subsystem Loader");
#else
                        return managerSettings.loaders.Any(loader => loader.name == "MARSXR Subsystem Loader");
#endif // XR_MANAGEMENT_4_0_OR_NEWER
                    },
                    fixItMessage = "Open Project Settings > XR Plug-in Management, under the Standalone Player tab " +
                        "(first tab), enable `MARS Simulation`.",

                    fixIt = () =>
                    {
                        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                            BuildTargetGroup.Standalone);

                        if (generalSettings == null)
                            return;

                        XRPackageMetadataStore.AssignLoader(generalSettings.Manager, "Unity.MARS.XRSubsystem.MARSXRSubsystemLoader", BuildTargetGroup.Standalone);
                    }
                },
#endif // INCLUDE_XR_MANAGEMENT
            };

            BuildValidator.AddRules(BuildTargetGroup.Standalone, globalMarsRules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, globalMarsRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, globalMarsRules);
            BuildValidator.AddRules(BuildTargetGroup.Lumin, globalMarsRules);
            BuildValidator.AddRules(BuildTargetGroup.WSA, globalMarsRules);
        }
    }
}
