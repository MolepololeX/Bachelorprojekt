using Godot;
using System.Threading.Tasks;

public partial class AutoDestroyEffect : GpuParticles3D
{
	[Export]
	private bool autoEmitting = true;
	public override void _Ready()
	{
		Emitting = true;
		WaitForFinished(Lifetime);
	}

	private async void WaitForFinished(double delay)
	{
		await Task.Delay((int)(delay * 1000.0));
		QueueFree();
	}
}
