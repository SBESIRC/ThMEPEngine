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
                Lines.ForEach(o=> lineObjs.Add(o));
                var centerLines = ThLaneLineSimplifier.Simplify(lineObjs, 100.0);               
                var dbObjs = new DBObjectCollection();
                centerLines.ForEach(o => dbObjs.Add(o));
                var bufferObjs = dbObjs.LineMerge().Buffer(Width / 2.0, EndCapStyle.Flat);
                var sideParameter = new ThFindSideLinesParameter
                {
                    CenterLines= centerLines,
                    SideLines= bufferObjs.GetLines(),
                    HalfWidth = Width / 2.0
                };
                var instane = ThFindSideLinesService.Find(sideParameter);
                CenterWithSides = instane.SideLinesDic;
                CenterWithPorts = instane.PortLinesDic;
            }
        }
        public ObjectIdList CreateGroup(ThRacewayParameter layerParameter)
        {
            var results = new ObjectIdList();
            CenterWithSides.ForEach(o =>
            {
                var ports = new List<Line>();
                if (CenterWithPorts.ContainsKey(o.Key))
                {
                    ports = CenterWithPorts[o.Key];
                }
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
