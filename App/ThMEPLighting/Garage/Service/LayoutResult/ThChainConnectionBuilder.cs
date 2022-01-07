using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPEngineCore;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThChainConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        #region ----------外部传入----------
        public Dictionary<string, int> DirectionConfig { get; set; }
        public Matrix3d CurrentUserCoordinateSystem { get; set; } = Matrix3d.Identity;
        #endregion
        public ObjectIdList ObjIds { get; }
        public DBObjectCollection Wires {get;private set;}
        public ThChainConnectionBuilder(List<ThLightGraphService> graphs):base(graphs)
        {
            Wires = new DBObjectCollection();
            ObjIds = new ObjectIdList(); 
            DirectionConfig = new Dictionary<string, int>();
        }
        public override void Build()
        {
            // 布灯点
            LightPositionDict = BuildLightPos();

            // 连线
            if (ArrangeParameter.IsSingleRow)
            {
                BuildSingleRow(); // 单排布置
            }
            else
            {
                BuildDoubleRow(); // 双排布置
            }

            // 创建灯文字
            NumberTexts = BuildNumberText(
                ArrangeParameter.JumpWireOffsetDistance,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private void BuildSingleRow()
        {
            // 创建连接线，按照灯长度把灯所在的边打断
            var linkWireObjs = CreateSingleRowLinkWire();

            // 建议允许最大的回路编号是4
            var jumpWireRes = CreateJumpWire(Graphs);

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);

            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去
        }

        private void BuildDoubleRow()
        {
            // 布灯点
            LightPositionDict = BuildLightPos();

            // 连接交叉处
            var linkEdges = AddLinkCrossEdges();
            var totalEdges = new List<ThLightEdge>();
            totalEdges.AddRange(linkEdges);
            totalEdges.AddRange(GetEdges());

            // 将1、2线边上的灯线用灯块打断，并过滤末端
            var linkWireObjs = CreateDoubleRowLinkWire(totalEdges);

            // 创建直段上的跳线(类似于拱形)
            var jumpWireRes = CreateJumpWire(totalEdges);

            // 连接T型跨区
            var threewayJumpWireRes = CreateThreeWayCornerJumpWire();

            // 创建十字路口的线
            var crossJumpWireRes = CreateCrossCornerStraitLinkJumpWire();

            // 收集创建的线            
            Wires = Wires.Union(jumpWireRes);
            Wires = Wires.Union(crossJumpWireRes);
            Wires = Wires.Union(threewayJumpWireRes);
            Wires = BreakWire(Wires, CurrentUserCoordinateSystem, ArrangeParameter.LightWireBreakLength); // 打断
            Wires = Wires.Union(linkWireObjs); // 切记：请在BreakWire之后，添加进去

            // 创建灯文字
            NumberTexts = BuildNumberText(
                ArrangeParameter.JumpWireOffsetDistance,
                ArrangeParameter.LightNumberTextGap,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }
        private DBObjectCollection CreateSingleRowLinkWire()
        {
            var edges = GetEdges();
            var linkWireObjs = CreateLinkWire(edges);
            linkWireObjs = FilerLinkWire(linkWireObjs);
            return linkWireObjs;
        }
        private DBObjectCollection CreateDoubleRowLinkWire(List<ThLightEdge> edges)
        {
            var results = new DBObjectCollection();
            var firstEdges =GetEdges(edges, EdgePattern.First);
            var secondEdges =GetEdges(edges, EdgePattern.Second);
            var firstLinkWireObjs = CreateLinkWire(firstEdges);
            firstLinkWireObjs = FilerLinkWire(firstLinkWireObjs);
            var secondLinkWireObjs = CreateLinkWire(secondEdges);
            secondLinkWireObjs = FilerLinkWire(secondLinkWireObjs);
            results = results.Union(firstLinkWireObjs);
            results = results.Union(secondLinkWireObjs);
            return results;
        }
        private DBObjectCollection CreateJumpWire(List<ThLightEdge> edges)
        {
            // 创建跳接线
            var results = new DBObjectCollection();
            var ductionCollector = new List<Point3dCollection>();
            var firstEdges = GetEdges(edges,EdgePattern.First);
            var secondEdges = GetEdges(edges,EdgePattern.Second);
            firstEdges.ForEach(o => o.IsTraversed = false);
            secondEdges.ForEach(o => o.IsTraversed = false);

            var graphs = new List<ThLightGraphService>();
            graphs.AddRange(firstEdges.CreateGraphs());
            graphs.AddRange(secondEdges.CreateGraphs());
            return CreateJumpWire(graphs);
        }
        private DBObjectCollection CreateJumpWire(List<ThLightGraphService> graphs)
        {
            // 创建跳接线
            var results = new DBObjectCollection();
            graphs.ForEach(g =>
            {
                var linkService = new ThLightNodeSameLinkService(g.Links);
                var lightNodeLinks = linkService.FindLightNodeLink1();
                var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
                {
                    DefaultNumbers = this.DefaultNumbers,
                    CenterSideDicts = this.CenterSideDicts,
                    DirectionConfig = this.DirectionConfig,
                    LampLength = this.ArrangeParameter.LampLength,
                    LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                    OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
                };
                jumpWireFactory.Build();
                lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            return results;
        }
        private DBObjectCollection CreateThreeWayCornerJumpWire()
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
            jumpWireFactory.BuildCrossLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }
        private DBObjectCollection CreateCrossCornerStraitLinkJumpWire()
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
            jumpWireFactory.BuildCrossLinks();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }

        private List<ThLightEdge> AddLinkCrossEdges()
        {
            // 将十字处、T字处具有相同EdgePattern的边直接连接
            var results = new List<ThLightEdge>();
            var edges = GetEdges();
            var calculator = new ThCrossLinkCalculator(edges, CenterSideDicts);
            calculator.BuildCrossLinkEdges().ForEach(o =>
            {
                if(!results.Select(e=>e.Edge).ToList().GeometryContains(o.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE))
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

        public override void Reset()
        {
            ResetObjIds(ObjIds);
        }  
        private DBObjectCollection BreakWire(DBObjectCollection objs,Matrix3d currentUserCoordinateSystem,double length)
        {
            var breakService = new ThBreakLineService(currentUserCoordinateSystem, length);
            return breakService.Break(objs);
        }

        public void Print(Database db)
        {
            SetDatabaseDefault(db);
            ObjIds.AddRange(PrintNumberTexts(db));
            ObjIds.AddRange(PrintWires(db));
            ObjIds.AddRange(PrintLightBlocks(db));
        }

        private ObjectIdList PrintWires(Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objIds = new ObjectIdList();
                Wires.OfType<Line>().ForEach(l =>
                {
                    var objId = acadDatabase.ModelSpace.Add(l);
                    l.Layer = CableTrayParameter.JumpWireParameter.Layer;
                    l.ColorIndex = (int)ColorIndex.BYLAYER;
                    l.LineWeight = LineWeight.ByLayer;
                    l.Linetype = "ByLayer";
                    objIds.Add(objId);
                });
                return objIds;
            }
        }
    }
}
