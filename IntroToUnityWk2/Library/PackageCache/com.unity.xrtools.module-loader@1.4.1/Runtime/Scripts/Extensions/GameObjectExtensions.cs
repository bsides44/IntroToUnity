using UnityEngine;

namespace Unity.XRTools.ModuleLoader
{
    /// <summary>
    /// Extension methods for GameObjects
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Performs a functionality injection in the supplied GameObject and it's children.
        /// </summary>
        /// <param name="gameObject">The GameObject to perform the functionality injection</param>
        public static void InjectFunctionality(this GameObject gameObject)
        {
            var functionalityInjectionModule = ModuleLoaderCore.instance.GetModule<FunctionalityInjectionModule>();
            if (functionalityInjectionModule == null)
                return;

            var activeIsland = functionalityInjectionModule.activeIsland;
            if (activeIsland == null)
                return;

            activeIsland.InjectFunctionality(gameObject);
        }
    }
}
