using Godot;

/// <summary>
/// 挂在可抓取锚点的 <see cref="Area2D"/> 上；需配置碰撞形状与 collision_layer，
/// 且层需包含在 <see cref="HookData.AnchorCollisionMask"/> 中。
/// </summary>
[GlobalClass]
public partial class HookAnchor : Area2D
{
	public const string GroupName = "hook_anchor";

	public override void _Ready()
	{
		AddToGroup(GroupName);
		if (CollisionLayer == 0)
			CollisionLayer = 1u << 7;
	}
}
