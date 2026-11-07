using Rhino.Display;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OnonSmartView;

internal sealed class FocusSession
{
    public required Guid ViewportId { get; init; }
    public required ViewportInfo OriginalProjection { get; init; }
    public required Plane OriginalCPlane { get; init; }
    public required Point3d OriginalLocation { get; init; }
    public required Point3d OriginalTarget { get; init; }
    public required Vector3d OriginalDirection { get; init; }
    public required Vector3d OriginalUp { get; init; }
    public required double OriginalLensLength { get; init; }
    public required Plane FocusPlane { get; set; }
    public required Point3d FocusTarget { get; init; }
    public required double FocusDistance { get; init; }
    public required RhinoView View { get; init; }

    public static FocusSession Capture(RhinoView view, Plane focusPlane, Point3d focusTarget, double focusDistance)
    {
        var viewport = view.ActiveViewport;
        var projection = new ViewportInfo(viewport);
        return new FocusSession
        {
            ViewportId = viewport.Id,
            OriginalProjection = projection,
            OriginalCPlane = viewport.GetConstructionPlane().Plane,
            OriginalLocation = viewport.CameraLocation,
            OriginalTarget = viewport.CameraTarget,
            OriginalDirection = viewport.CameraDirection,
            OriginalUp = viewport.CameraUp,
            OriginalLensLength = viewport.Camera35mmLensLength,
            FocusPlane = focusPlane,
            FocusTarget = focusTarget,
            FocusDistance = Math.Max(focusDistance, 1.0),
            View = view
        };
    }

    public void Restore()
    {
        var viewport = View.ActiveViewport;
        viewport.SetViewProjection(OriginalProjection, false);
        viewport.SetCameraLocations(OriginalTarget, OriginalLocation);
        viewport.SetCameraDirection(OriginalDirection, false);
        var finalProjection = new ViewportInfo(OriginalProjection);
        finalProjection.SetCameraUp(OriginalUp);
        viewport.SetViewProjection(finalProjection, false);
        viewport.CameraUp = OriginalUp;
        viewport.Camera35mmLensLength = OriginalLensLength;
        viewport.SetConstructionPlane(OriginalCPlane);
        View.Redraw();
    }
}
