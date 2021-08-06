using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsCheckCircle
    {
        Tolerance tor;
        List<Line> edges;
        Point3d start_point;
        ThCADCoreNTSSpatialIndex index;
        
        public ThDuctPortsCheckCircle(DBObjectCollection lines, Point3d srt_p)
        {
            start_point = srt_p;
            edges = new List<Line>();
            tor = new Tolerance(1.1, 1.1);
            index = new ThCADCoreNTSSpatialIndex(lines);
        }
        public bool Have_circle(DBObjectCollection lines)
        {
            var start_line = new Line();
            foreach(Line l in lines)
            {
                if (start_point.IsEqualTo(l.StartPoint, tor) || start_point.IsEqualTo(l.EndPoint, tor))
                {
                    start_line = l;
                    break;
                }
            }
            if (start_line.Length < 1e-3)
                return false;
            var dis_mat = Matrix3d.Displacement(-start_point.GetAsVector());
            foreach (Line l in lines)
                l.TransformBy(dis_mat);
            Build_graph(start_point);
            return true;
        }
        // 根据线的方向确定sp和ep
        private void Build_graph(Point3d srt_p)
        {
            var q = new Queue<Point3d>();
            var set = new HashSet<Point3d>();
            q.Enqueue(srt_p);
            while (q.Count != 0)
            {
                var p = q.Dequeue();
                if (!set.Add(srt_p))
                    return;
                var detect_poly = ThDuctPortsService.Create_detect_poly(p);
                var res = index.SelectCrossingPolygon(detect_poly);
                foreach (Line l in res)
                {
                    var end_p = l.StartPoint.IsEqualTo(p, tor) ? l.EndPoint : l.StartPoint;
                    q.Enqueue(end_p);
                    edges.Add(new Line(p, end_p));
                }
            }
        }
    }
}
