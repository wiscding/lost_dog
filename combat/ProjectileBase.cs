using Godot;
using System.Collections.Generic;

public enum ProjectileTrajectoryMode
{
	Straight = 0,
	Parabolic = 1,
}

public enum ProjectileHitBehavior
{
	Disappear = 0,
	Pierce = 1,
	KeepTrajectory = 2,
}

/// <summary>
/// 通用投射物基类：
/// - 直线/抛物线弹道参数化
/// - 碰撞后行为参数化（消失/穿透/路线不变）
/// - 统一 Hurtbox 伤害判定
/// </summary>
public abstract partial class ProjectileBase : Area2D
{
	[Signal] public delegate void ProjectileHitEventHandler(Hurtbox hurtbox, int damageHalfHearts);
	[Signal] public delegate void ProjectileExpiredEventHandler();

	[ExportGroup("Projectile Motion")]
	[Export] public ProjectileTrajectoryMode TrajectoryMode { get; set; } = ProjectileTrajectoryMode.Straight;
	[Export(PropertyHint.Range, "0,3000,1")] public float Speed { get; set; } = 360f;
	[Export] public Vector2 GravityVector { get; set; } = new(0f, 1200f);
	[Export(PropertyHint.Range, "0.05,20,0.01")] public float Lifetime { get; set; } = 3f;

	[ExportGroup("Projectile Hit")]
	[Export] public ProjectileHitBehavior HitBehavior { get; set; } = ProjectileHitBehavior.Disappear;
	[Export(PropertyHint.Range, "1,100,1")] public int DamageHalfHearts { get; set; } = 1;
	[Export] public bool AllowRepeatedDamageOnSameTarget { get; set; } = false;
	[Export(PropertyHint.Range, "1,32,1")] public int HurtboxLayer { get; set; } = 6;

	protected Node2D DamageSource { get; private set; }
	protected Vector2 MoveDirection { get; private set; } = Vector2.Right;
	protected Vector2 Velocity { get; private set; } = Vector2.Right;

	private float _lifeLeft;
	private bool _keepTrajectoryHitProcessed;
	private readonly HashSet<ulong> _damagedHurtboxIds = new();

	public virtual void InitializeProjectile(Node2D source, Vector2 direction)
	{
		DamageSource = source;
		SetMoveDirection(direction);
		_lifeLeft = Mathf.Max(0.01f, Lifetime);
	}

	/// <summary>供 AI 使用：按方向直线发射。</summary>
	public void LaunchStraight(Node2D source, Vector2 direction, int damageHalfHearts)
	{
		TrajectoryMode = ProjectileTrajectoryMode.Straight;
		DamageHalfHearts = Mathf.Max(1, damageHalfHearts);
		InitializeProjectile(source, direction);
	}

	/// <summary>供 AI 使用：按初速度发射抛物线。</summary>
	public void LaunchParabolic(Node2D source, Vector2 initialVelocity, Vector2 gravityVector, int damageHalfHearts)
	{
		DamageSource = source;
		TrajectoryMode = ProjectileTrajectoryMode.Parabolic;
		GravityVector = gravityVector;
		DamageHalfHearts = Mathf.Max(1, damageHalfHearts);
		SetVelocity(initialVelocity);
		_lifeLeft = Mathf.Max(0.01f, Lifetime);
	}

	/// <summary>
	/// 供 AI 使用：已知目标点与飞行时长，自动解抛物线初速度。
	/// 返回 false 表示参数非法（飞行时长太小）。
	/// </summary>
	public bool LaunchParabolicToTarget(Node2D source, Vector2 targetPosition, float flightTime, Vector2 gravityVector, int damageHalfHearts)
	{
		if (flightTime <= 0.01f)
			return false;

		var displacement = targetPosition - GlobalPosition;
		var initialVelocity = (displacement - 0.5f * gravityVector * flightTime * flightTime) / flightTime;
		LaunchParabolic(source, initialVelocity, gravityVector, damageHalfHearts);
		return true;
	}

	protected void SetMoveDirection(Vector2 direction)
	{
		MoveDirection = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
		Velocity = MoveDirection * Speed;
	}

	protected void SetVelocity(Vector2 velocity)
	{
		Velocity = velocity;
		if (velocity.LengthSquared() > 0.0001f)
			MoveDirection = velocity.Normalized();
	}

	public override void _Ready()
	{
		CollisionLayer = 0;
		for (var i = 1; i <= 32; i++)
			SetCollisionMaskValue(i, i == HurtboxLayer);

		if (_lifeLeft <= 0f)
			_lifeLeft = Mathf.Max(0.01f, Lifetime);

		AreaEntered += OnAreaEnteredInternal;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		_lifeLeft -= dt;
		if (_lifeLeft <= 0f)
		{
			OnLifetimeEnded();
			return;
		}

		TickMovement(dt);
	}

	protected virtual void TickMovement(float dt)
	{
		if (TrajectoryMode == ProjectileTrajectoryMode.Parabolic)
		{
			Velocity += GravityVector * dt;
			GlobalPosition += Velocity * dt;
			return;
		}

		GlobalPosition += MoveDirection * Speed * dt;
	}

	protected virtual bool CanDamageHurtbox(Hurtbox hurtbox)
	{
		return true;
	}

	protected virtual void AfterSuccessfulHit(Hurtbox hurtbox)
	{
	}

	protected virtual int ResolveDamageHalfHearts(Hurtbox hurtbox)
	{
		return Mathf.Max(1, DamageHalfHearts);
	}

	protected virtual void OnLifetimeEnded()
	{
		EmitSignal(SignalName.ProjectileExpired);
		CallDeferred(Node.MethodName.QueueFree);
	}

	protected virtual void OnKeepTrajectoryFirstHit(Hurtbox hurtbox)
	{
	}

	private void OnAreaEnteredInternal(Area2D area)
	{
		if (area is not Hurtbox hurtbox)
			return;
		if (!CanDamageHurtbox(hurtbox))
			return;

		var targetId = hurtbox.GetInstanceId();
		if (!AllowRepeatedDamageOnSameTarget && !_damagedHurtboxIds.Add(targetId))
			return;

		if (HitBehavior == ProjectileHitBehavior.KeepTrajectory && _keepTrajectoryHitProcessed)
			return;

		var attacker = DamageSource as Player;
		var damage = ResolveDamageHalfHearts(hurtbox);
		hurtbox.NotifyMeleeHit(attacker, damage);
		EmitSignal(SignalName.ProjectileHit, hurtbox, damage);
		AfterSuccessfulHit(hurtbox);

		switch (HitBehavior)
		{
			case ProjectileHitBehavior.Disappear:
				CallDeferred(Node.MethodName.QueueFree);
				break;
			case ProjectileHitBehavior.KeepTrajectory:
				if (!_keepTrajectoryHitProcessed)
				{
					_keepTrajectoryHitProcessed = true;
					OnKeepTrajectoryFirstHit(hurtbox);
				}
				break;
			case ProjectileHitBehavior.Pierce:
			default:
				break;
		}
	}
}
