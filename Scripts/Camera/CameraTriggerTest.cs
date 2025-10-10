namespace Game.Camera
{
	public partial class CameraZoneTrigger : Area3D
	{
		private Node3D cameraRig;

		public override void _Ready()
		{
			cameraRig = GameManager.Instance.CameraRig;
			BodyEntered += OnBodyEntered;
		}

		private void OnBodyEntered(Node3D body)
		{
			cameraRig.Position = Position;
		}
	}
}