using Unity.Entities;

[GenerateAuthoringComponent]
public struct PickUpData : IComponentData {
    public PickUpType Type;
    public bool PickedUp;
}

public enum PickUpType {
    Empty,
    Desaturate,
    ColorChange,
    Animal
}