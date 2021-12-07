using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Algorithm;

namespace ThMEPHVAC.Model
{
    public enum ModifyerStatus
    {
        OK,
        NO_PORT,
        NO_CROSS_PORT,
        MULTI_PORT_RANGE,
        PORT_CROSS_MULTI_ENTITY
    }
    public class ThDuctPortsModifyPort
    {
        public ModifyerStatus status;
        public List<EndlineSegInfo> ducts;
        public DBObjectCollection centerLines;
        public double avgAirVolume;
        private Point3d sp;
        private Point3d ep;
        private Tolerance tor;
        private Matrix3d disMat;
        private PortParam portParam;
        private HashSet<Handle> handles;
        private List<Handle> connGroupHandles;
        private List<Handle> crossPortHandles;
        private DBObjectCollection mpObjs;
        private HashSet<Polyline> polySet;
        private ThCADCoreNTSSpatialIndex portIndex;
        private ThCADCoreNTSSpatialIndex groupIndex;
        private ThCADCoreNTSSpatialIndex mpGroupIndex;
        private Dictionary<Polyline, ObjectId> bounds2IdDic;
        public Dictionary<Point3d, SidePortInfo> dicPort2Handle;
        public ThDuctPortsModifyPort() { }
        public ThDuctPortsModifyPort(PortParam portParam)
        {
            using (var db = AcadDatabase.Active())
            {
                Init(portParam);
                bounds2IdDic = ThDuctPortsReadComponent.ReadAllComponent();
                var groupBounds = bounds2IdDic.Keys.ToCollection();
                MoveBoundsToZero(groupBounds);
                connGroupHandles = GetConnCompIndex(groupBounds);
                var components = ThDuctPortsReadComponent.ReadPortComponents();
                var allPortBlk = components.Select(o => db.Element<BlockReference>(o)).ToList();
                GetPortInfo(allPortBlk);
                if (status != ModifyerStatus.OK)
                    return;
                GetConnPort(allPortBlk, out crossPortHandles);
                if (status != ModifyerStatus.OK)
                    return;
                int portNum = crossPortHandles.Count;
                portParam.param.portNum = portNum;
                avgAirVolume = portParam.param.airVolume / portNum;
            }
        }
        private void Init(PortParam portParam)
        {
            tor = new Tolerance(1.1, 1.1);
            polySet = new HashSet<Polyline>();
            centerLines = new DBObjectCollection();
            mpObjs = new DBObjectCollection();
            ducts = new List<EndlineSegInfo> ();
            handles = new HashSet<Handle>();
            this.portParam = portParam;
            disMat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
        }
        public void Construct()
        {
            using (var db = AcadDatabase.Active())
            {
                AdjustPort();
                PrepareForSearch(out Polyline detectPoly, out Point3d detectP);
                SetSuctAirVolume(detectPoly, detectP, out _);
                DeleteOrgGraph();
            }
        }
        private void PrepareForSearch(out Polyline detectPoly, out Point3d detectP)
        {
            detectPoly = ThMEPHVACService.CreateDetectPoly(Point3d.Origin);
            detectP = Point3d.Origin;
            polySet.Clear();
        }
        private List<Handle> GetConnCompIndex(DBObjectCollection groupBounds)
        {
            var handles = new List<Handle>();
            groupIndex = new ThCADCoreNTSSpatialIndex(groupBounds);
            Search_conn_comp(Point3d.Origin);
            var objs = new DBObjectCollection();
            foreach (var pl in polySet)
            {
                objs.Add(pl);
                handles.Add(bounds2IdDic[pl].Handle);
                mpObjs.Add(pl.ToNTSPolygon().ToDbMPolygon());
            }
            groupIndex = new ThCADCoreNTSSpatialIndex(objs);
            mpGroupIndex = new ThCADCoreNTSSpatialIndex(mpObjs);
            polySet.Clear();
            return handles;
        }
        private void GetConnPort(List<BlockReference> all_port_blk, out List<Handle> portHandles)
        {
            var cross_port = new List<Polyline>();
            portHandles = new List<Handle>();
            dicPort2Handle = CreatePortBounds(all_port_blk);
            if (dicPort2Handle.Count == 0)
            {
                status = ModifyerStatus.NO_PORT;
                return;
            }
            foreach (var p in dicPort2Handle.Keys)
            {
                var portBound = ThMEPHVACService.CreateDetectPoly(p);
                var res = mpGroupIndex.SelectCrossingPolygon(portBound);
                if (res.Count > 0)
                {
                    portHandles.AddRange(dicPort2Handle[p].portHandles);
                    cross_port.Add(portBound);
                }
            }
            status = Checkout_port(portHandles);
            portIndex = new ThCADCoreNTSSpatialIndex(cross_port.ToCollection());
        }
        private ModifyerStatus Checkout_port(List<Handle> port_handles)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var handle in port_handles)
                {
                    var id = db.Database.HandleToObjectId(handle.ToString());
                    var param = ThDuctPortsInterpreter.GetPortParam(id);
                    ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double w, out double h);
                    if (!ThMEPHVACService.Is_equal(w, param.portWidth) || 
                        portParam.param.portRange != param.portRange ||
                        !ThMEPHVACService.Is_equal(h, param.portHeight))
                    {
                        return ModifyerStatus.MULTI_PORT_RANGE;
                    }
                }
                return ModifyerStatus.OK;
            }
        }
        private void AdjustPort()
        {
            if (portParam.param.portRange.Contains("下"))
            {
                var portBounds = new DBObjectCollection();
                var dic = new Dictionary<Point3d, SidePortInfo>();
                foreach (MPolygon duct in mpObjs)
                {
                    var res = portIndex.SelectCrossingPolygon(duct);
                    if (res.Count > 0)
                    {
                        var center_line = Get_duct_center_line(duct.Bounds.Value);
                        if (center_line.StartPoint.IsEqualTo(center_line.EndPoint, tor))
                            continue;
                        center_line.TransformBy(disMat);
                        foreach (Polyline pl in res)
                        {
                            var pl_cp = ThMEPHVACService.RoundPoint(pl.GetCentroidPoint(), 6);
                            var p = ThMEPHVACService.RoundPoint(center_line.GetClosestPointTo(pl_cp, false), 6);
                            var handles = dicPort2Handle[pl_cp].portHandles;
                            dic.Add(p, new SidePortInfo(true, handles));
                            portBounds.Add(ThMEPHVACService.CreateDetectPoly(p));
                        }
                    }
                }
                portIndex = new ThCADCoreNTSSpatialIndex(portBounds);
                dicPort2Handle.Clear();
                dicPort2Handle = dic;
            }
        }
        private Line Get_duct_center_line(Extents3d value)
        {
            foreach (var item in bounds2IdDic)
            {
                if (item.Key.Bounds.Value.IsEqualTo(value))
                {
                    var id = bounds2IdDic[item.Key];
                    var cur_duct = ThHvacAnalysisComponent.GetDuctParamById(id);
                    if (cur_duct.type == "Duct")
                    {
                        var sp = new Point3d(cur_duct.sp.X, cur_duct.sp.Y, 0);
                        var ep = new Point3d(cur_duct.ep.X, cur_duct.ep.Y, 0);
                        return new Line(sp, ep);
                    }
                }
            }
            return new Line();
        }
        private Dictionary<Point3d, SidePortInfo> CreatePortBounds(List<BlockReference> allPortBlk)
        {
            if (portParam.param.portRange.Contains("下"))
                return CreateDownPortBounds(allPortBlk);
            else if (portParam.param.portRange.Contains("侧"))
                return CreateSidePortBounds(allPortBlk);
            else
                throw new NotImplementedException();
        }
        private Dictionary<Point3d, SidePortInfo> CreateDownPortBounds(List<BlockReference> allPortBlk)
        {
            var pb = new Dictionary<Point3d, SidePortInfo>();
            var list = new List<Handle>();
            foreach (var port in allPortBlk)
            {
                var p = ThMEPHAVCBounds.GetDownPortCenterPoint(port, portParam);
                list.Add(port.Handle);
                pb.Add(p, new SidePortInfo (true, list));
                list = new List<Handle>();
            }
            return pb;
        }
        private Dictionary<Point3d, SidePortInfo> CreateSidePortBounds(List<BlockReference> allPortBlk)
        {
            var pb = new Dictionary<Point3d, SidePortInfo>();
            foreach (var blk in allPortBlk)
            {
                var duct = GetSidePortCrossDuct(blk.Bounds.Value);
                if (duct.Count == 0)
                    continue;
                if (duct.Count > 1)
                {
                    pb.Clear();
                    return pb;
                }
                var insert_p = blk.Position.TransformBy(disMat);
                var ductWidth = GetDuctWidth(duct[0] as Polyline, insert_p, out Line center_line);
                var p = ThMEPHAVCBounds.GetSidePortCenterPoint(blk, portParam.srtPoint, portParam.param.portSize, ductWidth);
                if (!pb.ContainsKey(p))
                {
                    var list = new List<Handle>() { blk.Handle };
                    var is_left = ThMEPHVACService.IsPointInLeftSide(center_line, insert_p);
                    pb.Add(p, new SidePortInfo (is_left, list));
                }
                else
                    pb[p].portHandles.Add(blk.Handle);//两个handle代表双边都存在
            }
            return pb;
        }
        private DBObjectCollection GetSidePortCrossDuct(Extents3d port_border)
        {
            var blk_pl = new Polyline();
            blk_pl.CreateRectangle(port_border.MinPoint.ToPoint2D(), port_border.MaxPoint.ToPoint2D());
            blk_pl.TransformBy(disMat);
            return groupIndex.SelectCrossingPolygon(blk_pl);
        }
        private double GetDuctWidth(Polyline pl, Point3d insert_p, out Line center_line)
        {
            var border = new DBObjectCollection();
            pl.Explode(border);
            double duct_len = 0;
            foreach (Line l in border)
            {
                if (l.GetClosestPointTo(insert_p, false).IsEqualTo(insert_p))
                    duct_len = l.Length;
            }
            var points = new List<Point3d>();
            foreach (Line l in border)
            {
                if (!ThMEPHVACService.Is_equal(l.Length, duct_len))
                    points.Add(ThMEPHVACService.GetMidPoint(l));
            }
            center_line = new Line(points[0], points[1]);
            foreach (Line l in border)
            {
                if (!ThMEPHVACService.Is_equal(l.Length, duct_len))
                    return l.Length;
            }
            throw new NotImplementedException();
        }
        private void GetPortInfo(List<BlockReference> portBlks)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var blk in portBlks)
                {
                    var pl = new Polyline();
                    pl.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                    pl.TransformBy(disMat);
                    var res = mpGroupIndex.SelectCrossingPolygon(pl);
                    if (res.Count > 0 && blk.GetEffectiveName() == ThHvacCommon.AI_PORT)
                    {
                        var id = db.Database.HandleToObjectId(blk.Handle.ToString());
                        var param = ThDuctPortsInterpreter.GetPortParam(id);
                        portParam.param.portSize = param.portWidth.ToString() + "x" + param.portHeight.ToString();
                        portParam.param.portSize = param.portRange;
                        break;
                    }
                }
                if (portParam.param.portRange == null)
                    status = ModifyerStatus.NO_CROSS_PORT;
            }
        }
        public void DeleteOrgGraph()
        {
            foreach (var handle in connGroupHandles)
                ThDuctPortsDrawService.ClearGraph(handle);
            foreach (var port_handle in crossPortHandles)
                ThDuctPortsDrawService.ClearGraph(port_handle);
            //DeleteTextDimValve();
        }
        public static void DeleteTextDimValve(Point3d srtP, DBObjectCollection centerLines)
        {
            using (var db = AcadDatabase.Active())
            {
                ThDuctPortsInterpreter.GetValvesDic(out Dictionary<Polyline, ValveModifyParam> valves_dic);
                ThDuctPortsInterpreter.GetTextsDic(out Dictionary<Polyline, TextModifyParam> text_dic);
                var port_mark_ids = ThDuctPortsReadComponent.ReadBlkIdsByName("风口标注");
                var port_marks = port_mark_ids.Select(o => db.Element<BlockReference>(o)).ToList();
                var dims = ThDuctPortsReadComponent.ReadDimension();
                var leaders = ThDuctPortsReadComponent.ReadLeader();
                var m = Matrix3d.Displacement(-srtP.GetAsVector());
                foreach (Polyline b in text_dic.Keys.ToCollection())
                    b.TransformBy(m);
                foreach (Polyline b in valves_dic.Keys.ToCollection())
                    b.TransformBy(m);
                var handles = new HashSet<Handle>();
                foreach (Line line in centerLines)
                {
                    DeleteText(text_dic, line);
                    DeleteDim(srtP, dims, line, handles);
                    DeleteValve(valves_dic, line);
                    DeletePortMark(srtP, port_marks, line, handles);
                    DeletePortLeader(srtP, leaders, line, handles);
                }
            }
        }
        private static void DeleteText(Dictionary<Polyline, TextModifyParam> text_dic, Line l)
        {
            var texts_index = new ThCADCoreNTSSpatialIndex(text_dic.Keys.ToCollection());
            var poly = ThMEPHVACService.GetLineExtend(l, 4000);//在风管附近两千范围内的text
            var res = texts_index.SelectCrossingPolygon(poly);
            foreach (Polyline pl in res)
                ThDuctPortsDrawService.ClearGraph(text_dic[pl].handle);
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
        private static void DeleteValve(Dictionary<Polyline, ValveModifyParam> valves_dic, Line l)
        {
            var valves_index = new ThCADCoreNTSSpatialIndex(valves_dic.Keys.ToCollection());
            var poly = ThMEPHVACService.GetLineExtend(l, 1);
            var res = valves_index.SelectCrossingPolygon(poly);
            foreach (Polyline pl in res)
            {
                var param = valves_dic[pl];
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
        private void MoveBoundsToZero(DBObjectCollection group_bounds)
        {
            var dis_mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            foreach (Polyline pl in group_bounds)
            {
                pl.TransformBy(dis_mat);
            }
        }
        private List<Point3d> MovePtsToZero(List<Point3d> portPts)
        {
            var pts = new List<Point3d>();
            var dis_mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            foreach (var p in portPts)
                pts.Add(p.TransformBy(dis_mat));
            return pts;
        }
        private double SetSuctAirVolume(Polyline curPoly, Point3d detectP, out Polyline prePoly)
        {
            double subAirVolume = 0;
            var res = DetectCrossGroup(detectP);
            prePoly = curPoly;
            if (res.Count == 1 && polySet.Count != 0)
            {
                ep = detectP;
                return GetCurPortAirVolume(curPoly);
            }
            double airVolume = 0;
            res.Remove(curPoly);
            foreach (Polyline pl in res)
            {
                if (!polySet.Add(pl))
                    continue;
                var portPts = Get_step_point(pl, detectP);
                foreach (var p in portPts)
                {
                    subAirVolume += SetSuctAirVolume(pl, p, out prePoly);
                    double curAirVolume = GetCurPortAirVolume(curPoly);
                    RecordComp(curPoly, subAirVolume);
                    airVolume = subAirVolume + curAirVolume;
                }
            }
            return airVolume;
        }
        private double GetCurPortAirVolume(Polyline cur_poly)
        {
            double air_volume = 0;
            var port_bound = portIndex.SelectCrossingPolygon(cur_poly);
            foreach (Polyline pl in port_bound)
            {
                var cp = pl.GetCentroidPoint();
                cp = ThMEPHVACService.RoundPoint(cp, 6);
                if (dicPort2Handle.ContainsKey(cp))
                    air_volume += dicPort2Handle[cp].portHandles.Count * avgAirVolume;
                else
                    throw new NotImplementedException();
            }
            return air_volume;
        }
        private void RecordComp(Polyline curPoly, double airVolume)
        {
            using (var db = AcadDatabase.Active())
            {
                if (bounds2IdDic.Keys.Contains(curPoly))
                {
                    var curEntity = ThHvacAnalysisComponent.GetConnectorParamById(bounds2IdDic[curPoly]);
                    if (curEntity.type == "Elbow" || curEntity.type == "Tee" || curEntity.type == "Cross")
                    {
                        //var center_p = ThDuctPortsShapeService.Get_entity_center_p(cur_entity);
                        var center_p = Point3d.Origin;
                        sp = new Point3d(center_p.X, center_p.Y, 0);
                        sp = sp.TransformBy(disMat);
                        var l = new Line(sp, ep);
                        //double width = cur_entity.portWidths.Max();
                        double width = 0;
                        int port_num = GetPortNum(sp, ep, out List<Point3d> insert_pts, width);
                        var seg = new SegInfo() { l = new Line(sp, ep), airVolume = airVolume };
                        var portsInfo = TransPositionToInfo(insert_pts);
                        ducts.Add(new EndlineSegInfo() { portNum = port_num, seg = seg, portsInfo = portsInfo });
                        ep = sp;
                    }
                }
                else
                {
                    // 回到起始点
                    double width = ThMEPHVACService.GetWidth(portParam.param.inDuctSize);
                    int port_num = GetPortNum(Point3d.Origin, ep, out List<Point3d> insertPts, width);
                    var seg = new SegInfo() { l = new Line(Point3d.Origin, ep), airVolume = airVolume };
                    var portsInfo = TransPositionToInfo(insertPts);
                    ducts.Add(new EndlineSegInfo() {portNum = port_num, seg = seg, portsInfo = portsInfo });
                }
            }
        }
        private List<PortInfo> TransPositionToInfo(List<Point3d> insertPositions)
        {
            var portsInfo = new List<PortInfo>();
            foreach (var p in insertPositions)
                portsInfo.Add(new PortInfo() { position = p });
            return portsInfo;
        }
        private int GetPortNum(Point3d sp, Point3d ep, out List<Point3d> insert_pts, double width)
        {
            insert_pts = new List<Point3d>();
            var l = new Line(sp, ep);
            centerLines.Add(l);
            var dir_vec = ThMEPHVACService.GetEdgeDirection(l);
            var pl = ThMEPHVACService.GetLineExtend(l, width);
            var res = portIndex.SelectCrossingPolygon(pl);
            foreach (Polyline port in res)
                insert_pts.Add(port.GetCentroidPoint());
            if (dir_vec.X >= 0)
                insert_pts = insert_pts.OrderBy(o => o.X).ToList();
            else
                insert_pts = insert_pts.OrderByDescending(o => o.X).ToList();
            if (dir_vec.Y >= 0)
                insert_pts = insert_pts.OrderBy(o => o.Y).ToList();
            else
                insert_pts = insert_pts.OrderByDescending(o => o.Y).ToList();
            return res.Count;
        }
        private void Search_conn_comp(Point3d start_p)
        {
            var queue = new Queue<Point3d>();
            queue.Enqueue(start_p);
            bool is_first = true;
            while (queue.Count != 0)
            {
                var curPt = queue.Dequeue();
                var poly = new Polyline();
                if (!is_first)
                    poly.CreatePolygon(curPt.ToPoint2D(), 4, 10);
                else
                {
                    is_first = false;
                    var dir_vec = ThMEPHVACService.GetDirVecByAngle(Math.PI / 3 - Math.PI / 2);
                    var srt_p = start_p.ToPoint2D() + (dir_vec * 50);
                    poly = ThMEPHVACService.CreateRect(srt_p, dir_vec, 50, 3000);
                }
                var selectedBounds = groupIndex.SelectCrossingPolygon(poly);
                foreach (Polyline b in selectedBounds)
                {
                    if (!polySet.Add(b))
                        continue;
                    var portPts = Get_step_point(b, curPt);
                    portPts.ForEach(pt => queue.Enqueue(pt));
                }
            }
        }
        private Polyline Get_polyline(Polyline poly)
        {
            foreach (var p in bounds2IdDic)
            {
                if (poly.Bounds.Value.IsEqualTo(p.Key.Bounds.Value, tor))
                {
                    return p.Key;
                }
            }
            throw new NotImplementedException();
        }
        private List<Point3d> Get_step_point(Polyline poly, Point3d exclude_point)
        {
            var p = Get_polyline(poly);
            var id = bounds2IdDic[p];
            var ports_dic = ThHvacAnalysisComponent.GetPortsOfGroup(id);
            var port_pts = ports_dic.Values.Select(v => v.Item1).ToList();
            var pts = MovePtsToZero(port_pts);
            for (int i = 0; i < pts.Count; ++i)
            {
                if (pts[i].IsEqualTo(exclude_point, tor))
                    pts.RemoveAt(i);
            }
            return pts;
        }
        private DBObjectCollection DetectCrossGroup(Point3d p)
        {
            var poly = ThMEPHVACService.CreateDetectPoly(p);
            var res = groupIndex.SelectCrossingPolygon(poly);
            return res;
        }
    }
}