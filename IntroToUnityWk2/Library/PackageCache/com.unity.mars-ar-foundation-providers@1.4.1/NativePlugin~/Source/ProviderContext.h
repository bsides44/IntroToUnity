#pragma once

#include <assert.h>
#include "XR/UnitySubsystemTypes.h"
#include "XR/IUnityXRTrace.h"

struct IUnityXRTrace;
struct IUnityXRInputInterface;
struct IUnityXRMeshInterface;

class MARSTrackingProvider;
class MARSMeshingProvider;

struct ProviderContext
{
    IUnityInterfaces* interfaces;
    IUnityXRTrace* trace;

    IUnityXRInputInterface* input;
    IUnityXRMeshInterface* mesh;
    MARSTrackingProvider* trackingProvider;
    MARSMeshingProvider* meshingProvider;
};

inline ProviderContext& GetProviderContext(void* data)
{
    assert(data != NULL);
    return *static_cast<ProviderContext*>(data);
}

ProviderContext* GetContextGlobal();

#define XR_TRACE_PTR (GetContextGlobal()->trace)

class ProviderImpl
{
public:
    ProviderImpl(ProviderContext& ctx, UnitySubsystemHandle handle)
        : m_Ctx(ctx)
        , m_Handle(handle)
    {}
    virtual ~ProviderImpl() {}

    virtual UnitySubsystemErrorCode Initialize() = 0;
    virtual UnitySubsystemErrorCode Start() = 0;

    virtual void Stop() = 0;
    virtual void Shutdown() = 0;

protected:
    ProviderContext& m_Ctx;
    UnitySubsystemHandle m_Handle;
};
