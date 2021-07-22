using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    class ThDuctPortsPrimitiveFence
    {
        public static Polyline Create_line_fence(List<Line> lines)
        {
            var center_line = lines[4];
            var dir_vec = ThDuctPortsService.Get_2D_edge_direction(center_line);
            var dir_l = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            var dir_r = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            var poly = new Polyline();
            double extend = 10;
            poly.CreateRectangle(center_line.StartPoint.ToPoint2D() + dir_r * extend,
                                 center_line.EndPoint.ToPoint2D() + dir_l * extend);
            return poly;
        }
    }
}
