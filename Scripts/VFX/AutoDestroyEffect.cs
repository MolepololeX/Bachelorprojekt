using Godot;
using System.Threading.Tasks;

public partial class AutoDestroyEffect : GpuParticles3D
{
	private float cumulativeTime;
	public override void _Ready()
	{
		Emitting = true;
	}

	public override void _Process(double delta)
	{
		cumulativeTime += (float)delta;
		if (cumulativeTime >= Lifetime + 0.1f)
		{
			QueueFree();
		}
	}
}
