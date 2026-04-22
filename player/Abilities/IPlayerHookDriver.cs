using Godot;

/// <summary>
/// 由 <see cref="HookAbility"/> 实现；在 <see cref="PlayerStateHook"/> 中代替常规移动。
/// </summary>
internal interface IPlayerHookDriver
{
	void PhysicsTickHook(PlayerStateMachine sm, Player player, PlayerStateMachine.PlayerInput rawInput, float dt);
}
