internal sealed class PlayerStateFall : IPlayerState
{
	public void Enter(PlayerStateMachine sm, Player player) { }
	public void Exit(PlayerStateMachine sm, Player player) { }

	public void PhysicsUpdate(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput input, float dt)
	{
		PlayerMovementUtil.TickLocomotion(sm, player, input, dt);
	}
}
