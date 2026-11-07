using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace OnonSmartView;

public sealed class ononSmartViewAutoFaceCommand : Command
{
    public override string EnglishName => "ononSmartViewAutoFace";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var plugin = ononSmartViewPlugin.Instance;
        if (plugin is null) return Result.Failure;
        plugin.SetAutoFaceEnabled(!plugin.AutoFaceEnabled);
        RhinoApp.WriteLine($"ononSmartView automatic work-plane facing {(plugin.AutoFaceEnabled ? "enabled" : "disabled")}.");
        return Result.Success;
    }
}

public sealed class ononSmartViewCommand : Command
{
    public override string EnglishName => "ononSmartView";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var pick = new GetObject { GeometryFilter = ObjectType.Surface, SubObjectSelect = true };
        pick.SetCommandPrompt("Select a Brep face to focus");
        pick.Get();
        if (pick.CommandResult() != Result.Success) return pick.CommandResult();
        var view = pick.View();
        if (view is null) return Result.Failure;
        var objRef = pick.Object(0);
        var face = objRef.Face();
        if (face is null)
        {
            RhinoApp.WriteLine("ononSmartView requires a Brep face selection.");
            return Result.Failure;
        }

        var focusPoint = objRef.SelectionPoint();
        if (!focusPoint.IsValid)
        {
            focusPoint = face.PointAt(face.Domain(0).Mid, face.Domain(1).Mid);
        }

        return FaceFocus.Focus(doc, view, face, focusPoint);
    }
}

// A scriptable entry point lets Rhino MCP and future ViewCube picks use the same focus path.
public sealed class ononSmartViewFocusFaceCommand : Command
{
    public override string EnglishName => "ononSmartViewFocusFace";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var idText = string.Empty;
        var result = RhinoGet.GetString("Brep object GUID", false, ref idText);
        if (result != Result.Success) return result;
        if (!Guid.TryParse(idText, out var id) || doc.Objects.FindId(id)?.Geometry is not Brep brep)
        {
            RhinoApp.WriteLine("The ID does not identify a Brep in this document.");
            return Result.Failure;
        }

        var faceIndex = 0;
        result = RhinoGet.GetInteger("Zero-based face index", false, ref faceIndex, 0, brep.Faces.Count - 1);
        if (result != Result.Success) return result;
        var viewName = string.Empty;
        result = RhinoGet.GetString("Viewport name", false, ref viewName);
        if (result != Result.Success) return result;
        var view = doc.Views.Find(viewName, false);
        if (view is null)
        {
            RhinoApp.WriteLine($"Viewport '{viewName}' was not found.");
            return Result.Failure;
        }

        var face = brep.Faces[faceIndex];
        var focusPoint = face.PointAt(face.Domain(0).Mid, face.Domain(1).Mid);
        return FaceFocus.Focus(doc, view, face, focusPoint);
    }
}

internal static class FaceFocus
{
    public static Result Focus(RhinoDoc doc, RhinoView view, BrepFace face, Point3d pickedPoint)
    {
        if (FocusSessions.GetFor(doc) is not null)
        {
            RhinoApp.WriteLine("Run ononSmartViewBack before focusing another face.");
            return Result.Failure;
        }

        if (!face.ClosestPoint(pickedPoint, out var u, out var v))
        {
            RhinoApp.WriteLine("Could not locate the point on the selected face.");
            return Result.Failure;
        }
        var focusPoint = face.PointAt(u, v);
        if (!TryGetFacePlane(face, focusPoint, doc.ModelAbsoluteTolerance, out var plane))
        {
            RhinoApp.WriteLine("Could not calculate a stable tangent plane at the picked point.");
            return Result.Failure;
        }

        var box = face.GetBoundingBox(plane);
        var width = Math.Max(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y);
        var distance = Math.Max(width * 2.2, doc.ModelAbsoluteTolerance * 1000);
        var session = FocusSession.Capture(view, plane, focusPoint, distance);
        FocusSessions.Set(session);
        view.ActiveViewport.SetConstructionPlane(plane);
        var appliedPlane = view.ActiveViewport.GetConstructionPlane().Plane;
        if (appliedPlane.IsValid) session.FocusPlane = appliedPlane;
        var target = CameraAnimator.MakeFocusProjection(session, false);
        CameraAnimator.Animate(view, target, () =>
        {
            var viewport = view.ActiveViewport;
            var actualPlane = viewport.GetConstructionPlane().Plane;
            if (actualPlane.IsValid) session.FocusPlane = actualPlane;
            CameraAnimator.AlignToPlane(viewport, session.FocusPlane,
                session.FocusTarget, session.FocusDistance);
            view.Redraw();
        });
        return Result.Success;
    }

    internal static bool TryGetFacePlane(BrepFace face, Point3d point, double tolerance, out Plane plane)
    {
        if (face.TryGetPlane(out plane, tolerance))
        {
            plane.Origin = point;
            return true;
        }

        var closest = face.ClosestPoint(point, out var u, out var v);
        if (!closest || !face.FrameAt(u, v, out plane)) return false;
        plane.Origin = face.PointAt(u, v);
        return plane.IsValid;
    }
}

public sealed class ononSmartViewBackCommand : Command
{
    public override string EnglishName => "ononSmartViewBack";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var session = FocusSessions.GetFor(doc);
        if (session is null)
        {
            RhinoApp.WriteLine("There is no active ononSmartView focus to restore.");
            return Result.Nothing;
        }

        CameraAnimator.Animate(session.View, session.OriginalProjection, () =>
        {
            session.Restore();
            FocusSessions.Clear();
        });
        return Result.Success;
    }
}

public sealed class ononSmartViewFlipCommand : Command
{
    public override string EnglishName => "ononSmartViewFlip";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var session = FocusSessions.GetFor(doc);
        if (session is null)
        {
            RhinoApp.WriteLine("There is no active ononSmartView focus to flip.");
            return Result.Nothing;
        }

        var view = session.View;
        var current = new ViewportInfo(view.ActiveViewport);
        var flipToBack = view.ActiveViewport.CameraDirection * session.FocusPlane.Normal < 0;
        var flipped = CameraAnimator.MakeFocusProjection(session, flipToBack);
        CameraAnimator.Animate(view, flipped, () =>
        {
            var viewport = view.ActiveViewport;
            viewport.SetConstructionPlane(session.FocusPlane);
            var actualPlane = viewport.GetConstructionPlane().Plane;
            if (actualPlane.IsValid) session.FocusPlane = actualPlane;
            CameraAnimator.AlignToPlane(viewport, session.FocusPlane,
                session.FocusTarget, session.FocusDistance, flipToBack);
            view.Redraw();
        }, current);
        return Result.Success;
    }
}

internal static class FocusSessions
{
    private static FocusSession? _current;
    public static FocusSession? GetFor(RhinoDoc doc)
    {
        if (_current is not null && _current.View.Document.RuntimeSerialNumber != doc.RuntimeSerialNumber)
            _current = null;
        return _current;
    }
    public static void Set(FocusSession session) => _current = session;
    public static void Clear() => _current = null;
}
