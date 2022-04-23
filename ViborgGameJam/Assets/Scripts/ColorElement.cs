using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(4), GenerateAuthoringComponent]
public struct ColorElement : IBufferElementData {
    public Color Color;
    public static implicit operator ColorElement(Color c) => new ColorElement { Color = c };
    public static implicit operator Color(ColorElement c) => c.Color;
}