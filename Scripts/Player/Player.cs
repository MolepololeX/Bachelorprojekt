using Game.Managers;
using Godot;
using System;
using System.ComponentModel;

namespace Game.PlayerStuff
{
	public partial class Player : CharacterBody3D
	{
		public enum ItemType
		{
			Empty,
			Copper,
			Slime
		}

		[Export]
		public AnimationTree AnimTree;

		private Label InvDebugLabel;

		[Export]
		public float Speed { get; set; } = 14;
		[Export]
		public int FallAcceleration { get; set; } = 75;
		[Export]
		public float JumpForce { get; set; } = 500;
		[Export]
		public float DragMult { get; set; } = .8f;
		[Export]
		public float LookAtInterpolation { get; set; } = .8f;

		private Vector3 _lastDir = Vector3.Forward;
		private Vector3 _targetVelocity = Vector3.Zero;
		private bool _canJump = false;

		public ItemType[] Inventory = {
		ItemType.Empty, ItemType.Empty, ItemType.Empty,
		ItemType.Empty, ItemType.Empty, ItemType.Empty,
		ItemType.Empty, ItemType.Empty, ItemType.Empty
	};
		public int InventorySelectedIndex = 0;

		public override void _Ready()
		{
			InvDebugLabel = GameManager.Instance.InventoryDebugLabel;
			GD.Print(Inventory.Length);
		}


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
			if (Input.IsActionJustPressed("selection_left"))
			{
				InventorySelectedIndex = InventorySelectedIndex > 0 ? InventorySelectedIndex - 1 : Inventory.Length - 1;
			}
			if (Input.IsActionJustPressed("selection_right"))
			{
				InventorySelectedIndex = InventorySelectedIndex < Inventory.Length - 1 ? InventorySelectedIndex + 1 : 0;
			}

			String test = "";
			for (int i = 0; i < Inventory.Length; i++)
			{
				if (i == InventorySelectedIndex)
				{
					test += "> " + Inventory[i].ToString() + "\n";
				}
				else
				{
					test += Inventory[i].ToString() + "\n";
				}
			}
			InvDebugLabel.Text = test;
		}

		public bool TryAddToInventory(ItemType item)
		{
			for (int i = 0; i < Inventory.Length; i++)
			{
				if (Inventory[i] == ItemType.Empty)
				{
					Inventory[i] = item;
					return true;
				}
			}
			return false;
		}
	}
}