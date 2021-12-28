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
        public void UpdateVHM(Polyline detectPl, Point3d newAirValvePos, double newWidth)
        {
            UpdateValve(detectPl, newAirValvePos, newWidth);
            UpdateHole(detectPl, newWidth);
            UpdateMuffler(detectPl, newWidth);
        }
        private void UpdateValve(Polyline detectPl, Point3d newAirValvePos, double newWidth)
        {
            var res = valvesIndex.SelectCrossingPolygon(detectPl);
            foreach (Polyline p in res)
            {
                if (valvesDic.ContainsKey(p))
                {
                    var param = valvesDic[p];
                    DoUpdateValve(newWidth, newAirValvePos, param);
                }
            }
        }
        private void UpdateHole(Polyline detectPl, double newWidth)
        {
            var res = holesIndex.SelectCrossingPolygon(detectPl);
            foreach (Polyline p in res)
            {
                if (holesDic.ContainsKey(p))
                {
                    var param = holesDic[p];
                    DoUpdateHole(newWidth + 100, param);
                }
            }
        }
        private void UpdateMuffler(Polyline detectPl, double newWidth)
        {
            var res = mufflersIndex.SelectCrossingPolygon(detectPl);
            foreach (Polyline p in res)
            {
                if (mufflersDic.ContainsKey(p))
                {
                    var param = mufflersDic[p];
                    DoUpdateMuffler(newWidth, param);
                }
            }
        }
        private void DoUpdateMuffler(double newWidth, MufflerModifyParam muffler)
        {
            //洞和阀应该分开
            var dirVec = -ThMEPHVACService.GetDirVecByAngle3(muffler.rotateAngle - Math.PI * 0.5);
            var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
            var mufflerService = new ThDuctPortsDrawValve("", muffler.name, muffler.mufflerLayer);
            var insertP = muffler.insertP + verticalR * (muffler.width - newWidth - 200) * 0.5;
            muffler.width = newWidth + 200;
            mufflerService.InsertMuffler(insertP, muffler);
            ThDuctPortsDrawService.ClearGraph(muffler.handle);
        }
        private void DoUpdateHole(double newWidth, HoleModifyParam hole)
        {
            //洞和阀应该分开
            var dirVec = -ThMEPHVACService.GetDirVecByAngle3(hole.rotateAngle - Math.PI * 0.5);
            var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
            var holeService = new ThDuctPortsDrawValve("", hole.holeName, hole.holeLayer);
            var insertP = hole.insertP + verticalR * (hole.width - newWidth) * 0.5;
            holeService.InsertHole(insertP, newWidth, hole.len, hole.rotateAngle);
            ThDuctPortsDrawService.ClearGraph(hole.handle);
        }
        private void DoUpdateValve(double newWidth, Point3d newP, ValveModifyParam valve)
        {
            if (valve.valveVisibility == "多叶调节风阀")
            {
                var dirVec = ThMEPHVACService.GetDirVecByAngle3(valve.rotateAngle - Math.PI * 0.5);
                var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
                var valveService = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insertP = (newP + verticalR * newWidth * 0.5) + moveSrtP.GetAsVector();
                valveService.InsertValve(insertP, newWidth, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveVisibility == "电动多叶调节风阀")
            {
                var dirVec = -ThMEPHVACService.GetDirVecByAngle3(valve.rotateAngle - Math.PI * 0.5);
                var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
                var valveService = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insertP = valve.insertP + verticalR * (valve.width - newWidth) * 0.5;
                valveService.InsertValve(insertP, newWidth, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveVisibility == "风管止回阀")
            {
                var dirVec = -ThMEPHVACService.GetDirVecByAngle3(valve.rotateAngle - Math.PI * 0.5);
                var verticalR = ThMEPHVACService.GetRightVerticalVec(dirVec);
                var valveService = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insertP = valve.insertP + verticalR * (valve.width - newWidth) * 0.5;
                valveService.InsertValve(insertP, newWidth, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
            if (valve.valveName == "防火阀")
            {
                var dirVec = ThMEPHVACService.GetDirVecByAngle3(valve.rotateAngle - Math.PI * 0.5);
                var verticalL = ThMEPHVACService.GetLeftVerticalVec(dirVec);
                var valveService = new ThDuctPortsDrawValve(valve.valveVisibility, valve.valveName, valve.valveLayer);
                var insertP = valve.insertP + verticalL * (valve.width - newWidth) * 0.5;
                valveService.InsertValve(insertP, newWidth, valve.rotateAngle, valve.textAngle);
                ThDuctPortsDrawService.ClearGraph(valve.handle);
            }
        }
    }
}
