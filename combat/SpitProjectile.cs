using Godot;

/// <summary>
/// 吐弹怪可复用的投射物（暂不接 AI）。
/// 直接继承通用 <see cref="ProjectileBase"/>，通过 Inspector 配置直线/抛物线与命中行为。
/// </summary>
public partial class SpitProjectile : ProjectileBase
{
	/// <summary>怪物 AI：朝方向发射直线弹。</summary>
	public void LaunchStraightFromAI(Node2D source, Vector2 direction, int damageHalfHearts)
	{
		LaunchStraight(source, direction, damageHalfHearts);
	}

	/// <summary>怪物 AI：按目标点与飞行时长发射抛物线弹。</summary>
	public bool LaunchParabolicFromAI(Node2D source, Vector2 targetPosition, float flightTime, Vector2 gravityVector, int damageHalfHearts)
	{
		return LaunchParabolicToTarget(source, targetPosition, flightTime, gravityVector, damageHalfHearts);
	}

	/// <summary>怪物 AI：先算方向再发射直线弹。</summary>
	public void LaunchStraightToTarget(Node2D source, Vector2 targetPosition, int damageHalfHearts)
	{
		var dir = (targetPosition - GlobalPosition).Normalized();
		if (dir.LengthSquared() < 0.0001f)
			dir = Vector2.Right;
		LaunchStraight(source, dir, damageHalfHearts);
	}

	public void Initialize(Node2D source, Vector2 direction, int damageHalfHearts)
	{
		LaunchStraight(source, direction, damageHalfHearts);
	}
}
