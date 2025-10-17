using Godot;
using System;

public partial class LeafShaderUniformSetterOnMultimesh : MultiMeshInstance3D
{
    private ShaderMaterial _mat;

    public override void _Ready()
    {
        _mat = Multimesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
    }

    // TODO: bidde nich so
    public override void _Process(double delta)
    {
        _mat.SetShaderParameter("targetPos", GlobalPosition);
    }
}
