using System.Collections.Generic;
using Unity.MARS;
using Unity.MARS.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityEditor.MARS.Build
{
    class BuildValidatorPreprocessBuild : IPreprocessBuildWithReport
    {
        /// <inheritdoc cref="IPreprocessBuildWithReport"/>
        int IOrderedCallback.callbackOrder => 0;

        /// <inheritdoc cref="IPreprocessBuildWithReport"/>
        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            var failures = new HashSet<BuildValidationRule>();
            BuildValidator.GetCurrentValidationIssues(failures, report.summary.platformGroup);
            if (failures.Count == 0)
                return;

            var fatal = false;
            foreach (var failure in failures)
            {
                if (failure.error)
                {
                    Debug.LogError($"{failure.message}\n{failure.fixItMessage}");
                    fatal = true;
                }
                else
                {
                    Debug.LogWarning($"{failure.message}\n{failure.fixItMessage}");
                }
            }

            if (!BuildValidatorSettings.instance.ignoreBuildErrors && fatal)
                throw new BuildFailedException("Unity MARS Build Failed.");

            Debug.LogWarning("Unity MARS build has Warnings.");
        }

        [OnOpenAsset]
        static bool ConsoleDoubleClicked(int instanceId, int line)
        {
            var objName = EditorUtility.InstanceIDToObject(instanceId).name;
            if (objName == nameof(BuildValidatorPreprocessBuild))
            {
                SettingsService.OpenProjectSettings(MarsProjectValidationSettingsProvider.ProjectValidationSettingsPath);
                return true;
            }

            return false;
        }
    }
}
