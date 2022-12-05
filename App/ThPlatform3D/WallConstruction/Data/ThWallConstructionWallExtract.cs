using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.WallConstruction.Data
{
    public class ThWallConstructionWallVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Curve  polyline)
            {
                elements.AddRange(HandleCurve(polyline, matrix));
            }
        }
        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }
        public override bool IsBuildElement(Entity entity)
        {
            if (entity is Polyline || entity is Line)
            {
                return true;
            }
            return false;
        }

        private List<ThRawIfcBuildingElementData> HandleCurve(Curve polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline))
            {
                var clone = polyline.WashClone();
                if (clone != null && (clone is Polyline || clone is Line))
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = clone,
                    });
                }
            }
            return results;
        }

        public override bool CheckLayerValid(Entity e)
        {
            var bReturn = false;

            if (LayerFilter.Count > 0)
            {
                foreach (var lf in LayerFilter)
                {
                    bReturn = e.Layer.Contains(lf);
                    if (bReturn)
                    {
                        break;
                    }
                }
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }

    }

    public class ThWallConstructionWallExtractionEngine : ThBuildingElementExtractionEngine
    {
        private ThBuildingElementExtractionVisitor Visitor { get; set; }
        public ThWallConstructionWallExtractionEngine(ThBuildingElementExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Extract(Database database)
        {
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(Visitor);
            extractor.Extract(database);
            Results.AddRange(Visitor.Results);
        }
        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }
        public override void ExtractFromMS(Database database)
        {

        }


    }

    public class ThWallConstructionWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        private ThBuildingElementExtractionVisitor Visitor;
        public ThWallConstructionWallRecognitionEngine(ThBuildingElementExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            //---提取
            var extractEngine = new ThWallConstructionWallExtractionEngine(Visitor);
            extractEngine.Extract(database);
            //--转回原点
            var centerPt = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(centerPt);
            var newFrame = transformer.Transform(polygon);
            extractEngine.Results.ForEach(x => transformer.Transform(x.Geometry));
            //--识别框内
            Recognize(extractEngine.Results, newFrame);
            //--转回原位置
            Elements.ForEach(x => transformer.Reset(x.Outline));
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var collection = datas.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collection);
            var items = spatialIndex.SelectCrossingPolygon(polygon);
            items.OfType<Entity>().ForEach(o =>
            {
                Elements.Add(ThIfcWall.Create(o));
            });
        }
        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {

        }
    }


}
