using Unity.Entities;
using UnityEngine.InputSystem;

[GenerateAuthoringComponent]
public class InputActionData : IComponentData {
    public InputAction movement;
}