# Проверка соответствия заданию

Проверка проекта на соответствие требованиям задания «2D Tower Defense prototype». Каждый пункт сверен с кодом (ссылки ведут на файл и строку), сценами, префабами и конфигами. Поведение в Play Mode проверялось через Unity MCP по ходу разработки.

Подробное описание архитектуры — в [ARCHITECTURE.md](ARCHITECTURE.md).

**Обозначения:** ✅ выполнено · ⚠️ выполнено с оговоркой · ❌ не выполнено

## Итог

| Раздел | Статус |
|---|---|
| 3.1 Враги | ✅ |
| 3.2 Башни (3 типа) | ✅ |
| 4 Снаряды и Object Pool | ✅ |
| 5 UI магазина и установка башен (включая бонус «одна башня на клетку») | ✅ |
| 6 MVP магазина | ✅ |
| 7.1 ScriptableObject-конфиги | ✅ |
| 7.2 Factory | ✅ |
| 7.4 События | ✅ |
| 7.5 Интерфейсы | ✅ |
| Запрещённые практики | ✅ ни одна не используется |

**Все обязательные требования и бонусное выполнены.** Оговорки (⚠️) касаются деталей трактовки и не нарушают требований; они собраны в конце документа.

---

## 1–2. Цель и описание игры

| Требование | Статус | Где |
|---|---|---|
| 2D Tower Defense с видом сверху | ✅ | Ортографическая камера, top-down спрайты, тайлы Kenney top-down |
| Враги идут волнами по заданному маршруту | ✅ | `EnemySpawner` + `WaypointPath`, маршруты из `LevelConfig` |
| Башни строятся перетаскиванием из UI-магазина | ✅ | `TowerCardView` → `TowerShopView` → `TowerShopPresenter` → `TowerPlacementService` |
| Башни убивают или замедляют врагов | ✅ | `ShooterTower`, `SlowAuraTower` |
| Золото за убийство, тратится на башни | ✅ | `EnemyOutcomeHandler` → `PlayerWallet.Add`; `TowerPlacementService` → `TrySpend` |

---

## 3.1. Враги

| Требование | Статус | Доказательство |
|---|---|---|
| Три типа врагов | ✅ | `Assets/Configs/Enemies/Enemy_Normal`, `Enemy_Fast`, `Enemy_Tank` |
| Параметр «здоровье» | ✅ | `EnemyConfig._maxHealth` |
| Параметр «скорость» | ✅ | `EnemyConfig._moveSpeed` |
| Параметр «награда» | ✅ | `EnemyConfig._goldReward` |
| Параметр «спрайт» | ✅ | `EnemyConfig._sprite` (+ кадры ходьбы `_walkFrames`) |
| Параметры в `EnemyConfig` (ScriptableObject) | ✅ | [EnemyConfig.cs](../Assets/Scripts/Configs/EnemyConfig.cs) — `[CreateAssetMenu]`, `ScriptableObject` |
| Fast: мало здоровья, высокая скорость | ✅ | HP 40, скорость 3 |
| Normal: средние характеристики | ✅ | HP 100, скорость 2 |
| Tank: много здоровья, низкая скорость | ✅ | HP 350, скорость 1 |
| Появляется в начале пути | ✅ | [EnemyFactory.cs:19](../Assets/Scripts/Enemies/EnemyFactory.cs#L19) и `Enemy.Init`: позиция = `path.GetPoint(0)` |
| Движется по точкам маршрута | ✅ | [Enemy.cs](../Assets/Scripts/Enemies/Enemy.cs) — `Move()` в цикле по `_nextWaypointIndex` |
| Получает урон | ✅ | `Enemy.TakeDamage(float)` |
| Умирает при здоровье ≤ 0 | ✅ | [Enemy.cs:144](../Assets/Scripts/Enemies/Enemy.cs#L144) — `if (Health <= 0f) Die();` |
| Даёт золото при смерти | ✅ | [EnemyOutcomeHandler.cs:34](../Assets/Scripts/Core/EnemyOutcomeHandler.cs#L34) — подписан на `Enemy.Died`, вызывает `_wallet.Add(GoldReward)` |

---

## 3.2. Башни

### Башня 1: Shooter Tower — ✅

| Требование | Статус | Доказательство |
|---|---|---|
| Ищет врагов в радиусе атаки | ✅ | [ShooterTower.cs:62](../Assets/Scripts/Towers/ShooterTower.cs#L62) — `Physics2D.OverlapCircle(..., AttackRadius, enemyFilter)` |
| Выбирает цель | ✅ | [ShooterTower.cs:59](../Assets/Scripts/Towers/ShooterTower.cs#L59) `FindTarget()` — враг, ближайший к финишу (`DistanceTravelled`) |
| Стреляет снарядами | ✅ | `ShooterTower.Fire` с периодом `1 / CurrentFireRate` |
| Наносит урон | ✅ | Урон из конфига передаётся в `Projectile.Launch`, снаряд вызывает `Enemy.TakeDamage` |
| Использует `ProjectilePool` | ✅ | [ShooterTower.cs:92](../Assets/Scripts/Towers/ShooterTower.cs#L92) — `_projectilePool.Rent()` |
| Параметры: радиус, урон, скорострельность, скорость снаряда | ✅ | [ShooterTowerConfig.cs](../Assets/Scripts/Configs/ShooterTowerConfig.cs) |
| Параметры: цена, спрайт/иконка | ✅ | [TowerConfig.cs](../Assets/Scripts/Configs/TowerConfig.cs) — `_cost`, `_sprite`, `_icon` |

### Башня 2: Attack Speed Tower — ✅

| Требование | Статус | Доказательство |
|---|---|---|
| Ищет соседние стреляющие башни | ✅ | [AttackSpeedAuraTower.cs](../Assets/Scripts/Towers/AttackSpeedAuraTower.cs) — `AuraTower<IAttackSpeedModifiable>`, поиск по слою Tower ([AuraTower.cs:90](../Assets/Scripts/Towers/AuraTower.cs#L90)) |
| Увеличивает их скорострельность | ✅ | `ApplyEffect` → `AddAttackSpeedModifier(this, 1.5)`; `ShooterTower.CurrentFireRate = FireRate × ModifierStack.Value` |
| Снимает эффект, когда башня выходит из радиуса | ✅ | [AuraTower.cs:73](../Assets/Scripts/Towers/AuraTower.cs#L73) — `RemoveEffect` для ушедших; при отключении ауры эффект снимается со всех ([AuraTower.cs:108](../Assets/Scripts/Towers/AuraTower.cs#L108)) |
| Пример «1.5 = на 50% быстрее» | ✅ | Множитель умножает выстрелы в секунду: 1.5 → в 1.5 раза чаще |
| Параметры: радиус, множитель, цена, спрайт/иконка | ✅ | [AuraTowerConfig.cs](../Assets/Scripts/Configs/AuraTowerConfig.cs), [AttackSpeedTowerConfig.cs](../Assets/Scripts/Configs/AttackSpeedTowerConfig.cs), `TowerConfig` |

### Башня 3: Slow Tower — ✅

| Требование | Статус | Доказательство |
|---|---|---|
| Ищет соседних врагов | ✅ | [SlowAuraTower.cs](../Assets/Scripts/Towers/SlowAuraTower.cs) — `AuraTower<ISlowable>`, поиск по слою Enemy |
| Замедляет врагов в радиусе | ✅ | `ApplyEffect` → `AddSpeedModifier(this, 0.5)`; `Enemy.CurrentSpeed = MoveSpeed × ModifierStack.Value` |
| Снимает замедление, когда враг выходит | ✅ | Тот же механизм `AuraTower`: сравнение «в радиусе сейчас» и «под эффектом» |
| Пример «0.5 = в два раза медленнее» | ✅ | Скорость умножается на 0.5 |
| Параметры: радиус, множитель скорости, цена, спрайт/иконка | ✅ | [SlowTowerConfig.cs](../Assets/Scripts/Configs/SlowTowerConfig.cs), `AuraTowerConfig`, `TowerConfig` |

---

## 4. Снаряды

| Требование | Статус | Доказательство |
|---|---|---|
| Снаряд — отдельная игровая сущность | ✅ | [Projectile.cs](../Assets/Scripts/Projectiles/Projectile.cs), префаб `Assets/Prefabs/Projectile.prefab` |
| Получает цель | ✅ | `Projectile.Launch(target, damage, speed)` |
| Летит к цели | ✅ | `Projectile.Update` — `MoveTowards` по направлению к цели |
| Наносит урон при попадании | ✅ | `_target.TakeDamage(_damage)` при расстоянии ≤ `_hitDistance` |
| Возвращается в пул после попадания | ✅ | [Projectile.cs:45](../Assets/Scripts/Projectiles/Projectile.cs#L45) |
| Возвращается в пул, если цель исчезла | ✅ | [Projectile.cs:31](../Assets/Scripts/Projectiles/Projectile.cs#L31) — `if (_target == null \|\| !_target.IsAlive) ReturnToPool();` |
| Реализован паттерн Object Pool | ✅ | [ObjectPool.cs:11](../Assets/Scripts/Projectiles/ObjectPool.cs#L11) — `ObjectPool<T> where T : Component, IPoolable` (стек свободных объектов, защита от двойного возврата, предзаполнение) |
| Нет `Instantiate`/`Destroy` на каждый выстрел | ✅ | `Instantiate` снаряда есть только в `ProjectilePool.CreateProjectile` — при предзаполнении (30 шт.) и если пул пуст; `Destroy` для снарядов не вызывается нигде |
| Используются `ProjectilePool.Rent()` и `ProjectilePool.Return()` | ✅ | [ProjectilePool.cs:19](../Assets/Scripts/Projectiles/ProjectilePool.cs#L19), [ProjectilePool.cs:25](../Assets/Scripts/Projectiles/ProjectilePool.cs#L25) |

---

## 5. UI и установка башен

### 5.1. Магазин

| Требование | Статус | Доказательство |
|---|---|---|
| В UI есть магазин с карточками башен | ✅ | `TowerShopView` создаёт по карточке на каждый конфиг из `Bootstrap._shopTowers` (3 шт., проверено в Play Mode) |
| Карточка показывает иконку | ✅ | `TowerCardView.Setup` → `_icon.sprite = config.Icon` |
| Карточка показывает цену | ✅ | `TowerCardView.Setup` → `_priceText.text = config.Cost` |
| Нажать на карточку, перетащить, отпустить, башня ставится | ✅ | `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` → события → `TowerShopPresenter.OnDragEnded` → `TowerPlacementService.TryPlace`. Во время перетаскивания превью «прилипает» к площадке и красится в зелёный или красный |

### 5.2. Установка

| Требование | Статус | Доказательство |
|---|---|---|
| Хватает золота | ✅ | [TowerPlacementService.cs:71](../Assets/Scripts/Placement/TowerPlacementService.cs#L71) — `NotEnoughGold` |
| Позиция в разрешённой зоне | ✅ | [TowerPlacementService.cs:74](../Assets/Scripts/Placement/TowerPlacementService.cs#L74) — `OutsideBuildZone` (`OverlapPoint` по слою BuildZone) |
| На месте нет другой башни (бонус) | ✅ | [TowerPlacementService.cs:76](../Assets/Scripts/Placement/TowerPlacementService.cs#L76) — `ZoneOccupied`; `BuildZone.Occupant` |
| После успеха башня появляется | ✅ | [TowerPlacementService.cs:57](../Assets/Scripts/Placement/TowerPlacementService.cs#L57) — `_factory.Create(config, zone.PlacementPosition)` |
| После успеха списывается золото | ✅ | [TowerPlacementService.cs:55](../Assets/Scripts/Placement/TowerPlacementService.cs#L55) — `_wallet.TrySpend(config.Cost)` |
| При неудаче башня не создаётся | ✅ | `TryPlace` выходит до вызова фабрики при любом результате, кроме `Success` |
| При неудаче золото не списывается | ✅ | `TrySpend` вызывается только после успешной проверки; игроку показывается причина («Not enough gold», «Can't build here», «This spot is taken») |

---

## 6. MVP магазина

| Требование | Статус | Доказательство |
|---|---|---|
| Классы `TowerShopModel`, `TowerShopView`, `TowerShopPresenter`, `TowerCardView` | ✅ | `Assets/Scripts/UI/Shop/` |

**TowerShopModel** — [TowerShopModel.cs](../Assets/Scripts/UI/Shop/TowerShopModel.cs)

| Требование | Статус | Комментарий |
|---|---|---|
| Хранит список башен | ✅ | `IReadOnlyList<TowerConfig> Towers` |
| Не взаимодействует с UI | ✅ | Обычный C#-класс без ссылок на UI |
| Не создаёт башни | ✅ | |
| Только хранит данные | ✅ | Один конструктор и одно свойство |

**TowerShopView** — [TowerShopView.cs](../Assets/Scripts/UI/Shop/TowerShopView.cs)

| Требование | Статус | Комментарий |
|---|---|---|
| Отвечает за отображение | ✅ | Карточки, превью перетаскивания, сообщения |
| Создаёт карточки | ✅ | `Show(towers)` |
| Показывает иконки и цены | ✅ | Через `TowerCardView.Setup` |
| Принимает drag-and-drop события | ✅ | Подписан на `DragStarted` / `Dragged` / `DragEnded` каждой карточки |
| Сообщает наружу о действиях | ✅ | Собственные события `DragStarted` / `Dragged` / `DragEnded(TowerConfig, Vector2)` |
| Не создаёт башни | ✅ | Ссылок на `TowerFactory` и `PlayerWallet` нет |

**TowerShopPresenter** — [TowerShopPresenter.cs](../Assets/Scripts/UI/Shop/TowerShopPresenter.cs)

| Требование | Статус | Комментарий |
|---|---|---|
| Связывает UI с игровой логикой | ✅ | View ↔ `TowerPlacementService`, `PlayerWallet` |
| Получает события View | ✅ | `OnDragStarted`, `OnDragged`, `OnDragEnded` |
| Передаёт команды в `TowerPlacementService` | ✅ | `Validate` (подсветка превью), `TryPlace` (установка) |
| Без физики | ✅ | Только перевод экранных координат в мировые через камеру; `Physics2D` вызывается в `TowerPlacementService` |
| Не создаёт башни | ✅ | |

**TowerCardView** — [TowerCardView.cs](../Assets/Scripts/UI/Shop/TowerCardView.cs)

| Требование | Статус | Комментарий |
|---|---|---|
| Отвечает за одну карточку | ✅ | |
| Показывает иконку | ✅ | |
| Показывает цену | ✅ | Плюс иконка кошелька |
| Начало перетаскивания | ✅ | `OnBeginDrag` → `DragStarted` |
| Перетаскивание | ✅ | `OnDrag` → `Dragged` |
| Отпускание | ✅ | `OnEndDrag` → `DragEnded` |

---

## 7. Архитектурные требования

### 7.1. ScriptableObject-конфиги — ✅

| Требование | Статус | Доказательство |
|---|---|---|
| `EnemyConfig` | ✅ | [EnemyConfig.cs](../Assets/Scripts/Configs/EnemyConfig.cs) + 3 ассета |
| `TowerConfig` | ✅ | [TowerConfig.cs](../Assets/Scripts/Configs/TowerConfig.cs) (абстрактный) + `ShooterTowerConfig`, `AttackSpeedTowerConfig`, `SlowTowerConfig` и 3 ассета |
| Параметры врагов и башен не захардкожены | ✅ | Башни читают `_config.AttackRadius`, `_config.Damage`, `_config.FireRate`, `_config.EffectRadius`, `_config.SpeedMultiplier`, `_config.AttackSpeedMultiplier`; враги — `Config.MoveSpeed`, `Config.MaxHealth`, `Config.GoldReward`. В логике нет числовых значений урона, радиусов, скоростей, здоровья и цен |

Сверх требования данными стали и уровни (`LevelConfig`: карта, пути, площадки, золото, жизни, волны) и тема карты (`MapTheme`).

### 7.2. Factory — ✅

| Требование | Статус | Доказательство |
|---|---|---|
| Класс `TowerFactory` | ✅ | [TowerFactory.cs:24](../Assets/Scripts/Towers/TowerFactory.cs#L24) — единственный `Instantiate` башни |
| UI и система установки не вызывают `Instantiate` для башен | ✅ | `TowerShopView` создаёт только UI-карточки; `TowerPlacementService` вызывает `_factory.Create` |

Дополнительно: `EnemyFactory` для врагов.

### 7.4. События / Observer — ✅

| Событие | Статус | Объявление | Подписчики |
|---|---|---|---|
| `Enemy.Died` | ✅ | [Enemy.cs:17](../Assets/Scripts/Enemies/Enemy.cs#L17) | `EnemyOutcomeHandler` |
| `Enemy.ReachedFinish` | ✅ | [Enemy.cs:18](../Assets/Scripts/Enemies/Enemy.cs#L18) | `EnemyOutcomeHandler` |
| `PlayerWallet.GoldChanged` | ✅ | [PlayerWallet.cs:12](../Assets/Scripts/Core/PlayerWallet.cs#L12) | `HudView`, `TowerShopPresenter` |
| `TowerCardView.DragStarted` | ✅ | [TowerCardView.cs:54](../Assets/Scripts/UI/Shop/TowerCardView.cs#L54) | `TowerShopView` |
| `TowerCardView.Dragged` | ✅ | [TowerCardView.cs:55](../Assets/Scripts/UI/Shop/TowerCardView.cs#L55) | `TowerShopView` |
| `TowerCardView.DragEnded` | ✅ | [TowerCardView.cs:56](../Assets/Scripts/UI/Shop/TowerCardView.cs#L56) | `TowerShopView` |

Сверх минимума ещё около 15 событий (`PlayerLives.Changed/Depleted`, `GameFlow.Victory/Defeat`, `EnemySpawner.WaveStarted/AllWavesSpawned`, `ShooterTower.Fired`, `IAuraTower.AffectedCountChanged` и др.), полный список — в [ARCHITECTURE.md, раздел 8](ARCHITECTURE.md#8-события-observer). Все подписки снимаются в `OnDestroy` / `OnDisable` / `Dispose`.

### 7.5. Интерфейсы — ✅

| Интерфейс | Должен реализовывать | Реализует | Должен использовать | Использует | Статус |
|---|---|---|---|---|---|
| `ISlowable` | `Enemy` | `Enemy` ([Enemy.cs:7](../Assets/Scripts/Enemies/Enemy.cs#L7)) | `SlowAuraTower` | `SlowAuraTower : AuraTower<ISlowable>` | ✅ |
| `IAttackSpeedModifiable` | `ShooterTower` | `ShooterTower` ([ShooterTower.cs:7](../Assets/Scripts/Towers/ShooterTower.cs#L7)) | `AttackSpeedAuraTower` | `AttackSpeedAuraTower : AuraTower<IAttackSpeedModifiable>` | ✅ |
| `IPoolable` | `Projectile` | `Projectile` ([Projectile.cs:5](../Assets/Scripts/Projectiles/Projectile.cs#L5)) | `ObjectPool` | `ObjectPool<T> where T : Component, IPoolable` | ✅ |

Дополнительно: `IAuraTower` — интерфейс «только для чтения», через который визуал (`OrchestraAnimator`, `BellAnimator`) видит ауру, не зная её обобщённого типа.

---

## Запрещённые практики

| Запрет | Статус | Как проверено |
|---|---|---|
| Создание башен прямо из UI | ✅ не используется | UI вызывает только презентер; башни создаёт `TowerFactory` через `TowerPlacementService` |
| Все системы в одном `GameManager` | ✅ не используется | GameManager нет. `Bootstrap` — composition root без логики (создаёт и связывает), решения приняты в отдельных классах (`GameFlow`, `TowerPlacementService`, `EnemyOutcomeHandler` и др.). Синглтонов и статических «менеджеров» нет (единственное статическое состояние — индекс выбранного уровня `LevelSelection`, без логики) |
| `Instantiate`/`Destroy` снаряда на каждый выстрел | ✅ не используется | Поиск по коду: `Instantiate` снаряда только в `ProjectilePool`, `Destroy` для снарядов нет |
| Захардкоженные параметры врагов и башен | ✅ не используется | См. 7.1 |
| UI напрямую работает с физикой или экономикой | ✅ не используется | Views не вызывают `Physics2D` и не меняют золото; `Physics2D` — в `TowerPlacementService` и башнях, золото меняют `TowerPlacementService` и `EnemyOutcomeHandler`. См. оговорку 2 |

---

## Оговорки и замечания

1. **«Click on a tower card».** Отдельного «клика» нет: перетаскивание начинается при нажатии и движении мыши (стандартный drag в Unity EventSystem). Простой клик без движения ничего не делает. Это соответствует сценарию задания «нажать → тащить → отпустить».
2. **`HudView` наблюдает `PlayerWallet` и `PlayerLives`.** View получает модели в `Bind()` и подписывается на их события, чтобы показывать золото и жизни. Он ничего в них не меняет, только читает (вариант MVP «Supervising Controller»). Если нужна строгая пассивная View, подписку можно перенести в отдельный `HudPresenter`, а View оставить с методами `SetGold`/`SetLives`.
3. **`TowerShopPresenter` читает `PlayerWallet.CanAfford`**, чтобы покрасить недоступные карточки. Это работа презентера (связь UI и экономики); View о золоте не знает.
4. **Нумерация задания пропускает 7.3.** Проверены все пункты, которые есть в тексте.

## Что сделано сверх требований

- Три уровня как данные (`LevelConfig`), генерация карты из конфига (`MapBuilder`, Rule Tile для краёв дороги), две дороги на последнем уровне.
- Главное меню, переключение уровней (Next level / Restart / Menu) с перезагрузкой сцены.
- Победа и поражение (`GameFlow`), жизни игрока, счётчик волн.
- Анимации башен и врагов как отдельные наблюдатели (ходьба по пройденной дистанции, кольцо замедления, пыль).
- Звуки выстрела и колокола, фоновая музыка при наличии оркестра, кнопка выключения звука (MVP).
- HUD и магазин в едином стиле, подсветка карточек, превью установки с цветом допустимости.
- Editor-инструменты, которые воспроизводят всю настройку проекта пунктами меню.
