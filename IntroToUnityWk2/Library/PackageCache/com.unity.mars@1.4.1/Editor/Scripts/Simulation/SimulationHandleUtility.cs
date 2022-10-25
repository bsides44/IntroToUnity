using System;
using UnityEngine;

#if !UNITY_2020_2_OR_NEWER
using System.Reflection;
#endif

namespace UnityEditor.MARS.Simulation
{
    static class SimulationHandleUtility
    {
#if !UNITY_2020_2_OR_NEWER
        static InternalPickClosestGODelegate s_InternalPickClosestGO;

        delegate GameObject InternalPickClosestGODelegate(Camera cam, int layers, Vector2 position, GameObject[] ignore,
            GameObject[] filter, out int materialIndex);
#endif

        [InitializeOnLoadMethod]
        static void SetupCustomPicking()
        {
            ConnectSimulationGameObjectPicking();
        }

        static void ConnectSimulationGameObjectPicking()
        {
#if UNITY_2020_2_OR_NEWER
            HandleUtility.pickGameObjectCustomPasses += OnPickGameObjectCustomPasses;
#else
            const BindingFlags bindingFlags = BindingFlags.Default | BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.NonPublic | BindingFlags.Public;

            const string pickClosestGameObjectDelegateName = "pickClosestGameObjectDelegate";
            const string internalPickClosestGOMethodName = "Internal_PickClosestGO";

            var handleUtilityType = typeof(HandleUtility);

            var pickClosestGameObjectDelegateFuncInfo = handleUtilityType.GetField(pickClosestGameObjectDelegateName, bindingFlags);
            var pickClosestGameObjectFuncDelegateInfo = typeof(SimulationHandleUtility).GetMethod(nameof(OnPickGameObjectCustomPasses), bindingFlags);
            var internalPickClosestGOMethodInfo = handleUtilityType.GetMethod(internalPickClosestGOMethodName, bindingFlags);

            if (pickClosestGameObjectDelegateFuncInfo == null || pickClosestGameObjectFuncDelegateInfo == null || internalPickClosestGOMethodInfo == null)
            {
                throw new InvalidOperationException();
            }

            var onPickGameObjectCustomPassesDelegate = Delegate.CreateDelegate(
                pickClosestGameObjectDelegateFuncInfo.FieldType, null, pickClosestGameObjectFuncDelegateInfo);

            pickClosestGameObjectDelegateFuncInfo.SetValue(null, onPickGameObjectCustomPassesDelegate);

            s_InternalPickClosestGO = (InternalPickClosestGODelegate)Delegate.CreateDelegate(typeof(InternalPickClosestGODelegate),
                internalPickClosestGOMethodInfo);

            if (s_InternalPickClosestGO == null)
            {
                throw new Exception("Cannot create delegate for 'HandleUtility.Internal_PickClosestGO'!");
            }
#endif
        }

        /// <summary>
        /// This method is used to check if the GameObject picking behavior of HandleUtility failed to select an object.
        /// This is not actually used to select a GameObject except for in versions of Unity before Unity 2020.2 where
        /// the internal GameObject picking is called after custom picking.
        /// </summary>
        /// <returns>The internally picked GameObject in Unity 2020.2+. If no object picked, or in earlier versions of Unity, then <c>null</c>.</returns>
        static GameObject OnPickGameObjectCustomPasses(Camera cam, int layers, Vector2 position, GameObject[] ignore,
            GameObject[] filter, out int materialIndex)
        {
            // Before Unity 2020.2 internal pick is called after custom picking
            // We need to make sure internal picking happens first since we are looking for a null selection.
#if !UNITY_2020_2_OR_NEWER
            // If the selection is a null click the internal pick method will be called twice.
            // Once here and since this method always returns null again in the HandleUtility.
            if (s_InternalPickClosestGO != null)
            {
                var selection = s_InternalPickClosestGO(cam, layers, position, ignore, filter, out materialIndex);
                if (selection != null)
                    return selection;
            }
#endif

            if (SimulationView.ActiveSimulationView is SimulationView simView && simView == EditorWindow.focusedWindow)
                simView.StartFadePulse();

            materialIndex = -1;
            return null;
        }
    }
}
