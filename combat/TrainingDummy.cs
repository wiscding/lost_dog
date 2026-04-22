using Godot;

/// <summary>
/// 测试用木桩：被近战命中时在控制台打印，便于验证攻击判定。
/// </summary>
public partial class TrainingDummy : StaticBody2D, IAttackReceiver
{
	[Signal] public delegate void DiedEventHandler();

	[Export(PropertyHint.Range, "1,100,1")]
	public int MaxHalfHearts { get; set; } = 8;

	private int _currentHalfHearts;

	public override void _Ready()
	{
		_currentHalfHearts = Mathf.Max(1, MaxHalfHearts);
	}

	public void ReceiveMeleeHit(Player attacker, int damageHalfHearts)
	{
		var who = attacker != null ? attacker.Name.ToString() : "?";
		_currentHalfHearts = Mathf.Max(0, _currentHalfHearts - Mathf.Max(1, damageHalfHearts));
		GD.Print($"[TrainingDummy] 被近战命中：伤害 {damageHalfHearts} 半心（来自 {who}），剩余={_currentHalfHearts}。");
		if (_currentHalfHearts > 0)
			return;

		GD.Print("[TrainingDummy] Died signal emitted.");
		EmitSignal(SignalName.Died);
		CallDeferred(Node.MethodName.QueueFree);
	}
}
