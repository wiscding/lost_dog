# 策划可调参数总表

本文档汇总当前项目中**可在 Godot 检查器里直接改**的导出字段，方便关卡与数值策划调整。  
修改位置分三类：

| 改哪里 | 适用情况 |
|--------|----------|
| **关卡里某个实例** | 只影响该关卡这一颗拾取、这一个木桩等 |
| **`res://Data/*.tres` 资源** | 影响所有引用该资源的对象（如玩家身上绑的饼干/飞盘/钩索数据） |
| **`player/player.tscn` 根节点 `Player`** | 影响该场景实例化出来的玩家（主流程玩家一般改这里） |

**程序硬编码、策划一般不改：**部分能力按键写在 `Player.cs` 里（见文末「操作键」）。

---

## 1. 玩家 `Player`（`res://player/player.tscn` 根节点）

脚本：`res://player/scripts/Player.cs`

### Movement（移动）

| 参数 | 默认 | 说明 |
|------|------|------|
| BaseSpeed | 140 | 起步水平速度 |
| MaxSpeed | 260 | 跑满后最大水平速度 |
| DistanceToMaxSpeed | 320 | 按距离加速：累计移动多少像素后顶到 MaxSpeed |
| StopFriction | 18 | 松手后水平速度衰减强度 |
| MoveAcceleration | 90 | 按方向时朝目标速度靠近的快慢 |

### Crouch（下蹲）

| 参数 | 默认 | 说明 |
|------|------|------|
| CrouchSpeedMultiplier | 0.45 | 下蹲时速度 = 正常 × 该倍率 |

### Gravity & Jump（重力与跳跃）

| 参数 | 默认 | 说明 |
|------|------|------|
| Gravity | 1200 | 重力加速度（像素/秒²） |
| JumpVelocity | 360 | 起跳初速度（向上为负 Y 的数值） |
| CoyoteTime | 0.10 | 土狼时间（秒），离地后仍允许起跳 |
| JumpBufferTime | 0.10 | 跳跃缓冲（秒），提前按跳的记忆 |
| JumpCutMultiplier | 0.45 | 上升阶段松开跳跃键时垂直速度乘数（短跳） |

### Attack（攻击）

| 参数 | 默认 | 说明 |
|------|------|------|
| AttackCooldown | 0.25 | 攻击冷却（秒） |
| AttackStateTime | 0.12 | 攻击动作覆盖状态时长（秒） |
| MeleeDamageHalfHearts | 5 | 近战单次伤害（**半心**：1=½心，2=1心） |
| HitlagDurationSeconds | 0.05 | 命中停顿时长（秒） |
| HitlagTimeScale | 0 | 命中时全局时间缩放（0~1） |
| DamageShakeDurationSeconds | 0.18 | 受伤镜头震动时长 |
| DamageShakeStrength | 7 | 受伤镜头震动强度 |

### Health（生命）

| 参数 | 默认 | 说明 |
|------|------|------|
| MaxHearts | 3 | 最大心数（整颗心） |
| HitInvulnerabilitySeconds | 1 | 受伤后无敌时间（秒）；0 表示关闭 |

### Abilities（能力与开局）

| 参数 | 默认 | 说明 |
|------|------|------|
| CookieAbilityData | `CookieData.tres` | 饼干技能数值资源 |
| BoomerangAbilityData | `BoomerangData.tres` | 飞盘技能数值资源 |
| HookAbilityData | `HookData.tres` | 套索技能数值资源 |
| StartWithCookieAbility | false | 开局是否已解锁饼干 |
| StartWithBoomerangAbility | false | 开局是否已解锁飞盘 |
| StartWithHookAbility | false | 开局是否已解锁套索 |

---

## 2. 能力数值资源（`AbilityData` 公有字段）

三类技能数据均继承 `res://player/Abilities/AbilityData.cs`，在 **`CookieData` / `BoomrangData` / `HookData`** 资源里都能看到：

| 参数 | 说明 |
|------|------|
| DisplayName | 技能显示名（UI 可用） |
| Icon | 图标纹理 |
| CoolDown | 冷却（秒） |
| MaxUses | 最大使用次数；**-1 表示无限**（钩索默认脚本里是 -1） |
| Description | 描述（可多行） |
| UseSound | 释放音效 |
| EffectScene | 关联场景（飞盘为投射物场景等） |

### 2.1 饼干 `CookieData`（`res://Data/CookieData.tres`）

| 参数 | 默认（构造函数） | 说明 |
|------|------------------|------|
| MaxUses | 3 | 饼干剩余次数上限 |
| CoolDown | 0 | 使用间隔 |
| HealAmount | 2 | 每次使用回复量（与回血逻辑对齐的具体单位见程序实现） |
| RefillAtRestPoint | true | 是否在休息/存档点补满 |

### 2.2 飞盘 `BoomrangData`（`res://Data/BoomerangData.tres`）

继承 `AbilityData` 的字段 **外加**以下专有项：

| 参数 | 脚本默认 | 说明 |
|------|----------|------|
| FlySpeed | 400 | 飞出速度 |
| MaxDistance | 200 | 飞出多远后开始回程 |
| MaxDuration | 3 | 飞盘最长存活（秒） |
| Damage | 15 | 伤害相关数值（程序内换算半心） |
| HitboxRadius | 12 | 命中体积半径 |
| SpeedBuff | 100 | 丢出后加速量（加到 MaxSpeed） |
| BuffDuration | 0.8 | 加速持续时间（秒） |
| ReturnSpeed | 520 | 回程速度 |
| SpinSpeedDeg | 720 | 视觉旋转（度/秒） |
| CatchRadius | 14 | 回到玩家多近算接住 |
| CooldownStartsOnReturn | true | true：飞回后再开始算 CD |

`.tres` 里若单独写了 `FlySpeed`、`MaxUses` 等，**以资源文件为准**。

### 2.3 套索 `HookData`（`res://Data/HookData.tres`）

继承 `AbilityData` **外加**：

| 参数 | 默认 | 说明 |
|------|------|------|
| RayOriginYOffset | -22 | 射线起点相对玩家脚点的 Y 偏移 |
| MaxSearchRange | 220 | 向上扇形射线最长搜索距离 |
| RopeTipSpeed | 1400 | 出绳阶段绳头延伸速度 |
| MaxRopeOutTime | 0.35 | 出绳最长时长（秒） |
| LatchHoldSeconds | 0.05 | 钩住后硬直（秒） |
| PullSpeed | 900 | 拉向锚点速度 |
| ArriveDistance | 10 | 判定“到位”的距离 |
| ExitSpeed | 420 | 掠过锚点后飞出速度 |
| FlyOutLockSeconds | 0.22 | 飞出阶段锁输入时长 |
| FlyOutGravityScale | 1 | 飞出阶段重力倍率 |
| AnchorCollisionMask | 第 8 层 (bit7) | 能钩到的碰撞层，需与锚点 `collision_layer` 一致 |
| RefillAtRestPoint | true | 有限次数时是否在休息点补满 |

构造函数默认 **MaxUses = -1（无限）**、CoolDown = 0.6。

---

## 3. 成长拾取 `GrowthPickup`（基类 `res://growth_pickup.tscn`）

脚本：`res://combat/GrowthPickup.cs`  

子场景：磨牙玩具 / 神奇的心 / 饼干袋（均继承基类并覆写部分字段）：

| 场景 | growth_type（类型） |
|------|---------------------|
| `res://tooth_toy_pickup.tscn` | tooth_toy |
| `res://magic_heart_pickup.tscn` | magic_heart |
| `res://cookie_bag_pickup.tscn` | cookie_bag |

| 参数 | 默认 | 说明 |
|------|------|------|
| GrowthType | 见上表 | 成长类型枚举 |
| Amount | 1 | 叠乘次数（心/袋为“加几个”，磨牙玩具为套数） |
| ConsumeOnPickup | true | 捡到后是否删除该节点 |
| PickupLogLabel | 各子场景已设 | 调试用日志标签 |
| ToothToyAttackBonus | 4 | **仅 tooth_toy**：每份增加多少**半心**近战伤害 |

**效果摘要（程序逻辑）：**

- `tooth_toy`：增加 `ToothToyAttackBonus × Amount` 的半心伤害  
- `magic_heart`：增加 `Amount` 颗**整心**的上限与当前血  
- `cookie_bag`：增加 `Amount` 点饼干**上限**与当前可用次数  

---

## 4. 能力拾取 `AbilityPickup`（基类 `res://ability_pickup.tscn`）

子场景约定：

| 场景 | ability_id |
|------|------------|
| `res://cookie_pickup.tscn` | cookie |
| `res://boomerang_pickup.tscn` | boomerang |
| `res://hook_pickup.tscn` | hook |

| 参数 | 默认 | 说明 |
|------|------|------|
| AbilityId | 见上表 / 或 boomerang | 解锁的能力 id |
| ConsumeOnPickup | true | 解锁后是否删除 |
| PickupLogLabel | 各子场景 | 调试用 |

---

## 5. 日记拾取 `DiaryPickup`（`res://diary_pickup.tscn`）

| 参数 | 默认 | 说明 |
|------|------|------|
| Amount | 1 | 增加多少本「日记」计数（`Player.AddDiary`） |
| ConsumeOnPickup | true | 捡后是否消失 |
| PickupLogLabel | DiaryPickup | 调试用 |

---

## 6. Boss 能力掉落 `BossAbilityDropper`（`res://combat/BossAbilityDropper.cs`）

挂在关卡节点上，用于 Boss 死亡时在掉落点生成能力拾取。

| 参数 | 默认 | 说明 |
|------|------|------|
| BossNodePath | （脚本内默认 `../TrainingDummy`） | Boss 节点路径 |
| PickupScene | cookie_pickup | 要实例化的拾取场景 |
| DropPointPath | （脚本内默认 `../CookieDropPoint`） | 掉落位置 `Marker2D` 等 |
| BossDeathSignalName | Died | Boss 死亡信号名 |
| DropAbilityId | cookie | 生成拾取上的能力 id：cookie / boomerang / hook |
| DropOnlyOnce | true | 是否只掉一次 |

---

## 7. NPC 任务奖励桥 `NpcQuestRewardBridge`（`res://combat/NpcQuestRewardBridge.cs`）

程序同事挂在关卡里给 NPC 调用；策划如需自测可改：

| 参数 | 默认 | 说明 |
|------|------|------|
| GrowthPickupScene | growth_pickup.tscn | 生成地面成长道具时用 |
| DefaultPlayerPath | ../Player | 解析玩家节点 |

发奖方式由程序调用方法，**无额外导出开关**。详见 `docs/NPC_REWARD_INTEGRATION.md`。

---

## 8. 训练木桩 `TrainingDummy`（`res://training_dummy.tscn`）

| 参数 | 默认 | 说明 |
|------|------|------|
| MaxHalfHearts | 8 | 木桩血量（半心为单位）；归零发 `Died` 并移除 |

---

## 9. 钩索锚点 `HookAnchor`（`res://hook_anchor.tscn`）

脚本自动加入组 `hook_anchor`；默认若未设 layer，会用 **第 8 层**。  
策划需在编辑器里调整 **Area2D 的 CollisionShape 大小与 collision_layer**，并与 `HookData.AnchorCollisionMask` 对齐。

---

## 10. 投射物基类 `ProjectileBase`（飞盘实例继承）

若单独打开 `boomerang_projectile.tscn` 且脚本链上有基类导出，可调：

| 参数 | 说明 |
|------|------|
| Speed / GravityVector / Lifetime | 运动（飞盘脚本内多数字来自 BoomrangData，以实际脚本为准） |
| HitBehavior | 命中后行为 |
| DamageHalfHearts | 伤害（半心） |
| AllowRepeatedDamageOnSameTarget | 同一目标能否多次伤害 |
| HurtboxLayer | 检测层 |

**建议：**飞盘手感优先改 **`Data/BoomerangData.tres`**，避免与 `BoomerangProjectile` 内部逻辑打架。

---

## 11. 操作键（代码固定，非导出）

当前 `Player` 内逻辑（若要改键需程序改代码或改为 Input Map）：

| 键 | 功能 |
|----|------|
| **R** | 饼干 |
| **K** | 飞盘 |
| **F** | 套索 |

近战攻击若绑定 **J**，与飞盘 **K** 分开；请勿把飞盘改回 **J**。

---

## 12. 相关文档

- NPC 与神奇的心 / 饼干袋任务对接：`docs/NPC_REWARD_INTEGRATION.md`

---

*文档随导出字段变更时请同步更新此表。*
