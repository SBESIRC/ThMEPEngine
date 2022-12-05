using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThPlatform3D.WallConstruction.Data
{
    internal class ThWallConstructionDataFactory
    {
        public ThMEPOriginTransformer Transformer { get; set; }

        public List<Curve> Wall { get; set; } = new List<Curve>();//polyline or line
        public List<Curve> Door { get; set; } = new List<Curve>();//polyline or line
        public List<Curve> Axis { get; set; } = new List<Curve>();//polyline or line
        public List<Curve> FloorLevel { get; set; } = new List<Curve>();//polyline or line
        public List<Curve> Moldings { get; set; } = new List<Curve>();//polyline or line
        public List<Entity> FloorNum { get; set; } = new List<Entity>();//dbtext or Mtext
        public List<Polyline> BreakLine { get; set; } = new List<Polyline>();
        public ThWallConstructionDataFactory()
        {

        }

        public void GetElements(Database database, Point3dCollection framePts)
        {
            ExtractBasicArchitechObject(database, framePts);
            ExtractDoorWindow(database, framePts);
            ExtractAxis(database, framePts);
            ExtractMoldings(database, framePts);
            ExtractBreakLine(database, framePts);
            ExtractFloorNum(database, framePts);
        }
        private void ExtractBasicArchitechObject(Database database, Point3dCollection framePts)
        {
            var visitor = new ThWallConstructionWallVisitor()
            {
                LayerFilter = new HashSet<string> { "AE-WALL" },
            };
            var items = new ThWallConstructionWallRecognitionEngine(visitor);
            items.Recognize(database, framePts);
            items.Elements.ForEach(x => Wall.Add(x.Outline as Curve));

        }
        private void ExtractDoorWindow(Database database, Point3dCollection framePts)
        {
            var visitor = new ThWallConstructionWallVisitor()
            {
                LayerFilter = new HashSet<string> { "AE-DOOR-INSD", "AE-WIND" },
            };
            var items = new ThWallConstructionWallRecognitionEngine(visitor);
            items.Recognize(database, framePts);
            items.Elements.ForEach(x => Door.Add(x.Outline as Curve));
        }
        private void ExtractAxis(Database database, Point3dCollection framePts)
        {
            var visitor = new ThWallConstructionWallVisitor()
            {
                LayerFilter = new HashSet<string> { "AD-AXIS-AXIS", "XD-AXIS-AXIS", },
            };
            var items = new ThWallConstructionWallRecognitionEngine(visitor);
            items.Recognize(database, framePts);
            items.Elements.ForEach(x => Axis.Add(x.Outline as Curve));

            var visitorL = new ThWallConstructionWallVisitor()
            {
                LayerFilter = new HashSet<string> { "AD-LEVL-HIGH", "AD-ARCH-AXIS" },
            };
            var itemsL = new ThWallConstructionWallRecognitionEngine(visitorL);
            itemsL.Recognize(database, framePts);
            itemsL.Elements.ForEach(x => FloorLevel.Add(x.Outline as Curve));
        }
        private void ExtractMoldings(Database database, Point3dCollection framePts)
        {
            var visitor = new ThWallConstructionWallVisitor()
            {
                LayerFilter = new HashSet<string> { "AE-FNSH", },
            };
            var items = new ThWallConstructionWallRecognitionEngine(visitor);
            items.Recognize(database, framePts);
            items.Elements.ForEach(x => Moldings.Add(x.Outline as Curve));
        }

        private void ExtractFloorNum(Database database, Point3dCollection framePts)
        {
            var layer = new List<string> { "AD-LEVL-HIGH" };

            var textExtractor = new ThExtractTextService()
            {
                ElementLayer = layer[0],
            };
            textExtractor.Extract(database, framePts);

            FloorNum.AddRange(textExtractor.Texts);
        }

        private void ExtractBreakLine(Database database, Point3dCollection framePts)
        {
            var layerFilter = new List<string> { "AD-SIGN" };

            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var entities = acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .Where(o => IsElementLayer(layerFilter, o.Layer))
                    .Select(o => o.Clone() as Entity)
                    .ToList();

                var breakEntity = new List<Entity>();
                foreach (var e in entities)
                {
                    if (e is Polyline)
                    {
                        breakEntity.Add(e);
                    }
                    else if (e.IsTCHElement())
                    {
                        var obj = new DBObjectCollection();
                        e.Explode(obj);
                        breakEntity.AddRange(obj.OfType<Polyline>());
                    }
                }

                if (framePts.Count >= 3)
                {
                    var center = framePts.Envelope().CenterPoint();
                    var transformer = new ThMEPOriginTransformer(center);
                    var newPts = transformer.Transform(framePts);
                    breakEntity.ForEach(o => transformer.Transform(o));
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(breakEntity.ToCollection());
                    var selectItems = spatialIndex.SelectCrossingPolygon(newPts).OfType<Polyline>().ToList();
                    selectItems.ForEach(o => transformer.Reset(o));
                    BreakLine.AddRange(selectItems);
                }
            }



            //块里的
        }

        public virtual bool IsElementLayer(List<string> layerFilter, string layer)
        {
            foreach (string single in layerFilter)
            {
                if (single.ToUpper() == layer.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
