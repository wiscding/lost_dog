using Godot;

/// <summary>
/// 能力契约：所有技能的统一接口。
/// 遵循接口隔离原则（ISP）：只包含技能必需的方法，绝不冗余。
/// 设计目标：最终5个技能（现在3个）都实现此接口，加第4、5个时无需修改此文件。
/// </summary>
public interface IAbility
{
    /// <summary>
    /// 能力唯一标识符（如"cookie", "boomerang", "hook"）。
    /// 用于存档、查询、解锁判定。不可重复。
    /// </summary>
    string AbilityId { get; }
    
    /// <summary>
    /// 能力数据配置（Resource）。
    /// 通过Data访问MaxUses/Cooldown等数值，实现数据驱动。
    /// </summary>
    AbilityData Data { get; set; }
    
    /// <summary>
    /// 是否已解锁（运行时状态，不存Resource里，因为解锁是进度而非配置）。
    /// </summary>
    bool IsUnlocked { get; set; }
    
    /// <summary>
    /// 当前剩余使用次数（如饼干还剩几个）。
    /// 运行时状态，需要存档。
    /// </summary>
    int CurrentUses { get; set; }
    
    /// <summary>
    /// 冷却计时器（秒）。>0表示在CD中，=0表示可用。
    /// 由Update每帧递减，或由外部管理。
    /// </summary>
    float CooldownTimer { get; set; }

    /// <summary>
    /// 每物理帧更新（由AbilityManager调用）。
    /// 处理：CD倒计时、被动效果（如饼干自动回复）、持续状态（如套索摆荡）。
    /// </summary>
    /// <param name="player">玩家主体，用于修改血量/速度/位置</param>
    /// <param name="sm">状态机，用于强制切换状态（如套索抓取时进入Swing状态）</param>
    /// <param name="dt">delta time（秒）</param>
    void Update(Player player, PlayerStateMachine sm, float dt);
    
    /// <summary>
    /// 尝试使用能力（主动触发，如按键发射）。
    /// 返回true表示"我成功使用了，消耗了资源/CD"，返回false表示"条件不满足（没解锁/CD中/没次数）"。
    /// 注意：即使返回true，也不代表效果一定生效（如飞盘可能撞墙）。
    /// </summary>
    /// <param name="player">玩家主体</param>
    /// <param name="sm">状态机</param>
    /// <returns>是否成功发起使用（消耗了成本）</returns>
    bool TryUse(Player player, PlayerStateMachine sm);
    
    /// <summary>
    /// 检查当前是否可以使用（用于UI灰显/高亮提示）。
    /// 不消耗资源，纯查询。
    /// </summary>
    bool CanUse(Player player, PlayerStateMachine sm);
    
    /// <summary>
    /// 当玩家进入新状态时，通知技能（状态钩子）。
    /// 例如：进入Jump状态时，二段跳重置计数；进入Hurt状态时，套索强制断开。
    /// </summary>
    /// <param name="player">玩家主体</param>
    /// <param name="sm">状态机</param>
    /// <param name="newState">进入的新状态</param>
    void OnStateEntered(Player player, PlayerStateMachine sm, Player.PlayerState newState);
    
    /// <summary>
    /// 当玩家退出状态时通知（清理工作）。
    /// 例如：退出套索Swing状态时，恢复重力。
    /// </summary>
    /// <param name="oldState">退出的旧状态</param>
    void OnStateExited(Player player, PlayerStateMachine sm, Player.PlayerState oldState);
}