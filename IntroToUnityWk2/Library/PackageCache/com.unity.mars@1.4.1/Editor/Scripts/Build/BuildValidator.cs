using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.MARS;
using Unity.MARS.Data;
using Unity.MARS.Settings;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly:InternalsVisibleTo("Unity.MARS.Build.ARCore")]
[assembly:InternalsVisibleTo("Unity.MARS.Build.ARKit")]
namespace UnityEditor.MARS.Build
{
    [InitializeOnLoad]
    static class BuildValidator
    {
        const string k_NewVersionDialogTitle = "Open Project Validation";
        const string k_NewVersionDialogMessage = "A new version of MARS has been installed, and your project may not " +
            "be configured for this version of MARS. Would you like to check your settings with Project Validation? " +
            "You can access the Project Validator at any time with Project Settings > Project Validation.";

        static Dictionary<BuildTargetGroup, List<BuildValidationRule>> s_PlatformRules =
            new Dictionary<BuildTargetGroup, List<BuildValidationRule>>();

        internal static Dictionary<BuildTargetGroup, List<BuildValidationRule>> PlatformRules => s_PlatformRules;

        static BuildValidator()
        {
            PackageVersionWatcher.packageUpdated += OpenProjectValidator;
        }

        static void OpenProjectValidator()
        {
            if (EditorUtility.DisplayDialog(k_NewVersionDialogTitle, k_NewVersionDialogMessage, "Open", "Cancel"))
                SettingsService.OpenProjectSettings(MarsProjectValidationSettingsProvider.ProjectValidationSettingsPath);
        }

        internal static void AddRules(BuildTargetGroup group, IEnumerable<BuildValidationRule> rules)
        {
            if (s_PlatformRules.TryGetValue(group, out var groupRules))
            {
                groupRules.AddRange(rules);
            }
            else
            {
                groupRules = new List<BuildValidationRule>(rules);
                s_PlatformRules.Add(group, groupRules);
            }
        }

        internal static void GetCurrentValidationIssues(HashSet<BuildValidationRule> failures,
            BuildTargetGroup buildTargetGroup)
        {
            failures.Clear();
            if (!s_PlatformRules.TryGetValue(buildTargetGroup, out var rules))
                return;

            var inPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
            foreach (var validation in rules)
            {
                // If current scene is prefab isolation do not run scene validation
                if (inPrefabStage && validation.sceneOnlyValidation)
                    continue;

                if (validation.checkPredicate == null)
                    failures.Add(validation);
                else if (!validation.checkPredicate.Invoke())
                    failures.Add(validation);
            }
        }

        internal static bool HasTypesInSceneSetup(IEnumerable<Type> subscribers)
        {
            if (Application.isPlaying)
                return false;

            foreach (var sceneSetup in EditorSceneManager.GetSceneManagerSetup())
            {
                if (!sceneSetup.isLoaded)
                    continue;

                var scene = SceneManager.GetSceneByPath(sceneSetup.path);

                foreach (var go in scene.GetRootGameObjects())
                {
                    if (subscribers.Any(subscriber => go.GetComponentInChildren(subscriber, true)))
                        return true;
                }
            }

            return false;
        }

        internal static bool HasTraitDefinitionInSceneSetup(IEnumerable<TraitDefinition> traits)
        {
            if (traits == null)
                return false;

            var session = MARSSession.Instance;
            if (session == null)
                return false;

            foreach (var traitRequirement in session.requirements.TraitRequirements)
            {
                if (traits.Contains(traitRequirement.Definition))
                    return true;
            }

            return false;
        }

        internal static bool HasRulesForPlatform(BuildTargetGroup buildTarget)
        {
            return s_PlatformRules.TryGetValue(buildTarget, out _);
        }
    }
}
