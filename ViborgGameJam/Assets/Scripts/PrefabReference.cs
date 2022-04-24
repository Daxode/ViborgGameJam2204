using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public class PrefabReference : IComponentData {
    public GameObject Prefab;
}