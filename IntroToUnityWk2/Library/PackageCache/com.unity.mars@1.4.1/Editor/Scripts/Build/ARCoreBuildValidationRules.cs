using System.Linq;
using UnityEngine.Rendering;

#if INCLUDE_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.MARS.Build
{
    static class ARCoreBuildValidationRules
    {
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";
        const string k_ARCorePackage = "com.unity.xr.arcore";
        const string k_ARCoreMinimumPackage = "2.1.8";
#if INCLUDE_AR_FOUNDATION
        const string k_ProjectXRPlugInManagement = "Project/XR Plug-in Management";
#endif

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var androidGlobalRules = new[]
            {
                new BuildValidationRule
                {
                    name = "ARCore XR Plugin installed",
                    message = "ARCore XR Plugin package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARCorePackage);
                        return version != default && version >= k_ARCoreMinimumPackage;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `ARCore XR Plugin` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_ARCorePackage);
                    },
                },
                new BuildValidationRule
                {
                    name = "ARCore API level",
                    message = $"ARCore requires targeting minimum Android 7.0 'Nougat' API level 24 (currently {PlayerSettings.Android.minSdkVersion}).",
                    checkPredicate = () => PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel24,
                    fixItMessage = "Open Project Settings > Player Settings > Android tab and increase the 'Minimum API " +
                        "Level' to 'API Level 24' or greater.",
                    fixIt = () =>
                    {
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                    },
                    error = true
                },
                new BuildValidationRule
                {
                    name = "GLES Graphics API",
                    message = "Vulcan Graphics API is not supported with ARCore.",
                    checkPredicate = () => !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(
                        GraphicsDeviceType.Vulkan),
                    fixItMessage = "Open Project Settings > Player Settings > Android tab and disable " +
                        "'Auto Graphics API'. In the list of 'Graphics APIs' does not include 'Vulcan' by selecting the " +
                        "entry and pressing the '-' button at the bottom of the list. Also, make sure 'Graphics APIs' " +
                        "list includes 'OpenGLES3' and 'OpenGLES2' by pressing the '+' button at the bottom of the list.",
                    fixIt = () =>
                    {
                        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
                        {
                            GraphicsDeviceType.OpenGLES3,
                            GraphicsDeviceType.OpenGLES2,
                        });
                    },
                    error = true
                },
                new BuildValidationRule
                {
                    name = "ARCore Scripting Backend",
                    message = "The IL2CPP Scripting Backend is required for ARM64 Target Architecture, which is recommended for ARCore.",
                    checkPredicate = () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP,
                    fixItMessage = "Open Project Settings > Player Settings > Android tab and change 'Scripting Backend'" +
                        " to `IL2CPP`.",
                    fixIt = () =>
                    {
                        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    },
                },
                new BuildValidationRule
                {
                    name = "ARCore Target Architecture",
                    message = "ARM64 architecture is recommended for ARCore.",
                    helpLink = "https://developers.google.com/ar/64bit",
                    checkPredicate = () =>
                    {
                        var hasArm64 = (int)(PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
                        return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP
                            && hasArm64;
                    },
                    fixItMessage = "Open Project Settings > Player Settings > Android tab and ensure 'Scripting Backend'" +
                        " is set to `IL2CPP`. Then under `Target Architectures` enable `ARM64`.",
                    fixIt = () =>
                    {
                        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                        PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
                    },
                },
            };

            var androidARFoundationRules = new[]
            {
#if INCLUDE_AR_FOUNDATION
                new BuildValidationRule
                {
                    name = "ARCore plugin enabled",
                    message = "Please enable the 'ARCore' plugin in 'XR Plug-in Management'.",
                    checkPredicate = () =>
                    {
#if INCLUDE_XR_MANAGEMENT
                        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                            BuildTargetGroup.Android);
                        if (generalSettings == null)
                            return false;

                        var managerSettings = generalSettings.AssignedSettings;
#if XR_MANAGEMENT_4_0_OR_NEWER
                        return managerSettings.activeLoaders.Any(loader => loader.name == "AR Core Loader");
#else
                        return managerSettings.loaders.Any(loader => loader.name == "AR Core Loader");
#endif // XR_MANAGEMENT_4_0_OR_NEWER
#else
                        return false;
#endif // INCLUDE_XR_MANAGEMENT
                    },
                    fixItMessage = "Open Project Setting > XR Plug-in Management > Android tab and enable `ARCore`.",
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectXRPlugInManagement);
                    },
                    error = true,
                    fixItAutomatic = false
                },
#endif // INCLUDE_AR_FOUNDATION
                new BuildValidationRule
                {
                    name = "ARCore XR Plugin version matches AR Foundation",
                    message = "ARCore XR Plugin and AR Foundation versions should be on the same version number.",
                    checkPredicate = () =>
                    {
                        var arcoreVersion = PackageVersionUtility.GetPackageVersion(k_ARCorePackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return arcoreVersion != default && arfVersion != default
                            && arcoreVersion.ToMajorMinor() == arfVersion.ToMajorMinor();
                    },
                    fixItMessage = "Open Windows > Package Manager and update 'AR Foundation' and 'ARCore XR Plugin' " +
                        "to the same version.",
                    fixIt = () =>
                    {
                        var arcoreVersion = PackageVersionUtility.GetPackageVersion(k_ARCorePackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        if (arcoreVersion == default)
                            PackageManager.UI.Window.Open(k_ARCorePackage);
                        else if (arfVersion == default)
                            PackageManager.UI.Window.Open(k_ARFoundationPackage);

                        PackageManager.UI.Window.Open(arcoreVersion.ToMajorMinor() < arfVersion.ToMajorMinor()
                            ? k_ARCorePackage : k_ARFoundationPackage);
                    },
                    fixItAutomatic = false
                },
            };

            BuildValidator.AddRules(BuildTargetGroup.Android, androidGlobalRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, androidARFoundationRules);
        }
    }
}
