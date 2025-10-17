using Godot;
using System;

public partial class CampFireLightFlicker : Node3D
{
    [Export] private OmniLight3D light;
    [Export] private float amplitude = 0.03f;
    [Export] private float timeInterval = 1.0f;

    private float cumulativeTime;
    private float originalLightRange;
    private bool shrink = false;

    public override void _Ready()
    {
        originalLightRange = light.OmniAttenuation;
    }


    public override void _Process(double delta)
    {
        if (cumulativeTime >= timeInterval)
        {
            if (shrink)
            {
                light.OmniAttenuation = originalLightRange + GD.Randf() * amplitude;
            }
            else
            {
                light.OmniAttenuation = originalLightRange - GD.Randf() * amplitude;
            }
            cumulativeTime = 0.0f;
        }
        cumulativeTime += (float)delta;
    }
}