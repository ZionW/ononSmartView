using System.Drawing;
using Point = System.Drawing.Point;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;

namespace OnonSmartView;

public sealed class SmartViewCubeCommand : Command
{
    public override string EnglishName => "SmartViewCube";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        ViewCubeController.SetEnabled(!ViewCubeController.Enabled);
        RhinoApp.WriteLine($"ononSmartView ViewCube {(ViewCubeController.Enabled ? "enabled" : "disabled")}.");
        return Result.Success;
    }
}

internal static class ViewCubeController
{
    private static readonly CubeConduit Conduit = new();
    private static readonly CubeMouse Mouse = new();
    private static readonly Dictionary<Guid, double> PreferredLenses = new();
    private static readonly Dictionary<Guid, PerspectiveReturnView> PerspectiveReturnViews = new();
    public static bool Enabled { get; private set; }

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Conduit.Enabled = enabled;
        Mouse.Enabled = enabled;
        if (!enabled) PerspectiveReturnViews.Clear();
        RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    private sealed class CubeConduit : DisplayConduit
    {
        protected override void DrawForeground(DrawEventArgs e)
        {
            if (!Enabled) return;
            try
            {
            var viewport = e.Viewport;
            var frame = CubeFrame.From(viewport);
            var border = Color.FromArgb(225, 228, 231);

            foreach (var face in frame.VisibleFaces())
            {
                var corners = frame.FaceCorners(face);
                FillFace(e.Display, corners, CubeFrame.FaceColor(face));
                for (var i = 0; i < 4; i++)
                    e.Display.Draw2dLine(corners[i], corners[(i + 1) % 4], border, 2);
                var center = frame.Project(face);
                e.Display.Draw2dText(CubeFrame.Label(face), Color.Black,
                    new Point2d(center.X, center.Y), true, 19);
            }

            // Edge ticks and corner diamonds remain identifiable without a panel backdrop.
            foreach (var edge in frame.VisibleEdges())
            {
                var center = frame.Project(edge);
                var end = frame.Project(edge + frame.EdgeAxis(edge) * 0.24);
                var start = frame.Project(edge - frame.EdgeAxis(edge) * 0.24);
                e.Display.Draw2dLine(start, end, Color.FromArgb(173, 190, 198), 3);
            }
            foreach (var corner in frame.VisibleCorners())
            {
                var p = frame.Project(corner);
                var color = Color.FromArgb(91, 113, 126);
                var top = new PointF(p.X, p.Y - 3);
                var right = new PointF(p.X + 3, p.Y);
                var bottom = new PointF(p.X, p.Y + 3);
                var left = new PointF(p.X - 3, p.Y);
                e.Display.Draw2dLine(top, right, color, 2);
                e.Display.Draw2dLine(right, bottom, color, 2);
                e.Display.Draw2dLine(bottom, left, color, 2);
                e.Display.Draw2dLine(left, top, color, 2);
            }

            DrawAxis(e.Display, frame, Vector3d.XAxis, "X", Color.FromArgb(238, 111, 116));
            DrawAxis(e.Display, frame, Vector3d.YAxis, "Y", Color.FromArgb(109, 200, 133));
            DrawAxis(e.Display, frame, Vector3d.ZAxis, "Z", Color.FromArgb(105, 163, 238));

            foreach (var turn in new[] { -1, 1 })
            {
                var center = frame.TurnButton(turn);
                e.Display.DrawRoundedRectangle(center, 33, 33, 16,
                    Color.FromArgb(140, 145, 150), 2, Color.White);
                e.Display.Draw2dText(turn < 0 ? "↷" : "↶", Color.Black,
                    new Point2d(center.X, center.Y), true, 20);
            }
            var home = frame.ResetButton();
            e.Display.DrawRoundedRectangle(home, 27, 27, 13,
                Color.FromArgb(140, 145, 150), 2, Color.White);
            e.Display.Draw2dText("⌂", Color.Black,
                new Point2d(home.X, home.Y), true, 19);
            var autoFace = frame.AutoFaceButton();
            var autoFaceEnabled = ononSmartViewPlugin.Instance?.AutoFaceEnabled ?? false;
            e.Display.DrawRoundedRectangle(autoFace, 28, 28, 14,
                autoFaceEnabled ? Color.FromArgb(56, 139, 103) : Color.FromArgb(120, 127, 132),
                2, autoFaceEnabled ? Color.FromArgb(214, 243, 225) : Color.White);
            e.Display.Draw2dText("C", autoFaceEnabled ? Color.FromArgb(31, 100, 72) : Color.FromArgb(85, 92, 97),
                new Point2d(autoFace.X, autoFace.Y), true, 16);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"ononSmartView ViewCube draw error: {ex.Message}");
            }
        }

        private static void FillFace(DisplayPipeline display, PointF[] corners, Color color)
        {
            var minY = (int)Math.Ceiling(corners.Min(p => p.Y));
            var maxY = (int)Math.Floor(corners.Max(p => p.Y));
            var crossings = new List<float>(4);
            for (var y = minY; y <= maxY; y++)
            {
                crossings.Clear();
                for (var i = 0; i < corners.Length; i++)
                {
                    var a = corners[i];
                    var b = corners[(i + 1) % corners.Length];
                    if ((a.Y <= y && b.Y > y) || (b.Y <= y && a.Y > y))
                        crossings.Add(a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y));
                }
                if (crossings.Count < 2) continue;
                crossings.Sort();
                display.Draw2dLine(new PointF(crossings[0], y),
                    new PointF(crossings[crossings.Count - 1], y), color, 1.5f);
            }
        }

        private static void DrawAxis(DisplayPipeline display, CubeFrame frame,
            Vector3d axis, string label, Color color)
        {
            var start = frame.Project(axis * 1.35);
            var end = frame.Project(axis * 2.0);
            if (Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) < 4) return;
            display.Draw2dLine(start, end, color, 2);
            var textPoint = frame.Project(axis * 2.18);
            display.Draw2dText(label, color, new Point2d(textPoint.X, textPoint.Y), true, 13);
        }
    }

    private sealed class CubeMouse : MouseCallback
    {
        private RhinoView? _dragView;
        private Guid _dragViewportId;
        private Point _start;
        private Point3d _target;
        private Vector3d _eye;
        private Vector3d _up;
        private Vector3d _pickedDirection;
        private int _turn;
        private bool _reset;
        private bool _autoFace;
        private bool _moved;

        protected override void OnMouseDown(MouseCallbackEventArgs e)
        {
            if (!Enabled || e.MouseButton != MouseButton.Left) return;
            var frame = CubeFrame.From(e.View.ActiveViewport);
            var p = e.ViewportPoint;
            if (!frame.Contains(p)) return;

            CameraAnimator.CancelAll();
            _dragView = e.View;
            _dragViewportId = e.View.ActiveViewport.Id;
            _start = p;
            _target = RotationTarget(e.View);
            _eye = e.View.ActiveViewport.CameraLocation - _target;
            _up = e.View.ActiveViewport.CameraUp;
            _pickedDirection = frame.HitTest(p);
            _turn = frame.HitTurnButton(p);
            _reset = frame.HitResetButton(p);
            _autoFace = frame.HitAutoFaceButton(p);
            _moved = false;
            e.Cancel = true;
        }

        protected override void OnMouseMove(MouseCallbackEventArgs e)
        {
            if (_dragView is null || e.View.ActiveViewport.Id != _dragViewportId) return;
            if (_turn != 0 || _reset || _autoFace) { e.Cancel = true; return; }
            var dx = e.ViewportPoint.X - _start.X;
            var dy = e.ViewportPoint.Y - _start.Y;
            if (!_moved && dx * dx + dy * dy < 25) return;
            if (!_moved) RememberPerspective(_dragView);
            _moved = true;
            e.Cancel = true;

            var eye = _eye;
            var up = _up;
            var yaw = -dx * 0.012;
            eye.Rotate(yaw, Vector3d.ZAxis);
            up.Rotate(yaw, Vector3d.ZAxis);
            var right = Vector3d.CrossProduct(-eye, up);
            if (!right.Unitize()) return;
            var pitch = -dy * 0.012;
            eye.Rotate(pitch, right);
            up.Rotate(pitch, right);

            var viewport = _dragView.ActiveViewport;
            var frame = new ViewportInfo(viewport);
            frame.TargetPoint = _target;
            frame.SetCameraLocation(_target + eye);
            frame.SetCameraDirection(-eye);
            frame.SetCameraUp(up);
            viewport.SetViewProjection(frame, false);
            viewport.SetCameraLocations(_target, _target + eye);
            _dragView.Redraw();
        }

        protected override void OnMouseUp(MouseCallbackEventArgs e)
        {
            if (_dragView is null || e.View.ActiveViewport.Id != _dragViewportId) return;
            e.Cancel = true;
            if (!_moved)
            {
                if (_autoFace)
                {
                    var plugin = ononSmartViewPlugin.Instance;
                    if (plugin is not null)
                    {
                        plugin.SetAutoFaceEnabled(!plugin.AutoFaceEnabled);
                        RhinoApp.WriteLine($"ononSmartView work-plane facing {(plugin.AutoFaceEnabled ? "enabled" : "disabled")}.");
                    }
                }
                else if (_reset) ResetView(_dragView);
                else if (_turn != 0)
                {
                    RememberPerspective(_dragView);
                    RotateYaw(_dragView, _turn);
                }
                else if (_pickedDirection.IsValid && !_pickedDirection.IsZero)
                {
                    RememberPerspective(_dragView);
                    Snap(_dragView, _pickedDirection);
                }
            }
            _dragView = null;
        }
    }

    private static void Snap(RhinoView view, Vector3d eye)
    {
        if (!eye.Unitize()) return;
        var viewport = view.ActiveViewport;
        var up = Math.Abs(eye * Vector3d.ZAxis) > 0.95 ? Vector3d.YAxis : Vector3d.ZAxis;
        var projection = MakeNavigationProjection(view, eye, up, true);
        if (projection is null) return;
        CameraAnimator.Animate(view, projection, view.Redraw);
    }

    private static ViewportInfo? MakeNavigationProjection(RhinoView view, Vector3d eye,
        Vector3d up, bool toggleProjection, Point3d? rotationTarget = null)
    {
        if (!eye.Unitize() || !up.Unitize()) return null;
        var viewport = view.ActiveViewport;
        var projection = new ViewportInfo(viewport);
        var target = rotationTarget ?? viewport.CameraTarget;
        var currentDistance = Math.Max(viewport.CameraLocation.DistanceTo(target), 1e-6);
        var lens = viewport.Camera35mmLensLength;
        if (!viewport.IsParallelProjection && Compatibility.IsFinite(lens) && lens is >= 10 and <= 200)
            PreferredLenses[viewport.Id] = lens;
        else if (!PreferredLenses.TryGetValue(viewport.Id, out lens))
            lens = 50.0;

        // Convert first: Rhino may move the camera while changing projection.
        if (viewport.IsParallelProjection)
        {
            if (toggleProjection && !projection.ChangeToPerspectiveProjection(currentDistance, true, lens))
                return null;
        }
        else if (toggleProjection && !projection.ChangeToParallelProjection(true))
        {
            return null;
        }

        var distance = currentDistance;

        projection.TargetPoint = target;
        projection.SetCameraLocation(target + eye * distance);
        projection.SetCameraDirection(-eye);
        projection.SetCameraUp(up);
        if (toggleProjection && projection.IsPerspectiveProjection)
            projection.Camera35mmLensLength = lens;
        if (toggleProjection)
        {
            var box = VisibleObjectBounds(view);
            if (box.IsValid) projection.SetFrustumNearFar(box);
        }
        return projection;
    }

    private static void RememberPerspective(RhinoView view)
    {
        var viewport = view.ActiveViewport;
        if (!viewport.Name.Equals("Perspective", StringComparison.OrdinalIgnoreCase) ||
            PerspectiveReturnViews.ContainsKey(viewport.Id)) return;
        var lens = viewport.Camera35mmLensLength;
        if (viewport.IsPerspectiveProjection && Compatibility.IsFinite(lens) && lens is >= 10 and <= 200)
            PreferredLenses[viewport.Id] = lens;
        PerspectiveReturnViews[viewport.Id] = new PerspectiveReturnView(
            new ViewportInfo(viewport), viewport.CameraLocation, viewport.CameraTarget,
            viewport.CameraDirection, viewport.CameraUp, lens);
    }

    private sealed record PerspectiveReturnView(
        ViewportInfo Projection, Point3d Location, Point3d Target,
        Vector3d Direction, Vector3d Up, double Lens);

    private static void ResetView(RhinoView view)
    {
        var viewport = view.ActiveViewport;
        var name = viewport.Name.ToLowerInvariant();
        var perspective = name == "perspective";
        if (perspective)
        {
            if (!PerspectiveReturnViews.TryGetValue(viewport.Id, out var saved)) return;
            CameraAnimator.Animate(view, saved.Projection, () =>
            {
                viewport.SetViewProjection(saved.Projection, false);
                viewport.SetCameraLocations(saved.Target, saved.Location);
                viewport.SetCameraDirection(saved.Direction, false);
                viewport.CameraUp = saved.Up;
                viewport.Camera35mmLensLength = saved.Lens;
                PerspectiveReturnViews.Remove(viewport.Id);
                view.Redraw();
            });
            return;
        }
        Vector3d eye, up;
        (eye, up) = name switch
        {
            "top" => (Vector3d.ZAxis, Vector3d.YAxis),
            "bottom" => (-Vector3d.ZAxis, -Vector3d.YAxis),
            "front" => (-Vector3d.YAxis, Vector3d.ZAxis),
            "back" => (Vector3d.YAxis, Vector3d.ZAxis),
            "right" => (Vector3d.XAxis, Vector3d.ZAxis),
            "left" => (-Vector3d.XAxis, Vector3d.ZAxis),
            _ => (-viewport.CameraDirection, viewport.CameraUp)
        };
        eye.Unitize();
        up.Unitize();
        var lens = PreferredLenses.TryGetValue(viewport.Id, out var savedLens) ? savedLens : 50.0;
        var projection = new ViewportInfo(viewport);
        if (!projection.IsParallelProjection)
            projection.ChangeToParallelProjection(true);

        var box = VisibleObjectBounds(view);
        var target = box.IsValid ? box.Center : viewport.CameraTarget;
        var distance = Math.Max(viewport.CameraLocation.DistanceTo(viewport.CameraTarget), 1);
        if (box.IsValid)
        {
            var aspect = Math.Max(viewport.Size.Width / (double)Math.Max(viewport.Size.Height, 1), 0.1);
            var radius = Math.Max(box.Diagonal.Length * 0.5, 1);
            var halfAngle = Math.Atan(18.0 / (lens * Math.Max(aspect, 1.0)));
            distance = Math.Max(radius * 1.2 / Math.Sin(halfAngle), radius * 2);
        }
        projection.TargetPoint = target;
        projection.SetCameraLocation(target + eye * distance);
        projection.SetCameraDirection(-eye);
        projection.SetCameraUp(up);
        if (box.IsValid &&
            projection.GetFrustum(out _, out _, out _, out _, out var near, out var far))
        {
            var right = Vector3d.CrossProduct(-eye, up);
            right.Unitize();
            var vertical = Vector3d.CrossProduct(eye, right);
            vertical.Unitize();
            var halfWidth = 0.0;
            var halfHeight = 0.0;
            foreach (var corner in box.GetCorners())
            {
                var offset = corner - target;
                halfWidth = Math.Max(halfWidth, Math.Abs(offset * right));
                halfHeight = Math.Max(halfHeight, Math.Abs(offset * vertical));
            }
            halfHeight = Math.Max(Math.Max(halfHeight, halfWidth / Math.Max(viewport.Size.Width / (double)Math.Max(viewport.Size.Height, 1), 0.1)) * 1.2, 1);
            halfWidth = halfHeight * viewport.Size.Width / Math.Max(viewport.Size.Height, 1);
            projection.SetFrustum(-halfWidth, halfWidth, -halfHeight, halfHeight, near, far);
        }
        if (box.IsValid) projection.SetFrustumNearFar(box);
        CameraAnimator.Animate(view, projection, view.Redraw);
    }

    private static BoundingBox VisibleObjectBounds(RhinoView view)
    {
        var result = BoundingBox.Empty;
        foreach (var obj in view.Document.Objects)
        {
            if (!obj.Visible) continue;
            var box = obj.Geometry.GetBoundingBox(true);
            if (box.IsValid) result.Union(box);
        }
        return result;
    }

    private static Point3d RotationTarget(RhinoView view)
    {
        var bounds = BoundingBox.Empty;
        foreach (var obj in view.Document.Objects.GetSelectedObjects(false, false))
        {
            var box = obj.Geometry.GetBoundingBox(true);
            if (box.IsValid) bounds.Union(box);
        }
        var center = bounds.IsValid ? bounds.Center : view.ActiveViewport.CameraTarget;
        return center.DistanceTo(view.ActiveViewport.CameraLocation) > 1e-6
            ? center : view.ActiveViewport.CameraTarget;
    }

    private static void RotateYaw(RhinoView view, int turn)
    {
        var viewport = view.ActiveViewport;
        var target = RotationTarget(view);
        var eye = viewport.CameraLocation - target;
        var up = viewport.CameraUp;
        eye.Rotate(turn * Math.PI / 2, Vector3d.ZAxis);
        up.Rotate(turn * Math.PI / 2, Vector3d.ZAxis);
        var projection = MakeNavigationProjection(view, eye, up, false, target);
        if (projection is null) return;
        CameraAnimator.Animate(view, projection, view.Redraw, orbitCenter: target);
    }

    internal static bool Contains(RhinoViewport viewport, Point point) =>
        Enabled && CubeFrame.From(viewport).Contains(point);

    private readonly struct CubeFrame
    {
        private const float Scale = 30f;
        public PointF Center { get; }
        private Vector3d Right { get; }
        private Vector3d Up { get; }
        private Vector3d Eye { get; }

        private CubeFrame(PointF center, Vector3d right, Vector3d up, Vector3d eye)
        {
            Center = center;
            Right = right;
            Up = up;
            Eye = eye;
        }

        public static CubeFrame From(RhinoViewport viewport)
        {
            var eye = -viewport.CameraDirection;
            eye.Unitize();
            var up = viewport.CameraUp;
            up.Unitize();
            var right = Vector3d.CrossProduct(-eye, up);
            right.Unitize();
            up = Vector3d.CrossProduct(eye, right);
            up.Unitize();
            return new CubeFrame(new PointF(viewport.Size.Width - 100, 95), right, up, eye);
        }

        public bool Contains(Point p) => (Math.Abs(p.X - Center.X) <= 57 && Math.Abs(p.Y - Center.Y) <= 57) || HitTurnButton(p) != 0 || HitResetButton(p) || HitAutoFaceButton(p);

        public PointF ResetButton() => new(Center.X + 56, Center.Y + 56);

        public bool HitResetButton(Point p) => DistanceSquared(ResetButton(), p) <= 15 * 15;

        public PointF AutoFaceButton() => new(Center.X - 56, Center.Y + 56);

        public bool HitAutoFaceButton(Point p) => DistanceSquared(AutoFaceButton(), p) <= 15 * 15;

        public PointF TurnButton(int turn) => new(Center.X + 78 * turn, 42);

        public int HitTurnButton(Point p)
        {
            foreach (var turn in new[] { -1, 1 })
                if (DistanceSquared(TurnButton(turn), p) <= 18 * 18) return turn;
            return 0;
        }

        public PointF Project(Vector3d point) => new(
            Center.X + Scale * (float)(point * Right),
            Center.Y - Scale * (float)(point * Up));

        public IEnumerable<Vector3d> VisibleFaces()
        {
            foreach (var face in new[] { Vector3d.XAxis, -Vector3d.XAxis,
                         Vector3d.YAxis, -Vector3d.YAxis, Vector3d.ZAxis, -Vector3d.ZAxis })
                if (face * Eye > 0.01) yield return face;
        }

        public PointF[] FaceCorners(Vector3d normal)
        {
            var a = Math.Abs(normal.X) > 0.5 ? Vector3d.YAxis : Vector3d.XAxis;
            var b = Math.Abs(normal.Z) > 0.5 ? Vector3d.YAxis : Vector3d.ZAxis;
            return new[] { Project(normal - a - b), Project(normal + a - b),
                           Project(normal + a + b), Project(normal - a + b) };
        }

        public static string Label(Vector3d normal) => normal.X > 0.5 ? "右" : normal.X < -0.5 ? "左" :
            normal.Y > 0.5 ? "後" : normal.Y < -0.5 ? "前" : normal.Z > 0 ? "上" : "下";

        public static Color FaceColor(Vector3d normal) => normal.Z > 0.5 ? Color.White :
            normal.X > 0.5 || normal.Y < -0.5 ? Color.FromArgb(250, 250, 250) :
            Color.FromArgb(240, 240, 240);

        public IEnumerable<Vector3d> VisibleCorners()
        {
            foreach (var x in new[] { -1, 1 })
            foreach (var y in new[] { -1, 1 })
            foreach (var z in new[] { -1, 1 })
            {
                var corner = new Vector3d(x, y, z);
                if (VisiblePoint(corner)) yield return corner;
            }
        }

        public IEnumerable<Vector3d> VisibleEdges()
        {
            foreach (var zeroAxis in new[] { 0, 1, 2 })
            foreach (var a in new[] { -1, 1 })
            foreach (var b in new[] { -1, 1 })
            {
                var edge = zeroAxis == 0 ? new Vector3d(0, a, b) :
                           zeroAxis == 1 ? new Vector3d(a, 0, b) : new Vector3d(a, b, 0);
                if (VisiblePoint(edge)) yield return edge;
            }
        }

        public Vector3d EdgeAxis(Vector3d edge) => edge.X == 0 ? Vector3d.XAxis :
            edge.Y == 0 ? Vector3d.YAxis : Vector3d.ZAxis;

        public Vector3d HitTest(Point p)
        {
            // Corners and edges take precedence over face interiors.
            foreach (var corner in VisibleCorners())
            {
                if (DistanceSquared(Project(corner), p) > 100) continue;
                return corner;
            }

            foreach (var edge in VisibleEdges())
            {
                if (DistanceSquared(Project(edge), p) > 144) continue;
                return edge;
            }

            foreach (var face in VisibleFaces())
                if (InsidePolygon(FaceCorners(face), p)) return face;
            return Vector3d.Zero;
        }

        private bool VisiblePoint(Vector3d p) =>
            (p.X > 0 && Eye.X > 0.01) || (p.X < 0 && Eye.X < -0.01) ||
            (p.Y > 0 && Eye.Y > 0.01) || (p.Y < 0 && Eye.Y < -0.01) ||
            (p.Z > 0 && Eye.Z > 0.01) || (p.Z < 0 && Eye.Z < -0.01);

        private static double DistanceSquared(PointF a, Point b) =>
            (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

        private static bool InsidePolygon(PointF[] corners, Point p)
        {
            var inside = false;
            for (var i = 0; i < corners.Length; i++)
            {
                var j = (i + corners.Length - 1) % corners.Length;
                if ((corners[i].Y > p.Y) == (corners[j].Y > p.Y)) continue;
                var crossing = (corners[j].X - corners[i].X) * (p.Y - corners[i].Y) /
                               (corners[j].Y - corners[i].Y) + corners[i].X;
                if (p.X < crossing) inside = !inside;
            }
            return inside;
        }
    }
}
