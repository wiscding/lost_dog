using Godot;

/// <summary>
/// NPC 任务奖励桥接器（给负责 NPC 逻辑的同伴调用）：
/// 1) 直接发奖励到玩家（推荐：对话提交后立即生效）
/// 2) 在地面生成 GrowthPickup（推荐：做“任务交付后掉落”表现）
/// </summary>
[GlobalClass]
public partial class NpcQuestRewardBridge : Node
{
	[Export] public PackedScene GrowthPickupScene { get; set; }
	[Export] public NodePath DefaultPlayerPath { get; set; } = new("../Player");

	public override void _Ready()
	{
		if (GrowthPickupScene == null)
			GrowthPickupScene = ResourceLoader.Load<PackedScene>("res://growth_pickup.tscn");
	}

	/// <summary>
	/// 直接给玩家发成长奖励（无需拾取）。
	/// growthType: tooth_toy / magic_heart / cookie_bag
	/// </summary>
	public bool GrantGrowthDirect(string growthType, int amount = 1, Node playerNode = null)
	{
		var player = ResolvePlayer(playerNode);
		if (player == null)
		{
			GD.PushWarning("[NpcQuestRewardBridge] player not found, direct grant skipped.");
			return false;
		}

		var safeAmount = Mathf.Max(1, amount);
		switch (growthType)
		{
			case "tooth_toy":
				// 与 GrowthPickup 默认保持一致：每个磨牙玩具 +4 半心伤害。
				player.AddMeleeDamage(4 * safeAmount);
				return true;
			case "magic_heart":
				player.AddMaxHearts(safeAmount);
				return true;
			case "cookie_bag":
				player.AddCookieCapacity(safeAmount);
				return true;
			default:
				GD.PushWarning($"[NpcQuestRewardBridge] unknown growthType: {growthType}");
				return false;
		}
	}

	/// <summary>
	/// 在指定世界坐标生成成长拾取物（玩家接触后生效）。
	/// </summary>
	public GrowthPickup SpawnGrowthPickup(string growthType, Vector2 worldPosition, int amount = 1)
	{
		if (GrowthPickupScene == null)
		{
			GD.PushWarning("[NpcQuestRewardBridge] GrowthPickupScene is null.");
			return null;
		}

		var pickup = GrowthPickupScene.Instantiate<GrowthPickup>();
		if (pickup == null)
		{
			GD.PushWarning("[NpcQuestRewardBridge] GrowthPickupScene is not GrowthPickup.");
			return null;
		}

		pickup.GrowthType = growthType;
		pickup.Amount = Mathf.Max(1, amount);
		pickup.PickupLogLabel = MakeLabel(growthType);

		var parent = GetTree()?.CurrentScene ?? GetParent();
		parent?.AddChild(pickup);
		pickup.GlobalPosition = worldPosition;

		GD.Print($"[NpcQuestRewardBridge] spawn growth pickup: type={pickup.GrowthType}, amount={pickup.Amount}, pos={pickup.GlobalPosition}");
		return pickup;
	}

	public bool GrantMagicHeartFromQuest(Node playerNode = null, int hearts = 1) =>
		GrantGrowthDirect("magic_heart", hearts, playerNode);

	/// <summary>
	/// 「神奇的心」任务交付：玩家已持有足够日记碎片时，扣除并发放神奇的心（加最大生命）。
	/// NPC 在 E 交互且判定可领奖时调用即可。
	/// </summary>
	public bool TryGrantMagicHeartForDiaryDelivery(Node playerNode = null, int diaryFragmentsRequired = 1, int heartsReward = 1)
	{
		var player = ResolvePlayer(playerNode);
		if (player == null)
		{
			GD.PushWarning("[NpcQuestRewardBridge] player not found, diary delivery skipped.");
			return false;
		}

		if (player.GetDiaryCount() < diaryFragmentsRequired)
			return false;

		if (!player.ConsumeDiary(diaryFragmentsRequired))
			return false;

		return GrantMagicHeartFromQuest(player, heartsReward);
	}

	public bool GrantCookieBagFromQuest(Node playerNode = null, int bags = 1) =>
		GrantGrowthDirect("cookie_bag", bags, playerNode);

	private Player ResolvePlayer(Node playerNode)
	{
		if (playerNode is Player p)
			return p;

		if (DefaultPlayerPath != null && !DefaultPlayerPath.IsEmpty)
			return GetNodeOrNull<Player>(DefaultPlayerPath);

		return null;
	}

	private static string MakeLabel(string growthType)
	{
		return growthType switch
		{
			"tooth_toy" => "ToothToyPickup",
			"magic_heart" => "MagicHeartPickup",
			"cookie_bag" => "CookieBagPickup",
			_ => "GrowthPickup"
		};
	}
}
