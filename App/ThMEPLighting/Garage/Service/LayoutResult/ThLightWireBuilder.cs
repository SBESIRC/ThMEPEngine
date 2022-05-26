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

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public abstract class ThLightWireBuilder
    {
        public List<string> DefaultNumbers { get; set; } = new List<string>();
        protected List<ThLightGraphService> Graphs { get; set; } = new List<ThLightGraphService>();
        public ThMEPOriginTransformer Transformer { get; set; }
        public ThCableTrayParameter CableTrayParameter { get; set; }
        public ThLightArrangeParameter ArrangeParameter { get; set; }
        protected DBObjectCollection NumberTexts { get; set; } = new DBObjectCollection();
        protected Dictionary<Point3d, double> LightPositionDict { get; set; } = new Dictionary<Point3d, double>();
        /// <summary>
        /// 中心往两边偏移的1、2号线
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>>  CenterSideDicts { get; set; } = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
        public List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; set; } = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
        public ThLightWireBuilder(List<ThLightGraphService> graphs)
        {
            Graphs = graphs;
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

        protected List<ThLightNodeLink> GetCrossJumpWireLinks()
        {
            // 创建十字路口跳接线
            if(CenterSideDicts.Count>0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkCross();
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }

        protected List<ThLightNodeLink> GetThreeWayJumpWireLinks()
        {
            // 创建T型路口跳接线
            if (CenterSideDicts.Count > 0)
            {
                var edges = Graphs.SelectMany(g => g.GraphEdges).ToList();
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LineThreeWay();
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
