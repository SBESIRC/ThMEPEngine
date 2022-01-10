using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThBeamBuilderEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {

        }

        public DBObjectCollection Geometries
        {
            get
            {
                return Elements.Select(e => e.Outline).ToCollection();
            }
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                return ExtractDB3Beam(db);
            }
            else
            {
                return ExtractRawBeam(db);
            }
        }

        private List<ThRawIfcBuildingElementData> ExtractDB3Beam(Database db)
        {
            var extraction = new ThDB3BeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        private List<ThRawIfcBuildingElementData> ExtractRawBeam(Database db)
        {
            var extraction = new ThRawBeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        public override void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var elements = Extract(db);

            // 识别
            Recognize(elements, pts);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                RecognizeDB3(datas, pts);
            }
            else
            {
                RecognizeRaw(datas, pts);
            }
        }

        private void RecognizeDB3(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            // 移动到近原点位置            
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            var newPts = transformer.Transform(pts);

            var elements = ToBuildingElements(datas);
            var lineBeams = elements.OfType<ThIfcLineBeam>().ToList();
            lineBeams.ForEach(o => o.TransformBy(transformer.Displacement));

            var engine = new ThDB3BeamRecognitionEngine();
            engine.Recognize(lineBeams.OfType<ThIfcBuildingElement>().ToList(), newPts);

            // 恢复到原始位置
            Elements = engine.Elements;
            Matrix3d inverse = transformer.Displacement.Inverse();
            Elements.OfType<ThIfcLineBeam>().ForEach(o => o.TransformBy(inverse));
        }

        private void RecognizeRaw(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            // 移动到近原点位置
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            datas.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = transformer.Transform(pts);
            var engine = new ThRawBeamRecognitionEngine();
            engine.Recognize(datas, newPts);

            // 恢复到原始位置
            Elements = engine.Elements;
            Matrix3d inverse = transformer.Displacement.Inverse();
            Elements.OfType<ThIfcLineBeam>().ForEach(o => o.TransformBy(inverse));
        }

        public override void Transform(Matrix3d matrix)
        {
            Elements.ForEach(o => o.TransformBy(matrix));
        }

        public void ResetSpatialIndex(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            spatialIndex.Reset(Geometries);
        }

        public IEnumerable<ThIfcBuildingElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }

        public ThIfcBuildingElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }

        private List<ThIfcBuildingElement> ToBuildingElements(List<ThRawIfcBuildingElementData> db3Elements)
        {
            return db3Elements
                .Select(o => ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation))
                .OfType<ThIfcBuildingElement>()
                .ToList();
        }
    }
}
