using Unity.Entities;

[GenerateAuthoringComponent]
public struct BulletDamage : IComponentData {

}

public struct BulletOrigin : IComponentData {
    public Entity Value;
}