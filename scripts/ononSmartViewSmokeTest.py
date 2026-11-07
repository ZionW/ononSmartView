"""Run inside Rhino 8 Python editor after installing ononSmartView.rhp.

Creates a box plus a separate slanted planar face, then checks command
registration and Auto CPlane state. Face picking remains an interactive step.
"""
import Rhino
import Rhino.Geometry as rg
import scriptcontext as sc

box = rg.Box(rg.Plane.WorldXY, rg.Interval(-5, 5), rg.Interval(-5, 5), rg.Interval(0, 10))
box_id = sc.doc.Objects.AddBrep(box.ToBrep())

corners = [
    rg.Point3d(12, 0, 0),
    rg.Point3d(22, 0, 0),
    rg.Point3d(22, 10, 4),
    rg.Point3d(12, 10, 4),
]
slanted_face = rg.Brep.CreateFromCornerPoints(corners[0], corners[1], corners[2], corners[3], sc.doc.ModelAbsoluteTolerance)
slanted_id = sc.doc.Objects.AddBrep(slanted_face)
sc.doc.Views.Redraw()

expected = {"ononSmartView", "ononSmartViewFlip", "ononSmartViewBack"}
missing = {name for name in expected if not Rhino.Commands.Command.IsCommand(name)}
print("ononSmartView smoke test geometry created:", box_id, slanted_id)
print("Missing commands:", sorted(missing))
print("Rhino version:", Rhino.RhinoApp.Version)
print("Auto CPlane preference (inspect before/after focus): open Rhino Options > Modeling Aids > Auto CPlane")
if missing:
    raise Exception("ononSmartView commands are not registered: " + ", ".join(sorted(missing)))
print("Select a box face and run ononSmartView; then test ononSmartViewFlip and ononSmartViewBack.")
