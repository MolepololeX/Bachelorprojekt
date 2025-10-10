using Godot;
using System;
using System.ComponentModel.DataAnnotations;

public partial class ZoomShaderController : Camera3D
{
	private Node3D player;
	private ColorRect postColorRect;

	[ExportCategory("Settings")]
	[Export(PropertyHint.Range, "0,1,")]
	public float zoom = 0.4f;
	[Export]
	public float zoomIncrement = 0.1f;
	[Export]
	public float zoomSpeed = 1.0f;
	[Export]
	public float maxZoom = 1.0f;
	[Export]
	public float minZoom = 0.1f;
	[Export]
	public bool staticZoom = true;

	private ShaderMaterial mat;

	private float delay;

	private float zoomTarget;

	public override void _Ready()
	{
		player 
		mat = rect.Material as ShaderMaterial;
		GD.Print(mat);
		zoomTarget = zoom;
	}

	private void SmoothZoom(double delta)
	{
		if (Input.IsActionJustPressed("camera_zoom_in"))
		{
			zoomTarget -= zoomIncrement;
			if (zoomTarget <= minZoom) zoomTarget = minZoom;
		}
		if (Input.IsActionJustPressed("camera_zoom_out"))
		{
			zoomTarget += zoomIncrement;
			if (zoomTarget >= maxZoom) zoomTarget = maxZoom;
		}

		zoom = Mathf.Lerp(zoom, zoomTarget, zoomSpeed * (float)delta);
	}

	public override void _Process(double delta)
	{
		SmoothZoom(delta);

		Vector2 pos = UnprojectPosition(playerNode.Position);
		mat.SetShaderParameter("offsetX", pos.X);
		mat.SetShaderParameter("offsetY", pos.Y);
		mat.SetShaderParameter("zoom", zoom);
		mat.SetShaderParameter("staticZoom", staticZoom);

		if (delay > 1.0)
		{
			delay = 0.0f;
			GD.Print(pos);
		}
		delay += (float)delta;
	}
}
