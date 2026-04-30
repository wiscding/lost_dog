using Godot;
using System;

public partial class BoomerangAbility : IAbility
{
	private static readonly Vector2 SpawnOffset = new(0f, -32f);

	public string AbilityId => "boomerang";
	public AbilityData Data { get; set; }
	public bool IsUnlocked { get; set; } = false;
	public int CurrentUses { get; set; }
	public float CooldownTimer { get; set; } = 0f;

	private BoomrangData Config => Data as BoomrangData;

	private BoomerangProjectile _activeProjectile;
	private bool _pendingCooldownOnReturn;
	private float _buffLeft;
	private float _savedMaxSpeed;
	private bool _buffApplied;

	public BoomerangAbility()
	{
		Data ??= new BoomrangData();
		CurrentUses = Data.MaxUses;
	}

	public void Update(Player player, PlayerStateMachine sm, float dt)
	{
		CooldownTimer = Mathf.Max(0f, CooldownTimer - dt);

		if (_buffApplied)
		{
			_buffLeft = Mathf.Max(0f, _buffLeft - dt);
			if (_buffLeft <= 0f)
				ClearSpeedBuff(player);
		}

		// 框架阶段：投掷物已销毁时确保状态回收。
		if (_activeProjectile != null && !GodotObject.IsInstanceValid(_activeProjectile))
		{
			_activeProjectile = null;
			FinalizeReturn();
		}
	}

	public bool CanUse(Player player, PlayerStateMachine sm)
	{
		if (!IsUnlocked || player == null || player.IsDead)
			return false;
		if (_activeProjectile != null)
			return false;
		if (CooldownTimer > 0f)
			return false;
		return CurrentUses != 0;
	}

	public bool TryUse(Player player, PlayerStateMachine sm)
	{
		if (!CanUse(player, sm))
		{
			GD.Print(
				$"[BoomerangAbility][debug] TryUse denied: " +
				$"unlocked={IsUnlocked}, playerNull={player == null}, dead={player?.IsDead == true}, " +
				$"activeProjectile={_activeProjectile != null}, cooldown={CooldownTimer:0.###}, " +
				$"currentUses={CurrentUses}, maxUses={Data?.MaxUses}");
			return false;
		}

		var cfg = Config ?? new BoomrangData();
		var dir = ReadAimDirection(player);
		if (dir == Vector2.Zero)
			dir = Vector2.Right;

		if (Data?.EffectScene == null)
		{
			GD.PushWarning("[BoomerangAbility] Data.EffectScene 未配置，已跳过实际投掷。");
			StartCooldownAfterThrowOrReturn(cfg);
			return true;
		}

		var projectile = Data.EffectScene.Instantiate<BoomerangProjectile>();
		if (projectile == null)
		{
			GD.PushWarning("[BoomerangAbility] EffectScene 不是 BoomerangProjectile。");
			StartCooldownAfterThrowOrReturn(cfg);
			return true;
		}

		player.GetTree().CurrentScene?.AddChild(projectile);
		projectile.GlobalPosition = player.GlobalPosition + SpawnOffset;
		projectile.Initialize(player, dir, cfg, this);
		_activeProjectile = projectile;

		if (CurrentUses > 0)
			CurrentUses--;

		ApplySpeedBuff(player, cfg);
		StartCooldownAfterThrowOrReturn(cfg);
		return true;
	}

	public void OnStateEntered(Player player, PlayerStateMachine sm, Player.PlayerState newState)
	{
	}

	public void OnStateExited(Player player, PlayerStateMachine sm, Player.PlayerState oldState)
	{
	}

	internal void NotifyProjectileReturned()
	{
		_activeProjectile = null;
		FinalizeReturn();
	}

	internal int GetDamageHalfHearts()
	{
		var cfg = Config;
		return Mathf.Max(1, cfg?.Damage ?? 1);
	}

	private Vector2 ReadAimDirection(Player player)
	{
		var x = Input.GetAxis("left", "right");
		var y = Input.GetAxis("up", "down");
		const float deadzone = 0.2f;
		var left = x < -deadzone;
		var right = x > deadzone;
		var up = y < -deadzone;

		// 五方向：上、左、右、左上、右上（与 HookAbility 一致）
		if (up)
		{
			if (left)
				return new Vector2(-0.5f, -0.8660254f);
			if (right)
				return new Vector2(0.5f, -0.8660254f);
			return Vector2.Up;
		}

		if (left)
			return Vector2.Left;
		if (right)
			return Vector2.Right;

		// 仅按技能键时，按玩家当前朝向做水平直线发射。
		return new Vector2(Mathf.Sign(player.FacingDirectionX == 0f ? 1f : player.FacingDirectionX), 0f);
	}

	private void StartCooldownAfterThrowOrReturn(BoomrangData cfg)
	{
		if (cfg == null)
			return;

		if (cfg.CooldownStartsOnReturn)
		{
			_pendingCooldownOnReturn = true;
			return;
		}

		CooldownTimer = Mathf.Max(0f, cfg.CoolDown);
	}

	private void FinalizeReturn()
	{
		var cfg = Config;
		if (cfg == null)
			return;

		if (_pendingCooldownOnReturn)
		{
			CooldownTimer = Mathf.Max(0f, cfg.CoolDown);
			_pendingCooldownOnReturn = false;
		}

		if (Data.MaxUses > 0)
			CurrentUses = Mathf.Min(Data.MaxUses, CurrentUses + 1);
	}

	private void ApplySpeedBuff(Player player, BoomrangData cfg)
	{
		if (player == null || cfg == null || cfg.SpeedBuff <= 0f || cfg.BuffDuration <= 0f)
			return;

		if (!_buffApplied)
		{
			_savedMaxSpeed = player.MaxSpeed;
			player.MaxSpeed = _savedMaxSpeed + cfg.SpeedBuff;
			_buffApplied = true;
		}

		_buffLeft = Mathf.Max(_buffLeft, cfg.BuffDuration);
	}

	private void ClearSpeedBuff(Player player)
	{
		if (!_buffApplied || player == null)
			return;

		player.MaxSpeed = _savedMaxSpeed;
		_buffApplied = false;
		_buffLeft = 0f;
	}
}

