using Game.Camera;
using Game.InventoryStuff;
using Game.Item;
using Game.Managers;
using Godot;
using System;
using System.ComponentModel;

namespace Game.PlayerStuff
{
	public partial class Player : CharacterBody3D
	{
		[Export] public AnimationTree AnimTree;
		[Export] public float Speed { get; set; } = 14;
		[Export] public int FallAcceleration { get; set; } = 75;
		[Export] public float JumpForce { get; set; } = 500;
		[Export] public float DragMult { get; set; } = .8f;
		[Export] public float LookAtInterpolation { get; set; } = .8f;


		private Vector3 _lastDir = Vector3.Forward;
		private Vector3 _targetVelocity = Vector3.Zero;
		private bool _canJump = false;
		private Node3D camRig;

		public Inventory Inventory = new Inventory();

		public override void _Ready()
		{
			camRig = GameManager.Instance.CameraRig;
		}

		public override void _PhysicsProcess(double delta)
		{
			var direction = Vector3.Zero;

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
				direction.Z += Speed * (float)delta;
			}
			if (Input.IsActionPressed("move_up"))
			{
				direction.Z -= Speed * (float)delta;
			}

			direction = direction.Rotated(Vector3.Up, camRig.Rotation.Y);

			if (direction != Vector3.Zero)
			{
				direction = direction.Normalized();
				Vector3 newDir = direction.Lerp(_lastDir, LookAtInterpolation);
				GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(newDir);
				_lastDir = newDir;
				GetNode<CpuParticles3D>("RunParticles").Emitting = true;
			}
			else
			{
				GetNode<CpuParticles3D>("RunParticles").Emitting = false;
			}

			_targetVelocity.X = direction.X * Speed;
			_targetVelocity.Z = direction.Z * Speed;

			if (!IsOnFloor())
			{
				_targetVelocity.Y -= FallAcceleration * (float)delta;
			}

			if (IsOnFloor() && _canJump)
			{
				AnimTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.FadeOut);
			}

			Velocity += _targetVelocity;
			Velocity += (-Velocity) * DragMult * ((float)delta);
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

		public override void _Process(double delta)
		{
			if (Inventory.Content.Count != 0)
			{
				if (Input.IsActionJustPressed("selection_left"))
				{
					Inventory.SelectionIndex = Inventory.SelectionIndex > 0 ? Inventory.SelectionIndex - 1 : Inventory.Content.Count - 1;
				}
				if (Input.IsActionJustPressed("selection_right"))
				{
					Inventory.SelectionIndex = Inventory.SelectionIndex < Inventory.Content.Count - 1 ? Inventory.SelectionIndex + 1 : 0;
				}
			}
		}
	}
}