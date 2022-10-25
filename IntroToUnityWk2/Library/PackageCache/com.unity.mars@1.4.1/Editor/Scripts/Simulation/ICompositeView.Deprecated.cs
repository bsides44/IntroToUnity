using System;

namespace UnityEditor.MARS.Simulation.Rendering
{
    partial interface ICompositeView
    {
        /// <summary>
        /// Context view type of the view
        /// </summary>
        [Obsolete]
        ContextViewType ContextViewType { get; }
    }
}
