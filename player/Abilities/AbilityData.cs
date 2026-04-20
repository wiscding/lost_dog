using Godot;
using System;

[GlobalClass]
public partial class AbilityData : Resource
{
	//技能显示名称 ui用
	[Export] public string DisplayName { get; set; } = "未命名技能";
	//技能图标
	[Export] public Texture2D Icon { get; set; }
	//冷却
	[Export(PropertyHint.Range, "0,60,0.1")]
	public float CoolDown {get;set;} = 0f;
	//最大使用次数
	[Export] public int MaxUses {get;set;} = -1;
	//描述文本
	[Export(PropertyHint.MultilineText)]
	public string Description {get;set;} = "";
	//音效
	[Export] public AudioStream UseSound {get;set;}
	//特效
	[Export] public PackedScene EffectScene {get;set;}
}
