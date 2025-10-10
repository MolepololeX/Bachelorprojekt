using Godot;

namespace Game.Camera
{
	public partial class CameraRig : Node3D
	{
		[Export]
		public float rotationSpeed;

		public float currentRotationGoal;

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

			RotationDegrees = new Vector3(
				RotationDegrees.X,
				Mathf.Lerp(RotationDegrees.Y, currentRotationGoal, rotationSpeed * (float)delta),
				RotationDegrees.Z
				);
		}
	}
}