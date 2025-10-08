using Godot;
using System;

public partial class TestAudio : AudioStreamPlayer
{
	[Export]
	public AudioStreamPlayer audioPlayer;

	[Export]
	public float DelayMax = 5.0f;
	[Export]
	public float DelayMin = 0.5f;
	[Export]
	public float PitchMax = 0.8f;
	[Export]
	public float PitchMin = 1.2f;

	private float timer;

	public override void _Ready()
	{
		timer = DelayMin;
	}

	public override void _Process(double delta)
	{
		timer -= (float)delta;
		if (timer <= 0)
		{
			audioPlayer.PitchScale = (float)GD.RandRange(PitchMin, PitchMax);
			timer = (float)GD.RandRange(DelayMin, DelayMax);
			audioPlayer.Play();
		}
	}
}
