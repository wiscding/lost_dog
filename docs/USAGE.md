# Lost Dog 项目使用说明

面向：自己跑关卡测试、关卡策划摆东西、NPC 程序对接任务发奖。

---

## 1. 在编辑器里运行

1. 用 Godot 打开本项目（建议与团队统一版本，如 4.3/4.5 mono）。
2. 打开场景 `res://playground.tscn`（或其它你们的主关卡）。
3. 按 **F6** 运行**当前场景**，或把 `playground` 设为启动场景后 **F5**。

---

## 2. 操作说明（当前代码）

| 按键 | 功能 |
|------|------|
| 左右 / 上下 | 移动、部分能力瞄准方向 |
| 跳跃 | 跳跃（具体键位以项目 Input Map 为准） |
| **J** | 近战攻击（若项目里已绑定） |
| **R** | 使用饼干（需已解锁 + 有次数） |
| **K** | 使用飞盘（需已解锁 + 有次数 + 不在 Hook/攻击锁定时更易触发） |
| **F** | 使用套索（需已解锁；前方须有可钩的 `HookAnchor`） |

改键需要改 `Player.cs` 或改用 Godot **Input Map**（可让程序统一接）。

---

## 3. 关卡里摆放拾取物（策划）

### 能力类（解锁技能）

从文件系统拖入场景：

| 预制体 | 作用 |
|--------|------|
| `res://cookie_pickup.tscn` | 捡到解锁饼干 |
| `res://boomerang_pickup.tscn` | 捡到解锁飞盘 |
| `res://hook_pickup.tscn` | 捡到解锁套索 |

选中实例后可在检查器改：

- **Ability Id**：一般保持与子预制体一致（不要改成别的能力，易混）。
- **Consume On Pickup**：通常勾选，捡一次消失。

### 成长类（加属性）

| 预制体 | 作用 |
|--------|------|
| `res://tooth_toy_pickup.tscn` | 加近战伤害 |
| `res://magic_heart_pickup.tscn` | 加最大生命 |
| `res://cookie_bag_pickup.tscn` | 加饼干携带上限 |

可调：

- **Growth Type**：与预制体一致即可。
- **Amount**：份数（多颗心、多袋等）。
- **Tooth Toy Attack Bonus**：仅磨牙玩具有效，每份加多少半心伤害。

### 日记（任务用）

| 预制体 | 作用 |
|--------|------|
| `res://diary_pickup.tscn` | 碰到后增加玩家日记计数 |

可调 **Amount**（一次捡多页）。  
NPC 发「神奇的心」见下文第 5 节。

---

## 4. Boss 死亡掉能力（关卡策划）

1. 在关卡放 `BossAbilityDropper` 节点（脚本 `BossAbilityDropper.cs`）。
2. 配置：
   - **Boss Node Path**：要监听的 Boss（如 TrainingDummy）。
   - **Drop Point Path**：掉落位置（如 `CookieDropPoint` 的 Marker2D）。
   - **Pickup Scene**：要生成的拾取预制（如 `cookie_pickup.tscn`）。
   - **Drop Ability Id**：`cookie` / `boomerang` / `hook`，与拾取物一致。
3. Boss 必须能发出 **Died** 信号（当前 `TrainingDummy` 血量为 0 时会发）。

---

## 5. NPC 任务与奖励（给负责 NPC 的同事）

### 5.1 挂载桥接器

在关卡加一个节点，脚本：`res://combat/NpcQuestRewardBridge.cs`。

- **Default Player Path**：指向玩家，默认 `../Player` 可按层级改。
- **Growth Pickup Scene**：留空会用 `growth_pickup.tscn`。

### 5.2 神奇的心（对话 → 捡日记 → 回来交）

1. 在隐藏房摆 `diary_pickup.tscn`（或 NPC 在剧情后 `Instantiate`）。
2. 玩家捡到后日记数增加；回到 NPC 按你们交互键对话。
3. 可领奖时调用：

```csharp
rewardBridge.TryGrantMagicHeartForDiaryDelivery(player, diaryFragmentsRequired: 1, heartsReward: 1);
```

成功会扣日记并发神奇的心（加最大生命）。详细状态机示例见 `docs/NPC_REWARD_INTEGRATION.md`。

### 5.3 其它直接发奖（不交日记）

- 加生命上限：`GrantMagicHeartFromQuest(player, 1)`
- 加饼干上限：`GrantCookieBagFromQuest(player, 1)`
- 地上再掉拾取物：`SpawnGrowthPickup("magic_heart", globalPos, 1)` 等

---

## 6. 套索钩点（关卡）

1. 实例化 `res://hook_anchor.tscn` 放在可钩位置。
2. 调整 **Area2D 的 collision_layer**，必须与 `HookData` 里的 **Anchor Collision Mask** 能打到的一层一致（默认锚点用第 8 层，具体以资源为准）。
3. 拉大 **CollisionShape2D**，否则射线很难中。

---

## 7. 改数值（策划）

- **玩家手感、血、开局是否带能力**：选 `player/player.tscn` 根节点 `Player`，改各分组导出字段。
- **饼干 / 飞盘 / 套索详细数值**：改 `res://Data/CookieData.tres`、`BoomerangData.tres`、`HookData.tres`（或复制新资源再在 Player 上换引用）。

参数列表与默认值见：`docs/DESIGN_TUNABLE_PARAMETERS.md`。  
每个字段含义说明见：飞书/团队文档或向程序要「参数释义」一节。

---

## 8. 常见问题

| 现象 | 排查 |
|------|------|
| 套索按 F 没反应 | 是否解锁；冷却；前方是否有 HookAnchor；Mask 与 layer 是否一致。 |
| 飞盘按 K 没反应 | 是否解锁；次数是否为 0；是否 CD 中；是否在攻击/Hook 锁定（可看控制台 `Boomerang (K)` 日志）。 |
| 拾取物效果不对 | 检查实例上 **Ability Id / Growth Type** 是否被误改成别的；保存场景后重开。 |
| 日记交了没发奖 | 是否先 `AddDiary`；`GetDiaryCount` 是否够；是否调用了 `TryGrantMagicHeartForDiaryDelivery`。 |

---

## 9. 文档索引

| 文档 | 内容 |
|------|------|
| `docs/USAGE.md` | 本使用说明 |
| `docs/DESIGN_TUNABLE_PARAMETERS.md` | 策划可调参数总表 |
| `docs/NPC_REWARD_INTEGRATION.md` | NPC 发奖与神奇的心任务对接 |
