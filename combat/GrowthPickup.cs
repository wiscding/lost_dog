using Godot;

/// <summary>
/// 成长道具拾取物：直接触碰生效，不依赖 NPC 任务链。
/// </summary>
public partial class GrowthPickup : Area2D
{
	[Export(PropertyHint.Enum, "tooth_toy,magic_heart,cookie_bag")]
	public string GrowthType { get; set; } = "tooth_toy";

	[Export] public int Amount { get; set; } = 1;
	[Export] public bool ConsumeOnPickup { get; set; } = true;
	[Export] public string PickupLogLabel { get; set; } = "GrowthPickup";

	// 磨牙玩具：+4 普攻伤害（与图中配置一致）
	[Export] public int ToothToyAttackBonus { get; set; } = 4;

	public override void _Ready()
	{
		ApplyRuntimeFallbackByNodeName();
		BodyEntered += OnBodyEntered;
		GD.Print($"[PickupDebug][{PickupLogLabel}:{Name}] ready growth={GrowthType}, amount={Amount}, consume={ConsumeOnPickup}");
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

		var applied = ApplyGrowth(player);
		GD.Print($"[PickupDebug][{PickupLogLabel}:{Name}] picked growth={GrowthType}, amount={Amount}, applied={applied}");
		if (applied && ConsumeOnPickup)
			CallDeferred(Node.MethodName.QueueFree);
	}

	private bool ApplyGrowth(Player player)
	{
		var amount = Mathf.Max(1, Amount);
		switch (GrowthType)
		{
			case "tooth_toy":
				player.AddMeleeDamage(ToothToyAttackBonus * amount);
				return true;
			case "magic_heart":
				player.AddMaxHearts(amount);
				return true;
			case "cookie_bag":
				player.AddCookieCapacity(amount);
				return true;
			default:
				GD.PushWarning($"[GrowthPickup] unknown growth type: {GrowthType}");
				return false;
		}
	}

	private void ApplyRuntimeFallbackByNodeName()
	{
		var nodeName = Name.ToString();
		if (nodeName.Contains("ToothToy", System.StringComparison.OrdinalIgnoreCase) && GrowthType != "tooth_toy")
		{
			GD.PushWarning($"[GrowthPickup] auto-fix growth_type for {nodeName}: {GrowthType} -> tooth_toy");
			GrowthType = "tooth_toy";
			if (string.IsNullOrEmpty(PickupLogLabel) || PickupLogLabel == "GrowthPickup")
				PickupLogLabel = "ToothToyPickup";
		}
		else if (nodeName.Contains("MagicHeart", System.StringComparison.OrdinalIgnoreCase) && GrowthType != "magic_heart")
		{
			GD.PushWarning($"[GrowthPickup] auto-fix growth_type for {nodeName}: {GrowthType} -> magic_heart");
			GrowthType = "magic_heart";
			if (string.IsNullOrEmpty(PickupLogLabel) || PickupLogLabel == "GrowthPickup")
				PickupLogLabel = "MagicHeartPickup";
		}
		else if (nodeName.Contains("CookieBag", System.StringComparison.OrdinalIgnoreCase) && GrowthType != "cookie_bag")
		{
			GD.PushWarning($"[GrowthPickup] auto-fix growth_type for {nodeName}: {GrowthType} -> cookie_bag");
			GrowthType = "cookie_bag";
			if (string.IsNullOrEmpty(PickupLogLabel) || PickupLogLabel == "GrowthPickup")
				PickupLogLabel = "CookieBagPickup";
		}
	}
}
