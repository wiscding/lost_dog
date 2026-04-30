# NPC Reward Integration

给负责 NPC 逻辑的同伴使用：通过 `NpcQuestRewardBridge` 发放任务奖励。

## 1) 场景挂载

- 在关卡里新建一个节点（例如 `QuestRewardBridge`）。
- 挂脚本：`res://combat/NpcQuestRewardBridge.cs`
- `DefaultPlayerPath` 指向 `Player`（默认是 `../Player`）。
- `GrowthPickupScene` 留空会自动用 `res://growth_pickup.tscn`。

## 2) 两种发奖方式

- 直接发给玩家（推荐给“提交任务立刻生效”）
  - `GrantGrowthDirect("magic_heart", 1, player)`
  - `GrantGrowthDirect("cookie_bag", 1, player)`
- 生成地面拾取物（推荐给“任务交付后掉落”表现）
  - `SpawnGrowthPickup("magic_heart", dropPos, 1)`
  - `SpawnGrowthPickup("cookie_bag", dropPos, 1)`

可用 `growthType`：
- `tooth_toy`
- `magic_heart`
- `cookie_bag`

## 3) 与 NPC 交互流程对接（E 键）

在 NPC 逻辑里，当玩家按 E 并且任务状态到达“可领奖”时，调用桥接器方法。

示例（伪代码）：

```csharp
if (interactPressed && questState == QuestState.ReadyToReward)
{
    rewardBridge.GrantMagicHeartFromQuest(player, 1);
    questState = QuestState.Rewarded;
}
```

或者：

```csharp
if (interactPressed && questState == QuestState.ReadyToReward)
{
    rewardBridge.SpawnGrowthPickup("cookie_bag", rewardDropPoint.GlobalPosition, 1);
    questState = QuestState.Rewarded;
}
```

## 4) 协作约定（建议）

- NPC 侧只负责：
  - `E` 交互
  - 任务状态推进
  - 在“可领奖”节点调用桥接器
- 奖励数值与道具细节统一由 `NpcQuestRewardBridge` 和 `GrowthPickup` 维护。

## 5) 神奇的心：对话 → 别的房间捡日记 → 回来交给同一 NPC

设计要点（与 `Player` 已有 API 对齐）：

| 阶段 | 谁负责 | 做什么 |
|------|--------|--------|
| 与 NPC 首次对话 | NPC 脚本 | 推进状态，例如 `AwaitingDiaryPickup`；可在此打开侧门 / 刷出隐藏房里的「日记」交互物 |
| 在别的房间捡到日记 | 关卡里摆 `res://diary_pickup.tscn`，或 NPC 在合适时机 `Instantiate` 该场景 | 触碰即 `AddDiary`；也可在编辑器里把 `Amount` 改成多页日记 |
| 回到 NPC 按 E | NPC 脚本 | 若状态为「等待提交」且 `player.GetDiaryCount() >= 需要数量`，则扣日记并发奖 |

**推荐一键调用（扣日记 + 发神奇的心）：**

```csharp
// 默认扣 1 本日记，奖励 1 颗心的上限（即 magic_heart amount=1）
if (rewardBridge.TryGrantMagicHeartForDiaryDelivery(player, diaryFragmentsRequired: 1, heartsReward: 1))
{
    // 成功：已 ConsumeDiary + GrantMagicHeart
    questState = QuestState.Rewarded;
}
```

若你们希望「交日记后地上再出现神奇的心再捡」，不要用上面方法；改为先 `ConsumeDiary`，再 `SpawnGrowthPickup("magic_heart", dropPos, 1)`。

**Player 日记相关 API（`res://player/scripts/Player.cs`）：**

- `AddDiary(int amount = 1)`：拾取日记碎片时增加
- `GetDiaryCount()`：提交前检查
- `ConsumeDiary(int amount = 1)`：提交成功时扣除（`TryGrantMagicHeartForDiaryDelivery` 内部会调用）

NPC 状态机建议（可与你们现有对话系统映射）：

1. `MetNpc` — 首次对话结束 → `NeedDiaryFromRoom`
2. 玩家进入隐藏房拾取 → `AddDiary` → `HasDiary`
3. 回到 NPC 按 E → 若 `HasDiary` 且 `TryGrantMagicHeartForDiaryDelivery` 成功 → `Rewarded`

