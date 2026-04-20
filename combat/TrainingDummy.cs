using Godot;

/// <summary>
/// 测试用木桩：被近战命中时在控制台打印，便于验证攻击判定。
/// </summary>
public partial class TrainingDummy : StaticBody2D, IAttackReceiver
{
	public void ReceiveMeleeHit(Player attacker, int damageHalfHearts)
	{
		var who = attacker != null ? attacker.Name.ToString() : "?";
		GD.Print($"[TrainingDummy] 被近战命中：伤害 {damageHalfHearts} 半心（来自 {who}）。");
	}
}
