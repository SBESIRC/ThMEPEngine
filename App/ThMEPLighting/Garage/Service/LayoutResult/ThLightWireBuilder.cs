using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public abstract class ThLightWireBuilder
    {
        #region ---------- 输入 -----------
        public ThMEPOriginTransformer Transformer { get; set; }
        protected List<ThLightGraphService> Graphs { get; set; }
        public ThCableTrayParameter CableTrayParameter { get; set; }
        public Dictionary<string, int> DirectionConfig { get; set; }
        public ThLightArrangeParameter ArrangeParameter { get; set; }
        /// <summary>
        /// 记录默认编号，单排一个值，双排两个值（顺序不能反）
        /// </summary>
        public List<string> DefaultNumbers { get; set; }
        public Matrix3d CurrentUserCoordinateSystem { get; set; }
        /// <summary>
        /// 用于跨区连线
        /// </summary>
        public List<Line> ExtendLines { get; set; }
        /// <summary>
        /// 中心往两边偏移的1、2号线
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        public List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; set; }
        #endregion
        #region ---------- 输出 -----------
        public ObjectIdList ObjIds { get; protected set; }
        // 把最后要打印的灯线存入到此词典中
        public DBObjectCollection Wires { get; protected set; }
        // 把最后要打印的灯编号文字存入到此词典中
        protected DBObjectCollection NumberTexts { get; set; }
        // 把最后要打印的灯存入到此词典中
        protected Dictionary<Point3d, Tuple<double, string>> LightPositionDict { get; set; }
        // 把要移除的灯存入到此词典中
        protected Dictionary<Point3d, Tuple<double, string>> RemovedLightPositionDict { get; set; }
        // 将每个回路的灯线存入到此词典中
        protected Dictionary<string, DBObjectCollection> LoopWireGroupDict { get; set; }
        #endregion
        public ThLightWireBuilder(List<ThLightGraphService> graphs)
        {
            Graphs = graphs;
            ObjIds = new ObjectIdList();
            ExtendLines = new List<Line>();
            Wires = new DBObjectCollection();
            DefaultNumbers = new List<string>();
            NumberTexts = new DBObjectCollection();
            DirectionConfig = new Dictionary<string, int>();
            CableTrayParameter = new ThCableTrayParameter();
            ArrangeParameter = new ThLightArrangeParameter();
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
            CurrentUserCoordinateSystem = Matrix3d.Identity;
            LoopWireGroupDict = new Dictionary<string, DBObjectCollection>();
            LightPositionDict = new Dictionary<Point3d, Tuple<double, string>>();
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
            CenterGroupLines = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
            RemovedLightPositionDict = new Dictionary<Point3d, Tuple<double, string>>();
        }
        public abstract void Build();
        public abstract void Reset();
        protected DBObjectCollection BuildNumberText(
            double height, double gap, double textHeight, double textWidthFactor)
        {
            var lightEdges = Graphs.SelectMany(g => g.GraphEdges).ToList();
            var textFactory = new ThLightNumberTextFactory(lightEdges)
            {
                Gap = gap,
                Height = height,
                TextHeight = textHeight,
                TextWidthFactor = textWidthFactor,
                LightPositionDict = LightPositionDict
            };
            return textFactory.Build();
        }
        protected Dictionary<Point3d, Tuple<double, string>> BuildLightPos()
        {
            var lightEdges = Graphs.SelectMany(g => g.GraphEdges).ToList();
            var lightWireFactory = new ThLightBlockFactory(lightEdges);
            lightWireFactory.Build();
            return lightWireFactory.Results;
        }
        /// <summary>
        /// 用于处理默认灯号线
        /// </summary>
        /// <param name="lightNumber"></param>
        protected void CutLinkWireByLights(string lightNumber)
        {
            // 灯在线上，用线把灯裁剪掉
            //                  WL01                              WL01
            // -----------------------------------------------------------------------
            // eg. 传入的是WL01,得到：
            //                  WL01                              WL01
            // ------------------***-------------------------------***----------------
            var lights = BuildLightLines(lightNumber); // 创建灯线
            var wires = GetLinkWires(lightNumber);
            var lines = wires.OfType<Line>().ToCollection();
            var others = wires.Difference(lines);
            var results = lines.Break(lights);
            results = results.Union(others);
            AddToLoopWireGroup(lightNumber, results, true); // 添加默认回路的线
        }

        protected void CreateStraitLinkJumpWire(List<ThLightEdge> edges)
        {
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            var lightNodeLinks = creator.CreateStraitLinkJumpWire(edges, DefaultNumbers);
            if (ArrangeParameter.IsDoubleRow)
            {
                var links = creator.CreateStraitLinkJumpWire(ThDoubleRowLinker.DoubleRowMode(edges, ArrangeParameter.DoubleRowOffsetDis), DefaultNumbers);
                lightNodeLinks.AddRange(links);
            }
            lightNodeLinks.ForEach(l => AddToLoopWireGroup(l));
        }

        protected void CreateSingleRowBranchCornerJumpWire(List<ThLightGraphService> graphs, List<BranchLinkFilterPath> results)
        {
            /*
             *                         |
             *                         p1 WL01
             *                         |  \
             *                         |   \
             *                         |    \
             *  -----------------------p2----p3------------------------------
             *                          (b) WL01
             *  p1,p3是灯点，p2是分支点                        
             * 若从p1连到p3,返回 p2-p3的边
             * 若从p3连到p1,返回 p2-p1的边
             */
            // 连接主分支到分支的跳线
            graphs.ForEach(g =>
            {
                var defaultNumber = GetDefaultNumber(g.GraphEdges.SelectMany(o => o.LightNodes).Select(o => o.Number).ToList());
                if (!string.IsNullOrEmpty(defaultNumber))
                {
                    var res = FindLightNodeLinkOnMainBranch(g, defaultNumber);
                    results.AddRange(res.Item2);
                    BuildMainBranchLink(res.Item1);
                    res.Item1.ForEach(l => AddToLoopWireGroup(l));
                }
            });
        }
        protected void BuildMainBranchLink(List<ThLightNodeLink> lightNodeLinks)
        {
            // 用于单排布置
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            creator.CreateWireForStraitLink(lightNodeLinks);
        }

        protected DBObjectCollection BuildLightLines(string lightNumber)
        {
            var lightPosDict = GetLightPosition(lightNumber);
            return ThBuildLightLineService.Build(lightPosDict, ArrangeParameter.LampLength);
        }

        private Dictionary<Point3d, Tuple<double, string>> GetLightPosition(string lightNumber)
        {
            return LightPositionDict.Where(o => lightNumber == o.Value.Item2).ToDictionary();
        }

        protected DBObjectCollection FilterLinkWire1(DBObjectCollection wires, DBObjectCollection lights)
        {
            // 过滤默认编号灯末端没有连接的线
            var filter = new ThLinkWireFilter(wires, lights);
            filter.Filter1();
            return filter.Results;
        }
        protected DBObjectCollection FilterLinkWire2(DBObjectCollection wires, List<BranchLinkFilterPath> filterPaths)
        {
            //
            if (filterPaths.Count > 0)
            {
                var filter = new ThLinkWireFilter(wires, filterPaths);
                filter.Filter2();
                return filter.Results;
            }
            else
            {
                return wires;
            }
        }
        protected void FilterUnLinkWireLight(DBObjectCollection linkWires, string number)
        {
            // 过滤没有连接线的灯
            var results = new DBObjectCollection();
            var loopLightPos = GetLightPosition(number);
            var filter = new ThLightFilter(linkWires, ArrangeParameter.LampLength, loopLightPos);
            filter.Filter();
            // *** 更新 ***
            //filter.Results.ForEach(v => LightPositionDict.Remove(v.Key));
            //filter.Results.ForEach(v => RemovedLightPositionDict.Add(v.Key, v.Value));
        }

        protected DBObjectCollection FilterDefaultLinkWires(string defaultNumber, List<BranchLinkFilterPath> branchFilterPaths)
        {
            // 过滤默认编号的连接线
            var linkWires = GetLinkWires(defaultNumber);

            // 移除分支连接的线
            var default1BranchFilterPath = GetFilterPath(branchFilterPaths, defaultNumber);
            linkWires = FilterLinkWire2(linkWires, default1BranchFilterPath);
            var lightLines = BuildLightLines(defaultNumber);
            linkWires = FilterLinkWire1(linkWires, lightLines);
            //FilterUnLinkWireLight(linkWires, defaultNumber); // 过滤没有连接线的灯

            lightLines.MDispose();
            return linkWires;
        }

        private DBObjectCollection Merge(DBObjectCollection wires)
        {
            var results = new DBObjectCollection();
            var lines = wires.OfType<Line>().ToCollection();
            results = results.Union(wires.Difference(lines));
            var cleaner = new ThLaneLineCleanService();
            var newLines = cleaner.Clean(lines);
            results = results.Union(newLines);
            return results;
        }

        protected DBObjectCollection FilterJumpWire()
        {
            var results = new DBObjectCollection();
            // 过滤默认灯编号的线
            LoopWireGroupDict.ForEach(loop =>
            {
                if (!DefaultNumbers.Contains(loop.Key))
                {
                    var jumpWires = GetLinkWires(loop.Key);
                    results = results.Union(jumpWires);
                    var loopLightPos = GetLightPosition(loop.Key);
                    var filter = new ThJumpWireFilter(jumpWires, ArrangeParameter.LampLength, loopLightPos);
                    filter.Filter();

                    // *** 更新 ***
                    //filter.RemovedLightPos.ForEach(v => LightPositionDict.Remove(v.Key));
                    //filter.RemovedLightPos.ForEach(v => RemovedLightPositionDict.Add(v.Key,v.Value));
                }
            });
            return results;
        }
        protected DBObjectCollection BreakWire(DBObjectCollection objs, Matrix3d currentUserCoordinateSystem, double length)
        {
            // 对存在交叉的线路进行短线，更偏向于Y轴的线路被更偏向于X轴的线路打断，短线间距300（弧线不打断）
            var breakService = new ThBreakLineService(currentUserCoordinateSystem);
            return breakService.BreakByHeight(objs, length / 2.0);
        }
        protected DBObjectCollection BreakWire(DBObjectCollection wires)
        {
            // 用非默认编号灯打断默认编号灯线
            //                          WL02
            //----------WL01--------------X----------WL01---------
            //                          WL02
            //----------WL01------------  X  --------WL01---------
            var results = new DBObjectCollection();
            results = results.Union(wires.OfType<Arc>().ToCollection());
            var otherLights = LightPositionDict.Where(o => !DefaultNumbers.Contains(o.Value.Item2)).ToDictionary();
            var otherLightLines = ThBuildLightLineService.Build(otherLights,
                ArrangeParameter.LampLength + ArrangeParameter.LightWireBreakLength);
            var breakLines = wires.OfType<Line>().ToCollection().Break(otherLightLines);
            results = results.Union(breakLines);
            otherLightLines.ThDispose();
            return results;
        }
        protected DBObjectCollection MergeWire(DBObjectCollection linkWires)
        {
            var results = new DBObjectCollection();
            var lines = linkWires.OfType<Line>().ToCollection();
            var others = linkWires.Difference(lines);
            var mergeRes = ThMergeLightLineService.MergeNewLines(lines.OfType<Line>().ToList());
            results = results.Union(others);
            results = results.Union(mergeRes.ToCollection());
            return results;
        }
        protected List<ThLightEdge> GetEdges(List<ThLightEdge> edges, EdgePattern edgePattern)
        {
            return edges.Where(o => o.EdgePattern == edgePattern).ToList();
        }

        protected List<BranchLinkFilterPath> GetFilterPath(List<BranchLinkFilterPath> paths, string number)
        {
            return paths.Where(o => o.Number == number).ToList();
        }
        protected List<string> GetLightNodeNumbers(List<ThLightEdge> edges)
        {
            return edges.SelectMany(o => o.LightNodes).Select(o => o.Number).Distinct().ToList();
        }
        protected List<ThLightEdge> GetEdges()
        {
            return Graphs.SelectMany(g => g.GraphEdges).ToList();
        }
        protected List<ThLightNodeLink> FindLightNodeLinkOnSamePath(List<ThLinkPath> links)
        {
            var linkService = new ThLightNodeSameLinkService(links);
            return linkService.FindLightNodeLinkOnSamePath();
        }
        protected Tuple<List<ThLightNodeLink>, List<BranchLinkFilterPath>> FindLightNodeLinkOnMainBranch(
            ThLightGraphService graph, string defaultNumber)
        {
            var linkService = new ThLightNodeBranchLinkService(graph)
            {
                DefaultStartNumber = defaultNumber,
            };
            var nodeLinks = linkService.LinkMainBranch();
            return Tuple.Create(nodeLinks, linkService.BranchLinkFilterPaths);
        }
        protected List<ThLightNodeLink> FindLightNodeLinkOnBetweenBranch(ThLightGraphService graph)
        {
            var linkService = new ThLightNodeBranchLinkService(graph);
            return linkService.LinkBetweenBranch();
        }
        protected List<ThLightGraphService> BuildGraphs(List<ThLightEdge> edges)
        {
            // 为了1、2号线使用
            var results = new DBObjectCollection();
            var ductionCollector = new List<Point3dCollection>();
            var firstEdges = GetEdges(edges, EdgePattern.First);
            var secondEdges = GetEdges(edges, EdgePattern.Second);
            firstEdges.ForEach(o => o.IsTraversed = false);
            secondEdges.ForEach(o => o.IsTraversed = false);

            var graphs = new List<ThLightGraphService>();
            graphs.AddRange(firstEdges.CreateCdzmGraphs());
            graphs.AddRange(secondEdges.CreateCdzmGraphs());
            return graphs;
        }
        protected bool IsExsited(List<ThLightNodeLink> links, ThLightNodeLink link)
        {
            return links.Where(o => ThLinkLineUtils.IsGeometryEqual(
                o.First.Position, o.Second.Position, link.First.Position, link.Second.Position))
                .Any();
        }
        protected void AddToLoopWireGroup(ThLightNodeLink link)
        {
            // 此函数用来收集每个回路的灯线，用于后期过滤线
            AddToLoopWireGroup(link.First.Number, link.JumpWires.ToCollection());
        }

        protected void AddToLoopWireGroup(string lightNumber, DBObjectCollection objs, bool isForceReplace = false)
        {
            if (LoopWireGroupDict.ContainsKey(lightNumber))
            {
                if (isForceReplace)
                {
                    LoopWireGroupDict[lightNumber] = objs;
                }
                else
                {
                    var values = LoopWireGroupDict[lightNumber];
                    values = values.Union(objs);
                    LoopWireGroupDict[lightNumber] = values;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(lightNumber))
                {
                    LoopWireGroupDict.Add(lightNumber, objs);
                }
            }
        }

        protected DBObjectCollection GetLinkWires(string loopNumber)
        {
            return LoopWireGroupDict.ContainsKey(loopNumber) ? LoopWireGroupDict[loopNumber] : new DBObjectCollection();
        }
        protected string GetDefaultNumber(List<string> lightNodeNumbers)
        {
            foreach (var number in DefaultNumbers)
            {
                if (lightNodeNumbers.Contains(number))
                {
                    return number;
                }
            }
            return "";
        }
        protected DBObjectCollection BreakByLights(DBObjectCollection wires)
        {
            var lights = ThBuildLightLineService.Build(LightPositionDict, ArrangeParameter.LampLength);
            var results = BreakByLights(wires, lights, ArrangeParameter.LightWireBreakLength);
            lights.MDispose();
            return results;
        }
        private DBObjectCollection BreakByLights(DBObjectCollection wires, DBObjectCollection lights, double breakLength)
        {
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(lights);
            Func<Polyline, DBObjectCollection> QueryLights = (p) =>
               {
                   return spatialIndex.SelectCrossingPolygon(p);
               };
            wires.OfType<Curve>().ForEach(curve =>
            {
                if (curve is Line wire)
                {
                    var direction = wire.LineDirection();
                    var outline = ThDrawTool.ToRectangle(wire.StartPoint, wire.EndPoint, 1.0);
                    var crossLights = QueryLights(outline);
                    var breakLines = new List<Line>();
                    crossLights.OfType<Line>().ForEach(light =>
                    {
                        var inters = ThGeometryTool.IntersectWithEx(wire, light);
                        if (inters.Count > 0)
                        {
                            var pt1 = inters[0] + direction.MultiplyBy(breakLength / 2.0);
                            var pt2 = inters[0] - direction.MultiplyBy(breakLength / 2.0);
                            if (ThGeometryTool.IsPointInLine(wire.StartPoint, wire.EndPoint, pt1, 5.0) &&
                            ThGeometryTool.IsPointInLine(wire.StartPoint, wire.EndPoint, pt2, 5.0))
                            {
                                breakLines.Add(new Line(pt1, pt2));
                            }
                        }
                    });
                    if (breakLines.Count > 0)
                    {
                        wire.Difference(breakLines).ForEach(o => results.Add(o));
                    }
                    else
                    {
                        results.Add(wire);
                    }
                    outline.Dispose();
                }
                else
                {
                    results.Add(curve);
                }
            });
            return results;
        }

        #region----------Printer----------
        protected void SetDatabaseDefault(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                ArrangeParameter.SetDatabaseDefaults();
                CableTrayParameter.SetDatabaseDefaults();
            }
        }
        protected ObjectIdList PrintNumberTexts(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objIds = new ObjectIdList();
                NumberTexts.OfType<DBText>().ForEach(m =>
                {
                    objIds.Add(acadDatabase.ModelSpace.Add(m));
                    m.ColorIndex = (int)ColorIndex.BYLAYER;
                    m.Layer = CableTrayParameter.NumberTextParameter.Layer;
                    m.TextStyleId = acadDatabase.TextStyles.Element(ArrangeParameter.LightNumberTextStyle).Id;

                    // 双排双回路模式，为不影响之前的代码结构，这里考虑在输出时偷梁换柱，在这里替换输出回路编号
                    if (ArrangeParameter.IsDoubleRow)
                    {
                        switch (m.TextString)
                        {
                            case "WL01":
                                break;
                            case "WL02":
                                break;
                            case "WL03":
                                m.TextString = "WL02";
                                break;
                            case "WL04":
                                m.TextString = "WL01";
                                break;
                        }
                    }
                });
                return objIds;
            }
        }
        protected ObjectIdList PrintLightBlocks(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objIds = new ObjectIdList();
                LightPositionDict.ForEach(m =>
                {
                    ObjectId blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                CableTrayParameter.LaneLineBlockParameter.Layer,
                                ThGarageLightCommon.LaneLineLightBlockName,
                                m.Key,
                                new Scale3d(100.0),
                                m.Value.Item1
                                );
                    objIds.Add(blkId);
                });
                return objIds;
            }
        }
        #endregion
        protected void ResetObjIds(ObjectIdList objIds)
        {
            if (objIds.Count > 0)
            {
                using (AcadDatabase acadDb = AcadDatabase.Use(objIds[0].Database))
                {
                    var objs = objIds.Select(o => acadDb.Element<Entity>(o)).ToCollection();
                    objs.UpgradeOpen();
                    Transformer.Reset(objs);
                    objs.DowngradeOpen();
                }
            }
        }
    }
}
