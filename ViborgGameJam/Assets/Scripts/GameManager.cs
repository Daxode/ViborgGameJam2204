﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

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
            if (!HasSingleton<GamePreloadTag>() && InputUser.listenForUnpairedDeviceActivity < 3)
                EntityManager.CreateEntity(typeof(GamePreloadTag));
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
            
            var root = EntityManager.GetComponentObject<UIDocument>(GetSingletonEntity<UIDocument>()).rootVisualElement;
            var playerColors = GetBuffer<ColorElement>(colorQuery.GetSingletonEntity());
            var playerThingAsset = EntityManager.GetComponentObject<PlayerThingUXMLReference>(GetSingletonEntity<PlayerThingUXMLReference>()).Value;
            var playerThing = playerThingAsset.Instantiate();
            playerThing.Q("Hat").style.unityBackgroundImageTintColor = playerColors[4-InputUser.listenForUnpairedDeviceActivity].Color;
            root.Q("Players").Add(playerThing);
            EntityManager.AddComponentObject(e, new PlayerThingReference{Element = playerThing});

            
            InputUser.listenForUnpairedDeviceActivity--;
        };
    }

    protected override void OnUpdate() {}
}

public class PlayerThingReference : IComponentData {
    public VisualElement Element;
}

partial class PreStartSystem : SystemBase {
    private EntityQuery puppetControllerQuery;
    private EntityQuery colorQuery;
    protected override void OnCreate() {
        RequireForUpdate(GetEntityQuery(typeof(GamePreloadTag)));
        colorQuery = GetEntityQuery(typeof(ColorElement));
    }

    protected override void OnStartRunning() {
        var playersConnected = puppetControllerQuery.CalculateEntityCount();

        var rnd = new Random((uint)DateTime.Now.Ticks);
        var numbers = new NativeArray<int>(playersConnected, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < numbers.Length; i++) {
            numbers[i] = i;
        }
        for (int i = 0; i < 10; i++) {
            var swapNumbers = rnd.NextInt2(playersConnected);
            if (swapNumbers.x == swapNumbers.y) continue;
            if (numbers[swapNumbers.x] == swapNumbers.y || numbers[swapNumbers.y] == swapNumbers.x) continue;
            (numbers[swapNumbers.y], numbers[swapNumbers.x]) = (numbers[swapNumbers.x], numbers[swapNumbers.y]);
        }

        Entities.WithStoreEntityQueryInField(ref puppetControllerQuery).WithAll<PuppetTag, ControllerReference>().ForEach((Entity e, int entityInQueryIndex, PlayerThingReference playerThing) => {
            var number = numbers[entityInQueryIndex];
            EntityManager.AddComponentData(e, new PlayerTargetIndex{Value = number});
            var playerColors = GetBuffer<ColorElement>(colorQuery.GetSingletonEntity());
            playerThing.Element.Q("Skull").style.unityBackgroundImageTintColor = playerColors[number].Color;
        }).WithStructuralChanges().Run();

        EntityManager.CreateEntity(typeof(GameStartedTag));
        numbers.Dispose();
    }

    protected override void OnUpdate() {}
}

partial class PlayerSystem : SystemBase {
    private EntityQuery _playerQuery;
    protected override void OnCreate() {
        RequireForUpdate(GetEntityQuery(typeof(GameStartedTag)));
        _playerQuery = GetEntityQuery(typeof(PlayerTag));
    }

    protected override void OnStartRunning() {
        var playerPrefab = GetSingleton<PlayerPrefabReference>().Value;
        var playerModelPrefab = this.GetSingleton<PlayerModelPrefabReference>().Value;
        Entities.WithAll<PuppetTag>().ForEach((ControllerReference c, in PlayerTargetIndex index) => {
            Debug.Log($"Controller Registered: {c.Value.devices.Value[0].displayName}");
            var player = EntityManager.Instantiate(playerPrefab);

            // Setup player model
            var playerModel = EntityManager.GetComponentData<ModelReference>(player).Value;
            var instance = Object.Instantiate(playerModelPrefab.gameObject);
            EntityManager.AddComponentObject(playerModel, instance.transform);
            EntityManager.AddComponentData(playerModel, new GameObjectStateComponent{GameObject = instance.transform.gameObject});
            EntityManager.AddComponentData(playerModel, new CopyTransformToGameObject());

            // Setup Player
            EntityManager.AddComponentObject(player, instance.GetComponentInChildren<PowerUpIndicatorRendererReference>());
            EntityManager.AddComponentObject(player, instance.GetComponent<Animator>());
            EntityManager.AddComponentObject(player, instance.GetComponent<SpriteRenderer>());
            EntityManager.AddComponentObject(player, c);
            EntityManager.AddComponentData(player, index);
            var h = GetComponent<HealthCooldown>(player);
            h.TimeLeft = h.Interval;
            EntityManager.SetComponentData(player, h);
            var playerMass = EntityManager.GetComponentData<PhysicsMass>(player);
            playerMass.InverseInertia = 0;
            EntityManager.SetComponentData(player, playerMass);
        }).WithStructuralChanges().Run();


        var playerEntities = _playerQuery.ToEntityArray(Allocator.Temp);
        Entities.WithAll<PlayerTag>().ForEach((Entity e, in PlayerTargetIndex i) => {
            EntityManager.AddComponentData(e, new TargetEntity { target = playerEntities[i.Value] });
        }).WithStructuralChanges().Run();
        
        var playerColors = GetBuffer<ColorElement>(GetSingletonEntity<ColorElement>());
        Entities.WithAll<PlayerTag>().ForEach((int entityInQueryIndex, SpriteRenderer renderer) => {
            renderer.color = playerColors[entityInQueryIndex];
        }).WithoutBurst().Run();
        
        var startingPositions = GetBuffer<StartingPositionElement>(GetSingletonEntity<StartingPositionElement>());
        Entities.WithAll<PlayerTag>().ForEach((int entityInQueryIndex, ref Translation t) => {
            t.Value = startingPositions[entityInQueryIndex];
        }).WithoutBurst().Run();

        playerEntities.Dispose();
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
            var mov = playerActions.Movement.ReadValue<Vector2>();
            a.SetFloat("BlendX", mov.x);
            a.SetFloat("BlendY", mov.y);
        }).WithoutBurst().Run();

        var bulletPrefab = GetSingleton<BulletPrefabReference>().Value;
        var bulletModelPrefab = this.GetSingleton<BulletModelPrefabReference>().Value;
        
        Entities.WithAll<PlayerTag>().ForEach((Entity e, ControllerReference c, ref ShootingCooldown cooldown, in Translation translation) => {
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

public struct PlayerTag : IComponentData {}