using Godot;
using GodotPlugins.Game;
using System;

public partial class ObliqueReflectionCamera : Camera3D
{
    [Export] private Camera3D mainCam;
    [Export] private MeshInstance3D reflector;
    [Export] private bool _snapping = false;
    private Plane plane;
    private ShaderMaterial mat;

    public override void _Ready()
    {
        mat = reflector.GetActiveMaterial(0) as ShaderMaterial;
    }


    public override void _Process(double delta)
    {
        SnapToGrid();

        //mirror, should work
        plane = new Plane(Vector3.Up, reflector.GlobalPosition.Y);
        Vector3 normal = plane.Normal;
        float d = plane.D;

        var reflectedOrigin = mainCam.GlobalTransform.Origin;
        reflectedOrigin -= 2.0f * plane.DistanceTo(reflectedOrigin) * normal;

        var reflectedBasis = mainCam.GlobalTransform.Basis;
        reflectedBasis.X = reflectedBasis.X - 2.0f * normal.Dot(reflectedBasis.X) * normal;
        reflectedBasis.Y = reflectedBasis.Y - 2.0f * normal.Dot(reflectedBasis.Y) * normal;
        reflectedBasis.Z = reflectedBasis.Z - 2.0f * normal.Dot(reflectedBasis.Z) * normal;

        Transform = new Transform3D(reflectedBasis, reflectedOrigin);
        Size = mainCam.Size;



        //oblique projection, idk chatgpt hat maybe ne gekocht
        var proj = mainCam.GetCameraProjection();
        var clipPlane = new Vector4(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
        var m = proj;
        var q = new Vector4(
            Math.Sign(clipPlane.X) + m[0][2] / m[0][0],
            Math.Sign(clipPlane.Y) + m[1][2] / m[1][1],
            -1.0f,
            (1.0f + m[2][2]) / m[2][3]
        );
        var c = (2.0f / clipPlane.Dot(q)) * clipPlane;

        m.Z = new Vector4(c.X, c.Y, c.Z + 1.0f, c.W);

        // m.W[0] = 0.1f;
        // GD.Print(m[3][0]);

        mat.SetShaderParameter("obliqueProjection", m);
        // GD.Print(mat is null);
        // mat.SetShaderParameter("alpha", 0.1f);
    }

    private void SnapToGrid()
    {
        if (_snapping)
        {
            var _snapSpace = GlobalTransform.Basis;
            Vector2 viewPortSize = (Vector2I)GetViewport().GetVisibleRect().Size;
            var _texelSizeInMeters = 1.0f / (viewPortSize.Y / (int)Size);

            Vector3 p1 = GlobalPosition * _snapSpace; //TODO: find out why this is so ass
            Vector3 p2 = p1.Snapped(_texelSizeInMeters);

            Vector3 snapError = p2 - p1;

            HOffset = snapError.X;
            VOffset = snapError.Y;
        }
    }

}
