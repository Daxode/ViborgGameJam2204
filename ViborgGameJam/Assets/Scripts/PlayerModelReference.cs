using Unity.Entities;

[GenerateAuthoringComponent]
public struct ModelReference : IComponentData {
    public Entity Value;
}