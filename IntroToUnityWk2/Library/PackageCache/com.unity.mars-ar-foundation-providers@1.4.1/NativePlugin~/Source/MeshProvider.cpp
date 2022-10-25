#include "MeshProvider.h"
#include "ProviderContext.h"
#include "XR/IUnityXRMeshing.h"

static MARSMeshingProvider* sProvider;

UnitySubsystemErrorCode MARSMeshingProvider::Initialize()
{
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode MARSMeshingProvider::Start()
{
    return kUnitySubsystemErrorCodeSuccess;
}

void MARSMeshingProvider::Stop() { }

void MARSMeshingProvider::Shutdown() { }

UnitySubsystemErrorCode MARSMeshingProvider::GetMeshInfos(UnityXRMeshInfoAllocator* allocator)
{
    auto* meshInfos = m_Ctx.mesh->MeshInfoAllocator_Allocate(allocator, meshes.size());
    if (meshInfos == nullptr)
        return kUnitySubsystemErrorCodeFailure;

    int i = 0;
    for (auto p : meshes)
    {
        meshInfos[i].meshId = p.first;
        meshInfos[i].updated = p.second.dirty;
        meshInfos[i].priorityHint = 0;

        p.second.dirty = false;

        i++;
    }

    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode MARSMeshingProvider::AcquireMesh(const UnityXRMeshId* meshId, UnityXRMeshDataAllocator* allocator)
{
    const auto& mesh = meshes[*meshId];
    bool hasNormals = mesh.normals != nullptr;
    bool shortIndices = mesh.shortIndices;
    auto* meshData = m_Ctx.mesh->MeshDataAllocator_AllocateMesh(allocator,
        mesh.vertexCount,
        mesh.indexCount,
        shortIndices ? kUnityXRIndexFormat16Bit : kUnityXRIndexFormat32Bit,
        hasNormals ? kUnityXRMeshVertexAttributeFlagsNormals : static_cast<UnityXRMeshVertexAttributeFlags>(0),
        kUnityXRMeshTopologyTriangles);
    memcpy(meshData->positions, mesh.vertices, mesh.vertexCount * sizeof(UnityXRVector3));
    if (hasNormals)
        memcpy(meshData->normals, mesh.normals, mesh.vertexCount * sizeof(UnityXRVector3));
    meshData->tangents = nullptr;
    meshData->uvs = nullptr;
    meshData->colors = nullptr;
    if (shortIndices)
        memcpy(meshData->indices16, mesh.indices.indices16, mesh.indexCount * sizeof(uint16_t));
    else
        memcpy(meshData->indices32, mesh.indices.indices32, mesh.indexCount * sizeof(uint32_t));

    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode MARSMeshingProvider::ReleaseMesh(const UnityXRMeshId* meshId, const UnityXRMeshDescriptor* mesh)
{
    // Unity owns the memory, we don't need to do anything.
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode MARSMeshingProvider::SetMeshDensity(float density)
{
    return kUnitySubsystemErrorCodeFailure;
}

UnitySubsystemErrorCode MARSMeshingProvider::SetBoundingVolume(const UnityXRBoundingVolume* boundingVolume)
{
    return kUnitySubsystemErrorCodeFailure;
}

void MARSMeshingProvider::AddOrUpdateMesh16(UnityXRMeshId id, int numVertices, const float* vertices, const float* normals,
    int numTriangles, const uint16_t* indices)
{
    // Memory belongs to NativeArrays managed in C#, so we don't need to worry about existing allocations
    auto mesh = Mesh {
        numVertices,
        vertices,
        normals,
        numTriangles,
        indices,
        true,
        true,
    };

    meshes[id] = mesh;
}

void MARSMeshingProvider::AddOrUpdateMesh32(UnityXRMeshId id, int numVertices, const float* vertices, const float* normals,
    int numTriangles, const uint32_t* indices)
{
    // Memory belongs to NativeArrays managed in C#, so we don't need to worry about existing allocations
    auto mesh = Mesh {
        numVertices,
        vertices,
        normals,
        numTriangles,
        nullptr,
        false,
        true,
    };
    // Need C++20 to initialize this inline
    mesh.indices.indices32 = indices;

    meshes[id] = mesh;
}

void MARSMeshingProvider::RemoveMesh(UnityXRMeshId id)
{
    meshes.erase(id);
}

void MARSMeshingProvider::ClearMeshes()
{
    meshes.clear();
}

// C# binding
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API MARSXRSubsystem_AddOrUpdateMesh(uint64_t id1, uint64_t id2, int numVertices,
    const float* vertices, const float* normals, int numTriangles, const void* indices, bool shortIndices)
{
    UnityXRMeshId id = { id1, id2 };
    if (shortIndices)
        sProvider->AddOrUpdateMesh16(id, numVertices, vertices, normals, numTriangles, static_cast<const uint16_t*>(indices));
    else
        sProvider->AddOrUpdateMesh32(id, numVertices, vertices, normals, numTriangles, static_cast<const uint32_t*>(indices));
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API MARSXRSubsystem_RemoveMesh(uint64_t id1, uint64_t id2)
{
    UnityXRMeshId id = { id1, id2};
    sProvider->RemoveMesh(id);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API MARSXRSubsystem_ClearMeshes()
{
    sProvider->ClearMeshes();
}

// Initialization
static UnitySubsystemErrorCode UNITY_INTERFACE_API Mesh_Initialize(UnitySubsystemHandle handle, void* userData)
{
    auto& ctx = GetProviderContext(userData);

    ctx.meshingProvider = new MARSMeshingProvider(ctx, handle);
    sProvider = ctx.meshingProvider;

    UnityXRMeshProvider meshProvider{};
    meshProvider.userData = &ctx;

    meshProvider.GetMeshInfos =
        [](UnitySubsystemHandle handle, void* userData, UnityXRMeshInfoAllocator* allocator) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        return ctx.meshingProvider->GetMeshInfos(allocator);
    };
    meshProvider.AcquireMesh =
        [](UnitySubsystemHandle handle, void* userData, const UnityXRMeshId* meshId, UnityXRMeshDataAllocator* allocator) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        return ctx.meshingProvider->AcquireMesh(meshId, allocator);
    };
    meshProvider.ReleaseMesh =
        [](UnitySubsystemHandle handle, void* userData, const UnityXRMeshId* meshId, const UnityXRMeshDescriptor* mesh, void* pluginData) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        return ctx.meshingProvider->ReleaseMesh(meshId, mesh);
    };
    meshProvider.SetMeshDensity =
        [](UnitySubsystemHandle handle, void* userData, float density) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        return ctx.meshingProvider->SetMeshDensity(density);
    };
    meshProvider.SetBoundingVolume =
        [](UnitySubsystemHandle handle, void* userData, const UnityXRBoundingVolume* boundingVolume) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        return ctx.meshingProvider->SetBoundingVolume(boundingVolume);
    };

    ctx.mesh->RegisterMeshProvider(handle, &meshProvider);

    return ctx.meshingProvider->Initialize();
}

UnitySubsystemErrorCode Load_Meshing(ProviderContext& ctx)
{
    ctx.mesh = ctx.interfaces->Get<IUnityXRMeshInterface>();
    if (ctx.input == nullptr) {
        return kUnitySubsystemErrorCodeFailure;
    }

    UnityLifecycleProvider meshLifecycleHandler{};
    meshLifecycleHandler.userData = &ctx;

    meshLifecycleHandler.Initialize = &Mesh_Initialize;

    meshLifecycleHandler.Start = [](UnitySubsystemHandle handle, void* userData) -> UnitySubsystemErrorCode
    {
        auto& ctx = GetProviderContext(userData);
        auto r = ctx.meshingProvider->Start();
        return r;
    };

    meshLifecycleHandler.Stop = [](UnitySubsystemHandle handle, void* userData) -> void
    {
        auto& ctx = GetProviderContext(userData);
        ctx.meshingProvider->Stop();
    };

    meshLifecycleHandler.Shutdown = [](UnitySubsystemHandle handle, void* userData) -> void
    {
        auto& ctx = GetProviderContext(userData);
        ctx.meshingProvider->Shutdown();

        delete ctx.meshingProvider;
    };

    return ctx.mesh->RegisterLifecycleProvider("MARS XR Plugin", "MARS Meshing", &meshLifecycleHandler);
}
