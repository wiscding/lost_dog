using Godot;
using System;

public partial class CookieAbility : IAbility
{
	public string AbilityId => "cookie";
	public AbilityData Data { get; set; }
	public bool IsUnlocked { get; set; } = false;
	public int CurrentUses { get; set; }
	public float CooldownTimer { get; set; } = 0f;
	// 缓存 CookieData 的强类型引用（避免每次强制转换）
	private CookieData CookieConfig => Data as CookieData;
	private float _lastUseTime = -999f;

	public CookieAbility()
	{
		Data ??= new CookieData();

		// 初始化当前次数为配置的最大值
		CurrentUses = Data.MaxUses;
	}

	public void Update(Player player, PlayerStateMachine sm, float dt)
	{
		CooldownTimer = Mathf.Max(0f, CooldownTimer - dt);
	}

	public bool CanUse(Player player, PlayerStateMachine sm)
	{
		if (!IsUnlocked || player == null)
			return false;

		// 明确允许“移动中使用”：不限制 Idle/Run/Jump/Fall 等状态。
		// 这里只校验生存、冷却和次数。
		if (player.IsDead)
			return false;

		if (CooldownTimer > 0f)
			return false;

		return CurrentUses != 0;
	}

	public bool TryUse(Player player, PlayerStateMachine sm)
	{
		if (!CanUse(player, sm))
			return false;

		var cfg = CookieConfig;
		CooldownTimer = Mathf.Max(0f, cfg?.CoolDown ?? 0f);
		_lastUseTime = Time.GetTicksMsec() / 1000f;

		if (CurrentUses > 0)
			CurrentUses--;

		// 当前 Player 未暴露“按半心加血”接口，先使用回满作为饼干效果。
		player.RefillHealth();
		return true;
	}

	public void OnStateEntered(Player player, PlayerStateMachine sm, Player.PlayerState newState)
	{
	}

	public void OnStateExited(Player player, PlayerStateMachine sm, Player.PlayerState oldState)
	{
	}
}
