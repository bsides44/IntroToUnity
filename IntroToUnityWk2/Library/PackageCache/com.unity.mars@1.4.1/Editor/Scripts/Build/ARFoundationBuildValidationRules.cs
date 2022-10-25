namespace UnityEditor.MARS.Build
{
    static class ARFoundationValidatorRules
    {
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";
        const string k_XRManagementPackage = "com.unity.xr.management";
        const string k_MarsARFoundationProvidersPackage = "com.unity.mars-ar-foundation-providers";
        const string k_MarsPackage = "com.unity.mars";

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var globalARFoundationRules = new[]
            {
                new BuildValidationRule()
                {
                    name = "AR Foundation package",
                    message = "'AR Foundation' package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `AR Foundation` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_ARFoundationPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "XR Plugin Management package",
                    message = "'XR Plugin Management' package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_XRManagementPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `XR Plugin Management` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_XRManagementPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "Unity MARS AR Foundation Providers package",
                    message = "'Unity MARS AR Foundation Providers' package is not installed.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_MarsARFoundationProvidersPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `Unity MARS AR Foundation " +
                        "Providers` package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_MarsARFoundationProvidersPackage);
                    },
                },
                new BuildValidationRule()
                {
                    name = "Unity MARS AR Foundation Providers version matches Unity MARS",
                    message = "'Unity MARS AR Foundation Providers' and 'Unity MARS' versions should be on the same " +
                        "version number.",
                    checkPredicate = () =>
                    {
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_MarsARFoundationProvidersPackage).ToMajorMinor();
                        var marsVersion = PackageVersionUtility.GetPackageVersion(k_MarsPackage).ToMajorMinor();
                        return arfVersion != default && marsVersion != default && arfVersion == marsVersion;
                    },
                    fixItMessage = "Open Windows > Package Manager and update 'Unity MARS' and 'Unity MARS AR " +
                        "Foundation Providers' to the same version.",
                    fixIt = () =>
                    {
                        var arfVersion = PackageVersionUtility.GetPackageVersion(k_MarsARFoundationProvidersPackage).ToMajorMinor();
                        var marsVersion = PackageVersionUtility.GetPackageVersion(k_MarsPackage).ToMajorMinor();
                        if (arfVersion == default)
                            PackageManager.UI.Window.Open(k_MarsARFoundationProvidersPackage);
                        else if (marsVersion == default)
                            PackageManager.UI.Window.Open(k_MarsPackage);

                        PackageManager.UI.Window.Open(arfVersion < marsVersion ? k_MarsARFoundationProvidersPackage
                            : k_MarsPackage);
                    },
                    fixItAutomatic = false
                },
            };

            BuildValidator.AddRules(BuildTargetGroup.Standalone, globalARFoundationRules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, globalARFoundationRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, globalARFoundationRules);
            BuildValidator.AddRules(BuildTargetGroup.Lumin, globalARFoundationRules);
            BuildValidator.AddRules(BuildTargetGroup.WSA, globalARFoundationRules);
        }
    }
}
