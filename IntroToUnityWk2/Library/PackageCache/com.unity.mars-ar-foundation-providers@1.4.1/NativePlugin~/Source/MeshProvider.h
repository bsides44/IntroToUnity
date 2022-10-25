#pragma once

#include "ProviderContext.h"
#include "XR/IUnityXRInput.h"
#include "XR/IUnityXRMeshing.h"
#include <map>

class MARSMeshingProvider : public ProviderImpl
{
public:
    MARSMeshingProvider(ProviderContext& ctx, UnitySubsystemHandle handle)
    : ProviderImpl(ctx, handle) {}
    virtual ~MARSMeshingProvider() {}

    UnitySubsystemErrorCode Initialize() override;
    UnitySubsystemErrorCode Start() override;

    UnitySubsystemErrorCode GetMeshInfos(UnityXRMeshInfoAllocator* allocator);
    UnitySubsystemErrorCode AcquireMesh(const UnityXRMeshId* meshId, UnityXRMeshDataAllocator* allocator);
    UnitySubsystemErrorCode ReleaseMesh(const UnityXRMeshId* meshId, const UnityXRMeshDescriptor* mesh);
    UnitySubsystemErrorCode SetMeshDensity(float density);
    UnitySubsystemErrorCode SetBoundingVolume(const UnityXRBoundingVolume* boundingVolume);

    void Stop() override;
    void Shutdown() override;

    // Methods exposed via C# wrappers
    void AddOrUpdateMesh16(UnityXRMeshId id, int vertexCount, const float* vertices, const float* normals, int indexCount, const uint16_t* indecies);
    void AddOrUpdateMesh32(UnityXRMeshId id, int vertexCount, const float* vertices, const float* normals, int indexCount, const uint32_t* indecies);
    void RemoveMesh(UnityXRMeshId id);
    void ClearMeshes();

private:
    struct Mesh
    {
        int vertexCount;
        const float* vertices;
        const float* normals;
        int indexCount;
        union {
            const uint16_t* indices16;
            const uint32_t* indices32;
        } indices;

        bool shortIndices;
        bool dirty;
    };

    std::map<UnityXRMeshId, Mesh, MeshIdLessThanComparator> meshes;
};
