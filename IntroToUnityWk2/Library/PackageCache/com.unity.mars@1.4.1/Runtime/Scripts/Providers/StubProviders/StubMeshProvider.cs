using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.MARS.Providers
{
    [AddComponentMenu("")]
    [ProviderSelectionOptions(ProviderPriorities.StubProviderPriority)]
    class StubMeshProvider : StubProvider, IProvidesMeshes
    {
#pragma warning disable 67
        public event Action<MRMesh> MeshAdded;
        public event Action<MRMesh> MeshUpdated;
        public event Action<MarsTrackableId> MeshRemoved;
#pragma warning restore 67

        public override void ConnectSubscriber(object obj)
        {
            this.TryConnectSubscriber<IProvidesMeshes>(obj);
        }

        public void GetMeshes(List<MRMesh> meshes) { }

        public void StartDetectingMeshes() { }

        public void StopDetectingMeshes() { }
    }
}
