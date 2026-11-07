using Rhino.PlugIns;
using System.Runtime.InteropServices;

[assembly: Guid("8C981DCE-6E31-4C7A-A9BC-7F25701B9D34")]

namespace OnonSmartView;

public sealed class ononSmartViewPlugin : PlugIn
{
    private const string AutoFaceSetting = "AutoFaceEnabled";
    private readonly AutoCPlaneFollower _autoCPlaneFollower = new();
    public static ononSmartViewPlugin? Instance { get; private set; }
    internal bool AutoFaceEnabled => _autoCPlaneFollower.Enabled;

    public ononSmartViewPlugin() => Instance = this;

    internal void SetAutoFaceEnabled(bool enabled)
    {
        _autoCPlaneFollower.SetEnabled(enabled);
        Settings.SetBool(AutoFaceSetting, enabled);
        Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        ViewCubeController.SetEnabled(true);
        _autoCPlaneFollower.SetEnabled(Settings.GetBool(AutoFaceSetting, false));
        _autoCPlaneFollower.Start();
        return LoadReturnCode.Success;
    }
}
