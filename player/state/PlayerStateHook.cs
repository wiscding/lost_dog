using Godot;

internal sealed class PlayerStateHook : IPlayerState
{
	public void Enter(PlayerStateMachine sm, Player player)
	{
	}

	public void Exit(PlayerStateMachine sm, Player player)
	{
	}

	public void PhysicsUpdate(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput input, float dt)
	{
		if (player.HookDriver == null)
		{
			sm.ChangeState(player.IsOnFloor() ? Player.PlayerState.Idle : Player.PlayerState.Fall);
			return;
		}

		player.HookDriver.PhysicsTickHook(sm, player, input, dt);
	}
}
