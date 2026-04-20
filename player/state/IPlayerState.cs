internal interface IPlayerState
{
	void Enter(PlayerStateMachine sm, Player player);
	void Exit(PlayerStateMachine sm, Player player);
	void PhysicsUpdate(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput input, float dt);
}
