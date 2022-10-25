namespace UnityEditor.MARS.Build
{
    static class LuminBuildValidationRules
    {
        const string k_MagicLeapPackage = "com.unity.xr.magicleap";
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";

#if UNITY_2020_1_OR_NEWER
        static readonly PackageVersion k_MinimumLuminVersion = "5.0.0";
#elif UNITY_2019_3_OR_NEWER
        static readonly PackageVersion k_MinimumLuminVersion = "4.0.5";
#endif

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var luminGlobalRules = new[]
            {
                new BuildValidationRule()
                {
                    name = "Magic Leap XR Plugin package",
                    message = "'Magic Leap XR Plugin' package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_MagicLeapPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `Magic Leap XR Plugin` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_MagicLeapPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "Magic Leap XR Plugin version",
                    message = "Please update your version of the 'Magic Leap XR Plugin' package to version " +
                        $"{k_MinimumLuminVersion} or greater.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_MagicLeapPackage);
                        return version != default && version >= k_MinimumLuminVersion;
                    },
                    fixItMessage = "Open Windows > Package Manager and update the `Magic Leap XR Plugin` package to " +
                        $"version {k_MinimumLuminVersion} or greater",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_MagicLeapPackage);
                    },
                    fixItAutomatic = false,
                },
                new BuildValidationRule()
                {
                    name = "AR Foundation Version Compatible with Magic Leap XR Plugin version",
                    message = "Versions of the 'Magic Leap XR Plugin' are only compatible with a range of 'AR Foundation' " +
                        "versions. The correct version of the 'AR Foundation' is usually that of the 'AR Subsystem' version " +
                        "that the 'Magic Leap XR Plugin' is dependent on.",
                    checkPredicate = () =>
                    {
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        var luminVersion = PackageVersionUtility.GetPackageVersion(k_MagicLeapPackage);
                        if (arfVersion == default || luminVersion == default)
                            return false;

                        var luminMajorMinor = luminVersion.ToMajorMinor();
                        var arfMajorMinor = arfVersion.ToMajorMinor();

                        // Currently Lumin is at v6.2.2 anything larger is undefined
                        if (luminMajorMinor > "6.3.0")
                        {
                            if (arfVersion >= "4.0.2")
                                return true;
                        }
                        else if (luminMajorMinor >= "6.0.0")
                        {
                            if (arfMajorMinor >= "4.0.2")
                                return true;
                        }
                        else if (luminMajorMinor >= "5.0.0")
                        {
                            if (arfMajorMinor <= "3.1.0" && arfMajorMinor >= "3.0.1")
                                return true;
                        }
                        else if (luminVersion >= "4.0.5")
                        {
                            if (arfMajorMinor <= "2.1.0" && arfVersion >= "2.1.1")
                                return true;
                        }

                        return false;
                    },
                    fixItMessage = "Open Windows > Package Manager and update the `AR Foundation` package to match the " +
                        "version of the 'AR Subsystem' version that the 'Magic Leap XR Plugin' is dependant on.",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_ARFoundationPackage);
                    },
                    fixItAutomatic = false,
                }
            };

            BuildValidator.AddRules(BuildTargetGroup.Lumin, luminGlobalRules);
        }
    }
}
