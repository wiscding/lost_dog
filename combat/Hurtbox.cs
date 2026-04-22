using Godot;

/// <summary>
/// 受击盒：只负责「被 Hitbox 扫到」并把命中转发给实现了 <see cref="IAttackReceiver"/> 的节点（通常是父级实体）。
/// 与 <see cref="PlayerMeleeHitbox"/> 通过物理层配对（默认第 6 层 Hurtbox ↔ 第 7 层 MeleeHit）。
/// </summary>
public partial class Hurtbox : Area2D
{
	/// <summary>
	/// 可选：在检查器里把实现 <see cref="IAttackReceiver"/> 的节点拖到这里（例如木桩根节点）。
	/// 留空时自动在父节点链上查找第一个 <see cref="IAttackReceiver"/>。
	/// </summary>
	[Export] public Node DamageReceiverOverride { get; set; }

	private IAttackReceiver _receiver;

	public override void _Ready()
	{
		TryResolveReceiver();
		if (_receiver == null)
			Callable.From(TryResolveReceiver).CallDeferred();
	}

	/// <summary>由 Hitbox 调用：将本次近战命中交给接收者。</summary>
	public void NotifyMeleeHit(Player attacker, int damageHalfHearts)
	{
		if (_receiver == null)
			TryResolveReceiver();

		if (_receiver == null)
			return;

		_receiver.ReceiveMeleeHit(attacker, damageHalfHearts);
		attacker?.OnAttackHitConfirmed();
	}

	private void TryResolveReceiver()
	{
		var o = DamageReceiverOverride;
		if (GodotObject.IsInstanceValid(o) && o is IAttackReceiver r0)
		{
			_receiver = r0;
			return;
		}

		for (var p = GetParent(); p != null; p = p.GetParent())
		{
			if (p is IAttackReceiver r2)
			{
				_receiver = r2;
				return;
			}
		}

		if (_receiver != null)
			return;

		GD.PushWarning(
			$"{nameof(Hurtbox)} `{Name}`: 未找到 {nameof(IAttackReceiver)}。请在根节点挂接收脚本，或将「Damage Receiver Override」拖到该节点。");
	}
}
