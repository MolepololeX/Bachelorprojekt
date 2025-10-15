using System.Transactions;
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

		private float _currentZoom;
		private float _originalCamSize;

		public float CurrentZoom { get { return _currentZoom; } }


		private float _currentRotationGoal;
		private CharacterBody3D _player;
		private Node3D _playerPivot;
		private Camera3D _cam;

		public override void _Ready()
		{
			_player = GameManager.Instance.Player as CharacterBody3D;
			_playerPivot = _player.GetNode<Node3D>("Pivot");
			_cam = GameManager.Instance.Camera as Camera3D;
			_originalCamSize = _cam.Size;
		}

		public override void _Process(double delta)
		{
			if (Input.IsActionJustPressed("camera_right"))
			{
				_currentRotationGoal += 45.0f;
			}
			if (Input.IsActionJustPressed("camera_left"))
			{
				_currentRotationGoal -= 45.0f;
			}

			if (Input.IsActionJustPressed("camera_zoom_out"))
			{
				float newSize = _cam.Size + _zoomIncrement;
				if (newSize <= _minZoom)
				{
					_cam.Size = newSize;
                }
                else
                {
					_cam.Size = _minZoom;
                }
			}
			if (Input.IsActionJustPressed("camera_zoom_in"))
			{
				float newSize = _cam.Size - _zoomIncrement;
				if (newSize >= _maxZoom)
				{
					_cam.Size = newSize;
                }
                else
                {
					_cam.Size = _maxZoom;
                }
			}
			_currentZoom = _originalCamSize / _cam.Size;

			RotationDegrees = new Vector3(
				RotationDegrees.X,
				Mathf.Lerp(RotationDegrees.Y, _currentRotationGoal, _rotationSpeed * (float)delta),
				RotationDegrees.Z
				);

			Vector3 lookOffset = -_playerPivot.GlobalTransform.Basis.Z * _lookOffsetMultiplier * _player.Velocity.Length();
			Vector3 targetPos = _player.Position + lookOffset;

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
