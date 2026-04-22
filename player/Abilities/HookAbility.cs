using Godot;

internal sealed class HookAbility : IAbility, IPlayerHookDriver
{
	private enum HookPhase
	{
		None,
		RopeOut,
		Latch,
		Pull,
		FlyOut,
	}

	private static readonly Vector2[] UpFanDirections =
	{
		new(-0.8660254f, -0.5f),
		new(-0.5f, -0.8660254f),
		new(0f, -1f),
		new(0.5f, -0.8660254f),
		new(0.8660254f, -0.5f),
	};

	public string AbilityId => "hook";
	public AbilityData Data { get; set; }
	public bool IsUnlocked { get; set; }
	public int CurrentUses { get; set; }
	public float CooldownTimer { get; set; }

	private HookData Config => Data as HookData;

	private HookPhase _phase = HookPhase.None;
	private Vector2 _anchorGlobal;
	private Vector2 _castDir;
	private float _ropeStartDist;
	private float _ropeAlpha;
	private float _ropeOutElapsed;
	private float _latchElapsed;
	private float _flyOutElapsed;
	private Vector2 _pullUnit = Vector2.Up;
	private Vector2 _flyOutVelocity = Vector2.Zero;

	public HookAbility()
	{
		Data ??= new HookData();
		CurrentUses = Data.MaxUses;
	}

	public void Update(Player player, PlayerStateMachine sm, float dt)
	{
		CooldownTimer = Mathf.Max(0f, CooldownTimer - dt);
	}

	public bool CanUse(Player player, PlayerStateMachine sm)
	{
		if (!IsUnlocked || player == null || player.IsDead)
			return false;
		if (CooldownTimer > 0f)
			return false;
		if (player.HookDriver != null)
			return false;
		if (sm.CurrentState == Player.PlayerState.Hook)
			return false;
		if (sm.IsAttackLocking)
			return false;
		if (Data.MaxUses == 0)
			return false;
		if (Data.MaxUses > 0 && CurrentUses <= 0)
			return false;

		return true;
	}

	public bool TryUse(Player player, PlayerStateMachine sm)
	{
		if (!CanUse(player, sm))
			return false;

		var cfg = Config ?? new HookData();
		var space = player.GetWorld2D()?.DirectSpaceState;
		if (space == null)
			return false;

		var origin = player.GlobalPosition + new Vector2(0f, cfg.RayOriginYOffset);
		if (!TryPickAnchor(space, origin, cfg, player.GetRid(), out var anchorPos, out var castDir))
			return false;

		GD.Print($"[HookAbility][debug] Hooked anchor at {anchorPos}");

		if (Data.MaxUses > 0 && CurrentUses > 0)
			CurrentUses--;

		_anchorGlobal = anchorPos;
		_castDir = castDir;
		_ropeStartDist = Mathf.Max(8f, origin.DistanceTo(_anchorGlobal));
		_ropeAlpha = 0f;
		_ropeOutElapsed = 0f;
		_latchElapsed = 0f;
		_flyOutElapsed = 0f;
		_flyOutVelocity = Vector2.Zero;
		_pullUnit = (_anchorGlobal - origin).Normalized();
		if (_pullUnit.LengthSquared() < 0.0001f)
			_pullUnit = _castDir;

		_phase = HookPhase.RopeOut;
		player.HookDriver = this;
		sm.ChangeState(Player.PlayerState.Hook);
		return true;
	}

	public void OnStateEntered(Player player, PlayerStateMachine sm, Player.PlayerState newState)
	{
		if (newState != Player.PlayerState.Hook && player.HookDriver == this)
			AbortHookExternal(player);
	}

	public void OnStateExited(Player player, PlayerStateMachine sm, Player.PlayerState oldState)
	{
	}

	public void PhysicsTickHook(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput rawInput, float dt)
	{
		var cfg = Config ?? new HookData();

		switch (_phase)
		{
			case HookPhase.RopeOut:
			{
				// RopeOut 阶段保持 Hook 态，不走通用状态分类，避免被提前切回 Run/Fall。
				var v = player.Velocity;
				if (!player.IsOnFloor())
					v.Y += player.Gravity * dt;
				v.X = Mathf.MoveToward(v.X, 0f, player.StopFriction * dt * 100f);
				player.Velocity = v;
				player.MoveAndSlide();
				if (player.IsOnCeiling())
				{
					ForceDropFromCeiling(player, sm);
					break;
				}

				_ropeOutElapsed += dt;
				_ropeAlpha += dt * cfg.RopeTipSpeed / _ropeStartDist;

				if (_ropeAlpha >= 1f - 0.02f || _ropeOutElapsed >= cfg.MaxRopeOutTime)
					BeginLatch(player, cfg);
				break;
			}
			case HookPhase.Latch:
			{
				player.Velocity = Vector2.Zero;
				player.MoveAndSlide();
				if (player.IsOnCeiling())
				{
					ForceDropFromCeiling(player, sm);
					break;
				}

				_latchElapsed += dt;
				if (_latchElapsed >= cfg.LatchHoldSeconds)
					_phase = HookPhase.Pull;
				break;
			}
			case HookPhase.Pull:
			{
				var to = _anchorGlobal - player.GlobalPosition;
				var distSq = to.LengthSquared();
				var arriveSq = cfg.ArriveDistance * cfg.ArriveDistance;
				if (distSq <= arriveSq)
				{
					ReleaseAlongPull(player, sm, cfg);
					break;
				}

				_pullUnit = to.Normalized();
				player.Velocity = _pullUnit * cfg.PullSpeed;
				player.MoveAndSlide();
				if (player.IsOnCeiling())
				{
					ForceDropFromCeiling(player, sm);
					break;
				}
				break;
			}
			case HookPhase.FlyOut:
			{
				_flyOutElapsed += dt;
				_flyOutVelocity.Y += player.Gravity * Mathf.Max(0f, cfg.FlyOutGravityScale) * dt;
				player.Velocity = _flyOutVelocity;
				player.MoveAndSlide();
				if (player.IsOnCeiling())
				{
					ForceDropFromCeiling(player, sm);
					break;
				}

				if (_flyOutElapsed >= Mathf.Max(0f, cfg.FlyOutLockSeconds))
				{
					_phase = HookPhase.None;
					player.HookDriver = null;
					sm.ChangeState(player.IsOnFloor() ? Player.PlayerState.Idle : Player.PlayerState.Fall);
				}
				break;
			}
			default:
				AbortHookExternal(player);
				if (sm.CurrentState == Player.PlayerState.Hook)
					sm.ChangeState(player.IsOnFloor() ? Player.PlayerState.Idle : Player.PlayerState.Fall);
				break;
		}
	}

	private static PlayerStateMachine.PlayerInput BlockNonMovement(PlayerStateMachine.PlayerInput input)
	{
		return new PlayerStateMachine.PlayerInput(
			attackJustPressed: false,
			wantCrouch: false,
			crouchJustPressed: false,
			jumpJustPressed: false,
			jumpJustReleased: false,
			inputX: input.InputX,
			moveX: input.MoveX);
	}

	private void BeginLatch(Player player, HookData cfg)
	{
		_phase = HookPhase.Latch;
		player.Velocity = Vector2.Zero;
		_latchElapsed = 0f;
	}

	private void ReleaseAlongPull(Player player, PlayerStateMachine sm, HookData cfg)
	{
		_flyOutVelocity = _pullUnit * cfg.ExitSpeed;
		player.Velocity = _flyOutVelocity;
		_flyOutElapsed = 0f;
		_phase = HookPhase.FlyOut;
		CooldownTimer = Mathf.Max(0f, cfg.CoolDown);
	}

	private void AbortHookExternal(Player player)
	{
		_phase = HookPhase.None;
		_flyOutElapsed = 0f;
		_flyOutVelocity = Vector2.Zero;
		if (player.HookDriver == this)
			player.HookDriver = null;
	}

	private void ForceDropFromCeiling(Player player, PlayerStateMachine sm)
	{
		AbortHookExternal(player);
		var v = player.Velocity;
		v.Y = Mathf.Max(140f, v.Y);
		player.Velocity = v;
		if (sm.CurrentState == Player.PlayerState.Hook)
			sm.ChangeState(Player.PlayerState.Fall);
	}

	private static bool TryPickAnchor(
		PhysicsDirectSpaceState2D space,
		Vector2 origin,
		HookData cfg,
		Rid playerRid,
		out Vector2 anchorGlobal,
		out Vector2 castDir)
	{
		anchorGlobal = default;
		castDir = default;
		var bestDistSq = float.MaxValue;
		var found = false;

		var exclude = new Godot.Collections.Array<Rid> { playerRid };

		foreach (var dir in UpFanDirections)
		{
			var d = dir;
			if (d.LengthSquared() < 0.0001f)
				continue;
			d = d.Normalized();

			var query = PhysicsRayQueryParameters2D.Create(origin, origin + d * cfg.MaxSearchRange);
			query.CollisionMask = cfg.AnchorCollisionMask;
			query.CollideWithAreas = true;
			query.CollideWithBodies = false;
			query.HitFromInside = true;
			query.Exclude = exclude;

			var hit = space.IntersectRay(query);
			if (hit == null || hit.Count == 0)
				continue;

			if (!hit.ContainsKey("collider"))
				continue;

			var node = hit["collider"].AsGodotObject() as Node;
			if (!IsUnderHookAnchorGroup(node))
				continue;

			var pos = hit.ContainsKey("position")
				? ((Vector2)hit["position"])
				: origin;

			var dsq = origin.DistanceSquaredTo(pos);
			if (dsq >= bestDistSq)
				continue;

			bestDistSq = dsq;
			anchorGlobal = pos;
			castDir = d;
			found = true;
		}

		return found;
	}

	private static bool IsUnderHookAnchorGroup(Node node)
	{
		for (var n = node; n != null; n = n.GetParent())
		{
			if (n.IsInGroup(HookAnchor.GroupName))
				return true;
		}

		return false;
	}
}
