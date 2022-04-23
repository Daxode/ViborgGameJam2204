using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;

public partial class GameManager : SystemBase {
    private Controller _global;

    private EntityQuery colorQuery;
    protected override void OnCreate() {
        colorQuery = GetEntityQuery(typeof(ColorElement));
        RequireForUpdate(colorQuery);
    }

    protected override void OnStartRunning() {
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

            var uiDocumentEntity = GetSingletonEntity<UIDocument>();
            var root = EntityManager.GetComponentObject<UIDocument>(uiDocumentEntity).rootVisualElement;
            var playerColors = GetBuffer<ColorElement>(colorQuery.GetSingletonEntity());
            var playerThingAsset = EntityManager.GetComponentObject<PlayerThingUXMLReference>(GetSingletonEntity<PlayerThingUXMLReference>()).Value;
            var playerThing = playerThingAsset.Instantiate();
            playerThing.Q("Background").style.backgroundColor = playerColors[4-InputUser.listenForUnpairedDeviceActivity].Color;
            root.Q("Players").Add(playerThing);
            
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
            var playerModel = EntityManager.GetComponentData<ModelReference>(player).Value;
            var instance = Object.Instantiate(playerModelPrefab.gameObject);
            EntityManager.AddComponentObject(playerModel, instance.transform);
            EntityManager.AddComponentData(playerModel, new GameObjectStateComponent{GameObject = instance.transform.gameObject});
            EntityManager.AddComponentData(playerModel, new CopyTransformToGameObject());

            // Setup Player
            EntityManager.AddComponentObject(player, instance.GetComponent<Animator>());
            EntityManager.AddComponentObject(player, instance.GetComponent<SpriteRenderer>());
            EntityManager.AddComponentObject(player, c);
            var playerMass = EntityManager.GetComponentData<PhysicsMass>(player);
            playerMass.InverseInertia = 0;
            EntityManager.SetComponentData(player, playerMass);
        }).WithStructuralChanges().Run();

        var playerColors = GetBuffer<ColorElement>(GetSingletonEntity<ColorElement>());
        Entities.WithAll<PlayerTag>().ForEach((int entityInQueryIndex, SpriteRenderer renderer) => {
            renderer.color = playerColors[entityInQueryIndex];
        }).WithoutBurst().Run();
        
        var startingPositions = GetBuffer<StartingPositionElement>(GetSingletonEntity<StartingPositionElement>());
        Entities.WithAll<PlayerTag>().ForEach((int entityInQueryIndex, ref Translation t) => {
            t.Value = startingPositions[entityInQueryIndex];
        }).WithoutBurst().Run();
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

        var bulletPrefab = GetSingleton<BulletPrefabReference>().Value;
        var bulletModelPrefab = this.GetSingleton<BulletModelPrefabReference>().Value;
        
        Entities.WithAll<PlayerTag>().ForEach((Entity e, ControllerReference c, ref Cooldown cooldown, in Translation translation) => {
            cooldown.TimeLeft -= deltaTime;
            if (cooldown.TimeLeft > 0) return;
            
            float2 direction = c.Value.Player.ShootingDirection.ReadValue<Vector2>();
            if (!math.any(math.abs(direction) > 0.1f)) return;

            cooldown.TimeLeft = cooldown.Interval;
            var bullet = EntityManager.Instantiate(bulletPrefab);
            var bulletModel = EntityManager.GetComponentData<ModelReference>(bullet).Value;                
            var instance = Object.Instantiate(bulletModelPrefab);
                
            EntityManager.SetName(bullet, "Bullet");
            EntityManager.SetComponentData(bullet, translation);
            EntityManager.SetComponentData(bullet, new PhysicsVelocity{Linear = new float3(direction,0)*10});
            EntityManager.AddComponentData(bullet, new BulletOrigin { Value = e });
                    
            EntityManager.SetName(bulletModel, "BulletModel");
            EntityManager.AddComponentData(bulletModel,  new GameObjectStateComponent{GameObject = instance.transform.gameObject});
            EntityManager.AddComponentObject(bulletModel, instance.transform);
        }).WithStructuralChanges().Run();
    }
}

class ControllerReference : IComponentData {
    public Controller Value;
}

struct PuppetTag : IComponentData {}
struct PlayerTag : IComponentData {}