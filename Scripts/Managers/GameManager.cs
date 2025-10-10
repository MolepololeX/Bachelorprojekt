using Godot;

namespace Game.Managers
{
	public partial class GameManager : Node
	{
		[Export]
		public Node3D Player;
		[Export]
		public Node3D Camera;
		[Export]
		public Node3D CameraRig;

		[Export]
		public ColorRect ZoomShaderColorRect;

		public static GameManager Instance;

		public override void _Ready()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		public override void _Process(double delta)
		{
		}
	}
}