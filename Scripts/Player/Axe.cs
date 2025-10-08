using Godot;
using System;

public partial class Axe : Node3D
{
	[Export]
	public AnimationPlayer animPlayer;
	[Export]
	public CpuParticles3D emitter;
	[Export]
	public RigidBody3D rb;
	[Export]
	public Camera3D cam;

	private const int RayLength = 1000;
	private bool swingForward = true;

	public override void _Ready()
	{
		animPlayer.AnimationStarted += StartEmitter;
		animPlayer.AnimationFinished += StopEmitter;
		rb.BodyEntered += OnHit;
	}

	public override void _PhysicsProcess(double delta)
	{
		var spaceState = GetWorld3D().DirectSpaceState;
		var mousePos = GetViewport().GetMousePosition();

		var origin = cam.ProjectRayOrigin(mousePos);
		var end = origin + cam.ProjectRayNormal(mousePos) * RayLength;
		var query = PhysicsRayQueryParameters3D.Create(origin, end);
		query.CollideWithAreas = true;

		var result = spaceState.IntersectRay(query);

		var resultPos = (Vector3)result["position"];
		var lookAtPos = new Vector3(resultPos.X, Position.Y, resultPos.Z);
		LookAt(lookAtPos);
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Attack"))
		{
			if (swingForward)
			{
				animPlayer.Play("Swing");
			}
			else
			{
				animPlayer.PlayBackwards("Swing");
			}
			swingForward = !swingForward;
		}
	}

	private void OnHit(Node body)
	{
		ChoppableTree tree = body as ChoppableTree;
		if (tree != null)
		{
			tree.OnHit();
		}
	}

	private void StopEmitter(StringName animName)
	{
		emitter.Emitting = false;
	}


	private void StartEmitter(StringName animName)
	{
		emitter.Emitting = true;
	}
}
