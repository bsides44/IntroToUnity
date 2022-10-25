namespace UnityEditor.MARS.Build
{
    static class WSABuildValidationRules
    {
        const string k_WindowsMRPackage = "com.unity.xr.windowsmr";
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";

#if UNITY_2021_1_OR_NEWER
        static readonly PackageVersion k_MinimumWmrVersion = "5.0.0";
#elif UNITY_2020_2_OR_NEWER
        static readonly PackageVersion k_MinimumWmrVersion = "4.0.0";
#elif UNITY_2020_1_OR_NEWER
        static readonly PackageVersion k_MinimumWmrVersion = "3.0.0";
#elif UNITY_2019_3_OR_NEWER
        static readonly PackageVersion k_MinimumWmrVersion = "2.0.0";
#endif

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
#if UNITY_2021_2_OR_NEWER // Should be using Open XR after 2021.1
            var windowsXRGlobalRules = new BuildValidationRule[]{};
#else
            var windowsXRGlobalRules = new []
            {
                new BuildValidationRule()
                {
                    name = "Windows XR Plugin package",
                    message = "Windows XR Plugin package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_WindowsMRPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `Windows XR Plugin` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_WindowsMRPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "Windows XR Plugin version",
                    message = "Please update your version of the 'Windows XR Plugin' package to version " +
                        $"{k_MinimumWmrVersion} or greater.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_WindowsMRPackage);
                        return version != default && version >= k_MinimumWmrVersion;
                    },
                    fixItMessage = "Open Windows > Package Manager and update the `Windows XR Plugin` package to version " +
                        "2.0.1 or greater",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_WindowsMRPackage);
                    },
                    fixItAutomatic = false,
                },
                new BuildValidationRule()
                {
                    name = "AR Foundation Version Compatible with Windows XR Plugin version",
                    message = "Versions of the 'Windows XR Plugin' are only compatible with a range of 'AR Foundation' " +
                        "versions. The correct version of the 'AR Foundation' is usually that of the 'AR Subsystem' version " +
                        "that the 'Windows XR Plugin' is dependent on.",
                    checkPredicate = () =>
                    {
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        var wxrVersion = PackageVersionUtility.GetPackageVersion(k_WindowsMRPackage);
                        if (arfVersion == default || wxrVersion == default)
                            return false;

                        var wxrMajorMinor = wxrVersion.ToMajorMinor();
                        var arfMajorMinor = arfVersion.ToMajorMinor();

                        if (wxrMajorMinor > "5.0.0")
                        {
                            if (arfMajorMinor >= "4.1.0")
                                return true;
                        }
                        else if (wxrVersion >= "4.0.0")
                        {
                            if (arfVersion >= "4.0.2")
                                return true;
                        }
                        else if (wxrMajorMinor >= "3.0.0")
                        {
                            // AR Foundation versions of 3.0.1 to 4.0.x work with versions of Windows XR
                            // Except for preview versions of ARF v4.0.0-preview.x
                            if (arfVersion >= "3.0.1" && arfMajorMinor <= "4.1.0"
                                && !(arfMajorMinor == "4.0.0" && arfVersion.IsPreview))
                                return true;
                        }
                        else if (wxrVersion >= "2.0.3")
                        {
                            if (arfVersion >= "2.1.1" && arfMajorMinor < "2.2.0")
                                return true;
                        }

                        return false;
                    },
                    fixItMessage = "Open Windows > Package Manager and update the `AR Foundation` package to match the " +
                        "version of the 'AR Subsystem' version that the 'Windows XR Plugin' is dependant on.",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_ARFoundationPackage);
                    },
                    fixItAutomatic = false,
                },
            };
#endif

            BuildValidator.AddRules(BuildTargetGroup.WSA, windowsXRGlobalRules);
        }
    }
}
