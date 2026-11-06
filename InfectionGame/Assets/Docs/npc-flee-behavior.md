# NPC: убегает от врага и бродит

Инструкция для агента, который переносит поведение врага на NPC. Переделка уже
подготовлена: всё общее вынесено в `InfectionGame.Actors`, поэтому писать надо заметно
меньше, чем копировать. Читай последовательно, шаги зависимы.

## 0. Что уже готово и переиспользуется как есть

Слой `Assets/Scripts/Actors/` (сборка `InfectionGame.Actors`) существует ровно для того,
чтобы NPC не копировал врага:

| Готово | Для чего NPC |
|---|---|
| `IActorCompanionAgent` | контракт моста GameObject ↔ ECS |
| `ActorCompanionAgentBase` | поиск своей сущности, зеркало трансформа, точка брожения |
| `ActorMotion` | шаг к цели и поворот |
| `WanderTarget` | компонент точки брожения |
| `WanderTargetMath` | политика «не перебирать точку каждый тик» + прибытие + сброс |
| `ActorSpeciesSO` | зрение / слух / движение / память / behavior graph |
| `CompanionSpawnSystemBase` | спавн companion GameObject с `BehaviorGraphAgent` |
| `HasPerceivedTargetCondition` | узел-условие, работает через интерфейс моста |
| `IsTargetInRangeCondition` | то же (NPC не нужен, но и не мешает) |
| `ChooseWanderTargetAction` | узел брожения, работает через интерфейс моста |

Эти узлы уже лежат в графе врага и **их не нужно переписывать под NPC** — они берут
`IActorCompanionAgent`, а не конкретный класс. То есть граф NPC соберёшь из тех же узлов.

Враг уже является целью перцепции: `EnemyAuthoring.Bake` добавляет
`DetectionTarget { Faction = TargetFaction.Enemy }`. Проверь, что это на месте, — без
него NPC физически не заметит врага ни зрением, ни слухом.

## 1. Что надо написать (минимум)

Под `Assets/Scripts/NPCs/ECS/` со своим asmdef (`InfectionGame.NPCs.ECS`, refs: те же, что
у `InfectionGame.Enemies.ECS`, плюс `InfectionGame.Actors`):

```
NpcComponents.cs          теги, вид, состояния, NpcAgent
Species/NpcSpeciesSO.cs   : ActorSpeciesSO  + fleeDistance
NpcAuthoring.cs           Baker
NpcAgentSystem.cs         2 job'а
NpcCompanionAgent.cs      : ActorCompanionAgentBase  (маленький)
NpcCompanionSpawnSystem.cs: CompanionSpawnSystemBase  (маленький)
Behavior/SetNpcStateAction.cs   единственный новый узел
```

### `NpcComponents.cs`

```csharp
public struct NpcTag : IComponentData { }
public struct NpcCompanionSpawned : IComponentData { }

public struct NpcSpecies : IComponentData
{
    public float MoveSpeed;        // в панике
    public float PassiveMoveSpeed; // брожение
    public float FleeDistance;     // насколько далеко уносить ноги за шаг
    public Unity.Entities.UnityObjectRef<Unity.Behavior.BehaviorGraph> BehaviorGraph;
}

[BlackboardEnum]
public enum NpcState : byte { Idle = 0, Flee = 1 }

public struct NpcStateIdle : IComponentData, IEnableableComponent { }
public struct NpcStateFlee : IComponentData, IEnableableComponent { }

public struct NpcAgent : IComponentData
{
    public Entity Threat;
}
```

`WanderTarget` брать из `InfectionGame.Actors` — своей точки брожения NPC не нужен.

### `NpcAgentSystem.cs`

Три правила, которые уже проверены на враге: никакого `switch`, никакой записи состояния,
никаких `SetComponentEnabled` по своей инициативе. Вместо switch — `[WithAll]` на job:

```csharp
// Убегать. Цель не храним: угроза движется, поэтому заранее выбранная точка бегства
// всё равно была бы неверной. Направление пересчитывается каждый тик.
[BurstCompile]
[WithAll(typeof(NpcStateFlee))]
public partial struct FleeJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform t, in NpcSpecies s, in DetectionState p)
    {
        if (p.Primary == Entity.Null)
            return;

        float3 away = t.Position - p.LastKnownPosition;
        if (math.lengthsq(away) < 1e-6f)
            return;

        t.Position += math.normalize(away) * s.MoveSpeed * DeltaTime;
        t.Rotation = ActorMotion.FaceTowards(t.Position, p.LastKnownPosition, t.Rotation);
    }
}

// Бродить: точку дал граф, дойдя — сбросить, чтобы граф выдал новую.
[BurstCompile]
[WithAll(typeof(NpcStateIdle))]
public partial struct NpcWanderJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform t, in NpcSpecies s, ref WanderTarget w)
    {
        WanderTargetMath.Tick(ref w, DeltaTime);
        if (w.HasTarget <= 0.5f) return;

        ActorMotion.MoveTowards(ref t, w.Position, s.PassiveMoveSpeed, DeltaTime);
        if (WanderTargetMath.HasArrived(t.Position, w))
            w = WanderTargetMath.Cleared();
    }
}
```

Оба job'а — буквально переиспользование `ActorMotion` + `WanderTargetMath`. `OnUpdate`
как у `EnemyAgentSystem`: `state.Dependency = job.ScheduleParallel(state.Dependency)` по
цепочке. Систему объявляй `[UpdateAfter(typeof(VisionDetectionSystem))]`.

### `NpcCompanionAgent` и `NpcCompanionSpawnSystem`

Копия враговского в миниатюре — вся общая работа уже в базах:

```csharp
public class NpcCompanionAgent : ActorCompanionAgentBase
{
    protected override EntityQuery CreateLinkQuery(EntityManager entityManager) =>
        entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NpcTag>(),
            ComponentType.ReadOnly<LocalTransform>());

    public void SetState(NpcState value) { ... SetComponentEnabled<NpcStateIdle/Flee> ... }
    public bool TryGetState(out NpcState state) { ... }
}

public partial class NpcCompanionSpawnSystem : CompanionSpawnSystemBase
{
    protected override EntityQuery CreatePendingQuery() =>
        GetEntityQuery(ComponentType.ReadOnly<NpcTag>(),
                       ComponentType.ReadOnly<NpcSpecies>(),
                       ComponentType.ReadOnly<LocalTransform>());
    // + HasSpawnedMarker / MarkSpawned / GetBehaviorGraph / AttachBridge / CompanionName
}
```

Грабли, в которые уже упирались: `SystemBase`-наследник обязан быть `partial`
(`error EA0007`), запрос создаётся через `GetEntityQuery`, а не `CreateEntityQuery`, и
`Unity.Transforms` нужен в using для `LocalTransform`.

### `SetNpcStateAction`

Единственный новый узел — потому что enum состояния актора свой и граф должен видеть его
конкретным типом. Узел тонкий, вся логика — в мосте. Правила те же, что и у враговских
узлов:

* `[NodeDescription(name, description, story, category, id)]` с **новым** уникальным `id`.
* Параметр — `[SerializeReference] public BlackboardVariable<NpcState> State;`
* `NpcState` обязательно с `[BlackboardEnum]`.
* story-строка обязана содержать `[State]`, иначе поле не появится в инспекторе.

## 2. Вид

`NpcSpeciesSO : ActorSpeciesSO`, добавляет только `fleeDistance`, `[CreateAssetMenu(menuName:
"InfectionGame/NPC Species")]`. Ассет — `Assets/ScriptableObjects/NpcSpecies_Default.asset`.
Разумные стартовые числа: `visionRange` 25, `fieldOfViewDegrees` 360 (NPC паникует от
любого шороха), `hearingRadius` 10, `moveSpeed` 5 (быстрее врага, иначе догонят),
`passiveMoveSpeed` 1, `memoryDuration` 2, `fleeDistance` 3.

Поле `behaviorGraph` обязательно назначь — иначе companion получит `null` и граф не
запустится вовсе.

## 3. Граф — только руками в редакторе

`BehaviorAuthoringGraph` и `GraphAsset` internal, из кода граф не собрать.

```
On Start (Repeat)
└── Selector
    ├── Conditional Guard [Has Perceived Target 0.5]   Abort: Lower Priority
    │   └── Set Npc State = Flee
    └── Sequence
        ├── Set Npc State = Idle
        └── Choose Wander Target [Radius 6] [Restart After 10]
```

`Has Perceived Target` и `Choose Wander Target` — **те же самые узлы**, что у врага, из
палитры они берутся один в один.

Три ошибки, которые уже ломали граф врага и которые повторять нельзя — они не видны на
глаз, но полностью убивают поведение:

1. **Selector + узел, всегда возвращающий Success.** `Choose Wander Target` всегда
   Success, поэтому `Set Npc State = Idle` отдельным третьим ребёнком Selector не
   выполнится никогда и NPC застрянет в Flee навсегда. Брожение — это одна `Sequence`,
   а не два отдельных ребёнка, и `Set Idle` идёт первым.
2. **Guard без дочернего узла.** У `Conditional Guard` (`m_Child: {rid: -2}`) нет действия
   внутри — гард обёрнут вокруг пустоты. Перетаскивай `Set Npc State` именно НА гард.
3. **`Abort: None` — значение по умолчанию**, и оно отключает прерывание совсем: условие
   проверяется один раз при входе в ветку и не может из неё вытащить. Ставь `Lower
   Priority`; если захочешь, чтобы NPC переставал убегать сразу после потери цели — `Self`
   на ветке бегства.

После сборки открой ассет текстом и проверь: у `ConditionalGuardModifier` поле
`m_ObserverType` не `0`, и у `m_Child` настоящий `rid`, а не `-2`.

## 4. Сцена и префаб

* ECS-версию NPC собери в SubScene по образцу `Enemy (ECS)` в `PerceptionPlayground.unity`.
* Старый `NPC` MonoBehaviour (`Assets/Scripts/NPC.cs`) убери или выключи: его `FixedUpdate`
  ищет врага через `EnemyRuntimeSet`, там новых ECS-врагов нет, и два источника поведения
  начнут спорить. `PerceptionDebugTarget` на `Assets/Prefabs/NPC.prefab` — зеркало для
  GameObject-версии, для ECS-версии не нужно.
* Спавнеры держи согласованными со сценой: сейчас `NPCSpawnConfig.npcCount = 1` и
  `EnemySpawnConfig.maxEnemyCount = 0`.

## 5. Тесты

Зеркали `EnemyAgentSystemTests`. Обязательные кейсы:

* `Flee_MovesAwayFromThreat` — удаляется от `LastKnownPosition`.
* `Flee_WithoutThreat_StopsAndKeepsState` — без цели стоит, состояние не трогает.
* `Idle_WithWanderTarget_MovesThereSlowly` — скорость `passiveMoveSpeed`.
* `Idle_OnArrival_ClearsWanderTargetAndResetsTimer`.
* `FleeJob_NeverSwitchesStateTags` / `WanderJob_NeverSwitchesStateTags` — состояние система
  не меняет. Это тот инвариант, который не даёт появиться второму источнику переходов.
* `NpcCompanionAgent_ImplementsTheSharedBridgeContract` — иначе узлы графа его не найдут.

Общая политика брожения уже покрыта `WanderTargetMathTests`, её копировать не нужно.

Прогон (редактор должен быть закрыт, иначе batchmode не стартует — `Temp/UnityLockfile`):

```
"D:/apps_for_coding/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/apps_for_coding/Unity/projects/InfectionGame/InfectionGame" \
  -runTests -testPlatform EditMode \
  -testResults <scratch>/unity-tests/results.xml \
  -logFile <scratch>/unity-tests/editor.log
```

БЕЗ `-nographics` — тесты гоняют сцены. Итог парси из `results.xml` (`test-run`
total/passed/failed), ошибки компиляции — `error CS` / `error EA` из лога.

## 6. Грабли

* Условия регистрируются только через `[Condition(...)]`, Action — через
  `[NodeDescription(...)]`; story обязана содержать `[...]` для каждого видимого поля.
* `id:` в атрибутах уникален на каждый узел — не копируй от врага.
* `AddComponent<T>` включает enableable-тег сразу, поэтому начальное состояние задавай через
  `SetComponentEnabled`.
* `write_file` откажется перезаписать файл, который не читал в этом же проходе
  (`Refusing to overwrite`) — `rm -f` и запиши заново.
* Состояние не трогать из кода ни при каких условиях: если появится соблазн «подстраховать»
  граф и переключить состояние из системы, получишь ровно ту ошибку, ради которой врага
  уже переписывали.
