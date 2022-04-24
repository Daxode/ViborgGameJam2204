using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public class FrontImages : IComponentData {
    public Sprite[] _sprites;
}