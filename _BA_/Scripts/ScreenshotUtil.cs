using Godot;
using System;

public partial class ScreenshotUtil : Node
{
    [ExportGroup("Image Capture")]
    // [ExportToolButton("Screenshot Viewport")] public Callable ScreenshotViewportCallable => Callable.From(CaptureViewportScreenshot);
    [Export] private string _viewportSceenshotPath = "res://_BA_/";
    [Export] private string _viewportScreenshotName = "screenshot_1";

    private bool b = false;

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.F12))
        {
            if (!b)
            {
                GD.Print("Captured Screenshot");
                CaptureViewportScreenshot();
                b = true;
            }
        }
        else
        {
            b = false;
        }
        base._Process(delta);
    }


    public void CaptureViewportScreenshot()
    {
        var img = GetTree().Root.GetViewport().GetTexture().GetImage();
        string imagePath = _viewportSceenshotPath + "/" + _viewportScreenshotName;

		imagePath += Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__");
        img.SavePng(imagePath + ".png");
    }
}
