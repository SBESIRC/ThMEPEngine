using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThCADExtension;

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
        public List<string> DefaultNumbers { get; set; }
        public Matrix3d CurrentUserCoordinateSystem { get; set; } 
        /// <summary>
        /// 中心往两边偏移的1、2号线
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        public List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; set; }
        #endregion
        #region ---------- 输出 ----------
        public ObjectIdList ObjIds { get; protected set; }
        public DBObjectCollection Wires { get; protected set; }
        protected DBObjectCollection NumberTexts { get; set; }
        protected Dictionary<Point3d, double> LightPositionDict { get; set; }
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
            LightPositionDict =  new Dictionary<Point3d, double>();
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
            CurrentUserCoordinateSystem = Matrix3d.Identity;
            CenterGroupLines = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
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
            };
            return textFactory.Build();
        }
        protected Dictionary<Point3d,double> BuildLightPos()
        {            
            var lightEdges = Graphs.SelectMany(g => g.GraphEdges).ToList();
            var lightWireFactory = new ThLightBlockFactory(lightEdges);
            lightWireFactory.Build();
            return lightWireFactory.Results;
        }
        protected DBObjectCollection CreateLinkWire(List<ThLightEdge> edges)
        {
            var lightWireFactory = new ThLightLinkWireFactory(edges)
            {
                LampLength = ArrangeParameter.LampLength,
                LampSideIntervalLength = ArrangeParameter.LampSideIntervalLength,
                DefaultNumbers = DefaultNumbers,
            };
            lightWireFactory.Build();
            return lightWireFactory.Results;
        }
        protected List<ThLightNodeLink> GetCrossOppositeLinks()
        {
            // 创建十字路口对面的跳接线
            if(CenterSideDicts.Count>0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkOppositeCross();
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        protected List<ThLightNodeLink> GetThreeWayOppositeLinks()
        {
            // 创建T型路口跳接线
            if (CenterSideDicts.Count > 0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkOppositeThreeWay();
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        protected List<ThLightNodeLink> GetCrossCornerStraitLinks()
        {
            // 创建十字路口同一域具有相同1、2线的跳接线
            if (CenterSideDicts.Count > 0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkCrossCorner();
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        protected DBObjectCollection CreateThreeWayCornerJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetThreeWayCornerStraitLinks();
            if (lightNodeLinks.Count == 0)
            {
                return results;
            }
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildStraitLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }
        protected DBObjectCollection CreateCrossCornerStraitLinkJumpWire()
        {
            //绘制十字路口跨区具有相同编号的的跳线
            var results = new DBObjectCollection();
            var lightNodeLinks = GetCrossCornerStraitLinks();
            if (lightNodeLinks.Count == 0)
            {
                return results;
            }
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildStraitLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }
        protected List<ThLightNodeLink> GetThreeWayCornerStraitLinks()
        {
            // 创建T型路口跳接线
            if (CenterSideDicts.Count > 0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkThreeWayCorner(); // 连接T型拐角处
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        protected DBObjectCollection FilerLinkWire(DBObjectCollection linkWires)
        {
            var lightLines = ThBuildLightLineService.Build(LightPositionDict, ArrangeParameter.LampLength);
            var filerService = new ThFilterLinkWireService(linkWires, lightLines, ArrangeParameter.LightWireBreakLength);
            var results = filerService.Filter();
            lightLines.Dispose();
            return results;
        }

        protected DBObjectCollection BreakWire(DBObjectCollection objs, Matrix3d currentUserCoordinateSystem, double length)
        {
            var breakService = new ThBreakLineService(currentUserCoordinateSystem, length);
            return breakService.Break(objs);
        }

        protected List<ThLightEdge> GetEdges(EdgePattern edgePattern)
        {
            return GetEdges(Graphs.SelectMany(g => g.GraphEdges).ToList(), edgePattern);
        }

        protected List<ThLightEdge> GetEdges(List<ThLightEdge> edges,EdgePattern edgePattern)
        {
            return edges
                .Where(o => o.EdgePattern == edgePattern)
               .ToList();
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

        protected List<ThLightNodeLink> FindLightNodeLinkOnMainBranch(ThLightGraphService graph)
        {
            var linkService = new ThLightNodeBranchLinkService(graph)
            {
                NumberLoop = ArrangeParameter.GetLoopNumber(graph.CalculateLightNumber()),
                DefaultStartNumber = DefaultNumbers.Count > 0 ? DefaultNumbers.First() : "",
            };
            return linkService.LinkMainBranch();
        }

        protected List<ThLightNodeLink> FindLightNodeLinkOnBetweenBranch(ThLightGraphService graph)
        {
            var linkService = new ThLightNodeBranchLinkService(graph);
            return linkService.LinkBetweenBranch();
        }

        protected List<ThLightEdge> AddLinkCrossEdges()
        {
            // 将十字处、T字处具有相同EdgePattern的边直接连接
            var results = new List<ThLightEdge>();
            var edges = GetEdges();
            var calculator = new ThCrossLinkCalculator(edges, CenterSideDicts);
            calculator.BuildCrossLinkEdges().ForEach(o =>
            {
                if (!results.Select(e => e.Edge).ToList().GeometryContains(o.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE))
                {
                    results.Add(o);
                }
            });
            calculator.BuildThreeWayLinkEdges().ForEach(o =>
            {
                if (!results.Select(e => e.Edge).ToList().GeometryContains(o.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE))
                {
                    results.Add(o);
                }
            });
            return results;
        }
        protected DBObjectCollection CreateSingleRowLinkWire()
        {
            var edges = GetEdges();
            var linkWireObjs = CreateLinkWire(edges);
            linkWireObjs = FilerLinkWire(linkWireObjs);
            return linkWireObjs;
        }
        protected DBObjectCollection CreateDoubleRowLinkWire(List<ThLightEdge> edges)
        {
            var results = new DBObjectCollection();
            var firstEdges = GetEdges(edges, EdgePattern.First);
            var secondEdges = GetEdges(edges, EdgePattern.Second);
            var firstLinkWireObjs = CreateLinkWire(firstEdges);
            firstLinkWireObjs = FilerLinkWire(firstLinkWireObjs);
            var secondLinkWireObjs = CreateLinkWire(secondEdges);
            secondLinkWireObjs = FilerLinkWire(secondLinkWireObjs);
            results = results.Union(firstLinkWireObjs);
            results = results.Union(secondLinkWireObjs);
            return results;
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
            graphs.AddRange(firstEdges.CreateGraphs());
            graphs.AddRange(secondEdges.CreateGraphs());
            return graphs;
        }
        protected bool IsExsited(List<ThLightNodeLink> links, ThLightNodeLink link)
        {
            return links.Where(o => ThLinkLineUtils.IsGeometryEqual(
                o.First.Position, o.Second.Position, link.First.Position, link.Second.Position))
                .Any();
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
                                new Scale3d(ArrangeParameter.PaperRatio),
                                m.Value
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
