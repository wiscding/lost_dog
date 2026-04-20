using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 能力管理器：管理所有能力的生命周期。
/// 由 Player 显式调用 Update/TryUse/NotifyStateChanged。
/// </summary>
public class AbilityManager
{
	private readonly List<IAbility> _abilities = new();
	private readonly Player _player;
	private readonly PlayerStateMachine _stateMachine;

	public AbilityManager(Player player, PlayerStateMachine stateMachine)
	{
		_player = player ?? throw new ArgumentNullException(nameof(player));
		_stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
	}

	public void RegisterAbility(IAbility ability)
	{
		if (ability == null)
		{
			GD.PushWarning("[AbilityManager] 尝试注册 null 能力，已忽略");
			return;
		}

		if (_abilities.Exists(a => a.AbilityId == ability.AbilityId))
		{
			GD.PushWarning($"[AbilityManager] 能力 {ability.AbilityId} 已注册，忽略重复");
			return;
		}

		_abilities.Add(ability);
	}

	public void Update(float dt)
	{
		foreach (var ability in _abilities)
		{
			if (ability.IsUnlocked)
				ability.Update(_player, _stateMachine, dt);
		}
	}

	public bool TryUseAbility<T>() where T : class, IAbility
	{
		foreach (var ability in _abilities)
		{
			if (ability is T typedAbility && ability.IsUnlocked)
				return typedAbility.TryUse(_player, _stateMachine);
		}

		return false;
	}

	public bool TryUseAbility(string abilityId)
	{
		var ability = _abilities.Find(a => a.AbilityId == abilityId);
		if (ability?.IsUnlocked == true)
			return ability.TryUse(_player, _stateMachine);

		return false;
	}

	public void UnlockAbility(string abilityId)
	{
		var ability = _abilities.Find(a => a.AbilityId == abilityId);
		if (ability != null && !ability.IsUnlocked)
		{
			ability.IsUnlocked = true;
			ability.CurrentUses = ability.Data?.MaxUses ?? 0;
		}
	}

	public bool IsUnlocked<T>() where T : class, IAbility
	{
		foreach (var ability in _abilities)
		{
			if (ability is T && ability.IsUnlocked)
				return true;
		}

		return false;
	}

	public bool IsUnlocked(string abilityId)
	{
		var ability = _abilities.Find(a => a.AbilityId == abilityId);
		return ability?.IsUnlocked ?? false;
	}

	public void NotifyStateChanged(Player.PlayerState from, Player.PlayerState to)
	{
		foreach (var ability in _abilities)
		{
			if (!ability.IsUnlocked)
				continue;

			ability.OnStateExited(_player, _stateMachine, from);
			ability.OnStateEntered(_player, _stateMachine, to);
		}
	}

	public IAbility GetAbility(string abilityId)
	{
		return _abilities.Find(a => a.AbilityId == abilityId);
	}

	/// <summary>
	/// 在休息点/存档点补充能力资源（按各能力配置决定是否补充）。
	/// </summary>
	public void RefillAtRestPoint()
	{
		foreach (var ability in _abilities)
		{
			if (!ability.IsUnlocked)
				continue;

			if (ability is CookieAbility cookie)
			{
				var cfg = cookie.Data as CookieData;
				if (cfg?.RefillAtRestPoint == true)
					cookie.CurrentUses = cookie.Data?.MaxUses ?? cookie.CurrentUses;
			}
		}
	}

	public void Dispose()
	{
		_abilities.Clear();
	}
}
