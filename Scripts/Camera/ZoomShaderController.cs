using Game.Managers;
using Godot;

namespace Game.Camera
{
	public partial class ZoomShaderController : Camera3D
	{
		[ExportCategory("Settings")]
		[Export(PropertyHint.Range, "0,1,")] private float zoom = 0.4f;
		[Export] private float zoomIncrement = 0.1f;
		[Export] private float zoomSpeed = 1.0f;
		[Export] private float maxZoom = 1.0f;
		[Export] private float minZoom = 0.1f;
		[Export] private bool staticZoom = true;

		[Export] private bool useSmoothCam = false;
		[Export] private float smoothing = 1.0f;


		private Node3D playerNode;
		private ColorRect rect;
		private ShaderMaterial mat;
		private float delay;
		private float zoomTarget;

		private Vector2 oldPos;


		public override void _Ready()
		{
			playerNode = GameManager.Instance.Player;
			rect = GameManager.Instance.ZoomShaderColorRect;
			mat = rect.Material as ShaderMaterial;
			zoomTarget = zoom;

			oldPos = UnprojectPosition(playerNode.Position);
		}

		public override void _Process(double delta)
		{
			SmoothZoom(delta);

			Vector2 currentPos = UnprojectPosition(playerNode.Position);
			if (useSmoothCam)
			{
				currentPos.X = Mathf.Lerp(oldPos.X, currentPos.X, smoothing * (float)delta);
				currentPos.Y = Mathf.Lerp(oldPos.Y, currentPos.Y, smoothing * (float)delta);
				oldPos.X = currentPos.X;
				oldPos.Y = currentPos.Y;
			}

			mat.SetShaderParameter("offsetX", currentPos.X);
			mat.SetShaderParameter("offsetY", currentPos.Y);

			mat.SetShaderParameter("zoom", zoom);
			mat.SetShaderParameter("staticZoom", staticZoom);

			delay += (float)delta;
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
	}
}