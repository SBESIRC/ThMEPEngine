using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;

namespace ThMEPHVAC.Model
{
    public class ThMEPHVACLineProc
    {
        public static DBObjectCollection Explode(DBObjectCollection lines)
        {
            return ThLaneLineEngine.Explode(lines);
        }
        public static DBObjectCollection PreProc(DBObjectCollection lines)
        {
            if (lines.Count == 0)
                return new DBObjectCollection();
            var service = new ThLaneLineCleanService();
            lines = service.CleanNoding(lines);
            return lines;
        }
        public static void LineMerge(ref DBObjectCollection line_set, double gap_tor)
        {
            var lines = new DBObjectCollection();
            foreach (Line l in line_set)
                lines.Add(l);
            var set = new HashSet<string>();
            var tor = new Tolerance(1.5, 1.5);
            foreach (Line l in lines)
            {
                foreach (Line o_l in lines)
                {
                    if (!ThMEPHVACService.IsSameLine(l, o_l, tor))
                    {
                        var v1 = ThMEPHVACService.GetEdgeDirection(l);
                        var v2 = ThMEPHVACService.GetEdgeDirection(o_l);
                        if (ThMEPHVACService.IsCollinear(v1, v2))
                        {
                            var dis = ThMEPHVACService.GetLineDis(l.StartPoint, l.EndPoint, o_l.StartPoint, o_l.EndPoint);
                            if (Math.Abs(dis) < gap_tor)
                            {
                                line_set.Remove(l);
                                line_set.Remove(o_l);
                                ThMEPHVACService.GetLongestDis(l.StartPoint, l.EndPoint, o_l.StartPoint, o_l.EndPoint, out Point3d p1, out Point3d p2);
                                var s = (ThMEPHVACService.RoundPoint(p1, 6) + ThMEPHVACService.RoundPoint(p2, 6).GetAsVector()).ToString();
                                if (set.Add(s))
                                    line_set.Add(new Line(p1, p2));
                            }
                        }
                    }
                }
            }
            line_set = PreProc(line_set);
        }
    }
}
