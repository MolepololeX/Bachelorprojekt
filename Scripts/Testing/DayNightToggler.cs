using Godot;
using Godot.Collections;

[Tool]
public partial class DayNightToggler : Node
{
    [ExportToolButton("Toggle Time of Day")] public Callable ToggleTimeOfDay => Callable.From(SetToggleTimeOfDay);
    [ExportToolButton("Toggle Rain")] public Callable ToggleRain => Callable.From(SetToggleRain);

    [Export] private Array<Node3D> _dayObjects;
    [Export] private Array<Node3D> _nightObjects;
    [Export] private Array<Node3D> _rainObjects;
    [Export] private Array<Node3D> _clearSkyObjects;
    [Export] private Color _nightSkyColor;
    [Export] private Color _daySkyColor;
    [Export] private Color _nightAmbientColor;
    [Export] private Color _dayAmbientColor;
    [Export] private WorldEnvironment _worldEnvironment;

    private bool _isDay = true;
    private bool _isRain = false;

    private void SetToggleRain()
    {
        if (_isRain)
        {
            GD.Print("Setting Rain");
            foreach (var rain in _rainObjects)
            {
                rain.Visible = true;
            }
            foreach (var clear in _clearSkyObjects)
            {
                clear.Visible = false;
            }
            _worldEnvironment.Environment.VolumetricFogEnabled = true;
        }
        else
        {
            GD.Print("Setting Clear Sky");
            foreach (var rain in _rainObjects)
            {
                rain.Visible = false;
            }
            foreach (var clear in _clearSkyObjects)
            {
                clear.Visible = true;
            }
            _worldEnvironment.Environment.VolumetricFogEnabled = false;
        }
        _isRain = !_isRain;
    }

    private void SetToggleTimeOfDay()
    {
        if (_isDay)
        {
            GD.Print("Setting Daytime");
            foreach (var light in _dayObjects)
            {
                light.Visible = true;
            }
            foreach (var light in _nightObjects)
            {
                light.Visible = false;
            }

            _worldEnvironment.Environment.BackgroundColor = _daySkyColor;
            _worldEnvironment.Environment.AmbientLightColor = _dayAmbientColor;
        }
        else
        {
            GD.Print("Setting Nighttime");
            foreach (var light in _dayObjects)
            {
                light.Visible = false;
            }
            foreach (var light in _nightObjects)
            {
                light.Visible = true;
            }

            _worldEnvironment.Environment.BackgroundColor = _nightSkyColor;
            _worldEnvironment.Environment.AmbientLightColor = _nightAmbientColor;
        }
        _isDay = !_isDay;
    }
}