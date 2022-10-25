using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.MARS.Build
{
    static class PackageVersionUtility
    {
        const string k_PackageCacheName = "PackageCache";
        static bool s_PackageLogLock;
        static Dictionary<string, PackageVersion> s_PackageCache;

        [Callbacks.DidReloadScripts]
        static void OnReloadScripts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode)
                return;

            Application.logMessageReceived += OnLogMessage;
        }

        static void OnLogMessage(string message, string stackTrace, LogType logType)
        {
            if (logType == LogType.Error && message.Contains(k_PackageCacheName) && !s_PackageLogLock)
            {
                UpdatePackageVersions();
                s_PackageLogLock = true;
            }
        }

        static void UpdatePackageVersions()
        {
            if (s_PackageCache == null)
                s_PackageCache = new Dictionary<string, PackageVersion>();

            s_PackageCache.Clear();
            var request = PackageManager.Client.List(true, true);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(50);
            }
            foreach (var package in request.Result)
            {
                s_PackageCache.Add(package.name, new PackageVersion(package.version));
            }

            s_PackageLogLock = false;
        }

        internal static PackageVersion GetPackageVersion(string packageName)
        {
            if (s_PackageCache == null)
            {
                s_PackageCache = new Dictionary<string, PackageVersion>();
                UpdatePackageVersions();
            }

            return s_PackageCache.TryGetValue(packageName, out var version) ? version : default;
        }

        internal static PackageVersion ToMajor(this PackageVersion value)
        {
            return new PackageVersion(value.MajorVersion, 0, 0,  0, false);
        }

        internal static PackageVersion ToMajorMinor(this PackageVersion value)
        {
            return new PackageVersion(value.MajorVersion, value.MinorVersion, 0, 0, false);
        }

        internal static PackageVersion ToMajorMinorPatch(this PackageVersion value)
        {
            return new PackageVersion(value.MajorVersion, value.MinorVersion, value.PatchVersion, 0, false);
        }
    }
}
