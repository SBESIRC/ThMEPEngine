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
                var unionLines = GetLines(objs.Cast<Polyline>().ToList()) ;
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

                //找ports
                List<Line> allLines = new List<Line>(FirstCurves.Cast<Line>().ToList());
                allLines.AddRange(SecondCurves.Cast<Line>().ToList());
                allLines.AddRange(FdxCurves.Cast<Line>().ToList());
                var ports = AddLine(allLines);

                instane.SideLinesDic.ForEach(o => sideLines.AddRange(o.Value));
                BuildCableTray(sidelines, allLines, ports);
            }
        }
        public List<Line> GetLines(List<Polyline> objs)
        {
            var lines = ThLaneLineEngine.Explode(objs.ToCollection());
            lines = ThLaneLineJoinEngine.Join(lines);
            var nodingLines = ThLaneLineEngine.Noding(lines);
            var resLines = nodingLines.Cast<Line>().Where(x => x.Length > Width * 1.5).ToList();
            var repairLines = RepairCableElbow(resLines).ToCollection();
            repairLines = ThLaneLineJoinEngine.Join(repairLines);
            nodingLines = ThLaneLineEngine.Noding(repairLines);
            resLines = nodingLines.Cast<Line>().Where(x => x.Length > Width * 1.5).ToList();
            return resLines;
        }
        private List<Line> RepairCableElbow(List<Line> lines)
        {
            var extendLines = lines.Select(x =>
                new Tuple<Line, Line, Line>(x, CreateExtendLine(x, Width + 1, true), CreateExtendLine(x, Width + 1, false))).ToList();
            var checkLines = new List<Tuple<Line, Line, Line>>(extendLines);
            List<Line> resLines = new List<Line>();
            foreach (var lineDic in extendLines)
            {
                checkLines.Remove(lineDic);
                bool sNeedExtend = checkLines.Any(x => {
                    return (x.Item2.IsIntersects(lineDic.Item2) || x.Item3.IsIntersects(lineDic.Item2))
                      && !x.Item1.IsIntersects(lineDic.Item1)
                      && (x.Item1.EndPoint - x.Item1.StartPoint).GetNormal()
                      .IsEqualTo((lineDic.Item1.EndPoint - lineDic.Item1.StartPoint).GetNormal(), new Tolerance(1, 1));
                });
                bool eNeedExtend =
                    checkLines.Any(x => {
                        return (x.Item2.IsIntersects(lineDic.Item3) || x.Item3.IsIntersects(lineDic.Item3))
                          && !x.Item1.IsIntersects(lineDic.Item1)
                          && (x.Item1.EndPoint - x.Item1.StartPoint).GetNormal()
                          .IsEqualTo((lineDic.Item1.EndPoint - lineDic.Item1.StartPoint).GetNormal(), new Tolerance(1, 1));
                    }); 

                var resLine = lineDic.Item1;
                var dir = (resLine.EndPoint - resLine.StartPoint).GetNormal();
                if (sNeedExtend && eNeedExtend)
                {
                    resLine = new Line(resLine.StartPoint - dir * (Width / 2 + 5), resLine.EndPoint + dir * (Width / 2 + 5));
                }
                else if (sNeedExtend)
                {
                    resLine = new Line(resLine.StartPoint - dir * (Width / 2 + 5), resLine.EndPoint);
                }
                else if (eNeedExtend)
                {
                    resLine = new Line(resLine.StartPoint, resLine.EndPoint + dir * (Width / 2 + 5));
                }

                resLines.Add(resLine);
            }

            return resLines;
        }
        private Line CreateExtendLine(Line line, double length, bool isStart)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var startPoint = line.StartPoint;
            if (!isStart)
            {
                dir = -dir;
                startPoint = line.EndPoint;
            }

            return new Line(startPoint, startPoint - dir * length);
        }
        private List<Line> AddLine(List<Line> allCurves)
        {
            List<Line> lines = new List<Line>();
            List<Line> checkLines = new List<Line>(allCurves);
            double moveWidth = Width / 2;
            foreach (var line in allCurves)
            {
                checkLines.Remove(line);
                var needLines = checkLines.Where(x => ThGarageLightUtils.IsPointOnLines(line.StartPoint, x)).ToList();
                if (needLines.Count <= 0)
                {
                    var dir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                    lines.Add(new Line(line.StartPoint + dir * moveWidth, line.StartPoint - dir * moveWidth));
                }

                needLines = checkLines.Where(x => ThGarageLightUtils.IsPointOnLines(line.EndPoint, x)).ToList();
                if (needLines.Count <= 0)
                {
                    var dir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                    lines.Add(new Line(line.EndPoint + dir * moveWidth, line.EndPoint - dir * moveWidth));
                }
                checkLines.Add(line);
            }

            return lines;
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
            if (objs.Count <= 0)
            {
                return objs;
            }
            objs=ThLaneLineEngine.Explode(objs);
            objs=ThLaneLineJoinEngine.Join(objs);
            objs=objs.LineMerge();
            var bufferCollection = new DBObjectCollection();
            foreach (Curve obj in objs)
            {
                bufferCollection.Add(new DBObjectCollection() { obj }.Buffer(Width / 2.0)[0]);
            }
            using (AcadDatabase db=AcadDatabase.Active())
            {
                foreach (Entity item in bufferCollection)
                {
                    //db.ModelSpace.Add(item);
                }
            }
            return bufferCollection;
        }
        private void BuildCableTray(List<Line> cableTrayLines,List<Line> centerLines, List<Line> portsLines)
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
                portsLines.ForEach(o =>
                {
                    o.Layer = RacewayParameter.PortLineParameter.Layer;
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
            CenterWithPorts.ForEach(o =>
            {
                o.Value.ForEach(v =>
                {
                    var midPt = v.StartPoint.GetMidPt(v.EndPoint);
                    ports.Add(midPt);
                });
            });
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
