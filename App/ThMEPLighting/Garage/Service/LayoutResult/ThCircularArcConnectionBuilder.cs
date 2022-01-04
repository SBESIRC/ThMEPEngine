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
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCircularArcConnectionBuilder : ThLightWireBuilder, IPrinter
    {
        #region ----------外部传入----------
        public Dictionary<string, int> DirectionConfig { get; set; }
        public Matrix3d CurrentUserCoordinateSystem { get; set; } = Matrix3d.Identity;
        #endregion
        public ObjectIdList ObjIds { get; }
        public DBObjectCollection Wires { get; private set; }
        public ThCircularArcConnectionBuilder(List<ThLightGraphService> graphs) : base(graphs)
        {
            Wires = new DBObjectCollection();
            ObjIds = new ObjectIdList();
            DirectionConfig = new Dictionary<string, int>();
        }
        public override void Build()
        {
            // 布灯点
            LightPositionDict = BuildLightPos();

            // 创建跳接线
            // 建议允许最大的回路编号是4
            var jumpWire = CreateJumpWire();
            Wires = Wires.Union(jumpWire);

            // 创建T型路口的连线
            //var threewayJumpWire = CreateThreeWayJumpWire();
            var threewayJumpWire = new DBObjectCollection();
            Wires = Wires.Union(threewayJumpWire);

            // 创建十字路口的连线
            //var crossJumpWire = CreateCrossJumpWire();
            var crossJumpWire = new DBObjectCollection();
            Wires = Wires.Union(crossJumpWire);

            // 与灯具避梁
            var avoidService = new ThCircularArcConflictAvoidService(
                ArrangeParameter.LampLength, Wires, LightPositionDict);
            avoidService.Avoid();
            Wires = avoidService.Results;

            // 创建连接线，按照灯长度把灯所在的边打断   
            var firstEdges = Graphs.SelectMany(g => g.GraphEdges).Where(o=>o.EdgePattern==EdgePattern.First).ToList();
            var secondEdges = Graphs.SelectMany(g => g.GraphEdges).Where(o => o.EdgePattern == EdgePattern.Second).ToList();
            var firstLinkWireObjs = CreateLinkWire(firstEdges);
            firstLinkWireObjs = FilerLinkWire(firstLinkWireObjs);
            var secondLinkWireObjs = CreateLinkWire(secondEdges);
            secondLinkWireObjs = FilerLinkWire(secondLinkWireObjs);
            Wires = Wires.Union(firstLinkWireObjs);
            Wires = Wires.Union(secondLinkWireObjs);

            // 创建灯文字
            NumberTexts = BuildNumberText(
                0.0,
                ArrangeParameter.LightNumberTextGap / 2.0,
                ArrangeParameter.LightNumberTextHeight,
                ArrangeParameter.LightNumberTextWidthFactor);
        }

        private DBObjectCollection CreateJumpWire()
        {
            if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Second)
            {
                return CreateJumpWire1();
            }
            else if (ArrangeParameter.ArrangeEdition == ArrangeEdition.Third)
            {
                return CreateJumpWire2();
            }
            else
            {
                return new DBObjectCollection();
            }
        }

        private DBObjectCollection CreateJumpWire1()
        {
            // 创建图链路上跳接线
            return CreateJumpWire(Graphs);
        }

        private DBObjectCollection CreateJumpWire2()
        {
            // 创建图链路上跳接线
            var results = new DBObjectCollection();
            var firstEdges = GetEdges(EdgePattern.First);
            var secondEdges = GetEdges(EdgePattern.Second);
            firstEdges.ForEach(o => o.IsTraversed = false);
            secondEdges.ForEach(o => o.IsTraversed = false);

            var graphs = new List<ThLightGraphService>();
            graphs.AddRange(firstEdges.CreateGraphs());
            graphs.AddRange(secondEdges.CreateGraphs());

            return CreateJumpWire(graphs);
        }

        private DBObjectCollection CreateJumpWire(List<ThLightGraphService> graphs)
        {
            // 创建图链路上跳接线
            var results = new DBObjectCollection();
            graphs.ForEach(g =>
            {
                var gLinks = g.Links;
                var linkService = new ThLightNodeSameLinkService(gLinks);
                var lightNodeLinks = linkService.FindLightNodeLink1();
                var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
                {
                    DefaultNumbers = this.DefaultNumbers,
                    CenterSideDicts = this.CenterSideDicts,
                    DirectionConfig = this.DirectionConfig,
                    LampLength = this.ArrangeParameter.LampLength,
                    LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                    Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine,
                };
                jumpWireFactory.Build();
                lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            });
            return results;
        }

        private DBObjectCollection CreateCrossJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetCrossJumpWireLinks();
            var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
            {
                LampLength = this.ArrangeParameter.LampLength,
                DefaultNumbers = this.DefaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.Build();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }

        private DBObjectCollection CreateThreeWayJumpWire()
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetThreeWayJumpWireLinks();
            var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
            {
                LampLength = this.ArrangeParameter.LampLength,
                DefaultNumbers = this.DefaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.Build();
            lightNodeLinks.SelectMany(l => l.JumpWires).ForEach(e => results.Add(e));
            return results;
        }

        public override void Reset()
        {
            ResetObjIds(ObjIds);
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
                Wires.OfType<Curve>().ForEach(c =>
                {
                    var objId = acadDatabase.ModelSpace.Add(c);
                    c.Layer = CableTrayParameter.JumpWireParameter.Layer;
                    c.ColorIndex = (int)ColorIndex.BYLAYER;
                    c.LineWeight = LineWeight.ByLayer;
                    c.Linetype = "ByLayer";
                    objIds.Add(objId);
                });
                return objIds;
            }
        }
    }
}
