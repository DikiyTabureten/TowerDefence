# CLAUDE.md — Tower Defense Prototype (Unity)

Контекст проекта для Claude Code. Положи этот файл в корень Unity-проекта (рядом с папками `Assets/`, `Packages/`, `ProjectSettings/`).
План текущего этапа работ — в `CLAUDE_CODE_INSTRUCTIONS.md` (лежит рядом).

## О проекте
- 2D top-down Tower Defense, учебное задание. **Оценивается архитектура и паттерны, а не графика.**
- Unity **2023.2.20f1**.
- Автор — новичок в Unity: объясняй шаги в редакторе подробно (куда кликнуть, какое поле заполнить), не только код.
- Общение — на русском. Имена в коде — на английском.
- К проекту подключён **Unity MCP**.

## Требования задания
- 3 типа врагов, 3 типа башен (одна стреляющая + две ауры: ускорение атаки и замедление).
- Установка башен drag-and-drop с проверкой золота и зон строительства.
- Object Pool для снарядов.
- Factory-паттерн.
- UI магазина по паттерну **MVP**.
- **Без god-object GameManager.** Bootstrap — только composition root (связывает зависимости, логики не содержит).
- Конфиги башен/врагов/волн/уровней — через **ScriptableObject**.

## Контент
**Башни:**
- Cannon — стреляющая (средневековая пушка).
- War Orchestra — 3 трубача, аура ускорения атаки соседних башен.
- Great Bell — аура замедления врагов.

**Враги** (фиолетово-золотая гамма):
- Heavy Knight — медленный, сильно бронированный.
- Swordsman — два варианта: двуручный меч / меч со щитом.
- Light Horseman — быстрый лёгкий всадник.

**Уровни:**
1. Village Road — обучающий, танк появляется в поздних волнах.
2. River Crossing — упор на быструю кавалерию (награда за Great Bell).
3. Castle Siege — финал, две сходящиеся дороги, тяжёлые волны (награда за Orchestra рядом с пушками).

## Архитектура (что уже сделано)
Скрипты в `Assets/Scripts/`, папки: `Interfaces`, `Configs`, `Core`, `Enemies`, `Towers`, `Projectiles`, `Placement`, `UI`, `Levels`.

Ключевые решения — не ломать без причины:
- **ModifierStack** — хранит множители по источникам и перемножает их, чтобы перекрывающиеся ауры корректно стакались и снимались.
- Аура-башни **пересканируют радиус по таймеру**, а не полагаются только на trigger-события.
- **Bootstrap** (MonoBehaviour) — composition root.
- Физические слои: `Enemy`, `Tower`, `BuildZone` (у каждого нужный коллайдер).
- **BuildZone** — одна башня на зону (зона хранит `Occupant`).
- **LevelConfig** (ScriptableObject) — путь врагов, слоты строительства, экономика, волны **и** данные местности (деревья, вода, камни, дома, стены замка). Один ассет = один уровень. Сетка 20×12, `tileSize = 1`, `GridToWorld()` — центр клетки.
- **LevelGenerator** (Editor-скрипт) — пункт меню *Tools → Tower Defense → Generate Sample Levels*, генерирует 3 ассета уровней.
- **MapTheme** (ScriptableObject) — ссылки на тайлы; **MapBuilder** рисует местность в Tilemap, никогда не перезаписывая клетки дороги и слоты строительства.
- Анимации — **только визуальные компоненты**, подписываются на события геймплея (Observer):
  `ShooterTower.Fired` → `CannonAnimator`, `IAuraTower.AffectedCountChanged` → `BellAnimator` / `OrchestraAnimator`,
  `Enemy.SpeedMultiplierChanged` → `SlowStatusView`, `EnemyWalkAnimator` (кадр по пройденной дистанции, не по времени).
  Анимационные скрипты не должны содержать игровой логики.

## Графика и звук
- Стиль Kenney "Tower Defense (top-down)". Сетка 64×64, **Pixels Per Unit = 64** для тайлов карты.
- Используются PNG (не SVG).
- Звуки: короткие (~0.6 с), без лишнего баса. Пушка, фанфара трубачей, удар колокола.

## Правила работы для Claude
- Перед изменениями — прочитай существующие скрипты, не дублируй системы (например, карты — часть LevelConfig, а не отдельная система).
- Сохраняй разделение ответственности; новые зависимости прокидывай через Bootstrap.
- Не правь `.meta`-файлы и не переименовывай ассеты без необходимости — это рвёт ссылки в инспекторе.
- Предлагай коммит в Git перед крупными изменениями (автор пользуется GitKraken).
- Настройку редактора по возможности автоматизируй Editor-скриптами с пунктом меню; MCP — для проверки, запуска пунктов меню и чтения Console.
- После изменения кода опиши, что нужно сделать руками в редакторе Unity.

## Что проверить в начале сессии
Описание выше составлено по прошлым обсуждениям. Сверь его с реальными файлами в `Assets/Scripts/` и сообщи о расхождениях (переименованные/отсутствующие скрипты), прежде чем писать новый код.

## Текущий статус / следующие шаги
<!-- Обновляй после каждой задачи -->
Итоги аудита (Задача 0, 2026-10-20):
- Работает: ядро (`Bootstrap`, `PlayerWallet`, `PlayerLives`, `EnemyOutcomeHandler`, `GameFlow`, `ModifierStack`), 3 башни + `TowerContext`, `TowerFactory`, `RangeIndicator`, пул снарядов, магазин MVP, `HudView` с result panel (только текст, без кнопок), установка в `BuildZone`, анимации башен. Ошибок в Console нет.
- Анимационные скрипты лежат в `Assets/Scripts/Visuals/` (а не в `Towers`/`Enemies`).
- **Системы уровней нет вообще:** `LevelConfig`, `MapTheme`, `MapBuilder`, `LevelGizmoPreview`, `Editor/LevelGenerator` отсутствуют, папок `Scripts/Levels`, `Scripts/Editor`, `Configs/Levels` нет. Писать с нуля.
- `WaveConfig.SpawnGroup` уже ссылается на `EnemyConfig` (enum `EnemyType` нет). `EnemySpawner` берёт `WaveConfig[]` из сцены, один `WaypointPath`, группы идут последовательно.
- Сцена `SampleScene` (единственная в Build Settings): `Path` (WP0 (-7,0) → WP1 (0,4) → WP2 (7,0)), 3 зоны `BuildZone_1*` (спрайт Square, коллайдер 1×1), Grid/Tilemap нет. Камера Orthographic, size 5, позиция (0,0).
- Враги: 3 конфига (`Enemy_Normal` = мечник со щитом, `Enemy_Fast` = всадник, `Enemy_Tank` = рыцарь), один вариант мечника. `Walk Frames` пустые, листов `enemy_*_walk_64` нет — есть только статичные PNG 256×256.
- Префаб `Enemy`: только `SpriteRenderer`, `CircleCollider2D` r=0.5, `Rigidbody2D` Kinematic. Нет `EnemyWalkAnimator`, `SlowStatusView`, детей `SlowRing` и `Dust`. Враги уничтожаются (`Destroy`), не в пуле.
- Размеры: спрайты башен 64×64 при PPU 64, видимая часть ≈ 58 px ≈ 0.91 юнита. Враги 256×256 при PPU 100: рыцарь 212×192 px ≈ 2.1×1.9 юнита, всадник 230×70 ≈ 2.3×0.7, мечник 146×122 ≈ 1.5×1.2. Отсюда враги в 1.5–2.5 раза больше башен.
- Трубачи и ноты на PPU 100, компенсируются Scale 1.5 / 0.2 (против правила «PPU, а не Scale»). Префаб `Projectile` со Scale 2. У `BoxCollider2D` башен размер 0.0001.
- Все спрайты: Filter Bilinear, Compression Compressed. Sorting Layer только `Default`.
- Kenney: 299 отдельных PNG 64×64 в `Assets/Sprites/Map/Default size` с **PPU 100** (нужно 64). Tile-ассетов нет. Исходный пак (с Tilesheet) лежит в `Downloads/kenney_tower-defense-top-down`.
- Пакеты: `com.unity.2d.sprite` и `com.unity.2d.tilemap.extras` 4.0.2 уже установлены через `com.unity.feature.2d`. Добавлен `com.coplaydev.unity-mcp` (не закоммичен).
- Звуков (AudioClip) в проекте нет.
- Следующие шаги: см. `CLAUDE_CODE_INSTRUCTIONS.md`, задачи 1–6.
