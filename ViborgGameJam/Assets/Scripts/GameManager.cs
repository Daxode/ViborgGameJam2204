using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Users;

[AlwaysUpdateSystem]
public partial class GameManager : SystemBase {
    private Controller _global;
    protected override void OnCreate() {
        _global = new Controller();
        _global.Enable();
        _global.Player.Start.started += context => {
            if (!HasSingleton<GameStartedTag>())
                EntityManager.CreateEntity(typeof(GameStartedTag));
        };

        InputUser.listenForUnpairedDeviceActivity = 4;
        InputUser.onUnpairedDeviceUsed += (control, ptr) => {
            if (!(control is ButtonControl))
                return;
            var user = InputUser.PerformPairingWithDevice(control.device);
            var playerController = new Controller();
            playerController.Enable();
            user.AssociateActionsWithUser(playerController);
            
            var e = EntityManager.CreateEntity(typeof(PuppetTag));
            EntityManager.AddComponentObject(e, new ControllerReference{Value = playerController});
            InputUser.listenForUnpairedDeviceActivity--;
        };
    }

    protected override void OnUpdate() {}
}

partial class PlayerSystem : SystemBase {
    protected override void OnCreate() {
        RequireForUpdate(GetEntityQuery(typeof(GameStartedTag)));
    }

    protected override void OnStartRunning() {
        var playerPrefab = GetSingleton<PlayerPrefabReference>().Value;
        var playerModelPrefab = this.GetSingleton<PlayerModelPrefabReference>().Value;
        Entities.WithAll<PuppetTag>().ForEach((ControllerReference c) => {
            Debug.Log($"Controller Registered: {c.Value.devices.Value[0].displayName}");
            var player = EntityManager.Instantiate(playerPrefab);

            // Setup player model
            var playerModel = EntityManager.GetComponentData<PlayerModelReference>(player).Value;
            var instance = Object.Instantiate(playerModelPrefab.gameObject);
            EntityManager.AddComponentObject(playerModel, instance.transform);
            EntityManager.AddComponentData(playerModel, new CopyTransformToGameObject());

            // Setup Player
            EntityManager.AddComponentObject(player, instance.GetComponent<Animator>());
            EntityManager.AddComponentObject(player, c);
            var playerMass = EntityManager.GetComponentData<PhysicsMass>(player);
            playerMass.InverseInertia = 0;
            EntityManager.SetComponentData(player, playerMass);
        }).WithStructuralChanges().Run();
    }

    protected override void OnUpdate() {
        var deltaTime = Time.DeltaTime;
        Entities.WithAll<PlayerTag>().ForEach((ControllerReference c, ref PhysicsVelocity vel) => {
            var moveDirection = c.Value.Player.Movement.ReadValue<Vector2>();
            vel.Linear += new float3(moveDirection,0)*deltaTime*20;
        }).WithoutBurst().Run();
        
        Entities.WithAll<PlayerTag>().ForEach((Animator a, ControllerReference c) => {
            var playerActions = c.Value.Player;
            a.SetBool("Walking", playerActions.Movement.IsPressed());
        }).WithoutBurst().Run();
    }
}

class ControllerReference : IComponentData {
    public Controller Value;
}

struct PuppetTag : IComponentData {}
struct PlayerTag : IComponentData {}