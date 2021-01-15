using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using EndCapStyle = NetTopologySuite.Operation.Buffer.EndCapStyle;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Engine
{
    public class ThBuildRacewayEngine:IDisposable
    {
        private List<Line> Lines { get; set; }
        public Dictionary<Line, List<Line>> CenterWithSides { get; private set; }
        public Dictionary<Line, List<Line>> CenterWithPorts { get; private set; }

        /// <summary>
        /// 线槽宽度
        /// </summary>
        private double Width { get; set; }
        public ThBuildRacewayEngine(List<Line> lines,double width)
        {
            Lines = new List<Line>();
            lines.ForEach(o => Lines.Add(new Line(o.StartPoint,o.EndPoint)));
            Width = width;
            CenterWithSides = new Dictionary<Line, List<Line>>();
            CenterWithPorts = new Dictionary<Line, List<Line>>();
        }
        public void Dispose()
        {
        }        
        public void Build()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var lineObjs = new DBObjectCollection();
                Lines.ForEach(o => lineObjs.Add(o));
                var centerLines = ThLaneLineSimplifier.Simplify(lineObjs, 100.0);
                var dbObjs = new DBObjectCollection();
                centerLines.ForEach(o => dbObjs.Add(o));
                var bufferObjs = dbObjs.LineMerge().Buffer(Width / 2.0, EndCapStyle.Flat);
                var sideParameter = new ThFindSideLinesParameter
                {
                    CenterLines = centerLines,
                    SideLines = bufferObjs.GetLines(),
                    HalfWidth = Width / 2.0
                };
                //查找合并线buffer后，获取中心线对应的两边线槽线
                var instane = ThFindSideLinesService.Find(sideParameter);
                CenterWithPorts = instane.PortLinesDic;
                //用中心线分割合并的线槽
                ThCableTrayCutService.Cut(instane.SideLinesDic, Width);
                var sidelines = new List<Line>();
                instane.SideLinesDic.ForEach(o => sidelines.AddRange(o.Value));
                var splitCenterLines = new List<Line>();
                using (var splitEngine=new ThSplitLineEngine(instane.SideLinesDic.Select(o=>o.Key).ToList()))
                {
                    splitEngine.Split();
                    splitEngine.Results.ForEach(o=>splitCenterLines.AddRange(o.Value));
                }
                var newSideParameter = new ThFindSideLinesParameter
                {
                    CenterLines = splitCenterLines,
                    SideLines = sidelines,
                    HalfWidth = Width / 2.0
                };       
                //对中心线分割后，找到其对应的两边
                CenterWithSides = ThFindCenterPairLinesService.Find(newSideParameter);
            }
        }
        public ObjectIdList CreateGroup(ThRacewayParameter layerParameter)
        {
            var results = new ObjectIdList();
            CenterWithSides.ForEach(o =>
            {
                var ports = FindPorts(o.Key, CenterWithPorts);
                var groupParameter = new ThRacewayGroupParameter
                {
                    RacewayParameter = layerParameter,
                    Center = o.Key,
                    Sides = o.Value,
                    Ports = ports,
                };
                results.AddRange(ThRacewayGroupService.Create(groupParameter));
            });
            return results;
        }
        private List<Line> FindPorts(Line center, Dictionary<Line, List<Line>> centerPorts)
        {
            var results = new List<Line>();
            var ports = new List<Line>();
            centerPorts.Where(o => ThGeometryTool.IsCollinearEx(
                center.StartPoint, center.EndPoint, o.Key.StartPoint, o.Key.EndPoint)).ForEach(o => ports.AddRange(o.Value));
            var spaticalIndex = ThGarageLightUtils.BuildSpatialIndex(ports);
            var spSquare = ThDrawTool.CreateSquare(center.StartPoint, 1.0);
            var epSquare = ThDrawTool.CreateSquare(center.EndPoint, 1.0);
            var spObjs = spaticalIndex.SelectCrossingPolygon(spSquare);
            var epObjs = spaticalIndex.SelectCrossingPolygon(epSquare);
            spObjs.Cast<Line>().ForEach(o => results.Add(o));
            epObjs.Cast<Line>().ForEach(o => results.Add(o));
            return results;
        }
        public List<Point3d> GetPorts()
        {
            var ports = new List<Point3d>();
            using (var fixedPrecision = new ThCADCoreNTSFixedPrecision())
            {
                CenterWithPorts.ForEach(m =>
                {
                    var normalize = m.Key.Normalize();
                    var pts = new List<Point3d>();
                    m.Value.ForEach(n =>
                    pts.Add(ThGeometryTool.GetMidPt(n.StartPoint, n.EndPoint)));
                    pts=pts.OrderBy(p => normalize.StartPoint.DistanceTo(p)).ToList();
                    ports.AddRange(pts);
                });
            }                
            return ports;
        }
    }
}
