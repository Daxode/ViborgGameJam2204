using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public class HealthSprites : IComponentData {
    public Sprite[] Values;
}