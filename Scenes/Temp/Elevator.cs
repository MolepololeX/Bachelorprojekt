using Godot;
using System;

public partial class Elevator : MeshInstance3D
{
    [Export] private float _speed = 1.0f;
    [Export] private float _traveldistance = 1.0f;

    private Vector3 _originalPosition;
    private float _cumulativeTime;

    public override void _Ready()
    {
        _originalPosition = Position;
    }

    public override void _PhysicsProcess(double delta)
    {
        _cumulativeTime += (float)delta;
        Position = new Vector3(
            Position.X,
            _originalPosition.Y + ((float)Math.Cos(_cumulativeTime * _speed) - 1.0f) * _traveldistance,
            Position.Z
        );
    }
}
