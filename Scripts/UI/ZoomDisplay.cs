using System;
using System.Threading.Tasks;
using Game.Camera;
using Godot;

public partial class ZoomDisplay : Control
{
    [Export] private CameraRig _camRig;
    [Export] private Label _label;

    private float _cachedZoom = 0.0f;

    public override void _Ready()
    {
        _label.Visible = false;
        _cachedZoom = _camRig.CurrentZoom;
    }


    public override void _Process(double delta)
    {
        if (_cachedZoom != _camRig.CurrentZoom)
        {
            _cachedZoom = _camRig.CurrentZoom;
            _label.Text = "x" + (Math.Truncate(_camRig.CurrentZoom * 10.0) / 10.0).ToString();
            if (!_label.Visible)
            {
                _label.Visible = true;
                HideLogo(1.0f);
            }
        }
    }

    private async void HideLogo(float delay)
    {
        await Task.Delay(TimeSpan.FromSeconds(delay));
        _label.Visible = false;
    }
}
