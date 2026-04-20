using Godot;
using System;

[GlobalClass]
public partial class CookieData : AbilityData
{
	/// 饼干特有配置：回血数值
    [Export] public int HealAmount { get; set; } = 2;
    
    // 饼干特有配置：是否在休息点（存档点）自动补满
    [Export] public bool RefillAtRestPoint { get; set; } = true;
    
    // 构造函数：设置默认值（可选，但推荐）
    public CookieData()
    {
        DisplayName = "神奇的饼干";
        MaxUses = 3;        
        CoolDown = 0f;      
        Description = "回复少量生命值，休息时自动补充";
    }
}
