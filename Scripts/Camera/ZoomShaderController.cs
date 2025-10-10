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


		private Node3D playerNode;
		private ColorRect rect;
		private ShaderMaterial mat;
		private float delay;
		private float zoomTarget;


		public override void _Ready()
		{
			playerNode = GameManager.Instance.Player;
			rect = GameManager.Instance.ZoomShaderColorRect;
			mat = rect.Material as ShaderMaterial;
			zoomTarget = zoom;
		}

		public override void _Process(double delta)
		{
			SmoothZoom(delta);

			Vector2 pos = UnprojectPosition(playerNode.Position);
			mat.SetShaderParameter("offsetX", pos.X);
			mat.SetShaderParameter("offsetY", pos.Y);
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