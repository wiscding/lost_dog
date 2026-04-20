using Godot;
using System.Collections.Generic;

/// <summary>
/// 近战 Hitbox：只与 <see cref="Hurtbox"/> 重叠时算命中，不直接扫实体刚体。
/// 期望层级：Player → AttackPivot(Node2D) → 本 Area2D。物理层默认：第 7 层 MeleeHit，mask 含第 6 层 Hurtbox。
/// </summary>
public partial class PlayerMeleeHitbox : Area2D
{
	private Player _player;
	private readonly HashSet<ulong> _hitHurtboxIds = new();
	private float _activeLeft;

	public override void _Ready()
	{
		_player = ResolvePlayerFrom(this);
		if (_player == null)
		{
			GD.PushError($"{nameof(PlayerMeleeHitbox)}: 找不到 {nameof(Player)}，攻击判定不会生效。");
			return;
		}

		Monitoring = false;
		AreaEntered += OnAreaEntered;
		_player.Attack += OnPlayerAttack;
	}

	public override void _ExitTree()
	{
		if (_player != null)
			_player.Attack -= OnPlayerAttack;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
			return;

		var dt = (float)delta;
		if (_activeLeft <= 0f)
			return;

		_activeLeft = Mathf.Max(0f, _activeLeft - dt);
		if (_activeLeft <= 0f)
			Monitoring = false;
	}

	private void OnPlayerAttack()
	{
		if (_player == null)
			return;

		UpdateAttackFacing();
		_hitHurtboxIds.Clear();
		Monitoring = true;
		_activeLeft = _player.AttackStateTime;
		FlushOverlappingHurtboxes();
	}

	private void UpdateAttackFacing()
	{
		var pivot = GetParent() as Node2D;
		if (pivot == null)
			return;

		var fx = 0f;
		if (Mathf.Abs(_player.Velocity.X) > 8f)
			fx = Mathf.Sign(_player.Velocity.X);
		else
			fx = Mathf.Sign(Input.GetAxis("left", "right"));

		if (fx == 0f)
		{
			var ps = pivot.Scale.X;
			fx = ps != 0f ? Mathf.Sign(ps) : 1f;
		}

		pivot.Scale = new Vector2(fx, 1f);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is Hurtbox hurt)
			TryHitHurtbox(hurt);
	}

	private void FlushOverlappingHurtboxes()
	{
		foreach (var area in GetOverlappingAreas())
		{
			if (area is Hurtbox hurt)
				TryHitHurtbox(hurt);
		}
	}

	private void TryHitHurtbox(Hurtbox hurt)
	{
		if (_player == null || hurt == null)
			return;

		if (_player.IsAncestorOf(hurt))
			return;

		var id = hurt.GetInstanceId();
		if (!_hitHurtboxIds.Add(id))
			return;

		var dmg = Mathf.Max(1, _player.MeleeDamageHalfHearts);
		hurt.NotifyMeleeHit(_player, dmg);
	}

	private static Player ResolvePlayerFrom(Node self)
	{
		var o = self.GetOwner();
		if (o is Player p)
			return p;

		for (var n = self; n != null; n = n.GetParent())
		{
			if (n is Player p2)
				return p2;
		}

		return null;
	}
}
