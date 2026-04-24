using Godot;
using System;

public partial class AudioFade : AudioStreamPlayer
{
    [Export] private float _targetDB = 0.0f;
    [Export] private float _minDB = -30.0f;
    [Export] private float _fadeInScale = 1.0f;
    [Export] private bool _autoPlay = true;

    private bool _isPlaying;
    private float _cumulativeTime;

    public override void _Ready()
    {
        if (_autoPlay)
        {
            Play();
            VolumeDb = _targetDB + _minDB;
            _isPlaying = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isPlaying)
        {
            _cumulativeTime += (float)delta;
            if (VolumeDb < _targetDB)
            {
                VolumeDb = (_minDB + _cumulativeTime * _fadeInScale);
            }
            else
            {
                VolumeDb = _targetDB;
            }
        }
    }
}