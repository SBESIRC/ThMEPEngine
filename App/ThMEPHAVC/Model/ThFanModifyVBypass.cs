using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThFanModifyVBypass
    {
        public ThVTee vt;
        private Tolerance tor;
        private Matrix2d dis_mat;
        private List<VT_elbow_modify_param> vt_elbows;
        private ThModifyDuctConnComponent duct_conn_service;
        public ThFanModifyVBypass(ObjectId[] ids, string modify_size, Duct_modify_param param)
        {
            using (var db = AcadDatabase.Active())
            {
                tor = new Tolerance(1.5, 1.5);
                var start_id = new ObjectId[] { db.Database.GetObjectId(false, param.start_handle, 0) };
                ThDuctPortsInterpreter.Get_basic_param(start_id, out ThMEPHVACParam flag_param, out Point2d start_point);
                ThDuctPortsInterpreter.Get_vt_elbow(out vt_elbows);
                Update_v_elbow_pos(start_point);
                Get_duct_end_point(ids, out Point2d f_detect, out Point2d l_detect);
                Clear_bypass(ids[0], f_detect, l_detect);
                Get_vt_pos(f_detect, l_detect, param.duct_size, out Point3d i_vt_pos, out Point3d o_vt_pos);
                vt = new ThVTee(i_vt_pos, o_vt_pos, modify_size);
                var p = new Point3d(start_point.X, start_point.Y, 0);
                var vt_pinter = new ThDrawVBypass(param.air_volume, flag_param.scale, flag_param.scenario, p, start_id[0], modify_size, param.elevation.ToString());
                var line_type = Get_line_type(ids[0]);
                if (!(line_type == ThHvacCommon.DASH_LINETYPE))
                    vt_pinter.Draw_4vertical_bypass(vt.vt_elbow, i_vt_pos, o_vt_pos);
                else
                    vt_pinter.Draw_5vertical_bypass(vt.vt_elbow, i_vt_pos, o_vt_pos);
                duct_conn_service = new ThModifyDuctConnComponent(p);
                var l = new Line(i_vt_pos, o_vt_pos);
                var pl = ThMEPHVACService.Get_line_extend(l, 10);
                var width = ThMEPHVACService.Get_width(modify_size);
                duct_conn_service.Update_valve(pl, Point2d.Origin, width);// 只更新电动多叶调节阀，不需要给new_p
            }
        }
        private void Update_v_elbow_pos(Point2d start_point)
        {
            dis_mat = Matrix2d.Displacement(-start_point.GetAsVector());
            foreach (var e in vt_elbows)
                e.detect_p = e.detect_p.TransformBy(dis_mat);
        }
        private void Get_vt_pos(Point2d f_detect, 
                                Point2d l_detect, 
                                string org_size,
                                out Point3d i_vt_pos,
                                out Point3d o_vt_pos)
        {
            var sp = new Point3d(f_detect.X, f_detect.Y, 0);
            var ep = new Point3d(l_detect.X, l_detect.Y, 0);
            var dir_vec = (ep - sp).GetNormal();
            ThMEPHVACService.Seperate_size_info(org_size, out double w, out double h);
            i_vt_pos = sp - (dir_vec * h * 0.5);
            o_vt_pos = ep + (dir_vec * h * 0.5);
        }
        private void Clear_bypass(ObjectId id, Point2d f_detect, Point2d l_detect)
        {
            foreach (var elbow in vt_elbows)
            {
                if (elbow.detect_p.IsEqualTo(f_detect, tor))
                    ThDuctPortsDrawService.Clear_graph(elbow.handle);
                if (elbow.detect_p.IsEqualTo(l_detect, tor))
                    ThDuctPortsDrawService.Clear_graph(elbow.handle);
            }
            ThDuctPortsDrawService.Remove_group_by_comp(id);
        }
        private void Get_duct_end_point(ObjectId[] duct_comp_ids, out Point2d srt_p, out Point2d end_p)
        {
            srt_p = Point2d.Origin;
            end_p = Point2d.Origin;
            if (duct_comp_ids.Length == 9)
            {
                using (var db = AcadDatabase.Active())
                {
                    var sp = db.Element<Entity>(duct_comp_ids[5]) as DBPoint;
                    var ep = db.Element<Entity>(duct_comp_ids[6]) as DBPoint;
                    srt_p = sp.Position.ToPoint2D().TransformBy(dis_mat);
                    end_p = ep.Position.ToPoint2D().TransformBy(dis_mat);
                }
            }
        }
        private string Get_line_type(ObjectId id)
        {
            using (var db = AcadDatabase.Active())
            {
                var l = db.Element<Entity>(id) as Line;
                return l.Linetype;
            }
        }
    }
}
