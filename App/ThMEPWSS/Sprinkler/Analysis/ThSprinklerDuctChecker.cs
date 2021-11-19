using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDuctChecker : ThSprinklerChecker
    {
        //风管
        public List<ThIfcDuctSegment> Ducts { get; set; }
        //弯头
        public List<ThIfcDuctElbow> Elbows { get; set; }
        //三通
        public List<ThIfcDuctTee> Tees { get; set; }
        //四通
        public List<ThIfcDuctCross> Crosses { get; set; }
        //变径
        public List<ThIfcDuctReducing> Reducings { get; set; }

        public ThSprinklerDuctChecker()
        {
            Ducts = new List<ThIfcDuctSegment>();
            Elbows = new List<ThIfcDuctElbow>();
            Tees = new List<ThIfcDuctTee>();
            Crosses = new List<ThIfcDuctCross>();
            Reducings = new List<ThIfcDuctReducing>();
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            var width = 1200.0;
            var objs = Check(entity, width);
            if(objs.Count > 0)
            {
                Present(objs);
            }
        }

        private DBObjectCollection Check(Entity entity, double width)
        {
            var results = new DBObjectCollection();
            //风管
            var ducts = Ducts.Where(o => o.Parameters.Width > width)
                             .Select(o => o.Parameters.Outline).ToCollection();
            var ductIndex = new ThCADCoreNTSSpatialIndex(ducts);
            ductIndex.SelectCrossingPolygon(entity)
                     .OfType<Polyline>()
                     .ForEach(o => results.Add(o));

            //弯头
            var elbows = Elbows.Where(o => o.Parameters.PipeOpenWidth > width)
                               .Select(o => o.Parameters.Outline).ToCollection();
            var elbowIndex = new ThCADCoreNTSSpatialIndex(elbows);
            elbowIndex.SelectCrossingPolygon(entity)
                      .OfType<Polyline>()
                      .ForEach(o => results.Add(o));

            //三通
            var tees = Tees.Where(o => o.Parameters.BranchDiameter > width
                                    || o.Parameters.MainBigDiameter > width
                                    || o.Parameters.MainSmallDiameter > width)
                           .Select(o => o.Parameters.Outline).ToCollection();
            var teeIndex = new ThCADCoreNTSSpatialIndex(tees);
            teeIndex.SelectCrossingPolygon(entity)
                    .OfType<Polyline>()
                    .ForEach(o => results.Add(o));

            //四通
            var crosses = Crosses.Where(o => o.Parameters.BigEndWidth > width
                                          || o.Parameters.MainSmallEndWidth > width
                                          || o.Parameters.SideBigEndWidth > width
                                          || o.Parameters.SideSmallEndWidth > width)
                                 .Select(o => o.Parameters.Outline).ToCollection();
            var crossIndex = new ThCADCoreNTSSpatialIndex(crosses);
            crossIndex.SelectCrossingPolygon(entity)
                      .OfType<Polyline>()
                      .ForEach(o => results.Add(o));

            //变径
            var reducings = Reducings.Where(o => o.Parameters.BigEndWidth > width
                                              || o.Parameters.SmallEndWidth > width)
                                     .Select(o => o.Parameters.Outline).ToCollection();
            var reducingIndex = new ThCADCoreNTSSpatialIndex(reducings);
            reducingIndex.SelectCrossingPolygon(entity)
                         .OfType<Polyline>()
                         .ForEach(o => results.Add(o));

            return results;
        }

        public override void Extract(Database database, Polyline pline)
        {
            // 天正风管、配件
            var engine = new ThTCHDuctRecognitionEngine();
            engine.Recognize(database, pline.Vertices());
            engine.RecognizeMS(database, pline.Vertices());
            Ducts.AddRange(engine.Elements.OfType<ThIfcDuctSegment>());

            var fittingEngine = new ThTCHFittingRecognitionEngine();
            fittingEngine.Recognize(database, pline.Vertices());
            fittingEngine.RecognizeMS(database, pline.Vertices());
            Elbows.AddRange(fittingEngine.Elbows);
            Tees.AddRange(fittingEngine.Tees);
            Crosses.AddRange(fittingEngine.Crosses);
            Reducings.AddRange(fittingEngine.Reducings);

            // AI风管、配件
            var AIDuctEngine = new ThMEPDuctExtractor();
            AIDuctEngine.Recognize(database, pline.Vertices());
            Ducts.AddRange(AIDuctEngine.Elements.OfType<ThIfcDuctSegment>());

            var list = new List<string> {"Elbow", "Tee", "Cross", "Reducing" };
            list.ForEach(o =>
            {
                var thFittingEngine = new ThMEPFittingExtractor
                {
                    Category = o
                };
                thFittingEngine.Recognize(database, pline.Vertices());
                Elbows.AddRange(thFittingEngine.Elements.OfType<ThIfcDuctElbow>());
                Tees.AddRange(thFittingEngine.Elements.OfType<ThIfcDuctTee>());
                Crosses.AddRange(thFittingEngine.Elements.OfType<ThIfcDuctCross>());
                Reducings.AddRange(thFittingEngine.Elements.OfType<ThIfcDuctReducing>());
            });
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThWSSCommon.Duct_Checker_LayerName, pline);
            CleanDimension(ThWSSCommon.Duct_Blind_Zone_LayerName, pline);
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAIDuctCheckerLayer();
                var results = new DBObjectCollection();
                objs.OfType<Polyline>().ForEach(o =>
                {
                    results.Add(o.Buffer(200).OfType<Polyline>().OrderByDescending(o => o.Area).First());
                });
                results.Outline().OfType<Polyline>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                    o.ConstantWidth = 50;
                });
            }
        }
    }
}
