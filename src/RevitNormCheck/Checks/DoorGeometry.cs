using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitNormCheck.Checks
{
    // Стороны двери и сторона открывания, определённые по плану.
    public static class DoorGeometry
    {
        private const double ProbeDistanceFt = 2.6;   // ~0,8 м от двери в обе стороны
        private const double ProbeHeightFt = 3.3;     // ~1 м над уровнем

        public static Room RoomAt(CheckContext context, FamilyInstance door, bool front)
        {
            var point = (door.Location as LocationPoint)?.Point;
            if (point == null) return null;
            var level = context.LevelOf(door);
            var z = (level?.Elevation ?? point.Z) + ProbeHeightFt;
            var sign = front ? 1.0 : -1.0;
            var facing = door.FacingOrientation;
            var probe = new XYZ(point.X + sign * facing.X * ProbeDistanceFt,
                                point.Y + sign * facing.Y * ProbeDistanceFt, z);
            return context.Doc.GetRoomAtPoint(probe);
        }

        // Положительное число: дверь открывается в сторону FacingOrientation (front), отрицательное: назад.
        // null: на плане нет символической дуги открывания.
        public static double? SwingSide(FamilyInstance door, View view)
        {
            var origin = (door.Location as LocationPoint)?.Point;
            if (origin == null || view == null) return null;

            var geometry = door.get_Geometry(new Options { View = view, ComputeReferences = false });
            if (geometry == null) return null;

            var facing = door.FacingOrientation;
            foreach (var obj in geometry)
            {
                var instance = obj as GeometryInstance;
                if (instance == null) continue;
                foreach (var inner in instance.GetInstanceGeometry())
                {
                    var arc = inner as Arc;
                    if (arc == null) continue;
                    var mid = arc.Evaluate(0.5, true);
                    var dot = (mid.X - origin.X) * facing.X + (mid.Y - origin.Y) * facing.Y;
                    if (Math.Abs(dot) > 1e-3) return dot;
                }
            }
            return null;
        }
    }
}
