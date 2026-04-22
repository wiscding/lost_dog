using Godot;

/// <summary>
/// 房间管理器 - 处理房间加载和切换
/// 使用方式: 在主场景中添加此脚本，或设为 AutoLoad
/// </summary>
public partial class LevelManager : Node
{
	public static LevelManager Instance { get; private set; }
	public RoomData CurrentRoom { get; private set; }
	private Node2D _currentRoomNode;

	[ExportCategory("设置")]
	[Export]
	public RoomData InitialRoom;
	[Export]
    private Player player;

    [Export]
	private Camera2D camera;

	/// <summary>切换动画时长</summary>
	[Export]
	public float TransitionDuration = 0.5f;
	public bool IsTransitioning { get; private set; }

	/// <summary>房间切换开始事件</summary>
	[Signal]
	public delegate void RoomTransitionStartedEventHandler(Variant newRoom);

	/// <summary>房间切换完成事件</summary>
	[Signal]
	public delegate void RoomTransitionCompletedEventHandler(Variant newRoom);

	public override void _Ready()
	{
		// 设置单例
		if (Instance == null){
			Instance = this;
		}
		else{
			QueueFree();
			return;
		}

		if (InitialRoom != null)
			LoadInitialRoom(InitialRoom);
	}

	/// <summary>
	/// 加载初始房间
	/// </summary>
	public void LoadInitialRoom(RoomData roomData)
	{
		if (roomData == null)
		{
			GD.PushError("[LevelManager] 初始房间数据为空!");
			return;
		}

		GD.Print($"[LevelManager] 加载初始房间: {roomData.RoomId}");

		CurrentRoom = roomData;
		_currentRoomNode = roomData.RoomScene.Instantiate<Node2D>();
		_currentRoomNode.Name = $"Room_{roomData.RoomId}";
		_currentRoomNode.GlobalPosition = roomData.RoomPosition; // 设置房间到指定位置
		AddChild(_currentRoomNode);

		// 设置摄像机边界
		UpdateCameraBounds(InitialRoom);
	}

	/// <summary>
	/// 切换到新房间
	/// </summary>
	public async void ChangeRoom(RoomData newRoom, Vector2 playerPosition)
	{
		// 防抖检查
		if (IsTransitioning || newRoom == null || newRoom == CurrentRoom)
		{
			return;
		}

		IsTransitioning = true;
		GD.Print($"[LevelManager] 开始切换房间: {CurrentRoom?.RoomId} -> {newRoom.RoomId}");

		// 发送开始信号
		EmitSignal(SignalName.RoomTransitionStarted, newRoom);

		// 1. 锁定玩家输入
		if (player != null)
		{
			SetProcessInput(true);
		}

		// 2. 实例化新房间
		Node2D newRoomNode = newRoom.RoomScene.Instantiate<Node2D>();
		newRoomNode.Name = $"Room_{newRoom.RoomId}";
		newRoomNode.GlobalPosition = newRoom.RoomPosition; // 设置房间到指定位置
		AddChild(newRoomNode);
		GD.Print($"[LevelManager] 加载房间: {newRoom.RoomId} 到位置: {newRoom.RoomPosition}");

		// 3. 移动玩家到新位置
		if (player != null)
		{
			player.GlobalPosition = playerPosition;
			GD.Print($"[LevelManager] 玩家移动到: {playerPosition}");
		}

		// 4. 平滑移动摄像机和边界到新房间
		await TransitionCameraAndBoundsAsync(newRoom, playerPosition);


		// 6. 清理旧房间
		if (_currentRoomNode != null)
		{
			_currentRoomNode.QueueFree();
		}

		// 7. 更新状态
		_currentRoomNode = newRoomNode;
		CurrentRoom = newRoom;

		// 8. 解锁玩家输入
		if (player != null)
		{
			SetProcessInput(false);
		}

		IsTransitioning = false;
		EmitSignal(SignalName.RoomTransitionCompleted, newRoom);

		GD.Print($"[LevelManager] 房间切换完成: {newRoom.RoomId}");
	}

	/// <summary>
	/// 平滑过渡摄像机位置和边界
	/// </summary>
	private async System.Threading.Tasks.Task TransitionCameraAndBoundsAsync(RoomData roomData, Vector2 targetPosition)
	{
		if (camera == null) return;

		// 计算绝对边界
		var bounds = roomData.CameraBounds;
		var newLeft = (int)bounds.Position.X;
		var newTop = (int)bounds.Position.Y;
		var newRight = (int)(bounds.Position.X + bounds.Size.X);
		var newBottom = (int)(bounds.Position.Y + bounds.Size.Y);

		//========== 平滑效果已注释 ==========
		// 临时移除限制
		camera.LimitLeft = -1000000;
		camera.LimitRight = 1000000;
		camera.LimitTop = -1000000;
		camera.LimitBottom = 1000000;

		// // 创建补间动画 - 同时移动摄像机位置和边界
		var tween = CreateTween();
		tween.SetParallel(true);
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);

		// 平滑移动摄像机位置
		tween.TweenProperty(camera, "global_position", targetPosition, TransitionDuration);

		// 平滑调整摄像机边界
		tween.TweenProperty(camera, "limit_left", newLeft, TransitionDuration);
		tween.TweenProperty(camera, "limit_top", newTop, TransitionDuration);
		tween.TweenProperty(camera, "limit_right", newRight, TransitionDuration);
		tween.TweenProperty(camera, "limit_bottom", newBottom, TransitionDuration);

		// 等待动画完成
		await ToSignal(tween, Tween.SignalName.Finished);

		GD.Print($"[LevelManager] 摄像机直接切换到位置: {targetPosition}, 边界: L={newLeft}, T={newTop}, R={newRight}, B={newBottom}");
	}

	/// <summary>
	/// 立即更新摄像机边界 (无动画)
	/// </summary>
	private void UpdateCameraBounds(RoomData roomData)
	{
		if (camera == null) return;

		var roomPos = roomData.RoomPosition;
		var bounds = roomData.CameraBounds;

		camera.LimitLeft = (int)(roomPos.X + bounds.Position.X);
		camera.LimitTop = (int)(roomPos.Y + bounds.Position.Y);
		camera.LimitRight = (int)(roomPos.X + bounds.Position.X + bounds.Size.X);
		camera.LimitBottom = (int)(roomPos.Y + bounds.Position.Y + bounds.Size.Y);

		GD.Print($"[LevelManager] 摄像机边界已更新: Left={camera.LimitLeft}, Top={camera.LimitTop}");
	}
}
