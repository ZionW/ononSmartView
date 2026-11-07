using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace OnonSmartView;

// Observes Rhino's active construction plane; never changes the plane itself.
internal sealed class AutoCPlaneFollower
{
    private readonly Dictionary<Guid, ViewState> _views = new();
    private uint _documentSerial;
    private long _animationGeneration;
    public bool Enabled { get; private set; }

    public void Start() => RhinoApp.Idle += OnIdle;

    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled) return;
        CameraAnimator.Cancel(_animationGeneration);
        _animationGeneration = 0;
        Enabled = enabled;
        _views.Clear();
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view is not null)
            _views[view.ActiveViewport.Id] = new ViewState(view.ActiveViewport.GetConstructionPlane().Plane);
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        var doc = RhinoDoc.ActiveDoc;
        var view = doc?.Views.ActiveView;
        if (doc is null || view is null) return;
        var viewport = view.ActiveViewport;

        if (_documentSerial != doc.RuntimeSerialNumber)
        {
            _documentSerial = doc.RuntimeSerialNumber;
            _views.Clear();
        }
        if (!_views.TryGetValue(viewport.Id, out var state))
        {
            state = new ViewState(viewport.GetConstructionPlane().Plane);
            _views.Add(viewport.Id, state);
        }

        if (!Enabled)
        {
            state.Baseline = viewport.GetConstructionPlane().Plane;
            return;
        }

        // A command-owned focus manages its own camera and CPlane until Back.
        if (FocusSessions.GetFor(doc) is not null) return;

        var current = viewport.GetConstructionPlane().Plane;
        if (!current.IsValid) return;
        var tolerance = Math.Max(doc.ModelAbsoluteTolerance, 0.001);
        if (SamePlane(current, state.Session?.LastPlane ?? state.Baseline, tolerance)) return;

        if (state.Session is null)
            state.Session = new FollowSession(
                Math.Max(viewport.CameraLocation.DistanceTo(viewport.CameraTarget), 1.0));

        state.Session.LastPlane = current;
        state.Baseline = current;
        var target = new ViewportInfo(viewport);
        var eye = current.Normal;
        var distance = state.Session.Distance;
        // Preserve perspective in the Perspective viewport while allowing
        // manually selected World and Object CPlanes to share this control.
        if (string.Equals(viewport.Name, "Perspective", StringComparison.OrdinalIgnoreCase) &&
            target.IsParallelProjection)
        {
            var lens = Compatibility.IsFinite(viewport.Camera35mmLensLength) &&
                       viewport.Camera35mmLensLength is >= 10 and <= 200
                ? viewport.Camera35mmLensLength
                : 50.0;
            target.ChangeToPerspectiveProjection(distance, true, lens);
        }
        #if RHINO7
        var autoObjectPlane = false;
#else
        var autoObjectPlane = ModelAidSettings.AutoAlignCPlane &&
                              ModelAidSettings.AutoCPlaneAlignment == 0;
#endif
        var focusTarget = autoObjectPlane ? current.Origin : viewport.CameraTarget;
        target.TargetPoint = focusTarget;
        target.SetCameraLocation(focusTarget + eye * distance);
        target.SetCameraDirection(-eye);
        target.SetCameraUp(current.YAxis);
        _animationGeneration = CameraAnimator.Animate(view, target, () =>
        {
            var applied = viewport.GetConstructionPlane().Plane;
            if (SamePlane(applied, current, tolerance))
                CameraAnimator.AlignToPlane(viewport, applied, focusTarget, distance);
            view.Redraw();
        });
    }

    private static bool SamePlane(Plane a, Plane b, double tolerance) =>
        a.IsValid && b.IsValid && a.Origin.DistanceTo(b.Origin) <= tolerance &&
        a.XAxis * b.XAxis > 0.9999 && a.YAxis * b.YAxis > 0.9999;

    private sealed class ViewState(Plane baseline)
    {
        public Plane Baseline { get; set; } = baseline;
        public FollowSession? Session { get; set; }
    }

    private sealed class FollowSession(double distance)
    {
        public double Distance { get; } = distance;
        public Plane LastPlane { get; set; } = Plane.Unset;
    }
}
