using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Sprinkler.Data;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using ThMEPEngineCore.Model.Hvac;
using Autodesk.AutoCAD.DatabaseServices;

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

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var width = 1200.0;
            var objs = Check(pline, width);
            if(objs.Count > 0)
            {
                Present(objs);
            }
        }

        private DBObjectCollection Check(Polyline pline, double width)
        {
            var results = new DBObjectCollection();
            //风管
            var ducts = Ducts.Where(o => o.Parameters.Width > width)
                             .Select(o => o.Parameters.Outline).ToCollection();
            var ductIndex = new ThCADCoreNTSSpatialIndex(ducts);
            ductIndex.SelectCrossingPolygon(pline)
                     .OfType<Polyline>()
                     .ForEach(o => results.Add(o));

            //弯头
            var elbows = Elbows.Where(o => o.Parameters.PipeOpenWidth > width)
                               .Select(o => o.Parameters.Outline).ToCollection();
            var elbowIndex = new ThCADCoreNTSSpatialIndex(elbows);
            elbowIndex.SelectCrossingPolygon(pline)
                      .OfType<Polyline>()
                      .ForEach(o => results.Add(o));

            //三通
            var tees = Tees.Where(o => o.Parameters.BranchDiameter > width
                                    || o.Parameters.MainBigDiameter > width
                                    || o.Parameters.MainSmallDiameter > width)
                           .Select(o => o.Parameters.Outline).ToCollection();
            var teeIndex = new ThCADCoreNTSSpatialIndex(tees);
            teeIndex.SelectCrossingPolygon(pline)
                    .OfType<Polyline>()
                    .ForEach(o => results.Add(o));

            //四通
            var crosses = Crosses.Where(o => o.Parameters.BigEndWidth > width
                                          || o.Parameters.MainSmallEndWidth > width
                                          || o.Parameters.SideBigEndWidth > width
                                          || o.Parameters.SideSmallEndWidth > width)
                                 .Select(o => o.Parameters.Outline).ToCollection();
            var crossIndex = new ThCADCoreNTSSpatialIndex(crosses);
            crossIndex.SelectCrossingPolygon(pline)
                      .OfType<Polyline>()
                      .ForEach(o => results.Add(o));

            //变径
            var reducings = Reducings.Where(o => o.Parameters.BigEndWidth > width
                                              || o.Parameters.SmallEndWidth > width)
                                     .Select(o => o.Parameters.Outline).ToCollection();
            var reducingIndex = new ThCADCoreNTSSpatialIndex(reducings);
            reducingIndex.SelectCrossingPolygon(pline)
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
            var list = new List<string> { "Duct", "Elbow", "Tee", "Cross", "Reducing" };
            list.ForEach(o =>
            {
                var thEngine = new ThSprinklerDuctExtractor();
                thEngine.Category = o;
                thEngine.Recognize(database, pline.Vertices());
                Ducts.AddRange(thEngine.Elements.OfType<ThIfcDuctSegment>());
                Elbows.AddRange(thEngine.Elements.OfType<ThIfcDuctElbow>());
                Tees.AddRange(thEngine.Elements.OfType<ThIfcDuctTee>());
                Crosses.AddRange(thEngine.Elements.OfType<ThIfcDuctCross>());
                Reducings.AddRange(thEngine.Elements.OfType<ThIfcDuctReducing>());
            });
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThSprinklerCheckerLayer.Duct_Checker_LayerName, pline);
            CleanDimension(ThSprinklerCheckerLayer.Duct_Blind_Zone_LayerName, pline);
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
