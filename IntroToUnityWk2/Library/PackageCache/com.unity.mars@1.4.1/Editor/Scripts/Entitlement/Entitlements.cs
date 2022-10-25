using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.MARS.MARSUtils;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.MARS
{
    [InitializeOnLoad]
    static class Entitlements
    {
        [Flags]
        public enum EntitlementStatus
        {
            None = 0,
            HasAccessToken = 1 << 0,
            HasRequestedEntitlements = 1 << 1,
            HasReceivedResponse = 1 << 2,
            HasFreeEntitlements = 1 << 3,
            HasBaseEntitlements = 1 << 4,

            EntitlementsCheckRequestProcess = HasAccessToken | HasRequestedEntitlements | HasReceivedResponse,
            HasAnyEntitlements = HasFreeEntitlements | HasBaseEntitlements,
        }

        static class Styles
        {
            public const string Title = "Mixed & Augmented Reality Studio";
            public const string TitleNarrow = "Mixed & Augmented\nReality Studio";
            public const string ButtonText = "Purchase Subscription";
            public const string LoginButtonText = "Sign in...";

            public const int MessageWidth = 270;
            public const int MessageWidthNarrow = 200;
            public const int WidthBreakpoint = 350;

            public static readonly GUIStyle TitleStyle;
            public static readonly GUIStyle SubscriptionMessageStyle;
            public static readonly GUIStyle ButtonStyle;

            static Styles()
            {
                SubscriptionMessageStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

                TitleStyle = new GUIStyle(SubscriptionMessageStyle)
                {
                    fontStyle = FontStyle.Bold
                };

                ButtonStyle = new GUIStyle("button")
                {
                    fixedHeight = 24,
                    fixedWidth = 170
                };
            }
        }

        const string k_BaseSku = "UnityMars";
        const string k_FreeSku = "UnityMarsFree";
        const string k_EntitlementEndpoint = "https://api.unity.com/v1/entitlements?userId={0}";
        const string k_EntitlementLastTime = "MARS.last_time_entitled";
        const string k_TokenExpiredMessageFormat =
            "MARS token issue: Please refresh your Unity Hub login by logging out and back in again\n{0}";
        const string k_NotSignedInMessage =
            "You are not signed in to your Unity Account. Click below to sign-in.";

        static readonly TimeSpan k_EntitlementDaysSpan = TimeSpan.FromDays(14);
        static readonly bool k_IsInSpecialBuildMode;
        static bool s_IsUpdating;
        static bool s_EntitlementsAlwaysFail;

        internal static bool EntitlementsAlwaysFail
        {
            get => s_EntitlementsAlwaysFail;
            set
            {
                var wasEntitled = IsEntitled;
                s_EntitlementsAlwaysFail = value;
                if (wasEntitled != IsEntitled)
                {
                    OnEntitlementsChanged?.Invoke(IsEntitled);
                }
            }
        }

        internal static bool DisableUnityEmailBypass { get; set; }

        public static DateTime? LastEntitledTime { get; private set; }
        public static DateTime? ExpiryTime { get; private set; }

        public static bool IsEntitled => !EntitlementsAlwaysFail &&
                                         (k_IsInSpecialBuildMode && HasFreeEntitlements || !HasEntitlementTokenExpired);

        public static bool HasRequest => (Status & EntitlementStatus.HasReceivedResponse) != 0;

        static bool HasFreeEntitlements => (Status & EntitlementStatus.HasFreeEntitlements) != 0;

        static bool HasEntitlementTokenExpired
        {
            get
            {
                var now = DateTime.UtcNow;
                return now >= (ExpiryTime ?? now);
            }
        }

        public static bool IsAccountSignedIn => (Status & EntitlementStatus.HasAccessToken) != 0;

        static bool HasAnyEntitlements => (Status & EntitlementStatus.HasAnyEntitlements) != 0;

        public static EntitlementStatus Status { get; private set; }

        public static event Action<bool> OnEntitlementsChanged;

        static Entitlements()
        {
            CheckAccessToken();
            UpdateEntitledTime();

            if (!HasEntitlementTokenExpired)
            {
                Status |= EntitlementStatus.HasBaseEntitlements;
            }

            if (Application.isBatchMode || BuildPipeline.isBuildingPlayer || CheckIsRunningTests())
            {
                Status |= EntitlementStatus.HasFreeEntitlements;
                k_IsInSpecialBuildMode = true;
            }

            StartUpdating();

            EditorOnlyDelegates.HasEntitlements += () => IsEntitled;
        }

        static bool CheckIsRunningTests()
        {
            var cmdLineArgs = Environment.GetCommandLineArgs();
            for (var i = 0; i < cmdLineArgs.Length; i++)
            {
                if (cmdLineArgs[i] != "-runTests")
                    continue;

                return true;
            }

            return false;
        }

        static void StartUpdating()
        {
            if(s_IsUpdating)
                return;

            s_IsUpdating = true;
            EditorApplication.update += Update;
        }

        static void StopUpdating()
        {
            s_IsUpdating = false;
            EditorApplication.update -= Update;
        }

        static void Update()
        {
            CheckAccessToken();

            if (!IsAccountSignedIn)
                return;

            StopUpdating();
            StartMarsEntitlementRequest();
        }

        static void StartMarsEntitlementRequest()
        {
            Status |= EntitlementStatus.HasRequestedEntitlements;

            StartEntitlementWebRequest();
        }

        static void ReceiveEntitlements(string licenceInfo, bool unityEmailOverride = false)
        {
            var wasEntitled = IsEntitled;
            Status &= ~EntitlementStatus.HasAnyEntitlements;
            Status |= !unityEmailOverride ? CheckForSubscriptionPlan(licenceInfo) : EntitlementStatus.HasBaseEntitlements;

            if(HasAnyEntitlements)
                MarkEntitledTime();

            Status |= EntitlementStatus.HasReceivedResponse;

            if(wasEntitled != IsEntitled)
                OnEntitlementsChanged?.Invoke(IsEntitled);
        }

        internal static EntitlementStatus CheckForSubscriptionPlan(string licenseInfo)
        {
            // Examples from GetLicenseInfo():
            //licenseInfo = "Unity Pro, Team License, iOS Pro, Android Pro, Windows Store Pro Serial number: I3-AB8P-9S8P-7ANM-EWWP-XXXX";
            //licenseInfo = "Unity Pro, UnityMars, Team License, Windows Store Pro Serial number: I3-ABCD-9S8P-7ANM-EWWP-XXXX";
            //licenseInfo = "UnityMarsFree, UnityMars Serial number: I3-ABCD-9S8P-7ANM-AAAA-XXXX";

            var check = licenseInfo.Contains(k_FreeSku) ? (true, Regex.Matches(licenseInfo, k_BaseSku).Count > 1) : (false, licenseInfo.Contains(k_BaseSku));

            EntitlementStatus result = default;

            if (check.Item1)
                result |= EntitlementStatus.HasFreeEntitlements;

            if (check.Item2)
                result |= EntitlementStatus.HasBaseEntitlements;

            return result;
        }

        static void StartEntitlementWebRequest()
        {
            var userEmail = CloudProjectSettings.userName.ToLower();
            if (!DisableUnityEmailBypass && (userEmail.EndsWith("@unity3d.com") || userEmail.EndsWith("@unity.com")))
            {
                ReceiveEntitlements(null, true);
                return;
            }

            var url = string.Format(k_EntitlementEndpoint, CloudProjectSettings.userId);
            var request = UnityWebRequest.Get(url);
            var bufferHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Cache-Control", "no-cache");
            request.SetRequestHeader("Authorization", $"Bearer {CloudProjectSettings.accessToken}");
            request.downloadHandler = bufferHandler;
            try
            {
                request.SendWebRequest().completed += _ =>
                {
                    var licenceInfo = bufferHandler.text;
                    if (licenceInfo.Contains("Expired Access Token") || licenceInfo.Contains("Invalid Access Token") ||
                        licenceInfo.Contains("132.107") || licenceInfo.Contains("132.108") ||
                        licenceInfo.Contains("120.003"))
                    {
                        Debug.LogWarningFormat(k_TokenExpiredMessageFormat, licenceInfo);
                        RefreshEntitlements();
                    }
                    else
                    {
                        ReceiveEntitlements(licenceInfo);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                RefreshEntitlements();
            }
        }

        static void CheckAccessToken()
        {
            Status = !string.IsNullOrEmpty(CloudProjectSettings.accessToken)
                ? Status | EntitlementStatus.HasAccessToken
                : Status & ~EntitlementStatus.HasAccessToken;
        }

        static void MarkEntitledTime()
        {
            var entitledTime = DateTime.UtcNow;
            EditorPrefs.SetString(k_EntitlementLastTime, entitledTime.ToString());
            UpdateEntitledTime(entitledTime);
        }

        internal static void RefreshEntitlements()
        {
            Status &= ~EntitlementStatus.EntitlementsCheckRequestProcess;
            StartUpdating();
        }

        internal static void ClearEntitlements()
        {
            ClearEntitledTime();
            Status = EntitlementStatus.None;
            OnEntitlementsChanged?.Invoke(false);
            StartUpdating();
        }

        static void ClearEntitledTime()
        {
            EditorPrefs.DeleteKey(k_EntitlementLastTime);
            ExpiryTime = null;
            LastEntitledTime = null;
        }

        static DateTime? GetLatestEntitledTime()
        {
            DateTime? result = null;
            var lastEntitlementTime = EditorPrefs.GetString(k_EntitlementLastTime, null);
            if (!string.IsNullOrEmpty(lastEntitlementTime)
                && DateTime.TryParse(lastEntitlementTime, out var time))
            {
                result = time;
            }
            return result;
        }

        static void UpdateEntitledTime(DateTime? entitledTime = null)
        {
            LastEntitledTime = entitledTime ?? GetLatestEntitledTime();

            ExpiryTime = LastEntitledTime + k_EntitlementDaysSpan;
        }

        public static bool EntitlementsCheckGUI(float viewWidth)
        {
            if(IsEntitled)
                return true;

            var displayNarrow = viewWidth < Styles.WidthBreakpoint;

            using (new GUILayout.HorizontalScope())
            {
                if (!IsAccountSignedIn)
                {
                    LoginPromptGUI(displayNarrow);
                }
                else
                {
                    UpSellGUI(displayNarrow);
                }
            }

            return false;
        }

        static void LoginPromptGUI(bool displayNarrow)
        {
            GUILayout.FlexibleSpace();

            using (new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();

                GUILayout.Label(displayNarrow ? Styles.TitleNarrow : Styles.Title, Styles.TitleStyle);
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label(k_NotSignedInMessage, Styles.SubscriptionMessageStyle,
                        GUILayout.Width(displayNarrow ? Styles.MessageWidthNarrow : Styles.MessageWidth));

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(Styles.LoginButtonText, Styles.ButtonStyle))
                    {
                        ShowLogin();
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        static void UpSellGUI(bool displayNarrow)
        {
            GUILayout.FlexibleSpace();

            using (new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();

                GUILayout.Label(displayNarrow ? Styles.TitleNarrow : Styles.Title, Styles.TitleStyle);
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label(
                        MARSSession.NotEntitledMessage, Styles.SubscriptionMessageStyle,
                        GUILayout.Width(displayNarrow ? Styles.MessageWidthNarrow : Styles.MessageWidth));

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(Styles.ButtonText, Styles.ButtonStyle))
                        Application.OpenURL(MARSSession.LicensingUrl);

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        static void ShowLogin()
        {
            const string unityConnectFullName = "UnityEditor.Connect.UnityConnect";
            const string instancePropertyName = "instance";
            const string showLoginMethodName = "ShowLogin";

            var unityConnectType = ReflectionUtils.FindType(t => t.FullName == unityConnectFullName);
            if (unityConnectType == null)
                return;

            var getInstanceInfo = unityConnectType.GetProperty(instancePropertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var showLoginInfo = unityConnectType.GetMethod(showLoginMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getInstanceInfo == null || showLoginInfo == null)
                return;

            var unityConnectInstance = getInstanceInfo.GetValue(null, null);
            if (unityConnectInstance != null)
                showLoginInfo.Invoke(unityConnectInstance, null);
        }
    }
}
