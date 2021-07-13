using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace ThMEPEngineCore.Service
{
    public class ThDoorUtils
    {
        private static double AngleTolerance = 10.0;
        public static bool IsQualified(Line neighborLine, Line doorStoneLine)
        {
            //两直线夹角在一定范围内、间距小于间隔距离、门垛线的投影点要在相邻线上
            if (!IsValidAngle(neighborLine.LineDirection(), doorStoneLine.LineDirection()))
            {
                return false;
            }
            if (neighborLine.Distance(doorStoneLine) > ThMEPEngineCoreCommon.DoorStoneInterval)
            {
                return false;
            }
            var shortLine = doorStoneLine.ExtendLine(-2.0);
            var sp = shortLine.StartPoint.GetProjectPtOnLine(neighborLine.StartPoint, neighborLine.EndPoint);
            var ep = shortLine.EndPoint.GetProjectPtOnLine(neighborLine.StartPoint, neighborLine.EndPoint);

            return sp.IsPointOnLine(neighborLine, 1.0) && ep.IsPointOnLine(neighborLine, 1.0);
        }
        public static bool IsQualified(Polyline neighbor, Polyline doorStone)
        {
            var doorStoneLines = doorStone.ToLines();
            var neighborLines = neighbor.ToLines();
            for (int i = 0; i < doorStoneLines.Count; i++)
            {
                for (int j = 0; j < neighborLines.Count; j++)
                {
                    if (IsQualified(neighborLines[j], doorStoneLines[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static double GetDoorThick(Line line)
        {
            if(line.Length<ThMEPEngineCoreCommon.DoorMinimumThick)
            {
                return ThMEPEngineCoreCommon.DoorMinimumThick;
            }
            else if (line.Length > ThMEPEngineCoreCommon.DoorMaximumThick)
            {
                return ThMEPEngineCoreCommon.DoorMaximumThick;
            }
            else
            {
                return line.Length;
            }
        }
        public static bool IsValidAngle(Vector3d vec1, Vector3d vec2)
        {
            var rad = vec1.GetAngleTo(vec2);
            var ang = (rad / Math.PI) * 180.0;
            ang = Math.Min(ang,180-ang);
            return ang <= AngleTolerance;
        }
    }
}
