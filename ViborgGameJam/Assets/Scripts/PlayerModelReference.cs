using Unity.Entities;

[GenerateAuthoringComponent]
public struct PlayerModelReference : IComponentData {
    public Entity Value;
}