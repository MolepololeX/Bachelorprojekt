using Godot;
using Godot.Collections;
using System;

public partial class LeafShaderUniformSetter : Node3D
{
    [Export] private Node3D _targetPos;
    [Export] private Array<MultiMeshInstance3D> _meshes;

    private ShaderMaterial[] _mats;

    public override void _Ready()
    {
        _mats = new ShaderMaterial[_meshes.Count];
        for (int i = 0; i < _mats.Length; i++)
        {
            _mats[i] = _meshes[i].Multimesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            _mats[i].SetShaderParameter("targetPos", _targetPos.GlobalPosition);
        }
    }


    // TODO: bidde nich so
    public override void _Process(double delta)
    {
    }
}
