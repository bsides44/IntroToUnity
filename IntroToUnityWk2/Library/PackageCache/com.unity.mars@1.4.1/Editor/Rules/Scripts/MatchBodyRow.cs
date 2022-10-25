using Unity.MARS.Actions;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Rules.RulesEditor
{
    [CustomRuleNode(typeof(Transform), "ValidateTarget")]
    class MatchBodyRow : ContentRow
    {
        const string k_MatchBodyContent = "Match Body";

        // ReSharper disable once UnusedMember.Local -- method is used by 'CustomRuleNodeAttribute'
        static bool ValidateTarget(UnityObject target)
        {
            var transform = target as Transform;
            if (transform == null)
                return false;

            var parentTransform = transform.parent;
            if (parentTransform == null || parentTransform.GetComponent<MatchBodyPoseAction>() == null)
                return false;

            var animator = transform.GetComponent<Animator>();
            if (animator == null)
                return false;

            var avatar = animator.avatar;
            return avatar != null && avatar.isHuman;
        }

        protected override void OnInit()
        {
            ContentSupportsActions = false;
            base.OnInit();
        }

        protected override void SetupContent()
        {
            base.SetupContent();

            SetTitleName(k_MatchBodyContent);
            SetObjectSetupAreaVisible(false);
        }
    }
}
