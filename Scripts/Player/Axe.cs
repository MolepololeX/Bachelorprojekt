using Game.Interactables;
using Game.Managers;
using Godot;
using System;
using System.Threading.Tasks;

namespace Game.PlayerStuff
{
	public partial class Axe : Node3D
	{
		[Export] private AnimationPlayer animPlayer;
		[Export] private CpuParticles3D emitter;
		[Export] private Area3D area3D;
		[Export] private Node3D model;
		[Export] private AudioStreamPlayer swingSound;
		[Export] private AudioStreamPlayer hitSound;

		private Camera3D cam;

		private const int RayLength = 1000;
		private bool isMouseTracking = true;
		private bool swingForward = true;
		private bool canAttack = true;
		private double currentAnimPos = 0.0;

		//temp save original pitches for testing
		private float opSwing;
		private float opHit;

		public override void _Ready()
		{
			cam = GameManager.Instance.Camera as Camera3D;
			animPlayer.AnimationStarted += OnAnimStarted;
			animPlayer.AnimationFinished += OnAnimFinish;
			area3D.BodyEntered += OnHit;

			emitter.Emitting = false;
			area3D.Monitoring = false;
			canAttack = true;

			model.Visible = false;

			opSwing = swingSound.PitchScale;
			opHit = hitSound.PitchScale;
		}

		public override void _PhysicsProcess(double delta)
		{
			if (isMouseTracking)
			{
				TrackMouse();
			}
		}

		private void TrackMouse()
		{
			var mousePos = GetViewport().GetMousePosition();
			float x = mousePos.X / GetViewport().GetWindow().Size.X;
			float y = mousePos.Y / GetViewport().GetWindow().Size.Y;
			Vector2 mpn = new Vector2(x * 2.0f - 1.0f, y * 2.0f - 1.0f);
			mpn = mpn.Rotated(-GameManager.Instance.CameraRig.Rotation.Y);
			var lookAtPos = GameManager.Instance.Player.Position + new Vector3(mpn.X, GameManager.Instance.Player.Position.Y, mpn.Y);
			LookAt(lookAtPos);

			// var origin = cam.ProjectRayOrigin(mousePos);
			// var end = origin + cam.ProjectRayNormal(mousePos) * RayLength;
			// var query = PhysicsRayQueryParameters3D.Create(origin, end);
			// query.CollideWithAreas = true;

			// var result = spaceState.IntersectRay(query);

			// if (result.ContainsKey("position"))
			// {
			// 	var resultPos = (Vector3)result["position"];
			// 	var lookAtPos = new Vector3(resultPos.X, Position.Y, resultPos.Z);
			// 	LookAt(lookAtPos);
			// }
		}

		public override void _Process(double delta)
		{
			if (canAttack && Input.IsActionPressed("Attack"))
			{
				if (swingForward)
				{
					animPlayer.Play("Swing");
				}
				else
				{
					animPlayer.PlayBackwards("Swing");
				}

				emitter.Emitting = true;
				area3D.Monitoring = true;
				swingForward = !swingForward;
				canAttack = false;
				isMouseTracking = false;
				model.Visible = true;

				swingSound.PitchScale = (float)GD.RandRange(opSwing - 0.2f, opSwing + 0.2f);
				swingSound.Play();
			}
		}

		private void OnHit(Node node)
		{
			ChoppableTree tree = node as ChoppableTree;
			if (tree != null)
			{
				animPlayer.Pause();
				area3D.SetDeferred("monitoring", false);
				currentAnimPos = animPlayer.CurrentAnimationPosition;
				tree.OnHit();

				hitSound.Play();
				hitSound.PitchScale = (float)GD.RandRange(opHit - 0.2f, opHit + 0.2f);

				HitStop();
			}
		}

		private async void HitStop()
		{
			await Task.Delay(TimeSpan.FromMilliseconds(350));
			if (!swingForward)
			{
				animPlayer.PlaySectionBackwards("Swing", 0.0, currentAnimPos);
			}
			else
			{
				animPlayer.PlaySection("Swing", currentAnimPos, 1.0);
			}
		}

		private void OnAnimFinish(StringName animName)
		{
			emitter.Emitting = false;
			area3D.Monitoring = false;
			isMouseTracking = true;
			SwingDelay();
		}

		private async void SwingDelay()
		{
			await Task.Delay(TimeSpan.FromMilliseconds(50));
			canAttack = true;
			model.Visible = false;
		}

		private void OnAnimStarted(StringName animName)
		{

		}
	}
}