using System.Collections;
using Unity.MARS.Actions;
using Unity.MARS.Conditions;
using Unity.MARS.Rules;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Assertions;

namespace Unity.MARS.Tests
{
    [AddComponentMenu("")]
    class InstantiateProxyRuleSetTest : MonoBehaviour, IMonoBehaviourTest
    {
        public bool IsTestFinished { get; private set; }

        IEnumerator Start()
        {
            // Initialize MARSSession
            MARSSession.TestMode = true;
            MARSSession.EnsureRuntimeState();
            var marsSession = MARSSession.Instance;

            // Wait one frame to do the test, this ensures that the test will run after the MARSSession initialization
            yield return null;

            // Create the GameObjects and build the hierarchy
            var proxyRuleSetGO = new GameObject("Proxy Rule Set");
            var replicatorGO = new GameObject("Replicator");
            var proxyGO = new GameObject("Proxy");
            // A primitive already have all components we need for the BuildSurfaceAction
            var actionGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            actionGO.name = "Action";

            replicatorGO.transform.parent = proxyRuleSetGO.transform;
            proxyGO.transform.parent = replicatorGO.transform;
            actionGO.transform.parent = proxyGO.transform;

            // Add components
            proxyRuleSetGO.SetActive(false);

            proxyRuleSetGO.AddComponent<ProxyRuleSet>();

            var replicator = replicatorGO.AddComponent<Replicator>();

            proxyGO.AddComponent<Proxy>();
            proxyGO.AddComponent<ShowChildrenOnTrackingAction>();
            proxyGO.AddComponent<SetPoseAction>();
            proxyGO.AddComponent<IsPlaneCondition>();

            actionGO.GetComponent<MeshFilter>().mesh = null;
            var buildSurfaceAction = actionGO.AddComponent<BuildSurfaceAction>();

            proxyRuleSetGO.SetActive(true);

            // Validate Functionality Injection
            var replicatorHasProvider = replicator.HasProvider();
            var buildSurfaceActionHasProvider = buildSurfaceAction.HasProvider();

            // Force destroy MARSSession and GameObjects here to prevent errors in the following tests
            if (proxyRuleSetGO)
                Destroy(proxyRuleSetGO);
            if (marsSession)
                Destroy(marsSession.gameObject);

            MARSSession.TestMode = false;
            IsTestFinished = true;
            Assert.IsTrue(replicatorHasProvider);
            Assert.IsTrue(buildSurfaceActionHasProvider);
        }
    }
}
