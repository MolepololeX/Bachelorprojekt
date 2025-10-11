using System.Transactions;
using Godot;

namespace Game.Managers
{
	public partial class GameManager : Node
	{
		//Scene Root for Placing Temporary Stuff like VFX Nodes
		[Export] public Node TempSceneRoot;
		[Export] public Node3D Player;
		[Export] public Node3D Camera;
		[Export] public Node3D CameraRig;

		[Export] public ColorRect ZoomShaderColorRect;

		[ExportCategory("Debug Stuff")]
		[Export] public Label InventoryDebugLabel;

		public static GameManager Instance;

		public GameManager() : base()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		public override void _Ready()
		{
		}

		public override void _Process(double delta)
		{
		}
	}
}