using Game.Managers;
using Godot;

namespace Game.Camera
{
	public partial class CameraRig : Node3D
	{
		[Export] private float _rotationSpeed;
		[Export] private bool _useSmoothCam = true;
		[Export] private float _lerpSpeed;
		[Export] private float _lookOffsetMultiplier;
		[Export] private float _zoomIncrement = 2.0f;
		[Export] private float _maxZoom = 2.0f;
		[Export] private float _minZoom = 20.0f;


		private float currentRotationGoal;
		private CharacterBody3D player;
		private Node3D playerPivot;
		private Camera3D cam;

		public override void _Ready()
		{
			player = GameManager.Instance.Player as CharacterBody3D;
			playerPivot = player.GetNode<Node3D>("Pivot");
			cam = GameManager.Instance.Camera as Camera3D;
		}

		public override void _Process(double delta)
		{
			if (Input.IsActionJustPressed("camera_right"))
			{
				currentRotationGoal += 45.0f;
			}
			if (Input.IsActionJustPressed("camera_left"))
			{
				currentRotationGoal -= 45.0f;
			}

			if (Input.IsActionJustPressed("camera_zoom_out"))
			{
				float newSize = cam.Size + _zoomIncrement;
				if (newSize < _minZoom)
				{
					cam.Size = newSize;
				}
			}
			if (Input.IsActionJustPressed("camera_zoom_in"))
			{
				float newSize = cam.Size - _zoomIncrement;
				if (newSize > _maxZoom)
				{
					cam.Size = newSize;
				}
			}

			RotationDegrees = new Vector3(
				RotationDegrees.X,
				Mathf.Lerp(RotationDegrees.Y, currentRotationGoal, _rotationSpeed * (float)delta),
				RotationDegrees.Z
				);

			Vector3 lookOffset = -playerPivot.GlobalTransform.Basis.Z * _lookOffsetMultiplier * player.Velocity.Length();
			Vector3 targetPos = player.Position + lookOffset;

			if (_useSmoothCam)
			{
				Position = Position.Lerp(targetPos, _lerpSpeed * (float)delta);
			}
			else
			{
				Position = targetPos;
			}
		}
	}
}