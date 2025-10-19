using Godot;
using Godot.Collections;

[Tool]
public partial class DayNightToggler : Node
{
    [ExportToolButton("Toggle Time of Day")] public Callable ToggleTimeOfDay => Callable.From(SetToggleTimeOfDay);

    [Export] private Array<Node3D> _dayTimeLights;
    [Export] private Array<Node3D> _nightTimeLights;
    [Export] private Color _nightSkyColor;
    [Export] private Color _daySkyColor;
    [Export] private Color _nightAmbientColor;
    [Export] private Color _dayAmbientColor;
    [Export] private WorldEnvironment _worldEnvironment;

    private bool _isDay = true;

    private void SetToggleTimeOfDay()
    {
        if (_isDay)
        {
            GD.Print("Setting Daytime");
            foreach(var light in _dayTimeLights)
            {
                light.Visible = true;
            }
            foreach (var light in _nightTimeLights)
            {
                light.Visible = false;
            }

            _worldEnvironment.Environment.BackgroundColor = _daySkyColor;
            _worldEnvironment.Environment.AmbientLightColor = _dayAmbientColor;
        }
        else
        {
            GD.Print("Setting Nighttime");
            foreach(var light in _dayTimeLights)
            {
                light.Visible = false;
            }
            foreach(var light in _nightTimeLights)
            {
                light.Visible = true;
            }

            _worldEnvironment.Environment.BackgroundColor = _nightSkyColor;
            _worldEnvironment.Environment.AmbientLightColor = _nightAmbientColor;
        }
        _isDay = !_isDay;
    }
}
