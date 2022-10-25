using UnityEngine;

namespace Unity.XRTools.ModuleLoader
{
    /// <summary>
    /// Extension methods for all Functionality Subscribers
    /// </summary>
    public static class FunctionalitySubscriberExtensions
    {
        /// <summary>
        /// Check whether this subscriber is a MonoBehaviour and has an invalid provider, in this case a functionality
        /// injection is performed starting from the root transform.
        /// Useful for <c>MonoBehaviour</c>s that implement an <c>IFunctionalitySubscriber</c> and are instantiated at
        /// runtime.
        /// </summary>
        /// <param name="subscriber">The functionality subscriber on which to check for a provider</param>
        /// <typeparam name="TProvider">The provider type for which to check</typeparam>
        /// <returns>True if the subscriber has a provider of the given type</returns>
        public static void InjectFunctionalityIfNeeded<TProvider>(this IFunctionalitySubscriber<TProvider> subscriber)
            where TProvider : class, IFunctionalityProvider
        {
            // Functionality was already injected?
            if (subscriber.HasProvider() || !(subscriber is MonoBehaviour monoBehaviour))
                return;

            monoBehaviour.transform.root.gameObject.InjectFunctionality();
        }
    }
}
