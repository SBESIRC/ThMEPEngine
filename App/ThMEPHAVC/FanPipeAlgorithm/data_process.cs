using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;

using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanPipeAlgorithm;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    class data_process
    {

        public data_process() {

        }

        //找到模型方向
        public void find_model_direction(ThFanCUModel model, ref double angle, ref Vector3d tmp_vector) {

            List<Line> tmp_poly = model.FanObb.ToLines();
            double max_length = 0;
            Line long_edge = new Line();
            for (int i = 0; i < tmp_poly.Count; i++) {
                if (tmp_poly[i].Length > max_length) {
                    max_length = tmp_poly[i].Length;
                    long_edge = tmp_poly[i];
                }
            }

            Point3d start = new Point3d(long_edge.StartPoint.X, long_edge.StartPoint.Y, 0);
            Point3d end = new Point3d(long_edge.EndPoint.X, long_edge.EndPoint.Y, 0);

            Line new_line = new Line();
            if (start.DistanceTo(model.FanPoint) < end.DistanceTo(model.FanPoint))
            {
                new_line = new Line(end, start);
            }
            else
            {
                new_line = long_edge;
            }

            angle = new_line.Angle;
            tmp_vector = new Vector3d(new_line.EndPoint.X - new_line.StartPoint.X, new_line.EndPoint.Y - new_line.StartPoint.Y, 0);
        }



        //调整区域内的点
        public void rotate_area(double angle, Point3d minxy,ref List<Point3d> real_end_points,ref Point3d real_start_point, ref List<edge> boundary, ref List<edge> hole)
        {
            //旋转终点
            List<Point3d> new_ends = new List<Point3d>(); 
            for (int i = 0; i < real_end_points.Count; i++) 
            {
                new_ends.Add(rotate_point(real_end_points[i], angle, minxy));
            }
            real_end_points = new_ends;

            //旋转起点
            real_start_point = rotate_point(real_start_point, angle, minxy);

            //旋转框线
            List<edge> new_boundary = new List<edge>();
            for (int i = 0; i < boundary.Count; i++)
            {
                Point3d old_start = new Point3d(boundary[i].rx1, boundary[i].ry1, 0);
                Point3d old_end = new Point3d(boundary[i].rx2, boundary[i].ry2, 0);
                Point3d new_start = rotate_point(old_start, angle, minxy);
                Point3d new_end= rotate_point(old_end, angle, minxy);
                new_boundary.Add(new edge(new_start.X, new_start.Y, new_end.X, new_end.Y));
            }
            boundary = new_boundary;

            //旋转hole 
            List<edge> new_hole = new List<edge>();
            for (int i = 0; i < hole.Count; i++)
            {
                Point3d old_start = new Point3d(hole[i].rx1, hole[i].ry1, 0);
                Point3d old_end = new Point3d(hole[i].rx2, hole[i].ry2, 0);
                Point3d new_start = rotate_point(old_start, angle, minxy);
                Point3d new_end = rotate_point(old_end, angle, minxy);
                new_hole.Add(new edge(new_start.X, new_start.Y, new_end.X, new_end.Y));
            }
            hole = new_hole;

        }

        public Point3d rotate_point(Point3d old_point, double angle, Point3d minxy) 
        {

            double minx = minxy.X;
            double miny = minxy.Y;

            double oldx = old_point.X;
            double oldy = old_point.Y;

            double newx = (oldx - minx) * Math.Cos(angle) - (oldy - miny) * Math.Sin(angle) + minx;
            double newy = (oldx - minx) * Math.Sin(angle) + (oldy - miny) * Math.Cos(angle) + miny;
            return new Point3d(newx, newy, 0);
        }



        public void rotate_edgelist(double angle, Point3d minxy, ref List<edge> edges_out) 
        {
            List<edge> new_edges_out = new List<edge>();
            for (int i = 0; i < edges_out.Count; i++)
            {
                Point3d old_start = new Point3d(edges_out[i].rx1, edges_out[i].ry1, 0);
                Point3d old_end = new Point3d(edges_out[i].rx2, edges_out[i].ry2, 0);
                Point3d new_start = rotate_point(old_start, angle, minxy);
                Point3d new_end = rotate_point(old_end, angle, minxy);
                new_edges_out.Add(new edge(new_start.X, new_start.Y, new_end.X, new_end.Y));
            }
            edges_out = new_edges_out;
        }


        //调整终点和起点（向外延申出一段线）
        void adjust_point() { 
        


        }


    }
}
