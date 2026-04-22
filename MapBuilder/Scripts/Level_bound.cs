using System.Threading.Tasks;
using Godot;

[Tool]
public partial class Level_bound : Node2D
{
    [Export]
    public int RectWidth { 
    get => _rectWidth; 
    set
        {
            _rectWidth = value;
            QueueRedraw(); 
        } 
    }
    private int _rectWidth = 200;

    [Export(PropertyHint.Range, "10,1000,")]
    public int RectHeight { 
        get => _rectHeight; 
        set
            {
                _rectHeight = value;
                QueueRedraw(); 
            } 
    }
    private int _rectHeight = 150;


    [Export]
    public Color BorderColor { get; set; } = Colors.Red;
   
    [Export(PropertyHint.Range, "1,10,")]
    public float BorderLineWidth { get; set; } = 2.0f;

    private Camera2D _camera;

    public override async void _Ready()
    {
        // 设置z-index,确保边界始终能够显示在最上层
        ZIndex = 1000;
        if (Engine.IsEditorHint())
            return;
      //  Camera2D _camera = null ;
        while (_camera==null)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);//每帧检查一次，不暂停游戏，直到找到Camera2D
            _camera = GetViewport().GetCamera2D();
        }
        //这一段有问题：
        _camera.LimitLeft = (int)GlobalPosition.X;
        _camera.LimitTop = (int)GlobalPosition.Y;
        _camera.LimitRight = (int)GlobalPosition.X + _rectWidth;
        _camera.LimitBottom = (int)GlobalPosition.Y + _rectHeight;
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _Draw()
    {

        var rect=new Rect2(GlobalPosition, _rectWidth, _rectHeight);
        // 绘制边框
        DrawRect(rect, BorderColor,false, BorderLineWidth);
    }
}
