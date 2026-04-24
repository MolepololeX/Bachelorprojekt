using Godot;

namespace Game.UI
{
	public partial class FpsCounter : Control
	{
		[Export]
		public Label FPSLabel;

		public override void _Process(double delta)
		{
			FPSLabel.Text = (1.0 / delta).ToString() + "\n" + delta;
		}
	}
}