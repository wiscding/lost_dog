using Godot;

/// <summary>
/// 日记碎片拾取：触碰后对玩家 <see cref="Player.AddDiary"/>。
/// 关卡里实例化 <c>res://diary_pickup.tscn</c>，或由 NPC 逻辑在任务进度后生成。
/// </summary>
public partial class DiaryPickup : Area2D
{
	[Export] public int Amount { get; set; } = 1;
	[Export] public bool ConsumeOnPickup { get; set; } = true;
	[Export] public string PickupLogLabel { get; set; } = "DiaryPickup";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		GD.Print($"[PickupDebug][{PickupLogLabel}:{Name}] ready diary amount={Amount}, consume={ConsumeOnPickup}");
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

		player.AddDiary(Mathf.Max(1, Amount));
		GD.Print($"[{PickupLogLabel}:{Name}] diary picked +{Mathf.Max(1, Amount)}, total={player.GetDiaryCount()}");
		if (ConsumeOnPickup)
			CallDeferred(Node.MethodName.QueueFree);
	}
}
