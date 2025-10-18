using System;
using Godot;

public partial class FishingRod : Node3D
{
    [Export] private AnimationPlayer rodAnim;
    [Export] private AnimationPlayer bobberAnim;
    [Export] private Node3D bobberRayCastStart;
    [Export] private Node3D bobber;
    [Export] private Area3D bobberArea;

    private bool isBobberDeployed = false;

    public override void _Ready()
    {
        isBobberDeployed = false;
        bobber.Visible = false;
        rodAnim.AnimationFinished += OnAnimationFinished;
        bobberAnim.AnimationFinished += OnAnimationFinished;
        rodAnim.Play("RESET");
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Windup")
        {
            DeployBobber();
        }
        if (animName == "drop")
        {
            bobberAnim.Play("swim");
        }
        if(animName == "Pull")
        {
            isBobberDeployed = false;
        }
        if(animName == "pull")
        {
        }
    }

    private void DeployBobber()
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        var origin = bobberRayCastStart.GlobalPosition;
        var end = origin + Vector3.Down * 100.0f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollideWithAreas = true;
        query.CollisionMask = bobberArea.CollisionMask;

        var result = spaceState.IntersectRay(query);
        if (result.ContainsKey("position"))
        {
            Vector3 position = (Vector3)result["position"];
            bobber.Visible = true;
            bobber.GlobalPosition = position;

            bobberAnim.Play("drop");
            isBobberDeployed = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isBobberDeployed && Input.IsActionJustPressed("fish"))
        {
            rodAnim.Play("Windup");
        }else if (Input.IsActionJustPressed("fish"))
        {
            rodAnim.Play("Pull");
            bobberAnim.Play("pull");
        }
    }
}
