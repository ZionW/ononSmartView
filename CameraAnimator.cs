using System.Diagnostics;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OnonSmartView;

internal static class CameraAnimator
{
    private const int DurationMilliseconds = 450;
    private static System.Threading.Timer? _timer;
    private static long _generation;

    public static long Animate(RhinoView view, ViewportInfo to, Action completed, ViewportInfo? fromProjection = null, Point3d? orbitCenter = null)
    {
        var generation = System.Threading.Interlocked.Increment(ref _generation);
        _timer?.Dispose();
        var viewport = view.ActiveViewport;
        var from = fromProjection ?? new ViewportInfo(viewport);
        var fromLocation = from.CameraLocation;
        var fromTarget = orbitCenter ?? viewport.CameraTarget;
        var fromUp = from.CameraUp;
        var toLocation = to.CameraLocation;
        var toTarget = to.TargetPoint;
        var toDirection = to.CameraDirection;
        var toUp = to.CameraUp;
        // Interpolate orientation on an orbit, not a chord through the target.
        var fromEye = fromLocation - fromTarget;
        var toEye = toLocation - toTarget;
        var fromRadius = fromEye.Length;
        var toRadius = toEye.Length;
        fromEye.Unitize();
        toEye.Unitize();
        var axis = Vector3d.CrossProduct(fromEye, toEye);
        var angle = Math.Acos(Compatibility.Clamp(fromEye * toEye, -1, 1));
        if (!axis.Unitize())
        {
            axis = fromUp - fromEye * (fromUp * fromEye);
            if (!axis.Unitize()) axis = Vector3d.CrossProduct(fromEye,
                Math.Abs(fromEye.Z) < 0.9 ? Vector3d.ZAxis : Vector3d.XAxis);
            axis.Unitize();
        }
        var endUp = fromUp;
        endUp.Rotate(angle, axis);
        endUp -= toEye * (endUp * toEye);
        endUp.Unitize();
        var desiredUp = toUp - toEye * (toUp * toEye);
        desiredUp.Unitize();
        var roll = Math.Atan2(Vector3d.CrossProduct(endUp, desiredUp) * toEye,
            endUp * desiredUp);
        var stopwatch = Stopwatch.StartNew();

        // Allow at most one queued UI update. Measure time on the UI thread so
        // a busy viewport skips stale frames rather than replaying them later.
        var pending = 0;
        System.Threading.Timer? timer = null;
        Action update = () =>
        {
            try
            {
                if (System.Threading.Volatile.Read(ref _generation) != generation) return;
                var vp = view.ActiveViewport;
                if (!vp.IsValidCamera || vp.Id != viewport.Id)
                {
                    timer?.Dispose();
                    _timer = null;
                    return;
                }

                var t = Compatibility.Clamp(stopwatch.Elapsed.TotalMilliseconds / DurationMilliseconds, 0, 1);
                if (t >= 1)
                {
                    // Apply the final projection once, then restore its exact pose.
                    timer?.Dispose();
                    _timer = null;
                    vp.SetViewProjection(to, false);
                    if (to.IsPerspectiveProjection)
                        vp.Camera35mmLensLength = to.Camera35mmLensLength;
                    vp.SetCameraLocations(toTarget, toLocation);
                    vp.SetCameraDirection(toDirection, false);
                    vp.CameraUp = toUp;
                    // Invalidate any previously dispatched callback before completion
                    // can start another animation.
                    System.Threading.Interlocked.CompareExchange(ref _generation, generation + 1, generation);
                    view.Redraw();
                    completed();
                    return;
                }

                var eased = t * t * (3 - 2 * t);
                var eye = fromEye;
                eye.Rotate(angle * eased, axis);
                var direction = -eye;
                var up = fromUp;
                up.Rotate(angle * eased, axis);
                up.Rotate(roll * eased, eye);
                var target = Lerp(fromTarget, toTarget, eased);
                var radius = fromRadius + (toRadius - fromRadius) * eased;
                vp.SetCameraLocations(target, target + eye * radius);
                vp.SetCameraDirection(direction, false);
                var frame = new ViewportInfo(vp);
                frame.SetCameraUp(up);
                vp.SetViewProjection(frame, false);
                vp.CameraUp = up;
                view.Redraw();
            }
            finally
            {
                System.Threading.Volatile.Write(ref pending, 0);
            }
        };
        timer = new System.Threading.Timer(_ =>
        {
            if (System.Threading.Volatile.Read(ref _generation) != generation ||
                System.Threading.Interlocked.Exchange(ref pending, 1) != 0) return;
            try
            {
                Rhino.RhinoApp.InvokeOnUiThread(update, null);
            }
            catch
            {
                System.Threading.Volatile.Write(ref pending, 0);
                timer?.Dispose();
            }
        }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        _timer = timer;
        timer.Change(0, 15);
        return generation;
    }

    public static void CancelAll()
    {
        System.Threading.Interlocked.Increment(ref _generation);
        _timer?.Dispose();
        _timer = null;
    }

    public static void Cancel(long generation)
    {
        if (generation == 0 ||
            System.Threading.Interlocked.CompareExchange(ref _generation, generation + 1, generation) != generation)
            return;
        _timer?.Dispose();
        _timer = null;
    }

    public static ViewportInfo MakeFocusProjection(FocusSession session, bool flipped)
    {
        var plane = session.FocusPlane;
        var normal = plane.Normal;
        if (flipped) normal = -normal;
        var info = new ViewportInfo(session.OriginalProjection);
        info.ChangeToParallelProjection(true);
        info.TargetPoint = session.FocusTarget;
        info.SetCameraLocation(session.FocusTarget + normal * session.FocusDistance);
        info.SetCameraDirection(-normal);
        info.SetCameraUp(plane.YAxis);
        return info;
    }

    public static void AlignToPlane(RhinoViewport viewport, Plane plane,
        Point3d target, double distance, bool flipped = false)
    {
        if (!plane.IsValid) return;
        var normal = flipped ? -plane.Normal : plane.Normal;
        viewport.SetCameraLocations(target, target + normal * distance);
        viewport.SetCameraDirection(-normal, false);
        viewport.CameraUp = plane.YAxis;
    }

    private static Point3d Lerp(Point3d a, Point3d b, double t) => new(
        a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

}
