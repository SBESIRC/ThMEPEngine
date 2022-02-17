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
using ThMEPLighting.Common;
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
        protected Dictionary<Point3d, Tuple<double,string>> LightPositionDict { get; set; }
        // 把要移除的灯存入到此词典中
        protected Dictionary<Point3d, Tuple<double, string>> RemovedLightPositionDict { get; set; }
        // 将每个回路的灯线存入到此词典中
        protected Dictionary<string,DBObjectCollection> LoopWireGroupDict { get; set; }
        #endregion
        public ThLightWireBuilder(List<ThLightGraphService> graphs)
        {
            Graphs = graphs;
            ObjIds = new ObjectIdList();
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
            double height,double gap,double textHeight,double textWidthFactor)
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
        protected void CreateLinkWire(string defaultNumber,List<ThLightEdge> edges)
        {
            var lightWireFactory = new ThLightLinkWireFactory(edges)
            {
                LampLength = ArrangeParameter.LampLength,
                DefaultNumbers = DefaultNumbers,
            };
            lightWireFactory.Build();
            LoopWireGroupDict.Add(defaultNumber, lightWireFactory.Results); // 添加默认回路的线
        }
        
        protected void CreateElbowStraitLinkJumpWire(List<ThLightEdge> edges)
        {
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            var lightNodeLinks = creator.CreateElbowStraitLinkJumpWire(edges);
            lightNodeLinks.ForEach(l=>AddToLoopWireGroup(l));
        }
        protected void CreateThreeWayStraitLinksJumpWire(List<ThLightEdge> edges)
        {
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            var lightNodeLinks = creator.CreateThreeWayStraitLinksJumpWire(edges);
            lightNodeLinks.ForEach(l=>AddToLoopWireGroup(l));
        }
        protected void CreateCrossCornerStraitLinkJumpWire(List<ThLightEdge> edges)
        {
            //绘制十字路口跨区具有相同编号的的跳线
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            var lightNodeLinks = creator.CreateCrossCornerStraitLinkJumpWire(edges);
            lightNodeLinks.ForEach(l=>AddToLoopWireGroup(l));
        }
        protected List<BranchLinkFilterPath> CreateSingleRowBranchCornerJumpWire(List<ThLightGraphService> graphs)
        {
            var results = new List<BranchLinkFilterPath>();
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
            return results;
        }
        protected void BuildMainBranchLink(List<ThLightNodeLink> lightNodeLinks)
        {
            // 用于单排布置
            var creator = new ThStraitLinkCreator(ArrangeParameter, DirectionConfig, CenterSideDicts);
            creator.CreateWireForStraitLink(lightNodeLinks);
        }

        protected DBObjectCollection FilerLinkWire1(DBObjectCollection linkWires,List<ThLightEdge> edges)
        {
            // 过滤默认灯编号的线
            var numbers = GetLightNodeNumbers(edges);
            var lightPosDict = LightPositionDict.Where(o => numbers.Contains(o.Value.Item2)).ToDictionary();

            var otherLights = lightPosDict.Where(o => !DefaultNumbers.Contains(o.Value.Item2)).ToDictionary();
            var defaultLights = lightPosDict.Where(o => DefaultNumbers.Contains(o.Value.Item2)).ToDictionary();

            var removeLights = BuildRemoveLightLines(numbers);
            var defaultLightLines = ThBuildLightLineService.Build(defaultLights, ArrangeParameter.LampLength);
            var otherLightLines = ThBuildLightLineService.Build(otherLights,ArrangeParameter.LampLength);
            linkWires = linkWires.Union(otherLightLines); // 把非默认编号的灯线传入，作为连接线使用
            linkWires = linkWires.Union(removeLights); // 把移除灯编号的线传入，作为连接线使用

            var results = FilterLinkWire1(linkWires, defaultLightLines);
            results = results.Difference(otherLightLines);
            removeLights = removeLights.OfType<DBObject>().Where(o => !results.Contains(o)).ToCollection();

            // 释放资源
            removeLights.ThDispose();
            otherLightLines.ThDispose();
            defaultLightLines.ThDispose();
            return results;
        }

        private DBObjectCollection FilterLinkWire1(DBObjectCollection wires, DBObjectCollection lights)
        {
            var filter = new ThLinkWireFilter(wires, lights);
            filter.Filter1();
            return filter.Results;
        }
        protected DBObjectCollection FilterLinkWire2(DBObjectCollection wires, List<BranchLinkFilterPath> filterPaths)
        {
            if(filterPaths.Count>0)
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
        protected void FilterUnLinkWireLight(DBObjectCollection linkWires,string number)
        {
            // 过滤没有连接线的灯
            var results = new DBObjectCollection();
            var loopLightPos = LightPositionDict.Where(o => o.Value.Item2 == number).ToDictionary();
            var filter = new ThLightFilter(linkWires, ArrangeParameter.LampLength, loopLightPos);
            filter.Filter();
            // *** 更新 ***
            //filter.Results.ForEach(v => LightPositionDict.Remove(v.Key));
            //filter.Results.ForEach(v => RemovedLightPositionDict.Add(v.Key, v.Value));
        }
        
        protected DBObjectCollection FilterJumpWire()
        {
            var results = new DBObjectCollection();
            // 过滤默认灯编号的线
            LoopWireGroupDict.ForEach(loop =>
            {
                if(!DefaultNumbers.Contains(loop.Key))
                {
                    var jumpWires = FindWires(loop.Key);
                    results = results.Union(jumpWires);
                    var loopLightPos = LightPositionDict.Where(o=>o.Value.Item2== loop.Key).ToDictionary();
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
            var breakService = new ThBreakLineService(currentUserCoordinateSystem, length);
            return breakService.Break(objs);
        }
        protected DBObjectCollection BreakWire(DBObjectCollection wires)
        {
            // 用非默认编号灯打断默认编号灯线
            var results = new DBObjectCollection();
            results = results.Union(wires.OfType<Arc>().ToCollection());
            var otherLights = LightPositionDict.Where(o => !DefaultNumbers.Contains(o.Value.Item2)).ToDictionary();
            var otherLightLines = ThBuildLightLineService.Build(otherLights,
                ArrangeParameter.LampLength + ArrangeParameter.LightWireBreakLength);
            var breakLines = ThLinkWireBreakService.Break(wires.OfType<Line>().ToCollection(), otherLightLines);
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
        protected List<ThLightEdge> GetEdges(List<ThLightEdge> edges,EdgePattern edgePattern)
        {
            return edges
                .Where(o => o.EdgePattern == edgePattern)
               .ToList();
        }

        protected List<BranchLinkFilterPath> GetFilterPath(List<BranchLinkFilterPath> paths,string number)
        {
            return paths.Where(o => o.Number == number).ToList();
        }
        protected List<string> GetLightNodeNumbers(List<ThLightEdge> edges)
        {
            return edges.SelectMany(o=>o.LightNodes).Select(o=>o.Number).Distinct().ToList();
        }
        protected List<ThLightEdge> GetEdges()
        {
            return Graphs
                .SelectMany(g => g.GraphEdges)
                .ToList();
        }
        protected List<ThLightNodeLink> FindLightNodeLinkOnSamePath(List<ThLinkPath> links)
        {
            var linkService = new ThLightNodeSameLinkService(links);
            return linkService.FindLightNodeLinkOnSamePath();
        }
        protected Tuple<List<ThLightNodeLink>,List<BranchLinkFilterPath>> FindLightNodeLinkOnMainBranch(
            ThLightGraphService graph,string defaultNumber)
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
        protected void CreateDoubleRowLinkWire(List<ThLightEdge> edges)
        {
            var firstEdges = GetEdges(edges, EdgePattern.First);
            var secondEdges = GetEdges(edges, EdgePattern.Second);
            CreateLinkWire(DefaultNumbers[0],firstEdges);
            CreateLinkWire(DefaultNumbers[1], secondEdges);
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
            if (LoopWireGroupDict.ContainsKey(link.First.Number))
            {
                var value = LoopWireGroupDict[link.First.Number];
                value = value.Union(link.JumpWires.ToCollection());
                LoopWireGroupDict[link.First.Number] = value;
            }
            else
            {
                if (!string.IsNullOrEmpty(link.First.Number))
                {
                    LoopWireGroupDict.Add(link.First.Number, link.JumpWires.ToCollection());
                }
            }
        }
        protected DBObjectCollection FindWires(string loopNumber)
        {
            return LoopWireGroupDict.ContainsKey(loopNumber) ? LoopWireGroupDict[loopNumber] : new DBObjectCollection();
        }
        protected DBObjectCollection BuildRemoveLightLines(List<ThLightEdge> edges)
        {
            var lightNodeNumbers = GetLightNodeNumbers(edges);
            lightNodeNumbers = lightNodeNumbers.Where(o => !DefaultNumbers.Contains(o)).ToList();
            return BuildRemoveLightLines(lightNodeNumbers);
        }
        protected string GetDefaultNumber(List<string> lightNodeNumbers)
        {
            foreach(var number in DefaultNumbers)
            {
                if(lightNodeNumbers.Contains(number))
                {
                    return number;
                }   
            }
            return "";
        }
        private DBObjectCollection BuildRemoveLightLines(List<string> numbers)
        {
            return RemovedLightPositionDict
                .Where(o => numbers.Contains(o.Value.Item2))
                .Select(o => ThBuildLightLineService.CreateLine(o.Key, o.Value.Item1, ArrangeParameter.LampLength))
                .ToCollection();
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
