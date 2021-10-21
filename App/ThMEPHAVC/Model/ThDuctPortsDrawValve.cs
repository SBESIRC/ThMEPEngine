using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawValve
    {
        public string valve_name;
        public string valve_layer;
        public string valve_visibility;
        public ThDuctPortsDrawValve(string valve_visibility, string valve_name, string valve_layer)
        {
            this.valve_name = valve_name;
            this.valve_layer = valve_layer;
            this.valve_visibility = valve_visibility;
        }
        public void Insert_valve(int count, Point3d start_point, ThDuctPortsConstructor endlines)
        {
            if (count == 1)
                return ;
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var endline in endlines.endline_segs)
                {
                    if (endline.is_in)
                    {
                        double width = endline.segs[0].width;
                        var dir_vec = ThMEPHVACService.Get_edge_direction(endline.segs[0].l);
                        var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                        double angle = dir_vec.GetAngleTo(Vector3d.XAxis);
                        if (Vector3d.XAxis.CrossProduct(dir_vec).Z < 0)
                            angle = 2 * Math.PI - angle;
                        angle += 0.5 * Math.PI;
                        double text_angle = (angle >= Math.PI * 0.5) ? Math.PI * 0.5 : 0;
                        var p = endline.segs[0].start_point;
                        var insert_p = p + vertical_r * width * 0.5 + start_point.GetAsVector();
                        Insert_valve(insert_p.ToPoint2D(), width, angle, text_angle);
                    }
                }
            }
        }
        public void Insert_valve(Point2d insert_p, double width, double angle, double text_angle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var insert_p_3 = new Point3d(insert_p.X, insert_p.Y, 0);
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valve_layer, valve_name, insert_p_3, new Scale3d(), angle);
                ThDuctPortsDrawService.Set_valve_dyn_block_properity(obj, width, 250, text_angle, valve_visibility);
            }
        }
        public void Insert_hole(Point2d insert_p, double width, double len, double angle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var insert_p_3 = new Point3d(insert_p.X, insert_p.Y, 0);
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valve_layer, valve_name, insert_p_3, new Scale3d(), angle);
                ThDuctPortsDrawService.Set_hole_dyn_block_properity(obj, width, len);
            }
        }
        public void Insert_muffler(Point2d insert_p, MufflerModifyParam muffler)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var insert_p_3 = new Point3d(insert_p.X, insert_p.Y, 0);
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(valve_layer, valve_name, insert_p_3, new Scale3d(), muffler.rotate_angle);
                ThDuctPortsDrawService.Set_muffler_dyn_block_properity(obj, muffler);
            }
        }
    }
}
