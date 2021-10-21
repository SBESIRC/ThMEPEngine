using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    class ThModifyDuctConnComponent
    {
        private Point3d move_srt_p;
        private ThCADCoreNTSSpatialIndex valves_index;
        private Dictionary<Polyline, ValveModifyParam> valves_dic;// 阀外包框到阀参数的映射
        private ThCADCoreNTSSpatialIndex mufflers_index;
        private Dictionary<Polyline, MufflerModifyParam> mufflers_dic;// 软接外包框到软接参数的映射
        private ThCADCoreNTSSpatialIndex holes_index;
        private Dictionary<Polyline, HoleModifyParam> holes_dic;// 开洞外包框到开洞参数的映射

        public ThModifyDuctConnComponent(Point3d move_srt_p)
        {
            Read_X_data();
            Move_to_org(move_srt_p);
        }
        private void Read_X_data()
        {
            ThDuctPortsInterpreter.Get_valves_dic(out valves_dic);
            ThDuctPortsInterpreter.Get_holes_dic(out holes_dic);
            ThDuctPortsInterpreter.Get_muffler_dic(out mufflers_dic);
        }
        private void Move_to_org(Point3d move_srt_p)
        {
            this.move_srt_p = move_srt_p;
            var m = Matrix3d.Displacement(-move_srt_p.GetAsVector());
            foreach (Polyline b in valves_dic.Keys.ToCollection())
                b.TransformBy(m);
            valves_index = new ThCADCoreNTSSpatialIndex(valves_dic.Keys.ToCollection());
            foreach (Polyline b in holes_dic.Keys.ToCollection())
                b.TransformBy(m);
            holes_index = new ThCADCoreNTSSpatialIndex(holes_dic.Keys.ToCollection());
            foreach (Polyline b in mufflers_dic.Keys.ToCollection())
                b.TransformBy(m);
            mufflers_index = new ThCADCoreNTSSpatialIndex(mufflers_dic.Keys.ToCollection());
        }
        public void Update_cur_duct_valve_hole(Point2d org_f_detect,
                                                Point2d f_detect,
                                                Point2d org_l_detect,
                                                Point2d l_detect,
                                                string cur_duct_size,
                                                string modify_duct_width)
        {
            double new_width = ThMEPHVACService.Get_width(modify_duct_width);
            var width = ThMEPHVACService.Get_width(cur_duct_size);
            var pl = ThMEPHVACService.Get_line_extend(org_f_detect, org_l_detect, width);
            Update_valve(pl, f_detect, new_width);// 多叶调节风阀都是在forward的位置
            Update_hole(pl, new_width);
            Update_muffler(pl, new_width);
        }
        public void Update_valve(Polyline detect_pl, Point2d new_air_valve_pos, double new_width)
        {
            var res = valves_index.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (valves_dic.ContainsKey(p))
                {
                    var param = valves_dic[p];
                    Do_update_valve(new_width, new_air_valve_pos, param);
                }
            }
        }
        private void Update_hole(Polyline detect_pl, double new_width)
        {
            var res = holes_index.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (holes_dic.ContainsKey(p))
                {
                    var param = holes_dic[p];
                    Do_update_hole(new_width + 100, param);
                }
            }
        }
        private void Update_muffler(Polyline detect_pl, double new_width)
        {
            var res = mufflers_index.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (mufflers_dic.ContainsKey(p))
                {
                    var param = mufflers_dic[p];
                    Do_update_muffler(new_width, param);
                }
            }
        }
        private void Do_update_muffler(double new_width, MufflerModifyParam muffler)
        {
            //洞和阀应该分开
            var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(muffler.rotate_angle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
            var muffler_service = new ThDuctPortsDrawValve("", muffler.name, muffler.muffler_layer);
            var insert_p = muffler.insert_p + vertical_r * (muffler.width - new_width - 200) * 0.5;
            muffler.width = new_width + 200;
            muffler_service.Insert_muffler(insert_p, muffler);
            ThDuctPortsDrawService.Clear_graph(muffler.handle);
        }
        private void Do_update_hole(double new_width, HoleModifyParam hole)
        {
            //洞和阀应该分开
            var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(hole.rotate_angle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
            var hole_service = new ThDuctPortsDrawValve("", hole.hole_name, hole.hole_layer);
            var insert_p = hole.insert_p + vertical_r * (hole.width - new_width) * 0.5;
            hole_service.Insert_hole(insert_p, new_width, hole.len, hole.rotate_angle);
            ThDuctPortsDrawService.Clear_graph(hole.handle);
        }
        private void Do_update_valve(double new_width, Point2d new_p, ValveModifyParam valve)
        {
            if (valve.valve_visibility == "多叶调节风阀")
            {
                var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = new_p + vertical_r * new_width * 0.5 + move_srt_p.ToPoint2D().GetAsVector();
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_visibility == "电动多叶调节风阀")
            {
                var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_visibility == "风管止回阀")
            {
                var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_name == "防火阀")
            {
                var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_l = ThMEPHVACService.Get_left_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_l * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
        }
    }
}
