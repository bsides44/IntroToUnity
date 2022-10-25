using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.XRTools.Utils;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Rules.RulesEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    class CustomRuleNodeAttribute : Attribute
    {
        const BindingFlags k_BindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        class ConditionalRuleNodeType
        {
            internal Type RuleNodeType { get; }
            internal Func<UnityObject, bool> ValidateMethod { get; }

            internal ConditionalRuleNodeType(Type ruleNodeType)
            {
                RuleNodeType = ruleNodeType;
            }

            internal ConditionalRuleNodeType(Type ruleNodeType, Func<UnityObject, bool> validateMethod) : this(ruleNodeType)
            {
                ValidateMethod = validateMethod;
            }
        }

        static Dictionary<Type, List<ConditionalRuleNodeType>> s_RuleNodeTypesByTargetType;
        static Dictionary<Type, List<ConditionalRuleNodeType>> RuleNodeTypesByTargetType
        {
            get
            {
                if (s_RuleNodeTypesByTargetType != null)
                    return s_RuleNodeTypesByTargetType;

                s_RuleNodeTypesByTargetType = new Dictionary<Type, List<ConditionalRuleNodeType>>();

                ReflectionUtils.ForEachType(ruleNodeType =>
                {
                    if (!ruleNodeType.IsSubclassOf(typeof(RuleNode)))
                        return;

                    if (!(GetCustomAttribute(ruleNodeType, typeof(CustomRuleNodeAttribute))
                        is CustomRuleNodeAttribute customNodeAttr))
                        return;

                    Func<UnityObject, bool> validateMethod = null;
                    var validateMethodName = customNodeAttr.ValidateMethodName;
                    if (!string.IsNullOrEmpty(validateMethodName))
                    {
                        var validateMethodInfo = ruleNodeType.GetMethod(validateMethodName, k_BindingFlags,
                            null, new[] { typeof(UnityObject) }, null);
                        if (validateMethodInfo != null && validateMethodInfo.ReturnType == typeof(bool))
                        {
                            validateMethod = (Func<UnityObject, bool>)Delegate.CreateDelegate(typeof(Func<UnityObject, bool>),
                                    validateMethodInfo, false);
                        }
                    }

                    AddRuleNodeType(customNodeAttr.TargetType, ruleNodeType, validateMethod);

                    if (customNodeAttr.NodeForChildClasses)
                    {
                        ReflectionUtils.ForEachType(targetSubclass =>
                        {
                            if (targetSubclass.IsSubclassOf(customNodeAttr.TargetType))
                                AddRuleNodeType(targetSubclass, ruleNodeType, validateMethod);
                        });
                    }
                });

                return s_RuleNodeTypesByTargetType;
            }
        }

        Type TargetType { get; }
        bool NodeForChildClasses { get; }
        string ValidateMethodName { get; }

        internal CustomRuleNodeAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        internal CustomRuleNodeAttribute(Type targetType, bool nodeForChildClasses) : this(targetType)
        {
            NodeForChildClasses = nodeForChildClasses;
        }

        internal CustomRuleNodeAttribute(Type targetType, string validateMethodName) : this(targetType)
        {
            ValidateMethodName = validateMethodName;
        }

        internal CustomRuleNodeAttribute(Type targetType, bool nodeForChildClasses, string validateMethodName)
            : this(targetType)
        {
            NodeForChildClasses = nodeForChildClasses;
            ValidateMethodName = validateMethodName;
        }

        static void AddRuleNodeType(Type targetType, Type rowType, Func<UnityObject, bool> validateMethod)
        {
            if (!s_RuleNodeTypesByTargetType.TryGetValue(targetType, out var ruleNodeTypes))
            {
                ruleNodeTypes = new List<ConditionalRuleNodeType>();
                s_RuleNodeTypesByTargetType.Add(targetType, ruleNodeTypes);
            }

            if (validateMethod == null)
                ruleNodeTypes.Insert(0,new ConditionalRuleNodeType(rowType));
            else
                ruleNodeTypes.Add(new ConditionalRuleNodeType(rowType, validateMethod));
        }

        internal static Type GetRuleNodeType(UnityObject target)
        {
            var ruleNodeTypesByTargetType = RuleNodeTypesByTargetType;
            if (!ruleNodeTypesByTargetType.TryGetValue(target.GetType(), out var ruleNodeTypes))
                return null;

            for (var i = ruleNodeTypes.Count - 1; i >= 0; i--)
            {
                var conditionalRuleNodeType = ruleNodeTypes[i];
                if (conditionalRuleNodeType.ValidateMethod == null || conditionalRuleNodeType.ValidateMethod.Invoke(target))
                    return conditionalRuleNodeType.RuleNodeType;
            }

            return null;
        }
    }
}
