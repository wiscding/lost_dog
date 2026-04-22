using Godot;
using System;

/// <summary>钩锁（套索）数值配置，继承通用 <see cref="AbilityData"/>。</summary>
[GlobalClass]
public partial class HookData : AbilityData
{
	/// <summary>射线起点相对玩家 <see cref="CharacterBody2D.GlobalPosition"/> 的 Y 偏移（负值略向上）。</summary>
	[Export(PropertyHint.Range, "-80,0,1")]
	public float RayOriginYOffset { get; set; } = -22f;

	/// <summary>五向射线最大搜索距离（像素）。</summary>
	[Export(PropertyHint.Range, "32,600,1")]
	public float MaxSearchRange { get; set; } = 220f;

	/// <summary>① 出绳阶段：绳头沿命中方向延伸速度（像素/秒）。</summary>
	[Export(PropertyHint.Range, "200,4000,10")]
	public float RopeTipSpeed { get; set; } = 1400f;

	/// <summary>① 出绳最长时间（秒），超时仍进入钩住以保底。</summary>
	[Export(PropertyHint.Range, "0.05,0.8,0.01")]
	public float MaxRopeOutTime { get; set; } = 0.35f;

	/// <summary>② 钩住后停顿时间（秒），角色速度清零。</summary>
	[Export(PropertyHint.Range, "0,0.2,0.005")]
	public float LatchHoldSeconds { get; set; } = 0.05f;

	/// <summary>③ 拉向锚点的直线速度（像素/秒）。</summary>
	[Export(PropertyHint.Range, "200,3000,10")]
	public float PullSpeed { get; set; } = 900f;

	/// <summary>判定到达锚点的距离阈值（像素）。</summary>
	[Export(PropertyHint.Range, "2,48,1")]
	public float ArriveDistance { get; set; } = 10f;

	/// <summary>④ 飞过锚点后沿拉线方向的飞出速度（像素/秒）。</summary>
	[Export(PropertyHint.Range, "100,2000,10")]
	public float ExitSpeed { get; set; } = 420f;

	/// <summary>④ 飞出锁定时长（秒）：期间保持“蜘蛛侠式”飞出，不受玩家输入影响。</summary>
	[Export(PropertyHint.Range, "0,0.8,0.01")]
	public float FlyOutLockSeconds { get; set; } = 0.22f;

	/// <summary>④ 飞出阶段重力倍率：1=正常重力，>1更快下坠形成更陡弧线。</summary>
	[Export(PropertyHint.Range, "0,3,0.05")]
	public float FlyOutGravityScale { get; set; } = 1f;

	/// <summary>射线可击中的物理层（需与场景里 <see cref="HookAnchor"/> 的 collision_layer 一致）。</summary>
	[Export(PropertyHint.Layers2DPhysics)]
	public uint AnchorCollisionMask { get; set; } = 1u << 7;

	/// <summary>是否在休息点补满次数（与饼干类似）。</summary>
	[Export] public bool RefillAtRestPoint { get; set; } = true;

	public HookData()
	{
		DisplayName = "神奇的套索";
		MaxUses = -1;
		CoolDown = 0.6f;
		Description = "五向出绳，钩住交互点后高速拉近，飞过锚点后以固定速度脱出。";
	}
}
