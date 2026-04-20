using Godot;

internal static class PlayerMovementUtil
{
	internal static void TickLocomotion(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput input, float dt)
	{
		var onFloor = player.IsOnFloor();
		var moveX = input.MoveX;
		var isCrouching = input.WantCrouch && onFloor;

		// ---- jump buffer consume + coyote
		if (sm.JumpBufferLeft > 0f && (onFloor || sm.CoyoteLeft > 0f))
		{
			sm.JumpBufferLeft = 0f;
			sm.CoyoteLeft = 0f;

			var v = player.Velocity;
			v.Y = -player.JumpVelocity;
			player.Velocity = v;

			onFloor = false;
			sm.ChangeState(Player.PlayerState.Jump);
		}
		else if (input.JumpJustReleased && player.Velocity.Y < 0f)
		{
			var v = player.Velocity;
			v.Y *= player.JumpCutMultiplier;
			player.Velocity = v;
		}

		// ---- gravity
		if (!onFloor)
		{
			var v = player.Velocity;
			v.Y += player.Gravity * dt;
			player.Velocity = v;
		}

		// ---- distance-based acceleration bookkeeping
		if (!onFloor || moveX == 0f)
		{
			sm.MoveDistanceAccum = 0f;
		}
		else if (sm.LastMoveDir.X != 0f && moveX != 0f && Mathf.Sign(moveX) != Mathf.Sign(sm.LastMoveDir.X))
		{
			sm.MoveDistanceAccum = 0f;
		}

		var currentSpeed = Mathf.Abs(player.Velocity.X);
		if (onFloor && moveX != 0f)
		{
			sm.MoveDistanceAccum += currentSpeed * dt;
			sm.LastMoveDir = new Vector2(moveX, 0f);
		}
		else
		{
			sm.LastMoveDir = Vector2.Zero;
		}

		var accelT = player.DistanceToMaxSpeed <= 0.001f
			? 1f
			: Mathf.Clamp(sm.MoveDistanceAccum / player.DistanceToMaxSpeed, 0f, 1f);

		var targetSpeed = Mathf.Lerp(player.BaseSpeed, player.MaxSpeed, accelT);
		if (isCrouching)
			targetSpeed *= player.CrouchSpeedMultiplier;

		// ---- horizontal move
		var desiredX = moveX == 0f ? 0f : moveX * targetSpeed;
		var newVel = player.Velocity;
		if (moveX == 0f)
			newVel.X = Mathf.MoveToward(newVel.X, 0f, player.StopFriction * dt * 100f);
		else
			newVel.X = Mathf.MoveToward(newVel.X, desiredX, player.MoveAcceleration * dt * 100f);

		player.Velocity = newVel;
		player.MoveAndSlide();

		// ---- classify state after movement
		if (!player.IsOnFloor())
		{
			sm.ChangeState(player.Velocity.Y < 0f ? Player.PlayerState.Jump : Player.PlayerState.Fall);
		}
		else
		{
			if (isCrouching)
				sm.ChangeState(Player.PlayerState.Crouch);
			else if (Mathf.Abs(player.Velocity.X) > 1f)
				sm.ChangeState(Player.PlayerState.Run);
			else
				sm.ChangeState(Player.PlayerState.Idle);
		}
	}
}
