using Godot;
using System.Collections.Generic;

/// <summary>
/// 飞盘投掷物框架：
/// - 先直线飞出，达到最大距离后开始回收。
/// - 回收到玩家附近后通知能力，能力再开始CD（可配置）。
/// - 使用 Area2D 以便与 Hurtbox 交互。
/// </summary>
public partial class BoomerangProjectile : Area2D
{
	private const int HurtboxLayer = 6;
	private static readonly Vector2 CatchOffset = new(0f, -32f);
	private const bool EnableDebugHitLog = true;

	private Player _owner;
	private BoomerangAbility _ability;
	private BoomrangData _cfg;
	private Vector2 _origin;
	private Vector2 _dir = Vector2.Right;
	private Vector2 _returnTarget;
	private Vector2 _returnDir = Vector2.Left;
	private bool _returning;
	private float _aliveLeft;
	private float _returnElapsed;
	private float _returnTimeout = 1.2f;
	private float _spinPhase;
	private Sprite2D _sprite;
	private Vector2 _spriteBaseScale = Vector2.One;
	private readonly HashSet<ulong> _damagedHurtboxIds = new();
	private readonly Dictionary<ulong, int> _collisionCounts = new();

	public void Initialize(Player owner, Vector2 direction, BoomrangData cfg, BoomerangAbility ability)
	{
		_owner = owner;
		_cfg = cfg ?? new BoomrangData();
		_ability = ability;
		_origin = GlobalPosition;
		_dir = direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
		_returnTarget = _origin;
		_returnDir = -_dir;
		_aliveLeft = Mathf.Max(0.1f, _cfg.MaxDuration);
	}

	public override void _Ready()
	{
		// 飞盘只关心 Hurtbox，保证不与墙体碰撞。
		CollisionLayer = 0;
		for (var i = 1; i <= 32; i++)
			SetCollisionMaskValue(i, i == HurtboxLayer);

		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (_sprite != null)
			_spriteBaseScale = _sprite.Scale;

		if (EnableDebugHitLog)
			GD.Print($"[BoomerangProjectile][生成] pos={GlobalPosition} dir={_dir}");

		AreaEntered += OnAreaEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_owner == null || !GodotObject.IsInstanceValid(_owner))
		{
			QueueFree();
			return;
		}

		var dt = (float)delta;
		_aliveLeft -= dt;
		if (_aliveLeft <= 0f)
		{
			NotifyReturnedAndFree();
			return;
		}

		var step = (_returning ? _cfg.ReturnSpeed : _cfg.FlySpeed) * dt;
		if (_returning)
		{
			_returnElapsed += dt;
			GlobalPosition += _returnDir * step;
		}
		else
		{
			GlobalPosition += _dir * step;
		}
		UpdateSideSpin(dt);

		if (!_returning)
		{
			var traveled = GlobalPosition.DistanceTo(_origin);
			if (traveled >= _cfg.MaxDistance)
			{
				_returning = true;
				_returnElapsed = 0f;
				// 锁定回程目标点，后续不再追踪玩家当前位置，保证回程轨迹为固定直线。
				_returnTarget = _owner.GlobalPosition + CatchOffset;
				var toTarget = _returnTarget - GlobalPosition;
				_returnDir = toTarget.LengthSquared() > 0.0001f ? toTarget.Normalized() : -_dir;
				// 直线回程的理论时长 + 冗余，保证一定回收。
				var expected = GlobalPosition.DistanceTo(_returnTarget) / Mathf.Max(1f, _cfg.ReturnSpeed);
				_returnTimeout = Mathf.Max(0.25f, expected * 2f);
			}
		}
		else
		{
			var distanceToTarget = GlobalPosition.DistanceTo(_returnTarget);
			if (distanceToTarget <= _cfg.CatchRadius || _returnElapsed >= _returnTimeout)
			{
				NotifyReturnedAndFree();
				return;
			}
		}
	}

	private void UpdateSideSpin(float dt)
	{
		if (_sprite == null)
			return;

		_spinPhase += Mathf.DegToRad(_cfg.SpinSpeedDeg) * dt;
		// 模拟侧面翻转：厚度在可见与极薄之间循环变化。
		var thicknessFactor = Mathf.Max(0.12f, Mathf.Abs(Mathf.Sin(_spinPhase)));
		_sprite.Scale = new Vector2(_spriteBaseScale.X, _spriteBaseScale.Y * thicknessFactor);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is not Hurtbox hurt || _owner == null || _ability == null)
			return;

		if (_owner.IsAncestorOf(hurt))
			return;

		var id = hurt.GetInstanceId();
		_collisionCounts.TryGetValue(id, out var hitCount);
		hitCount++;
		_collisionCounts[id] = hitCount;

		if (EnableDebugHitLog)
		{
			var phase = _returning ? "回程" : "去程";
			GD.Print($"[BoomerangProjectile][碰撞] 阶段={phase} 目标={hurt.Name} 次数={hitCount}");
		}

		// 往返可发生两次碰撞，但同一目标在一次投掷内只结算一次伤害。
		if (!_damagedHurtboxIds.Add(id))
			return;

		var damage = _ability.GetDamageHalfHearts();
		hurt.NotifyMeleeHit(_owner, damage);
		if (EnableDebugHitLog)
		{
			GD.Print($"[BoomerangProjectile][伤害] 目标={hurt.Name} 伤害(半心)={damage}");
		}
	}

	private void NotifyReturnedAndFree()
	{
		_ability?.NotifyProjectileReturned();
		QueueFree();
	}
}
