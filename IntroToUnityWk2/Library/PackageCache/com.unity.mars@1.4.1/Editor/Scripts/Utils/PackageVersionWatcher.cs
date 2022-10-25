using System;
using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.XRTools.Utils;
using UnityEngine;

namespace UnityEditor.MARS
{
    /// <summary>
    /// Notifies subscribers when the MARS package version changes
    /// </summary>
    [ScriptableSettingsPath(MARSCore.AssetMarsRootFolder + "/Settings")]
    [InitializeOnLoad]
    class PackageVersionWatcher : EditorScriptableSettings<PackageVersionWatcher>
    {
        static ListRequest s_PackageListRequest;

        [SerializeField]
        string m_PreviousMarsPackageVersion;

        internal static event Action packageUpdated;

        static PackageVersionWatcher()
        {
            EditorApplication.delayCall += StartCheck;
        }

        static void StartCheck()
        {
            if (Application.isBatchMode)
                return;

            s_PackageListRequest = Client.List(true);
            EditorApplication.update += PackageCheckUpdate;
        }

        static void PackageCheckUpdate()
        {
            if (s_PackageListRequest == null || !s_PackageListRequest.IsCompleted || EditorApplication.isCompiling)
                return;

            if (s_PackageListRequest.Status == StatusCode.Success)
            {
                foreach (var package in s_PackageListRequest.Result)
                {
                    if (package.name != MARSCore.PackageName)
                        continue;

                    if (package.version != instance.m_PreviousMarsPackageVersion)
                    {
                        instance.m_PreviousMarsPackageVersion = package.version;

                        EditorUtility.SetDirty(instance);

                        packageUpdated?.Invoke();
                    }

                    break;
                }
            }

            EditorApplication.update -= PackageCheckUpdate;
            s_PackageListRequest = null;
        }
    }
}
