using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThFansMainDuctAnalysis
    {
        public List<SegInfo> fanDucts;
        public Dictionary<int, SegInfo> ducts;
        public List<EntityModifyParam> connectors;
        private Tolerance tor;
        private HashSet<Point3d> points;
        private DBObjectCollection mainDucts;
        private ThCADCoreNTSSpatialIndex index;
        private Dictionary<int, FanParam> dicLineParam;
        public ThFansMainDuctAnalysis(DBObjectCollection mainDucts, Dictionary<int, FanParam> dicLineParam)
        {
            Init(mainDucts, dicLineParam);
            GetMainDuctInfo();
            FilterDucts();
        }
        private void Init(DBObjectCollection mainDucts, Dictionary<int, FanParam> dicLineParam)
        {
            this.mainDucts = mainDucts;
            this.dicLineParam = dicLineParam;
            tor = new Tolerance(1.5, 1.5);
            fanDucts = new List<SegInfo>();
            points = new HashSet<Point3d>();            
            ducts = new Dictionary<int, SegInfo>();
            connectors = new List<EntityModifyParam>();
            index = new ThCADCoreNTSSpatialIndex(mainDucts);
        }
        private void FilterDucts()
        {
            foreach (var duct in ducts.Values)
            {
                var len = duct.l.StartPoint.DistanceTo(duct.l.EndPoint);
                if ((len - (duct.srcShrink + duct.dstShrink)) > 1e-3)
                {
                    fanDucts.Add(duct);
                }
            }
            ducts.Clear();
        }
        private void GetMainDuctInfo()
        {
            foreach (Line l in mainDucts)
            {
                RecordMainDuctInfo(l.StartPoint, l, true);
                RecordMainDuctInfo(l.EndPoint, l, false);
            }
        }
        private void RecordMainDuctInfo(Point3d p, Line l, bool srtFlag)
        {
            var pl = new Polyline();
            pl.CreatePolygon(p.ToPoint2D(), 4, 1);
            var res = index.SelectCrossingPolygon(pl);
            if (res.Count == 0)
                return;
            if (points.Add(p) && res.Count > 1)
                CreateConnector(p, res);
            switch (res.Count)
            {
                case 1: RecordDirectLineShrink(l); break;
                case 2: RecordElbowLineShrink(p, l, res, srtFlag); break;
                case 3: RecordTeeLineShrink(l, res, srtFlag); break;
                case 4: break;
            }
        }
        private void RecordDirectLineShrink(Line l)
        {
            var code = l.GetHashCode();
            if (dicLineParam.ContainsKey(code))
            {
                var curParam = dicLineParam[l.GetHashCode()];
                if (!ducts.ContainsKey(code))
                {
                    var info = new SegInfo() { l = l, ductSize = curParam.notRoomDuctSize, airVolume = curParam.airVolume, elevation = curParam.notRoomElevation };
                    ducts.Add(code, info);
                }
            }
        }

        private void RecordTeeLineShrink(Line l, DBObjectCollection res, bool srtFlag)
        {
            var code = l.GetHashCode();
            if (dicLineParam.ContainsKey(code))
            {
                res.Remove(l);
                var otherLine1 = res[0] as Line;
                var otherLine2 = res[1] as Line;
                var curParam = dicLineParam[l.GetHashCode()];
                var param1 = dicLineParam[otherLine1.GetHashCode()];
                var param2 = dicLineParam[otherLine2.GetHashCode()];
                double shrinkLen = ThDuctPortsShapeService.GetTeeShrink(l, otherLine1, otherLine2, curParam, param1, param2)[l.GetHashCode()];
                if (!ducts.ContainsKey(code))
                {
                    var info = new SegInfo() { l = l, ductSize = curParam.notRoomDuctSize, airVolume = curParam.airVolume, elevation = curParam.notRoomElevation };
                    ducts.Add(code, info);
                }
                if (srtFlag)
                    ducts[code].srcShrink = shrinkLen;
                else
                    ducts[code].dstShrink = shrinkLen;
            }   
        }

        private void RecordElbowLineShrink(Point3d p, Line l, DBObjectCollection res, bool srtFlag)
        {
            // 每根线只管自己的shrink，因为并不知道otherLine的start和end对应的是什么点
            var code = l.GetHashCode();
            if (dicLineParam.ContainsKey(code))
            {
                res.Remove(l);
                var otherLine = res[0] as Line;
                var curParam = dicLineParam[code];
                if (!ducts.ContainsKey(code))
                {
                    var info = new SegInfo() { l = l, ductSize = curParam.notRoomDuctSize, airVolume = curParam.airVolume, elevation = curParam.notRoomElevation };
                    ducts.Add(code, info);
                }
                var w = ThMEPHVACService.GetWidth(curParam.notRoomDuctSize);
                var angle = ThMEPHVACService.GetElbowOpenAngle(l, otherLine, p);
                var shrinkLen = ThDuctPortsShapeService.GetElbowShrink(angle, w);
                if (srtFlag)
                    ducts[code].srcShrink = shrinkLen;
                else
                    ducts[code].dstShrink = shrinkLen;
            }
        }

        private void CreateConnector(Point3d centerP, DBObjectCollection lines)
        {
            var portWidths = new Dictionary<Point3d, string>();
            double maxW = 0.0;
            var inLine = new Line();
            foreach (Line l in lines)
            {
                var param = dicLineParam[l.GetHashCode()];
                var w = ThMEPHVACService.GetWidth(param.notRoomDuctSize);
                if (maxW < w)
                {
                    maxW = w;
                    inLine = l;
                }
            }
            Record(portWidths, centerP, inLine);
            foreach (Line l in lines)
            {
                if (l.Equals(inLine))
                    continue;
                Record(portWidths, centerP, l);
            }
            connectors.Add(new EntityModifyParam() { centerP = centerP, portWidths = portWidths });
        }
        private void Record(Dictionary<Point3d, string> portWidths, Point3d centerP, Line inLine)
        {
            var otherP = ThMEPHVACService.GetOtherPoint(inLine, centerP, tor);
            var param = dicLineParam[inLine.GetHashCode()];
            portWidths.Add(otherP, param.notRoomDuctSize);
        }
    }
}