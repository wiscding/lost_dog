using Godot;
using System;

//回旋飞盘数据
[GlobalClass]
public partial class BoomrangData : AbilityData
{
	//飞行速度
	[Export(PropertyHint.Range,"100,800,10")]
	public float FlySpeed {get;set;} = 400f;
	//最大飞行距离，飞过后开始返回
	[Export(PropertyHint.Range,"50,500,10")]
	public float MaxDistance {get;set;} = 200f;
	//最大持续时间
	[Export(PropertyHint.Range,"1,10,0.1")]
	public float MaxDuration {get;set;} = 3f;
	//伤害
	[Export(PropertyHint.Range,"1,100,1")]
	public int Damage {get;set;} = 15;
	//碰撞体大小
	[Export(PropertyHint.Range,"5,50,1")]
	public float HitboxRadius {get;set;} = 12f;
	//速度加成
	[Export(PropertyHint.Range,"0,300,10")]
	public float SpeedBuff {get;set;} = 100f;
	//加速持续时间，小于cd
	[Export(PropertyHint.Range,"0.1,2,0.1")]
	public float BuffDuration {get;set;} = 0.8f;
	//返回速度
	[Export(PropertyHint.Range,"100,1200,10")]
	public float ReturnSpeed {get;set;} = 520f;
	//旋转速度（度/秒）
	[Export(PropertyHint.Range,"0,1440,10")]
	public float SpinSpeedDeg {get;set;} = 720f;
	//回收判定半径（到玩家多近算回收）
	[Export(PropertyHint.Range,"4,64,1")]
	public float CatchRadius {get;set;} = 14f;
	//是否在飞盘回到玩家后再开始计算CD
	[Export] public bool CooldownStartsOnReturn { get; set; } = true;


	public BoomrangData()
	{
		DisplayName="神奇的飞盘";
		MaxUses=1;
		CoolDown=2f;
		Description="八方向投出，回旋飞回。飞出时获得加速，击中敌人造成伤害";
	}
}
