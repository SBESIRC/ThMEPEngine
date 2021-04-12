using DotNetARX;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPHVAC.Model
{
    // 完成每个末端duct风口数量的分配策略
    class ThDuctPortsAnalysis
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private DBObjectCollection Center_lines { get; set; }
        private List<Line> End_lines { get; set; }
        private Line In_line { get; set; }
        
        public ThDuctPortsAnalysis(DBObjectCollection center_lines_, 
                                   DBObjectCollection start_line_, 
                                   int ports_num,
                                   double len_floor)
        {
            End_lines = new List<Line>();
            In_line = start_line_[0] as Line;
            Center_lines = center_lines_;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(Center_lines);
            Search_endline(Center_lines);
            End_lines.Sort(delegate(Line a, Line b) {return a.Length.CompareTo(b.Length);});
            int ind = End_lines.FindIndex(u => (u.Length - len_floor) < 1e-9);

        }

        private Point3d Get_start_serach_point()
        {
            Point3d p = In_line.StartPoint;
            var poly = new Polyline();
            poly.CreatePolygon(p.ToPoint2D(), 4, 10);
            var res = SpatialIndex.SelectCrossingPolygon(poly);
            // Count为1说明该点是全图的入口
            return res.Count == 1 ? In_line.EndPoint: In_line.StartPoint;
        }
        private void Search_endline(DBObjectCollection lines)
        {
            Point3d p = Get_start_serach_point();
            Do_search(p, Center_lines[0] as Line);
        }
        private void Do_search(Point3d searchpoint, Line currentline)
        {
            var poly = new Polyline();
            poly.CreatePolygon(searchpoint.ToPoint2D(), 4, 10);

            //执行循环探测
            var res = SpatialIndex.SelectCrossingPolygon(poly);
            if (res.Count == 1)
            {
                End_lines.Add(currentline);
                return;
            }
            foreach (Line l in res)
            {
                Point3d p = searchpoint.IsEqualTo(l.StartPoint) ? l.EndPoint : l.StartPoint;
                Do_search(p, l);
            }
        }
    }
}
