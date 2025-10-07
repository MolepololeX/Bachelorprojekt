using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export]
	public AnimationTree AnimTree;

	// How fast the player moves in meters per second.
	[Export]
	public float Speed { get; set; } = 14;
	// The downward acceleration when in the air, in meters per second squared.
	[Export]
	public int FallAcceleration { get; set; } = 75;

	[Export]
	public float JumpForce { get; set; } = 500;

	[Export]
	public float DragMult { get; set; } = .8f;

	[Export]
	public float LookAtInterpolation { get; set; } = .8f;

	private Vector3 lastDir = Vector3.Forward;

	private Vector3 _targetVelocity = Vector3.Zero;

	private bool canJump = false;

	public override void _PhysicsProcess(double delta)
	{
		// if (Input.IsActionPressed("move_jump") && canJump)
		// {
		// 	_targetVelocity.Y = JumpForce;
		// 	canJump = false;
		// 	AnimTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
		// }
		// else if (!Input.IsActionPressed("move_jump") && !canJump)
		// {
		// 	canJump = true;
		// }

		// We create a local variable to store the input direction.
		var direction = Vector3.Zero;

		// We check for each move input and update the direction accordingly.
		if (Input.IsActionPressed("move_right"))
		{
			direction.X += Speed * (float)delta;
		}
		if (Input.IsActionPressed("move_left"))
		{
			direction.X -= Speed * (float)delta;
		}
		if (Input.IsActionPressed("move_down"))
		{
			// Notice how we are working with the vector's X and Z axes.
			// In 3D, the XZ plane is the ground plane.
			direction.Z += Speed * (float)delta;
		}
		if (Input.IsActionPressed("move_up"))
		{
			direction.Z -= Speed * (float)delta;
		}

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			Vector3 newDir = direction.Lerp(lastDir, LookAtInterpolation);
			GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(newDir);
			lastDir = newDir;
			GetNode<CpuParticles3D>("RunParticles").Emitting = true;
		}
		else
		{
			GetNode<CpuParticles3D>("RunParticles").Emitting = false;
		}

		// Ground velocity
		_targetVelocity.X = direction.X * Speed;
		_targetVelocity.Z = direction.Z * Speed;

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
		}

		if (IsOnFloor() && canJump)
		{
			AnimTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.FadeOut);
		}

		// Moving the character
		Velocity += _targetVelocity;
		Velocity += ((-Velocity) * DragMult * ((float)delta));
		Vector2 velXZ = new Vector2(Velocity.X, Velocity.Z);
		if (velXZ.Length() > Speed * 3.0f)
		{
			velXZ = velXZ.Normalized() * Speed * 3.0f;
			Velocity = new Vector3(velXZ.X, Velocity.Y, velXZ.Y);
		}
		if (Velocity.Y > FallAcceleration)
		{
			Velocity = new Vector3(Velocity.X, FallAcceleration, Velocity.Z);
		}
		MoveAndSlide();

		AnimTree.Set("parameters/RunningBlend/blend_amount", Math.Clamp((velXZ.Length() / Speed) - 1.0, -1.0, 1.0));
	}
}
