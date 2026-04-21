using Godot;

/// <summary>
/// Boss 死亡掉落能力拾取物（默认用于 Cookie）。
/// 用法：
/// 1) 挂在 Boss 或关卡节点
/// 2) 指定 BossNode（留空则用自身）
/// 3) 指定 PickupScene（应是挂了 AbilityPickup 的场景）
/// 4) Boss 触发 Died 信号时自动在掉落点生成拾取物
/// </summary>
public partial class BossAbilityDropper : Node2D
{
	private const string DefaultBossPath = "../TrainingDummy";
	private const string DefaultDropPointPath = "../CookieDropPoint";
	private const string DefaultCookiePickupScenePath = "res://cookie_pickup.tscn";

	[Export] public NodePath BossNodePath { get; set; }
	[Export] public PackedScene PickupScene { get; set; }
	[Export] public NodePath DropPointPath { get; set; }
	[Export] public string BossDeathSignalName { get; set; } = "Died";
	[Export(PropertyHint.Enum, "cookie,boomerang,hook")]
	public string DropAbilityId { get; set; } = "cookie";
	[Export] public bool DropOnlyOnce { get; set; } = true;

	private bool _dropped;
	private bool _dropTriggered;
	private Node _bossRef;
	private Node2D _dropPointRef;

	public override void _Ready()
	{
		EnsureDefaults();
		_bossRef = ResolveBossNode();
		_dropPointRef = ResolveDropPoint();
		var boss = _bossRef;
		if (boss == null || string.IsNullOrEmpty(BossDeathSignalName))
		{
			GD.PushWarning("[BossAbilityDropper] boss resolve failed or signal empty.");
			return;
		}

		var signal = new StringName(BossDeathSignalName);
		if (!boss.HasSignal(signal))
		{
			GD.PushWarning($"[BossAbilityDropper] Boss node `{boss.Name}` has no signal `{BossDeathSignalName}`.");
			return;
		}

		var err = boss.Connect(signal, Callable.From(OnBossDied));
		GD.Print($"[BossAbilityDropper] bind boss={boss.Name}, signal={BossDeathSignalName}, err={err}, dropAbility={DropAbilityId}");
		if (boss.HasSignal(Node.SignalName.TreeExited))
		{
			boss.Connect(Node.SignalName.TreeExited, Callable.From(OnBossExitedFallback));
		}
	}

	public override void _Process(double delta)
	{
		// 兜底：若 Boss 被删除但没发信号，仍掉落一次。
		if (_dropped || _dropTriggered || !DropOnlyOnce)
			return;
		if (_bossRef == null || !GodotObject.IsInstanceValid(_bossRef))
		{
			GD.Print("[BossAbilityDropper] boss missing, fallback drop.");
			OnBossDied();
		}
	}

	private void OnBossDied()
	{
		if ((_dropped || _dropTriggered) && DropOnlyOnce)
			return;
		_dropTriggered = true;
		GD.Print("[BossAbilityDropper] OnBossDied received.");
		CallDeferred(nameof(SpawnDropDeferred));
	}

	private void SpawnDropDeferred()
	{
		if (_dropped && DropOnlyOnce)
			return;
		if (PickupScene == null)
		{
			GD.PushWarning("[BossAbilityDropper] PickupScene is null, skip drop.");
			return;
		}

		var pickupNode = PickupScene.Instantiate<Node2D>();
		if (pickupNode == null)
			return;
		if (pickupNode is AbilityPickup abilityPickup)
			abilityPickup.AbilityId = DropAbilityId;

		var parent = GetTree()?.CurrentScene ?? GetParent();
		parent?.AddChild(pickupNode);
		pickupNode.GlobalPosition = _dropPointRef?.GlobalPosition ?? GlobalPosition;

		_dropped = true;
		GD.Print($"[BossAbilityDropper] Drop spawned at {pickupNode.GlobalPosition}, ability={DropAbilityId}");
	}

	private void OnBossExitedFallback()
	{
		if (_dropped || _dropTriggered)
			return;
		GD.Print("[BossAbilityDropper] boss tree_exited fallback.");
		OnBossDied();
	}

	private Node ResolveBossNode()
	{
		if (BossNodePath == null || BossNodePath.IsEmpty)
			return null;
		return GetNodeOrNull(BossNodePath);
	}

	private Node2D ResolveDropPoint()
	{
		if (DropPointPath == null || DropPointPath.IsEmpty)
			return null;
		return GetNodeOrNull<Node2D>(DropPointPath);
	}

	private void EnsureDefaults()
	{
		if (BossNodePath == null || BossNodePath.IsEmpty)
			BossNodePath = new NodePath(DefaultBossPath);

		if (DropPointPath == null || DropPointPath.IsEmpty)
			DropPointPath = new NodePath(DefaultDropPointPath);

		if (PickupScene == null)
			PickupScene = ResourceLoader.Load<PackedScene>(DefaultCookiePickupScenePath);
	}
}
