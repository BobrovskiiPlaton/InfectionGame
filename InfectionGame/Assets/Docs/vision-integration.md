# Vision для NPC и врагов — инструкция

Проект: InfectionGame (Unity 6000.3.8f1, URP 17.3.0, `com.unity.entities` 1.4.8, `com.unity.entities.graphics` 1.4.21).
Вся перцепция живёт в `Assets/Scripts/Perception/` (asmdef `InfectionGame.Perception`).

## Что уже готово

| Файл | Назначение |
|---|---|
| `PerceptionComponents.cs` | `TargetFaction` (Player/NPC/Enemy, битовая маска), `SenseKind` (Vision, заготовлен Hearing), `DetectionSensor`, `VisionSensor`, `DetectionTarget`, `DetectionState`, буфер `DetectedTarget` |
| `PerceptionMath.cs` | Engine-agnostic правила: `VisionCone.FromDegrees(fov)`, `IsInsideCone`, `CanSee`, `IsInteresting`, `TickMemory` |
| `VisionDetectionSystem.cs` | `ISystem`: сбор целей в `NativeList<TargetSample>` + параллельный `VisionDetectJob` (фракция → дальность → угол → память → выбор primary) |
| `ECSBridge/PerceptionSensorECSBehavior.cs` | Authoring для наблюдателя (Baker) |
| `ECSBridge/PerceptionTargetECSBehavior.cs` | Authoring для цели (Baker) |
| `Assets/Tests/EditMode/` | 57 тестов: правила, ECS-система, default world, сцены |

Система попадает в default ECS world автоматически (`ISystem` + `WorldSystemFilterFlags.Default`), руками регистрировать не нужно.

## Как добавить vision к NPC

NPC может быть и наблюдателем, и целью — компоненты независимы, вешаются в любом сочетании.

1. **Сделать NPC видимым для врагов** — добавить `PerceptionTargetECSBehavior`, выставить `faction = NPC` (или `Player`/`Enemy`).
2. **Дать NPC зрение** (например, чтобы избегать врагов) — добавить `PerceptionSensorECSBehavior`:
   - `targets = Enemy` (кого ищем),
   - `fieldOfViewDegrees` — полный угол конуса, 360 = круговой обзор,
   - `visionRange` — дальность в метрах,
   - `eyeOffsetLocal` — где глаза (по умолчанию `0, 1.6, 0`),
   - `memoryDuration` — сколько секунд цель помнится после потери из виду (0 = забывать сразу),
   - `visionEnabled` — снять галочку, чтобы ослепить, не снимая компонент.

**Обязательно: объект должен лежать в SubScene**, иначе Baker не сработает и ECS-сущности не будет. В `HealingFieldScene` SubScene уже подключён — это `Perception (ECS SubScene)` в иерархии, целевая сцена `Assets/Scenes/PerceptionPlayground.unity`. Открывается двойным кликом по объекту; всё, что в ней лежит, при входе в Play бэйкаится в сущности.

Читать результат в коде:
```csharp
var state = em.GetComponentData<DetectionState>(entity);   // state.Primary, state.LastKnownPosition, state.TimeSinceSensed
var buffer = em.GetBuffer<DetectedTarget>(entity);          // все известные цели
```
У `VisionSensor` и `DetectionTarget` есть `IEnableableComponent` — глушить/включать на лету через `em.SetComponentEnabled<T>(entity, bool)` без смены архетипа.

## Как протестировать

```
"<Unity>/Editor/Unity.exe" -batchmode -projectPath <proj> -runTests -testPlatform EditMode \
  -testResults <out>.xml -logFile <out>.log
```
Не передавать `-quit` вместе с `-runTests`. Exit code = числу упавших тестов. **Не передавать `-nographics`** — иначе `EditorSceneManager.OpenScene` валится с NullReferenceException из entities.graphics. Парсить `results.xml` (`test-case[@result='Failed']`).

Компоненты для ручного теста в сцене: `PerceptionDebugProbe` (визир, рисует конус через `Debug.DrawLine`) и `PerceptionDebugTarget` (зеркало GameObject → ECS-сущность). Лежат в `Assets/Scripts/Perception/Debugging/`, в `PerceptionPlayground` сейчас выключены (`DISABLED ...`) — включать только по одному варианту, иначе будут дубли сущностей.

## Грабли (уже наступали)

- **Материал**: Entities Graphics рендерит через BatchRendererGroup и требует шейдер с `DOTS_INSTANCING_ON`. Встроенный `Default-Material` (Standard) не подходит — будет `BatchDrawCommand ... does not define a DOTS_INSTANCING_ON variant`. В этом проекте все материалы на `Universal Render Pipeline/Lit` (guid `933532a4fcc9baf4fa0491de14d08ed7`), брать их как образец. Тест `PerceptionPlayground_MaterialsSupportDotsInstancing` это ловит.
- **`m_Name` в YAML сцены, начинающееся с `[`** — парсер сцены падает целиком (`Parser Failure ... Expect ':' between key and value`). Маркер в конец имени или словом.
- **`_SceneAsset` в SubScene-компоненте**: правильный `fileID: 102900000`, не `7900000`. `_SceneGUID` — хеш содержимого, не guid ассета; оставлять нулевым, Unity пересчитает.
- **Полный переход игрока на ECS не сделан**: Player — GameObject в основной сцене с `PerceptionDebugTarget` (зеркало в ECS). `PlayerControls` — MonoBehaviour, в SubScene он работать не будет. Чтобы сделать игрока настоящей ECS-сущностью — либо переносить его в SubScene и переписывать управление на ECS-систему, либо использовать companion GameObject.
