using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThModifyPortClear
    {
        public static void DeleteTextDimValve(Point3d srtP, DBObjectCollection centerLines)
        {
            using (var db = AcadDatabase.Active())
            {
                ThDuctPortsInterpreter.GetValvesDic(out Dictionary<Polyline, ValveModifyParam> valvesDic);
                ThDuctPortsInterpreter.GetTextsDic(out Dictionary<Polyline, TextModifyParam> textDic);
                var portMarkIds = ThDuctPortsReadComponent.ReadBlkIdsByName("风口标注");
                var portMarks = portMarkIds.Select(o => db.Element<BlockReference>(o)).ToList();
                var dims = ThDuctPortsReadComponent.ReadDimension();
                var leaders = ThDuctPortsReadComponent.ReadLeader();
                var m = Matrix3d.Displacement(-srtP.GetAsVector());
                foreach (Polyline b in textDic.Keys.ToCollection())
                    b.TransformBy(m);
                foreach (Polyline b in valvesDic.Keys.ToCollection())
                    b.TransformBy(m);
                var handles = new HashSet<Handle>();
                foreach (Line line in centerLines)
                {
                    DeleteText(textDic, line);
                    DeleteDim(srtP, dims, line, handles);
                    DeleteValve(valvesDic, line);
                    DeletePortMark(srtP, portMarks, line, handles);
                    DeletePortLeader(srtP, leaders, line, handles);
                }
            }
        }
        private static void DeleteText(Dictionary<Polyline, TextModifyParam> textDic, Line l)
        {
            var textsIndex = new ThCADCoreNTSSpatialIndex(textDic.Keys.ToCollection());
            var poly = ThMEPHVACService.GetLineExtend(l, 4000);//在风管附近两千范围内的text
            var res = textsIndex.SelectCrossingPolygon(poly);
            foreach (Polyline pl in res)
                ThDuctPortsDrawService.ClearGraph(textDic[pl].handle);
        }
        private static void DeleteDim(Point3d srtP, List<AlignedDimension> dims, Line l, HashSet<Handle> handles)
        {
            var m = Matrix3d.Displacement(-srtP.GetAsVector());
            foreach (var dim in dims)
            {
                var p = dim.Bounds.Value.CenterPoint().TransformBy(m);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 2000 && handles.Add(dim.Handle))
                {
                    ThDuctPortsDrawService.ClearGraph(dim.Handle);
                }
            }
        }
        private static void DeleteValve(Dictionary<Polyline, ValveModifyParam> valvesDic, Line l)
        {
            var valvesIndex = new ThCADCoreNTSSpatialIndex(valvesDic.Keys.ToCollection());
            var poly = ThMEPHVACService.GetLineExtend(l, 1);
            var res = valvesIndex.SelectCrossingPolygon(poly);
            foreach (Polyline pl in res)
            {
                var param = valvesDic[pl];
                if (param.valveVisibility == "多叶调节风阀")
                    ThDuctPortsDrawService.ClearGraph(param.handle);
            }
        }
        private static void DeletePortMark(Point3d srtP, List<BlockReference> portMark, Line l, HashSet<Handle> handles)
        {
            var m = Matrix3d.Displacement(-srtP.GetAsVector());
            foreach (var mark in portMark)
            {
                var p = mark.Position.TransformBy(m);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 2501 && handles.Add(mark.ObjectId.Handle)) // 2500^2 = 1500^2 + 2000^2
                    ThDuctPortsDrawService.ClearGraph(mark.ObjectId.Handle);
            }
        }
        private static void DeletePortLeader(Point3d srtP, List<Leader> leaders, Line l, HashSet<Handle> handles)
        {
            var m = Matrix3d.Displacement(-srtP.GetAsVector());
            foreach (var leader in leaders)
            {
                var p = leader.StartPoint.TransformBy(m);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 2501 && handles.Add(leader.ObjectId.Handle))
                    ThDuctPortsDrawService.ClearGraph(leader.ObjectId.Handle);
            }
        }
    }
}