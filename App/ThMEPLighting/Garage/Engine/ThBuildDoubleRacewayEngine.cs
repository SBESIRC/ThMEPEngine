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
using NFox.Cad;
using ThCADExtension;

namespace ThMEPLighting.Garage.Engine
{
    public class ThBuildDoubleRacewayEngine : IDisposable
    {
        private List<Curve> FirstCurves { get; set; }
        private List<Curve> SecondCurves { get; set; }
        private List<Curve> FdxCurves { get; set; }
        private ThRacewayParameter RacewayParameter { get; set; }
             
        public Dictionary<Line, List<Line>> CenterWithSides { get; private set; }
        public Dictionary<Line, List<Line>> CenterWithPorts { get; private set; }

        public ObjectIdList DrawObjIs { get; set; }

        /// <summary>
        /// 线槽宽度
        /// </summary>
        private double Width { get; set; }
        public ThBuildDoubleRacewayEngine(
            List<Curve> firstCurves,
            List<Curve> secondCurves,
            List<Curve> fdxCurves , 
            double width,
            ThRacewayParameter racewayParameter)
        {
            FirstCurves = new List<Curve>();
            SecondCurves = new List<Curve>();
            FdxCurves = new List<Curve>();
            firstCurves.ForEach(o => FirstCurves.Add(o.WashClone()));
            secondCurves.ForEach(o => SecondCurves.Add(o.WashClone()));
            fdxCurves.ForEach(o => FdxCurves.Add(o.WashClone()));
            Width = width;
            CenterWithSides = new Dictionary<Line, List<Line>>();
            CenterWithPorts = new Dictionary<Line, List<Line>>();
            RacewayParameter = racewayParameter;
            DrawObjIs = new ObjectIdList();
        }
        public void Dispose()
        {
        }        
        public void Build()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                var firstBuffers = Buffer(FirstCurves);
                var secondBuffers = Buffer(SecondCurves);
                var fdxBuffers = Buffer(FdxCurves);
                firstBuffers.Cast<Polyline>().ForEach(o => objs.Add(o));
                secondBuffers.Cast<Polyline>().ForEach(o => objs.Add(o));
                fdxBuffers.Cast<Polyline>().ForEach(o => objs.Add(o));
                var unionObjs = objs.UnionPolygons();
                var unionLines = unionObjs.GetLines();
                var sideParameter = new ThFindSideLinesParameter
                {
                    CenterLines = GetCenterLines(),
                    SideLines = unionLines,
                    HalfWidth = Width / 2.0
                };
                //查找合并线buffer后，获取中心线对应的两边线槽线
                var instane = ThFindSideLinesService.Find(sideParameter);
                CenterWithPorts = instane.PortLinesDic;
                //用中心线分割合并的线槽
                ThCableTrayCutService.Cut(instane.SideLinesDic, Width);
                var sidelines = new List<Line>();
                instane.SideLinesDic.ForEach(o => sidelines.AddRange(o.Value));
                sidelines = ThGarageLightUtils.DistinctLines(sidelines);
                var splitCenterLines = instane.SideLinesDic.Select(o => o.Key).ToList();
                splitCenterLines = ThGarageLightUtils.DistinctLines(splitCenterLines);
                using (var splitEngine=new ThSplitLineEngine(splitCenterLines))
                {
                    splitEngine.Split();
                    splitEngine.Results.ForEach(o=>splitCenterLines.AddRange(o.Value));
                }
                splitCenterLines = instane.SideLinesDic.Select(o => o.Key).ToList();
                splitCenterLines = ThGarageLightUtils.DistinctLines(splitCenterLines);
                sidelines = ThGarageLightUtils.DistinctLines(sidelines);
                var newSideParameter = new ThFindSideLinesParameter
                {
                    CenterLines = splitCenterLines,
                    SideLines = sidelines,
                    HalfWidth = Width / 2.0
                };
                //对中心线分割后，找到其对应的两边
                CenterWithSides = ThFindCenterPairLinesService.Find(newSideParameter);

                var unCutLines=GetUnCutLines(instane.SideLinesDic, unionLines);
                var sideLines = new List<Line>();
                //sideLines.AddRange(unCutLines);
                instane.SideLinesDic.ForEach(o => sideLines.AddRange(o.Value));
                BuildCableTray(sidelines, CenterWithSides.Select(o => o.Key).ToList());
            }
        }
        private void CreateGroup()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //CreateLayer(acadDatabase.Database);
                CenterWithSides.ForEach(o =>
                {
                    var objIds = new ObjectIdList();
                    objIds.Add(o.Key.Id);
                    o.Value.ForEach(v => objIds.Add(v.Id));
                    var ports = FindPorts(o.Key, CenterWithPorts);
                    ports.ForEach(p => p.Layer = RacewayParameter.PortLineParameter.Layer);
                    ports.ForEach(p => p.Linetype = "Bylayer");
                    ports.ForEach(p => objIds.Add(acadDatabase.ModelSpace.Add(p)));                   
                    DrawObjIs.AddRange(objIds);
                    var lineValueList = new TypedValueList
                    {
                        { (int)DxfCode.ExtendedDataAsciiString, "CableTray"},
                    };
                    objIds.ForEach(l=> XDataTools.AddXData(l, ThGarageLightCommon.ThGarageLightAppName, lineValueList));
                    var groupName = Guid.NewGuid().ToString();
                    GroupTools.CreateGroup(acadDatabase.Database, groupName, objIds);
                }); 
            }
        }
        private List<Line> GetUnCutLines(Dictionary<Line, List<Line>> cutDics, List<Line> unionLines)
        {
            var cutLines = cutDics.Where(o => o.Value.Count > 0).Select(o=>o.Key).ToList();
            return unionLines.Where(o => !(cutLines.Where(x=>
                (x.StartPoint.IsEqualTo(o.EndPoint, new Tolerance(1, 1)) &&
                x.EndPoint.IsEqualTo(o.StartPoint, new Tolerance(1, 1)))||
                (x.StartPoint.IsEqualTo(o.StartPoint, new Tolerance(1, 1)) &&
                x.EndPoint.IsEqualTo(o.EndPoint, new Tolerance(1, 1))))
                .Count() > 0))
                .ToList();
        }

        private List<Line> GetCenterLines()
        {
            var results = new List<Line>();
            results.AddRange(GetCenterLines(FirstCurves));
            results.AddRange(GetCenterLines(SecondCurves));
            results.AddRange(GetCenterLines(FdxCurves));
            return results;
        }
        private List<Line> GetCenterLines(List<Curve> curves)
        {
            var results = new List<Line>();
            curves.ForEach(o =>
            {
                if (o is Line line)
                {
                    results.Add(new Line(o.StartPoint,o.EndPoint));
                }
                else if (o is Polyline polyline)
                {
                    var objs = new DBObjectCollection();
                    polyline.Explode(objs);
                    results.AddRange(objs.Cast<Line>().ToList());
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
        private DBObjectCollection Buffer(List<Curve> curves)
        {
            var objs = curves.ToCollection();
            return objs.LineMerge().Buffer(Width / 2.0, EndCapStyle.Flat);
        }
        private void BuildCableTray(List<Line> cableTrayLines,List<Line> centerLines)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                cableTrayLines.ForEach(o =>
                {                    
                    o.Layer = RacewayParameter.SideLineParameter.Layer;
                    o.Linetype = "Bylayer";
                    acadDb.ModelSpace.Add(o);                    
                });
                centerLines.ForEach(o =>
                {
                    o.Layer = RacewayParameter.CenterLineParameter.Layer;
                    o.Linetype = "Bylayer";
                    acadDb.ModelSpace.Add(o);
                });
            }
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
        private Tuple<int,int> CheckLine(Line line,List<Line> lines)
        {
            var spObjs = Find(line.StartPoint, lines);
            var epObjs = Find(line.EndPoint, lines);
            spObjs.Remove(line);
            epObjs.Remove(line);
            return Tuple.Create(spObjs.Count,epObjs.Count);
        }
        private DBObjectCollection Find(Point3d pt, List<Line> lines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            var outline = ThDrawTool.CreateSquare(pt, 2.0);
            return spatialIndex.SelectCrossingPolygon(outline);
        }
    }
}
