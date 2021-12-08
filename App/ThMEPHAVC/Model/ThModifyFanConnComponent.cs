using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    class ThModifyFanConnComponent
    {
        private Point3d moveSrtP;
        private ThCADCoreNTSSpatialIndex holesIndex;
        private ThCADCoreNTSSpatialIndex valvesIndex;
        private ThCADCoreNTSSpatialIndex mufflersIndex;
        private Dictionary<Polyline, HoleModifyParam> holesDic;      // 开洞外包框到开洞参数的映射
        private Dictionary<Polyline, ValveModifyParam> valvesDic;    // 阀外包框到阀参数的映射
        private Dictionary<Polyline, MufflerModifyParam> mufflersDic;// 消声器外包框到消声器参数的映射

        public ThModifyFanConnComponent(Point3d moveSrtP)
        {
            ReadXData();
            MoveToOrg(moveSrtP);
        }
        private void ReadXData()
        {
            ThDuctPortsInterpreter.GetValvesDic(out valvesDic);
            ThDuctPortsInterpreter.GetHolesDic(out holesDic);
            ThDuctPortsInterpreter.GetMufflerDic(out mufflersDic);
        }
        private void MoveToOrg(Point3d moveSrtP)
        {
            this.moveSrtP = moveSrtP;
            var m = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            foreach (Polyline b in valvesDic.Keys.ToCollection())
                b.TransformBy(m);
            valvesIndex = new ThCADCoreNTSSpatialIndex(valvesDic.Keys.ToCollection());
            foreach (Polyline b in holesDic.Keys.ToCollection())
                b.TransformBy(m);
            holesIndex = new ThCADCoreNTSSpatialIndex(holesDic.Keys.ToCollection());
            foreach (Polyline b in mufflersDic.Keys.ToCollection())
                b.TransformBy(m);
            mufflersIndex = new ThCADCoreNTSSpatialIndex(mufflersDic.Keys.ToCollection());
        }
        public void UpdateValve(Polyline detect_pl, Point2d new_air_valve_pos, double new_width)
        {
            var res = valvesIndex.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (valvesDic.ContainsKey(p))
                {
                    var param = valvesDic[p];
                    DoUpdateValve(new_width, new_air_valve_pos, param);
                }
            }
        }
        private void UpdateHole(Polyline detect_pl, double new_width)
        {
            var res = holesIndex.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (holesDic.ContainsKey(p))
                {
                    var param = holesDic[p];
                    DoUpdateHole(new_width + 100, param);
                }
            }
        }
        private void UpdateMuffler(Polyline detect_pl, double new_width)
        {
            var res = mufflersIndex.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (mufflersDic.ContainsKey(p))
                {
                    var param = mufflersDic[p];
                    DoUpdateMuffler(new_width, param);
                }
            }
        }
        private void DoUpdateMuffler(double newWidth, MufflerModifyParam muffler)
        {
            //洞和阀应该分开
            var dir_vec = -ThMEPHVACService.GetDirVecByAngle(muffler.rotateAngle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.GetRightVerticalVec(dir_vec);
            var mufflerService = new ThDuctPortsDrawValve("", muffler.name, muffler.mufflerLayer);
            var insert_p = muffler.insertP + vertical_r * (muffler.width - newWidth - 200) * 0.5;
            muffler.width = newWidth + 200;
            mufflerService.InsertMuffler(insert_p, muffler);
            ThDuctPortsDrawService.ClearGraph(muffler.handle);
        }
        private void DoUpdateHole(double new_width, HoleModifyParam hole)
        {
            //洞和阀应该分开
            var dir_vec = -ThMEPHVACService.GetDirVecByAngle(hole.rotateAngle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.GetRightVerticalVec(dir_vec);
            var hole_service = new ThDuctPortsDrawValve("", hole.holeName, hole.holeLayer);
            var insert_p = hole.insertP + vertical_r * (hole.width - new_width) * 0.5;
            hole_service.InsertHole(insert_p, new_width, hole.len, hole.rotateAngle);
            ThDuctPortsDrawService.ClearGraph(hole.handle);
        }
        private void DoUpdateValve(double new_width, Point2d new_p, ValveModifyParam valve)
        {
            if (valve.valveVisibility == "多叶调节风阀")
            {
                var dir_vec = ThMEPHVACService.GetDirVecByAngle(valve.rotateAngle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.GetRightVerticalVec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insert_p = new_p + vertical_r * new_width * 0.5 + moveSrtP.ToPoint2D().GetAsVector();
                valve_service.InsertValve(insert_p, new_width, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveVisibility == "电动多叶调节风阀")
            {
                var dir_vec = -ThMEPHVACService.GetDirVecByAngle(valve.rotateAngle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.GetRightVerticalVec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insert_p = valve.insertP + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.InsertValve(insert_p, new_width, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveVisibility == "风管止回阀")
            {
                var dir_vec = -ThMEPHVACService.GetDirVecByAngle(valve.rotateAngle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.GetRightVerticalVec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insert_p = valve.insertP + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.InsertValve(insert_p, new_width, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveName == "防火阀")
            {
                var dir_vec = ThMEPHVACService.GetDirVecByAngle(valve.rotateAngle - Math.PI * 0.5);
                var vertical_l = ThMEPHVACService.GetLeftVerticalVec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insert_p = valve.insertP + vertical_l * (valve.width - new_width) * 0.5;
                valve_service.InsertValve(insert_p, new_width, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
        }
    }
}
