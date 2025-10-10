using Godot;
using System;
using System.Threading.Tasks;

public partial class Axe : Node3D
{
	[Export]
	public AnimationPlayer animPlayer;
	[Export]
	public CpuParticles3D emitter;
	[Export]
	public Area3D area3D;
	[Export]
	public Camera3D cam;

	private const int RayLength = 1000;
	private bool isMouseTracking = true;
	private bool swingForward = true;
	private bool canAttack = true;
	private double currentAnimPos = 0.0;

	public override void _Ready()
	{
		animPlayer.AnimationStarted += OnAnimStarted;
		animPlayer.AnimationFinished += OnAnimFinish;
		area3D.BodyEntered += OnHit;

		emitter.Emitting = false;
		area3D.Monitoring = false;
		canAttack = true;
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
		var spaceState = GetWorld3D().DirectSpaceState;
		var mousePos = GetViewport().GetMousePosition();

		var origin = cam.ProjectRayOrigin(mousePos);
		var end = origin + cam.ProjectRayNormal(mousePos) * RayLength;
		var query = PhysicsRayQueryParameters3D.Create(origin, end);
		query.CollideWithAreas = true;

		var result = spaceState.IntersectRay(query);

		if (result.ContainsKey("position"))
		{
			var resultPos = (Vector3)result["position"];
			var lookAtPos = new Vector3(resultPos.X, Position.Y, resultPos.Z);
			LookAt(lookAtPos);
		}
	}

	public override void _Process(double delta)
	{
		if (canAttack && Input.IsActionJustPressed("Attack"))
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
		}
	}

	private void OnHit(Node node)
	{
		ChoppableTree tree = node as ChoppableTree;
		if (tree != null)
		{
			animPlayer.Pause();
			currentAnimPos = animPlayer.CurrentAnimationPosition;
			tree.OnHit();
			HitStop();
		}
	}

	private async void HitStop()
	{
		await Task.Delay(TimeSpan.FromMilliseconds(500));
		GD.Print(currentAnimPos);
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
		canAttack = true;
		isMouseTracking = true;
	}


	private void OnAnimStarted(StringName animName)
	{

	}
}
