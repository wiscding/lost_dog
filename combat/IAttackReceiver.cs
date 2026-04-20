using Godot;

/// <summary>
/// 可被玩家近战打中的对象实现此接口（敌人、木桩、可破坏物等）。
/// </summary>
public interface IAttackReceiver
{
	/// <param name="damageHalfHearts">伤害（半心单位），来自 <see cref="Player.MeleeDamageHalfHearts"/>。</param>
	void ReceiveMeleeHit(Player attacker, int damageHalfHearts);
}
