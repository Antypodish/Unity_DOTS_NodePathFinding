using Unity.Entities;

namespace Antypodish.DOTS
{
    
    public struct IsDebuggingTag : IComponentData {}

    public struct IsPausedTag : IComponentData {}

    public struct IsAliveTag : IComponentData {}

    public struct IsInitializedTag : IComponentData {}
    
    public struct IsSpawningTag : IComponentData {}

    public struct IsSpawningCompleteTag : IComponentData {}

    
}