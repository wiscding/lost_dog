using Godot;

/// <summary>
/// 通用能力拾取物：
/// - 飞盘/套索：直接放在关卡里，玩家触碰即解锁
/// - Cookie：可由 Boss 掉落后再触碰解锁
/// </summary>
public partial class AbilityPickup : Area2D
{
	[Export(PropertyHint.Enum, "cookie,boomerang,hook")]
	public string AbilityId { get; set; } = "boomerang";

	[Export] public bool ConsumeOnPickup { get; set; } = true;
	[Export] public string PickupLogLabel { get; set; } = "AbilityPickup";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

		var unlocked = player.UnlockAbility(AbilityId);
		GD.Print($"[{PickupLogLabel}:{Name}] pickup ability={AbilityId}, unlocked={unlocked}");
		if (unlocked && ConsumeOnPickup)
			CallDeferred(Node.MethodName.QueueFree);
	}
}
