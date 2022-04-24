using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class DamageSystem : SystemBase {
    private StepPhysicsWorld physicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem _entityCommandBufferSystem;
    private EntityQuery _healthSpritesQuery;
    private EntityQuery _playerQuery;
    
    protected override void OnCreate() {
        physicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
        _entityCommandBufferSystem = World.GetExistingSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        _healthSpritesQuery = GetEntityQuery(typeof(HealthSprites));
        _playerQuery = GetEntityQuery(typeof(PlayerTag));
        RequireForUpdate(_healthSpritesQuery);
    }

    protected override void OnUpdate() {
        var entityToDepleteHealth = new NativeHashMap<Entity, Entity>(4, Allocator.TempJob);
        var job = new BulletTriggerJob {
            bullets = GetComponentDataFromEntity<BulletDamage>(true),
            bulletOrigin = GetComponentDataFromEntity<BulletOrigin>(true),
            healths = GetComponentDataFromEntity<Health>(true),
            ecb = _entityCommandBufferSystem.CreateCommandBuffer(),
            entityToDepleteHealth = entityToDepleteHealth
        };
        Dependency = job.Schedule(physicsWorld.Simulation, Dependency);
        _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        
        CompleteDependency();
        var healthSprites = EntityManager.GetComponentObject<HealthSprites>(_healthSpritesQuery.GetSingletonEntity()).Values;
        foreach (var kvp in entityToDepleteHealth) {
            var shouldTakeHealth = false;
            if (HasComponent<Hands>(kvp.Value)) {
                var throwingHand = GetComponent<Hands>(kvp.Value);
                if (throwingHand.Type == PickUpType.Empty) {
                    shouldTakeHealth = true;
                } else {
                    var powerUpIndicator = EntityManager.GetComponentObject<PowerUpIndicatorRendererReference>(kvp.Value);
                    powerUpIndicator.Renderer.color = new Color(0, 0, 0, 0);
                    EntityManager.DestroyEntity(throwingHand.HeldEntity);
                    SetComponent(kvp.Value, new Hands());

                    if (EntityManager.HasComponent<SpriteRenderer>(kvp.Key)) {
                        switch (throwingHand.Type) {
                            case PickUpType.Desaturate: 
                                var spriteHit = EntityManager.GetComponentObject<SpriteRenderer>(kvp.Key);
                                spriteHit.color = Color.gray;
                                break;
                            case PickUpType.ColorChange:
                                var spriteA = EntityManager.GetComponentObject<SpriteRenderer>(kvp.Value);
                                var spriteB = EntityManager.GetComponentObject<SpriteRenderer>(kvp.Key);
                                (spriteB.color, spriteA.color) = (spriteA.color, spriteB.color);
                                break;
                            case PickUpType.Animal: break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            } else {
                shouldTakeHealth = true;
            } 
            
            if (HasComponent<HealthCooldown>(kvp.Key) && shouldTakeHealth) {
                var timer = GetComponent<HealthCooldown>(kvp.Key);
                if (timer.TimeLeft > 0) continue;
                
                var h = GetComponent<Health>(kvp.Key);
                h.Left--;
                SetComponent(kvp.Key, h);
                timer.TimeLeft = timer.Interval;
                SetComponent(kvp.Key, timer);

                var renderer = EntityManager.GetComponentObject<SpriteRenderer>(kvp.Key).GetComponent<HealthbarReference>().r;
                renderer.sprite = healthSprites[h.Left];

                if (h.Left > 0) continue;
                if (GetComponent<TargetEntity>(kvp.Value).target != kvp.Key) {
                    EntityManager.DestroyEntity(kvp.Value);
                }
                EntityManager.DestroyEntity(kvp.Key);
                
                // If not enough players then win
                var playersLeft = _playerQuery.CalculateEntityCount();
                if (playersLeft >= 2) continue;
                var winScreenEntity = GetSingletonEntity<WinScreenModelReference>();
                var winScreen = EntityManager.GetComponentObject<GameObject>(winScreenEntity);
                winScreen.SetActive(true);

                if (playersLeft == 0) continue;
                // Set Winner Color
                var winnerEntity = GetSingletonEntity<PlayerTag>();
                var winnerIndex = GetComponent<PlayerIndex>(winnerEntity).Value;
                var changeColorOf = winScreen.GetComponent<WinScreenReferences>().hatToChangeColorOf;
                var color = GetBuffer<ColorElement>(GetSingletonEntity<ColorElement>())[winnerIndex].Color;
                changeColorOf.color = color;
            }
        }

        var deltaTime = Time.DeltaTime;
        Entities.ForEach((SpriteRenderer r, ref HealthCooldown cooldown) => {
            cooldown.TimeLeft -= deltaTime;
            var col = r.color;
            col.a = math.sin(cooldown.TimeLeft*10) * 0.4f + 0.5f;
            r.color = col;
            var renderer = r.GetComponent<HealthbarReference>().r;
            var c = renderer.color;
            c.a = math.sin(cooldown.TimeLeft*10+5) * 0.4f + 0.5f;
            renderer.color = c;
            if (cooldown.TimeLeft > 0) return;
            col.a = 1;
            r.color = col;
            
            renderer.sprite = null;
        }).WithoutBurst().Run();

        entityToDepleteHealth.Dispose();
    }
}

[BurstCompile]
public struct BulletTriggerJob : ITriggerEventsJob {
    [ReadOnly] public ComponentDataFromEntity<BulletDamage> bullets;
    [ReadOnly] public ComponentDataFromEntity<BulletOrigin> bulletOrigin;
    [ReadOnly] public ComponentDataFromEntity<Health> healths;

    public EntityCommandBuffer ecb;
    public NativeHashMap<Entity, Entity> entityToDepleteHealth;

    public void Execute(TriggerEvent triggerEvent) {
        BulletVHealth(triggerEvent.EntityA, triggerEvent.EntityB);
        BulletVHealth(triggerEvent.EntityB, triggerEvent.EntityA);
    }

    private void BulletVHealth(Entity bulletEntity, Entity healthEntity) {
        if (!bullets.HasComponent(bulletEntity)) return;
        var origin = bulletOrigin[bulletEntity].Value;
        if (origin == healthEntity || bullets.HasComponent(healthEntity)) return;
        entityToDepleteHealth[healthEntity]=origin;
        ecb.DestroyEntity(bulletEntity);
    }
}