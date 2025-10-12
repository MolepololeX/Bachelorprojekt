using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class CameraPixelSnapping : Camera3D
{
	[ExportCategory("Config")]
	[Export] private bool _snapping = true;
	[Export] private bool _snappingObjects = false;
	[Export] private float _objectSnappingTexelSizeMultiplier = 1.0f;
	[Export] private ColorRect rect;

	[ExportCategory("Debug")]
	[Export] private bool _debugInfo = true;
	[Export] private bool _debugMovement = true;
	[Export] private Label _debugLabel;
	[Export] private float _camSpeed = 1.0f;
	[Export] private float _testmult = 1.0f;

	private ShaderMaterial mat;

	private Basis _snapSpace;
	private float _texelSizeInMeters;

	public override void _Ready()
	{
		mat = rect.Material as ShaderMaterial;
		RenderingServer.FramePostDraw += RevertObjectSnapping;
	}



	public override void _Process(double delta)
	{
		if (_debugMovement)
		{
			DebugMoveCam(delta);
		}


		if (_snapping)
		{
			_snapSpace = GlobalTransform.Basis;
			Vector2 viewPortSize = (Vector2I)GetViewport().GetVisibleRect().Size;
			_texelSizeInMeters = 1.0f / (viewPortSize.Y / (int)Size);

			Vector3 p1 = GlobalPosition * _snapSpace; //TODO: find out why this is so ass
			Vector3 p2 = p1.Snapped(_texelSizeInMeters);

			Vector3 snapError = p2 - p1;

			HOffset = snapError.X;
			VOffset = snapError.Y;

			Vector2 snapDelta;
			snapDelta.X = snapError.X / _texelSizeInMeters;// * (1.0f / viewPortSize.X);
			snapDelta.Y = snapError.Y / _texelSizeInMeters;// * (1.0f / viewPortSize.X);

			mat.SetShaderParameter("snapDeltaX", snapDelta.X);
			mat.SetShaderParameter("snapDeltaY", snapDelta.Y);
			mat.SetShaderParameter("width", viewPortSize.X);
			mat.SetShaderParameter("height", viewPortSize.Y);
			mat.SetShaderParameter("testMult", _testmult);

			if (_snappingObjects)
			{

				CallDeferred("SnapObjectsToGrid");
			}

			if (_debugInfo)
			{
				_debugLabel.Text = "";
				_debugLabel.Text += viewPortSize.ToString() + "\n";
				_debugLabel.Text += "s " + _texelSizeInMeters + "\n";
				// _debugLabel.Text += "p1 " + p1 + "\n";
				// _debugLabel.Text += "p2 " + p2 + "\n";
				_debugLabel.Text += "r " + snapError + "\n";
				_debugLabel.Text += "d " + snapDelta + "\n";
			}

		}

	}

	Array<Node> _snapNodes = new Array<Node>();
	Array<Vector3> _preSnapPositions = new Array<Vector3>();

	private void SnapObjectsToGrid()
	{
		_snapNodes = GetTree().GetNodesInGroup("Snap");
		_preSnapPositions.Resize(_snapNodes.Count);

		for (int i = 0; i < _snapNodes.Count; i++)
		{
			var node = _snapNodes[i] as Node3D;
			var pos = node.GlobalPosition;
			_preSnapPositions[i] = pos;

			var p1 = pos * _snapSpace;
			var p2 = p1.Snapped(new Vector3(_texelSizeInMeters * _objectSnappingTexelSizeMultiplier, _texelSizeInMeters * _objectSnappingTexelSizeMultiplier, 0.0f));
			node.GlobalPosition = _snapSpace * p2;
		}
	}

	private void RevertObjectSnapping()
	{
		for (int i = 0; i < _snapNodes.Count; i++)
		{
			(_snapNodes[i] as Node3D).GlobalPosition = _preSnapPositions[i];
		}
		_snapNodes.Clear();
	}

	private void DebugMoveCam(double delta)
	{
		Vector3 dir = Vector3.Zero;
		if (Input.IsActionPressed("move_up"))
		{
			dir.Z -= _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_down"))
		{
			dir.Z += _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_right"))
		{
			dir.X += _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_left"))
		{
			dir.X -= _camSpeed * (float)delta;
		}

		Position += dir;
	}
}
