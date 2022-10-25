using System;
using Unity.MARS.Providers;
using Unity.MARS.Data;
using Unity.MARS.Query;
using UnityEngine;

#if ARKIT_4_1_OR_NEWER
using UnityEditor.XR.ARKit;
#endif

#if INCLUDE_XR_MANAGEMENT
using System.Linq;
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.MARS.Build
{
    static class ARKitBuildValidationRules
    {
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";
        const string k_ARKitPackage = "com.unity.xr.arkit";
        const string k_ARKitFacePackage = "com.unity.xr.arkit-face-tracking";
#if INCLUDE_AR_FOUNDATION
        const string k_ProjectXRPlugInManagement = "Project/XR Plug-in Management";
#endif

        static readonly Type[] k_FaceTrackingSubscribers = { typeof(IUsesFaceTracking), typeof(IUsesFacialExpressions) };
        static readonly Type[] k_LocationSubscribers = { typeof(IUsesGeoLocation) };
        static readonly TraitDefinition[] k_LocationTraits = { TraitDefinitions.GeoCoordinate, };

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var iOSGlobalRules = new[]
            {
                new BuildValidationRule()
                {
                    name = "ARKit XR Plugin package",
                    message = "ARKit XR Plugin package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARKitPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `ARKit XR Plugin` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_ARKitPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "iOS API level",
                    message = $"ARKit requires targeting minimum iOS 11.0.",
                    checkPredicate = () => float.Parse(PlayerSettings.iOS.targetOSVersionString) >= 11f,
                    fixItMessage = "Open Project Settings > Player Settings > iOS tab and increase the 'Target minimum " +
                        "iOS Version' to '11.0' or greater.",
                    fixIt = () =>
                    {
                        PlayerSettings.iOS.targetOSVersionString = "11.0";
                    },
                    error = true
                },
                new BuildValidationRule()
                {
                    name = "ARM 64",
                    message = "ARKit features require ARM64 build",
                    checkPredicate = () => PlayerSettings.GetArchitecture(BuildTargetGroup.iOS) == 1,
                    fixItMessage = "Open Project Settings > Player Settings > iOS tab and set 'Architecture' to 'ARM64'",
                    fixIt = () =>
                    {
                        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
                    },
                    error = true
                },
                new BuildValidationRule()
                {
                    name = "Camera permission text",
                    message = "Please set camera permission description in the Player Settings to be set to use the " +
                        "camera feed in the app.",
                    checkPredicate = () => !string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription),
                    fixItMessage = "Open Project Settings > Player Settings > iOS tab and set 'Camera Usage Description*'" +
                        " to a message explaining the camera usage.",
                    fixIt = () =>
                    {
                        PlayerSettings.iOS.cameraUsageDescription = "Augmented Reality requires the camera";
                    },
                },
                new BuildValidationRule()
                {
                    name = "Location permission text",
                    message = "Please set location permission description in the Player Settings to be set to use the " +
                        "geolocation in the app.",
                    checkPredicate = () =>
                    {
                        if (Application.isPlaying)
                            return false;

                        if (BuildValidator.HasTypesInSceneSetup(k_LocationSubscribers)
                            || BuildValidator.HasTraitDefinitionInSceneSetup(k_LocationTraits))
                        {
                            return !string.IsNullOrEmpty(PlayerSettings.iOS.locationUsageDescription);
                        }
                        return true;
                    },
                    fixItMessage = "Open Project Settings > Player Settings > iOS tab and set 'Location Usage Description*' " +
                        "to a message explaining the use of geolocation usage.",
                    fixIt = () =>
                    {
                        PlayerSettings.iOS.locationUsageDescription = "Location specific content requires location " +
                            "services";
                    },
                    error = BuildValidator.HasTypesInSceneSetup(k_LocationSubscribers),
                    sceneOnlyValidation = true,
                },
            };

            BuildValidator.AddRules(BuildTargetGroup.iOS, iOSGlobalRules);

            var iosARFoundationRules = new[]
            {
#if INCLUDE_AR_FOUNDATION
                new BuildValidationRule()
                {
                    name = "ARKit plugin enabled",
                    message = "Please enable the 'ARKit' plugin in 'XR Plug-in Management'.",
                    checkPredicate = () =>
                    {
#if INCLUDE_XR_MANAGEMENT
                        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                            BuildTargetGroup.iOS);
                        if (generalSettings == null)
                            return false;

                        var managerSettings = generalSettings.AssignedSettings;
#if XR_MANAGEMENT_4_0_OR_NEWER
                        return managerSettings.activeLoaders.Any(loader => loader.name == "AR Kit Loader");
#else
                        return managerSettings.loaders.Any(loader => loader.name == "AR Kit Loader");
#endif // XR_MANAGEMENT_4_0_OR_NEWER
#else
                        return false;
#endif // INCLUDE_XR_MANAGEMENT
                    },
                    fixItMessage = "Open Project Setting > XR Plug-in Management > iOS tab and enable `ARKit`.",
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectXRPlugInManagement);
                    },
                    error = true,
                    fixItAutomatic = false
                },
#endif // INCLUDE_AR_FOUNDATION
                new BuildValidationRule()
                {
                    name = "ARKit XR Plugin version matches AR Foundation",
                    message = "ARKit XR Plugin and AR Foundation versions should be on the same version number.",
                    checkPredicate = () =>
                    {
                        var arkitVersion = PackageVersionUtility.GetPackageVersion(k_ARKitPackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return arkitVersion != default && arfVersion != default
                            && arkitVersion.ToMajorMinor() == arfVersion.ToMajorMinor();
                    },
                    fixItMessage = "Open Windows > Package Manager and update 'AR Foundation' and 'ARKit XR Plugin' to " +
                        "the same version.",
                    fixIt = () =>
                    {
                        var arkitVersion = PackageVersionUtility.GetPackageVersion(k_ARKitPackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        if (arkitVersion == default)
                            PackageManager.UI.Window.Open(k_ARKitPackage);
                        else if (arfVersion == default)
                            PackageManager.UI.Window.Open(k_ARFoundationPackage);

                        PackageManager.UI.Window.Open(arkitVersion.ToMajorMinor() < arfVersion.ToMajorMinor()
                            ? k_ARKitPackage : k_ARFoundationPackage);
                    },
                    fixItAutomatic = false
                },
            };

            BuildValidator.AddRules(BuildTargetGroup.iOS, iosARFoundationRules);

            var iOSFaceTracking = new[]
            {
                new BuildValidationRule()
                {
                    name = "ARKit Face Tracking package",
                    message = "ARKit Face Tracking package is not installed.",
                    checkPredicate = () =>
                    {
                        if (Application.isPlaying)
                            return false;

                        if (BuildValidator.HasTypesInSceneSetup(k_FaceTrackingSubscribers))
                        {
                            var version = PackageVersionUtility.GetPackageVersion(k_ARKitFacePackage);
                            return version != default;
                        }

                        return true;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `ARKit Face Tracking` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_ARKitFacePackage);
                    },
                    sceneOnlyValidation = true,
                },
                new BuildValidationRule()
                {
                    name = "ARKit Face Tracking version",
                    message = "Please update your version of the 'ARKit XR Plugin' package to version 1.0.13 or greater.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARKitFacePackage);
                        return version != default && version >= "1.0.13";
                    },
                    fixItMessage = "Open Windows > Package Manager and update the `ARKit XR Plugin` package to version " +
                        "1.0.13 or greater",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_ARKitFacePackage);
                    },
                    fixItAutomatic = false,
                },
                new BuildValidationRule()
                {
                    name = "ARKit Face Tracking version compatible with AR Foundation",
                    message = "ARKit Face Tracking version is compatible with the version of AR Foundation.",
                    checkPredicate = () =>
                    {
                        var arFaceVersion = PackageVersionUtility.GetPackageVersion(k_ARKitFacePackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);

                        if (arFaceVersion == default || arfVersion == default)
                            return false;

                        var arFaceMajorMinorVersion = arFaceVersion.ToMajorMinor();
                        var arfMajorMinorVersion = arfVersion.ToMajorMinor();
                        var matchingVersionSwitch = new PackageVersion("3.0.0");

                        if (arFaceMajorMinorVersion < matchingVersionSwitch)
                        {
                            // ARKit Face in 1.0.x range line up with ARF versions in 2.1.x range
                            if (arFaceMajorMinorVersion < "1.1.0" && arfMajorMinorVersion == "2.1.0")
                                return true;

                            // ARKit Face in 1.1.x range line up with ARF versions in 2.2.x range
                            return arfMajorMinorVersion == "2.2.0";
                        }

                        // With ARF v3.x.x ARKit face jumps versions to match the version number of ARF
                        return arFaceMajorMinorVersion == arfMajorMinorVersion;
                    },
                    fixItMessage = "Open Windows > Package Manager and update 'AR Foundation' and 'ARKit Face Tracking' " +
                        "to compatible versions.",
                    fixIt = () =>
                    {
                        var arFaceVersion = PackageVersionUtility.GetPackageVersion(k_ARKitFacePackage);
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        if (arFaceVersion == default)
                            PackageManager.UI.Window.Open(k_ARKitFacePackage);
                        else if (arfVersion == default)
                            PackageManager.UI.Window.Open(k_ARFoundationPackage);

                        var arFaceMajorMinorVersion = arFaceVersion.ToMajorMinor();
                        var arfMajorMinorVersion = arfVersion.ToMajorMinor();
                        var matchingVersionSwitch = new PackageVersion("3.0.0");

                        if (arFaceMajorMinorVersion < matchingVersionSwitch)
                        {
                            // ARKit Face in 1.0.x range line up with ARF versions in 2.1.x range
                            if (arFaceMajorMinorVersion > "1.1.0" && arfMajorMinorVersion == "2.1.0")
                                PackageManager.UI.Window.Open(k_ARKitFacePackage);

                            // ARKit Face in 1.1.x range line up with ARF versions in 2.2.x range
                            if (arFaceMajorMinorVersion < "1.1.0" && arfMajorMinorVersion == "2.2.0")
                                PackageManager.UI.Window.Open(k_ARKitFacePackage);

                            PackageManager.UI.Window.Open(k_ARFoundationPackage);
                        }

                        // With ARF v3.x.x ARKit face jumps versions to match the version number of ARF
                        PackageManager.UI.Window.Open(arFaceMajorMinorVersion < arfMajorMinorVersion
                            ? k_ARKitFacePackage : k_ARFoundationPackage);
                    },
                    fixItAutomatic = false
                },
#if ARKIT_4_1_OR_NEWER
                new BuildValidationRule() {
                    name = "ARKit face tracking enabled",
                    message = "Please enable 'Face Tracking' in the ARKit XR Plug-in in the Project Settings.",
                    checkPredicate = () =>
                    {
                        if (BuildValidator.HasTypesInSceneSetup(k_FaceTrackingSubscribers))
                        {
                            return ARKitSettings.GetOrCreateSettings().faceTracking;
                        }

                        return true;
                    },
                    fixItMessage = "Open Project Setting > XR Plug-in Management > ARKit and enable `Face Tracking`.",
                    fixIt = () =>
                    {
                        ARKitSettings.GetOrCreateSettings().faceTracking = true;
                    },
                },
#endif
            };

            BuildValidator.AddRules(BuildTargetGroup.iOS, iOSFaceTracking);
        }
    }
}
