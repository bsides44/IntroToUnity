using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Unity.MARS.Tests
{
    class EntitlementUtilsTests
    {
        static readonly TimeSpan k_TenSeconds = TimeSpan.FromSeconds(10);

        [UnityTest]
        public IEnumerator CurrentEditorMarsIsEntitled()
        {
            if (!string.IsNullOrEmpty(CloudProjectSettings.accessToken))
            {
                var disableBypass = Entitlements.DisableUnityEmailBypass;
                var alwaysFail = Entitlements.EntitlementsAlwaysFail;

                Entitlements.DisableUnityEmailBypass = false;
                Entitlements.EntitlementsAlwaysFail = false;

                try
                {
                    var startTime = DateTime.UtcNow;
                    Entitlements.ClearEntitlements();
                    while (!Entitlements.HasRequest && (DateTime.UtcNow - startTime) < k_TenSeconds)
                    {
                        yield return null;
                    }

                    Assert.True(Entitlements.IsEntitled);
                }
                finally
                {
                    Entitlements.DisableUnityEmailBypass = disableBypass;
                    Entitlements.EntitlementsAlwaysFail = alwaysFail;
                }
            }
            else
            {
                Assert.Ignore("Must be signed in to test entitlement status.");
            }
        }

        [Test]
        public void CheckSubscriptionPlanText()
        {
            Assert.IsTrue(Entitlements.CheckForSubscriptionPlan("UnityMars") == Entitlements.EntitlementStatus.HasBaseEntitlements);
            Assert.IsTrue(Entitlements.CheckForSubscriptionPlan("UnityMarsFree") == Entitlements.EntitlementStatus.HasFreeEntitlements);
            Assert.IsTrue(Entitlements.CheckForSubscriptionPlan("UnityMars, UnityMarsFree") == Entitlements.EntitlementStatus.HasAnyEntitlements);
            Assert.IsTrue((Entitlements.CheckForSubscriptionPlan("") & Entitlements.EntitlementStatus.HasAnyEntitlements) == 0);
        }
    }
}
