using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]

public struct BulletPrefabReference : IComponentData {
    public Entity Value;

    public BulletPrefabReference(Entity value) {
        Value = value;
    }
}
