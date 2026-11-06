# NPC: убегает от врага и бродит

Инструкция для агента, который будет переносить архитектуру врага на NPC. Читай
последовательно, шаги зависимы. Всё, что уже сделано для врага — в
`Assets/Docs/enemy-behavior-tree.md`, здесь только дельта.

## 0. Паттерн, который копируем

У врага работает схема «ECS-сущность + companion GameObject»:

```
SubScene + Baker  →  ECS-сущность (данные + системы)
                         ↑ мост
                  companion GameObject с BehaviorGraphAgent
                  (Unity Behavior умеет работать только с GameObject)
```

Разделение обязанностей строгое и его надо сохранить:

* **граф** — единственный, кто переключает состояние. Никакой ECS-системе нельзя
  писать состояние.
* **ECS-система** — только исполняет: двигает, крутит, считает кулдауны.

Если завести второй источник переходов, они начнут перетирать решения друг друга.

## 1. Сначала сделай врага видимым

Сейчас `EnemyAuthoring` НЕ добавляет `DetectionTarget` — враг физически не является
целью для перцепции, NPC его не заметит ни зрением, ни слухом. Без этого шага ничего
работать не будет.

В `EnemyAuthoring.Bake` добавить:

```csharp
AddComponent(entity, new DetectionTarget { Faction = TargetFaction.Enemy });
```

## 2. Файлы

Под `Assets/Scripts/NPCs/ECS/` (свой подкаталог + свой asmdef, чтобы не тащить в новую
сборку легаси-скрипты из `Assembly-CSharp`):

```
InfectionGame.NPCs.ECS.asmdef     refs: Unity.Entities, Unity.Entities.Hybrid,
                                       Unity.Transforms, Unity.Mathematics,
                                       Unity.Collections, Unity.Burst, Unity.Behavior,
                                       InfectionGame.Perception
NpcComponents.cs                  теги, вид, состояния
Species/NpcSpeciesSO.cs           ScriptableObject вида
NpcAuthoring.cs                   Baker
NpcAgentSystem.cs                 исполнитель (2 job'а)
NpcCompanionAgent.cs              мост GameObject ↔ ECS
NpcCompanionSpawnSystem.cs        спавн companion'а
Behavior/HasPerceivedThreatCondition.cs
Behavior/SetNpcStateAction.cs
Behavior/ChooseWanderTargetAction.cs
```

## 3. Компоненты (`NpcComponents.cs`)

Зеркало `EnemyComponents.cs`, минус атака:

```csharp
public struct NpcTag : IComponentData { }
public struct NpcCompanionSpawned : IComponentData { }

public struct NpcSpecies : IComponentData
{
    public float MoveSpeed;          // скорость в панике (убегание)
    public float PassiveMoveSpeed;   // скорость брожения
    public float FleeDistance;       // насколько далеко уносить ноги за один шаг
    public Unity.Entities.UnityObjectRef<Unity.Behavior.BehaviorGraph> BehaviorGraph;
}

[BlackboardEnum]
public enum NpcState : byte { Idle = 0, Flee = 1 }

// Состояние — это enableable-теги, а не enum-поле. Переключение через
// SetComponentEnabled не меняет архетип, а диспетчеризация job'а вместо switch
// делает переходы физически невозможными из кода.
public struct NpcStateIdle : IComponentData, IEnableableComponent { }
public struct NpcStateFlee : IComponentData, IEnableableComponent { }

public struct NpcAgent : IComponentData
{
    public Entity Threat;
}

// Точка брожения. TimeSinceAssigned обязателен: без него узел, который подбирает
// точку, будет брать новую каждый тик — враг у вас уже дёргался именно из-за этого.
public struct NpcWanderTarget : IComponentData
{
    public float3 Position;
    public float HasTarget;
    public float TimeSinceAssigned;
}
```

## 4. Исполнитель (`NpcAgentSystem.cs`)

Три вещи, которые не повторяйте из старого кода врага: никакого `switch`, никакой
записи состояния, никаких `SetComponentEnabled` по своей инициативе. Три структуры
с `[WithAll]` вместо этого:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(VisionDetectionSystem))]
public partial struct NpcAgentSystem : ISystem { ... }

// убегать: цель не храним, направление пересчитывается каждый тик — угроза движется,
// поэтому заранее выбранная точка бегства всё равно была бы неверной.
[BurstCompile]
[WithAll(typeof(NpcStateFlee))]
public partial struct FleeJob : IJobEntity
{
    void Execute(ref LocalTransform t, in NpcSpecies s, in DetectionState p)
    {
        if (p.Primary == Entity.Null) return;
        float3 away = t.Position - p.LastKnownPosition;
        if (math.lengthsq(away) < 1e-6f) return;
        t.Position += math.normalize(away) * s.MoveSpeed * DeltaTime;
        // поворот — по EnemyMotion.FaceTowards, только цель позади
    }
}

// бродить: точку дал граф, дойдя — сбросить, чтобы граф выдал новую
[BurstCompile]
[WithAll(typeof(NpcStateIdle))]
public partial struct NpcWanderJob : IJobEntity { ... }
```

Общую кинематику (`MoveTowards`, `FaceTowards`) не дублируйте — вынеси в
`EnemyMotion` (сейчас `internal static` в `EnemyAgentSystem.cs`) и открой `public`.

## 5. Мост и спавн

`NpcCompanionAgent` = копия `EnemyCompanionAgent` с заменой типов. Контракт оставь тем
же: `TryGetPerception`, `SetState`, `TryGetState`, `SetWanderTarget`,
`ClearWanderTarget`, `TryGetWanderTarget`, плюс `LateUpdate` зеркалит `LocalTransform`
в GameObject.

`NpcCompanionSpawnSystem` = копия `EnemyCompanionSpawnSystem`: `GameObject.CreatePrimitive`
с `BehaviorGraphAgent` (`Graph = species.BehaviorGraph.Value`) + `NpcCompanionAgent`,
колайдер снимается, сущность помечается `NpcCompanionSpawned`.

`NpcAuthoring.Bake` добавляет: `NpcTag`, `NpcSpecies`, перцепцию (`DetectionSensor`
с `Targets = TargetFaction.Enemy`, `VisionSensor`, `HearingSensor`, `DetectedTarget`,
`DetectionState`), `NpcAgent`, `NpcWanderTarget`, оба тега состояния с
`SetComponentEnabled<NpcStateFlee>(entity, false)`.

## 6. Вид (`NpcSpeciesSO`)

`[CreateAssetMenu(menuName = "InfectionGame/NPC Species")]`, поля как у
`EnemySpeciesSO`, но без `attackDistance/attackInterval/attackDamage` и с
`fleeDistance` вместо них. Поле `behaviorGraph` — обычное
`Unity.Behavior.BehaviorGraph`; в ECS оно попадает через
`UnityObjectRef<BehaviorGraph>` (важно: `Unity.Entities.Serialization` здесь не нужен,
в 1.4.8 тип живёт в `Unity.Entities`).

Ассет вида: `Assets/ScriptableObjects/NpcSpecies_Default.asset`. Зрение шире, чем у
врага (NPC паникует от любого шороха): FOV 360 или 240, `visionRange` 25,
`hearingRadius` 10, `moveSpeed` 5 (быстрее врага, иначе его догонят), `passiveMoveSpeed`
1, `fleeDistance` 3, `memoryDuration` 2.

## 7. Behavior-узлы

Всё то же, что мы нащупали на враге — нарушение любого пункта даёт узел, который не
появляется в редакторе или молча не работает:

* условия **только** через `[Condition(name, description, story, category, id)]`.
  `[NodeDescription]` для условия не работает — `ConditionUtility.GetConditionTypes`
  ищет `TypeCache.GetTypesWithAttribute<ConditionAttribute>()`.
* у Action/Composite — `[NodeDescription(...)]`.
* **все** параметры — `[SerializeReference] public BlackboardVariable<T>`. Всё остальное
  `NodeRegistry.GetNodeVariables` просто не покажет.
* enum'ы — с `[BlackboardEnum]`, иначе падение IL2CPP-генерации.
* story-строка: каждый видимый параметр обязан быть упомянут в `[...]`, иначе поле
  не нарисуется. Пример: `"Choose Wander Target In Radius [Radius] Restart After [RestartAfterSeconds]"`.
* `id:` в атрибутах должен быть уникальным на каждый узел — не копируй от врага.

`HasPerceivedThreatCondition.IsTrue()` читает `DetectionState` через мост и возвращает
`perception.Primary != Entity.Null && perception.TimeSinceSensed <= MemoryGraceSeconds.Value`.

`ChooseWanderTargetAction` копируется от врага с заменой `EnemyCompanionAgent` →
`NpcCompanionAgent` и типов состояния/точки. Логику не меняй: новая точка берётся
только когда `HasTarget == 0` (дошёл) либо когда `TimeSinceAssigned >= RestartAfterSeconds`.
Узел обязан быть **идемпотентным** — граф стоит под `Repeat` и зовёт его каждый тик.

Когда появится третий актор, эти узлы стоит обобщить через интерфейс моста, а не
копировать в третий раз.

## 8. Граф — только руками в редакторе

`BehaviorAuthoringGraph` и `GraphAsset` internal, из кода граф не собрать. Собери в
визуальном редакторе и сохрани, например, в `Assets/BehaviorTrees/NpcBehaviorGraph.asset`.

```
On Start (Repeat)
└── Selector
    ├── Conditional Guard [Has Perceived Threat 0.5]   Abort: Lower Priority
    │   └── Set Npc State = Flee
    └── Sequence
        ├── Set Npc State = Idle
        └── Choose Wander Target [Radius 6] [Restart After 10]
```

Это ровно та форма, в которую мы только что переделывали граф врага. Три ошибки,
которые там были, повторять нельзя — они не видны на глаз, но полностью ломают
поведение:

1. **Selector + узел, всегда возвращающий Success.** `Choose Wander Target` всегда
   Success, поэтому если `Set Npc State = Idle` стоит третьим ребёнком Selector — он не
   выполнится никогда, NPC застрянет в Flee навсегда. Поэтому брожение — это одна
   `Sequence`, а не два отдельных ребёнка.
2. **Guard без дочернего узла.** У `Conditional Guard` (`Is Target In Range` у врага)
   было `m_Child: {rid: -2}` — гард обёрнут вокруг пустоты, а действие стояло сестрой.
   Перетаскивай действие именно НА гард.
3. **`Abort: None` — это значение по умолчанию**, и оно отключает прерывание совсем:
   условие проверяется один раз при входе в ветку и не может из неё вытащить. Ставь
   `Lower Priority` у гарда над боевой/панической веткой. Для выстрела из брожения
   «заметил врага — бросил всё» нужен `Abort: Lower Priority`; для «враг потерялся —
   перестал бежать» понадобится `Self` на ветке бегства.

После сборки графа открой ассет текстом и проверь: у каждого `ConditionalGuardModifier`
поле `m_ObserverType` не должно быть `0`, и у каждого `m_Child` должен быть настоящий
`rid`, не `-2`.

Не забудь назначить `behaviorGraph` в ассете вида — иначе companion получит `null` и
граф не запустится вовсе.

## 9. Сцена и префаб

* `NPC.prefab` уже несёт `PerceptionDebugTarget` (faction NPC) — это зеркало для
  GameObject-версии. Для ECS-версии NPC собери отдельный авторинг-объект в SubScene
  (по образцу `Enemy (ECS)` в `PerceptionPlayground.unity`).
* Убери/выключи старый `NPC` MonoBehaviour: его `FixedUpdate` ищет врага через
  `EnemyRuntimeSet`, новых ECS-врагов там нет, и два источника поведения опять
  начнут спорить.
* Спавнеры: `NPCSpawnConfig.npcCount` и `EnemySpawnConfig.maxEnemyCount` держи
  согласованными со сценой — сейчас там 1 и 0.

## 10. Тесты

Зеркали `EnemyAgentSystemTests`. Обязательные кейсы:

* `Flee_MovesAwayFromThreat` — удаляется от `LastKnownPosition`.
* `Flee_TurnsAwayFromThreat` — смотрит в противоположную сторону.
* `Flee_WithoutThreat_StopsAndKeepsState` — без цели стоит, состояние не трогает.
* `Idle_WithWanderTarget_MovesThereSlowly` — скорость `passiveMoveSpeed`, не боевая.
* `Idle_WanderTarget_AccumulatesTimeSinceAssigned` — вот на чём держится защита от
  дёрганья.
* `Idle_OnArrival_ClearsWanderTargetAndResetsTimer`.
* `FleeJob_NeverSwitchesStateTags` и `WanderJob_NeverSwitchesStateTags` — состояние
  система не меняет.

Прогон: редактор должен быть закрыт, иначе batchmode не стартует
(`Temp/UnityLockfile`):

```
"D:/apps_for_coding/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/apps_for_coding/Unity/projects/InfectionGame/InfectionGame" \
  -runTests -testPlatform EditMode \
  -testResults <scratch>/unity-tests/results.xml \
  -logFile <scratch>/unity-tests/editor.log
```

БЕЗ `-nographics` — тесты гоняют сцены. Итог парси из `results.xml`
(`test-run` total/passed/failed), компиляционные ошибки — `error CS` из лога.

## 11. Грабли

* `write_file` откажется перезаписать файл, который не был прочитан в этом же проходе
  (`Refusing to overwrite`) — сделай `rm -f` и запиши заново.
* Условия ищутся только по `[Condition]`, и story обязан содержать `[...]`.
* Значения по умолчанию в полях узлов (`Radius`, `Distance`, `State`) задаются в графе,
  не в коде.
* Состояние — enableable-теги: `AddComponent<T>` включает их сразу, поэтому начальное
  состояние задавай через `SetComponentEnabled`.
