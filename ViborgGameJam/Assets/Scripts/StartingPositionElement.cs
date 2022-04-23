using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(4), GenerateAuthoringComponent]
public struct StartingPositionElement : IBufferElementData {
    public float3 Value;
    public static implicit operator StartingPositionElement(float3 v) => new StartingPositionElement { Value = v };
    public static implicit operator float3(StartingPositionElement v) => v.Value;
}