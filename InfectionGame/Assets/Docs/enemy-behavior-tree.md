# Враги на ECS + Behavior Tree

Как устроено сейчас и что осталось доделать руками (сборка графа — визуальная операция
в Unity Behavior, из кода её не воспроизвести).

## Схема

```
SubScene (PerceptionPlayground.unity)
  └─ GameObject "Enemy (ECS)"  + EnemyAuthoring(species: EnemySpecies_Default)
        │ Baker
        ▼
   ECS-сущность:  EnemyTag + EnemySpecies + EnemyAgent + EnemyWanderTarget
                  + DetectionSensor + VisionSensor + HearingSensor
                  + DetectedTarget[] + DetectionState
        │
        ├─ VisionDetectionSystem  — зрение + слух + память → DetectionState
        ├─ EnemyAgentSystem       — читает состояние, двигает/бьёт (ноги и руки)
        └─ EnemyCompanionSpawnSystem — рождает GameObject-компаньон с BehaviorGraphAgent
                                          + EnemyCompanionAgent (мост в ECS)
```

## Что уже есть

| Где | Что |
|---|---|
| `Assets/Scripts/Enemies/ECS/Species/EnemySpeciesSO.cs` | Вид: зрение (FOV, дальность), слух (радиус), движение, атака, память |
| `Assets/ScriptableObjects/EnemySpecies_Default.asset` | Первый вид: FOV 120, дальность 20, слух 4 м |
| `Assets/Scripts/Enemies/ECS/EnemyComponents.cs` | `EnemyTag`, `EnemySpecies`, `EnemyAgent`, `EnemyWanderTarget`, теги состояний |
| `Assets/Scripts/Enemies/ECS/EnemyAuthoring.cs` | Baker: разворачивает вид в ECS-компоненты |
| `Assets/Scripts/Enemies/ECS/EnemyAgentSystem.cs` | Исполнитель: три job'а по состояниям, без единого перехода |
| `Assets/Scripts/Enemies/ECS/EnemyCompanionSpawnSystem.cs` | Спавнит companion GameObject с BehaviorGraphAgent |
| `Assets/Scripts/Enemies/ECS/EnemyCompanionAgent.cs` | Мост: ECS ↔ Unity Behavior |
| `Assets/Scripts/Enemies/ECS/Behavior/*.cs` | Узлы графа (см. ниже) |

Узлы графа, которые можно ставить в редакторе:

* **Condition/Enemy → Has Perceived Target** — есть ли цель (зрение/слух/память)
* **Condition/Enemy → Is Target In Range** — цель ближе заданной дистанции
* **Action/Enemy → Set Enemy State** — переключить Idle / Chase / Attack
* **Action/Enemy → Choose Wander Target** — случайная точка брожения в радиусе

## Что доделать руками: сам граф

1. `Assets > Create > Behavior > Behavior Graph` → сохрани как
   `Assets/Behavior/EnemyBrain.asset`.
2. Открой двойным кликом, собери дерево:

```
Start
└── Selector                      ("Try In Order")
    ├── Sequence
    │   ├── Has Perceived Target   [MemoryGraceSeconds = 0.5]
    │   └── Selector
    │       ├── Sequence
    │       │   ├── Is Target In Range  [Distance = 2]
    │       │   └── Set Enemy State     [State = Attack]
    │       └── Set Enemy State         [State = Chase]
    ├── Choose Wander Target       [Radius = 6]  [RestartAfterSeconds = 10]
    └── Set Enemy State            [State = Idle]
```

3. Если граф завершается и не перезапускается — оберни корень в **Repeat**
   (`Decorators/Repeat`), чтобы мозг работал каждый кадр.
4. Назначь граф в `Assets/ScriptableObjects/EnemySpecies_Default.asset`
   → поле **Behavior Graph**.
5. Запусти `HealingFieldScene` (SubScene автозагрузится). Должно быть:
   * враг бродит к случайным точкам, когда никого не видит;
   * увидев игрока/NPC — переходит в Chase и идёт к нему;
   * подойдя ближе `attackDistance` (2 м) — встаёт и бьёт с интервалом `attackInterval`;
   * если подкрасться **со спины** ближе чем на 4 м — слух сработает и враг развернётся.

## Проверка

```
"<Unity>/Editor/Unity.exe" -batchmode -projectPath <proj> -runTests -testPlatform EditMode \
  -testResults <out>.xml -logFile <out>.log
```
81 тест: математика перцепции (29), ECS-пайплайн зрения (19), слух (11), враги (10),
сцены (10). **Без `-nographics`** — иначе `OpenScene` валится.

## Грабли, на которые тут наступали

* `Action` из `Unity.Behavior` конфликтует с `System.Action` — наследоваться надо как
  `Unity.Behavior.Action`, иначе «cannot derive from sealed type 'Action'».
* `Unity.Behavior` нужен в `references` у **всех** asmdef, которые трогают
  `EnemySpecies` — внутри него `UnityObjectRef<BehaviorGraph>`, и зависимость тянется в
  транзитивные сборки (тесты падали с `CS0012: The type 'BehaviorGraph' is defined in an
  assembly that is not referenced`).
* `EntityManager.DestroyEntityQuery` не существует — `EntityQuery` принадлежит `World`
  и освобождается вместе с ним. Создал через `EntityManager.CreateEntityQuery` — просто
  забудь про него.
* `IJobEntity.ScheduleParallel()` без аргументов теряет dependency — всегда пиши
  `state.Dependency = job.ScheduleParallel(state.Dependency)`.
* Старый `Enemy.cs` / `EnemyManager.cs` намеренно оставлены в `Assembly-CSharp`
  (к ним привязан префаб `EnemyManager` и сцена). asmdef лежит только на `ECS/`,
  поэтому legacy-файлы продолжают собираться.
