using System.Collections;
using UnityEngine.TestTools;

namespace Unity.MARS.Tests
{
    class InstantiateProxyRuleSetTestList
    {
        [UnityTest]
        public IEnumerator InstantiateProxyRuletSet()
        {
            yield return new MonoBehaviourTest<InstantiateProxyRuleSetTest>();
        }
    }
}
