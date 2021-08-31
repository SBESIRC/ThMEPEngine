using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PtOnLine
    {
        public static bool PtIsOnLine(Point3d pt, Line line)//判断点是否在线上
        {
            if(line is null)
            {
                return false;
            }
            var tolerance = 10;
            if(line.GetClosestPointTo(pt, false).DistanceTo(pt) < tolerance)//点在线的内部
            {
                return true;
            }
            else//点在线的外部
            {
                if(line.StartPoint.DistanceTo(pt) < tolerance || line.StartPoint.DistanceTo(pt) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
