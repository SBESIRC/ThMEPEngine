using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamAreaBuilderEngine : IDisposable
    {
        private DBObjectCollection Columns { get; set; }
        public DBObjectCollection BeamAreas { get; private set; }
        public ThBeamAreaBuilderEngine()
        {
            Columns = new DBObjectCollection();
            BeamAreas = new DBObjectCollection();
        }

        public void Dispose()
        {
            Columns.OfType<Entity>().ForEach(e => e.Dispose());
        }
        public void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var elements = Extract(db);

            // 获取柱子
            Columns = GetColumns(db, pts);

            // 识别
            BeamAreas = Recognize(elements, pts);
        }
        private List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            switch(ThExtractBeamConfig.Instance.BeamEngineOption)
            {
                case BeamEngineOps.DB:
                    return ExtractDB3Beam(db);
                case BeamEngineOps.Layer:
                    return ExtractRawBeam(db);
                case BeamEngineOps.BeamArea:
                    return ExtractRawBeamSecond(db);
                default:
                    return new List<ThRawIfcBuildingElementData>();
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

        private List<ThRawIfcBuildingElementData> ExtractRawBeamSecond(Database db)
        {
            var visitor = new ThRawBeamExtractionSecondVisitor()
            {
                LayerFilter = ThExtractBeamConfig.Instance.GetSelectLayers(db).ToHashSet()
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(db);
            return visitor.Results;
        }        

        private DBObjectCollection GetColumns(Database db, Point3dCollection pts)
        {
            var builder = new ThColumnBuilderEngine();
            builder.Build(db, pts);
            return builder.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            switch (ThExtractBeamConfig.Instance.BeamEngineOption)
            {
                case BeamEngineOps.DB:
                    var db3Beams = RecognizeDB3Beams(datas, pts);
                    var results1 = RecognizeBeamAreas(db3Beams, Columns);
                    db3Beams.MDispose();
                    return results1;
                case BeamEngineOps.Layer:
                    var firstRawBeams = RecognizeFirstRawBeams(datas, pts);
                    var results2 = RecognizeBeamAreas(firstRawBeams, Columns);
                    firstRawBeams.MDispose();
                    return results2;
                case BeamEngineOps.BeamArea:
                    var secondRawBeams = RecognizeSecondRawBeams(datas, pts);
                    var results3 = RecognizeBeamAreas(secondRawBeams, Columns);
                    secondRawBeams.MDispose();
                    return results3;
                default:
                    return new DBObjectCollection();
            }
        }
        private DBObjectCollection RecognizeSecondRawBeams(List<ThRawIfcBuildingElementData> datas,
            Point3dCollection pts)
        {
            var results = datas.Select(o => o.Geometry).ToCollection();
            if (pts.Count > 0)
            {
                var center = pts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(center);
                var newPts = transformer.Transform(pts);
                transformer.Transform(results);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(results);
                var objs = spatialIndex.SelectCrossingPolygon(newPts);
                transformer.Reset(results);
                return objs;
            }
            else
            {
                return results;
            }
        }
        private DBObjectCollection RecognizeDB3Beams(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            // 移动到近原点位置
            var transformer = new ThMEPOriginTransformer();
            if(pts.Count>0)
            {
                var center = pts.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(datas.Select(o=>o.Geometry).ToCollection());
            }
            var newPts = transformer.Transform(pts);
            var elements = ToBuildingElements(datas);
            var lineBeams = elements.OfType<ThIfcLineBeam>().ToList();
            lineBeams.ForEach(o => o.TransformBy(transformer.Displacement));

            var engine = new ThDB3BeamRecognitionEngine();
            engine.Recognize(lineBeams.OfType<ThIfcBuildingElement>().ToList(), newPts);

            // 恢复到原始位置
            var results = engine.Elements.Select(o=>o.Outline).ToCollection();
            transformer.Reset(results);
            return results;
        }

        private DBObjectCollection RecognizeFirstRawBeams(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            // 移动到近原点位置
            var transformer = new ThMEPOriginTransformer();
            if (pts.Count > 0)
            {
                var center = pts.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(datas.Select(o => o.Geometry).ToCollection());
            }
            datas.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = transformer.Transform(pts);
            var engine = new ThRawBeamRecognitionEngine();
            engine.Recognize(datas, newPts);

            // 恢复到原始位置
            var results = engine.Elements.Select(o => o.Outline).ToCollection();
            transformer.Reset(results);
            return results;
        }

        private DBObjectCollection RecognizeBeamAreas(DBObjectCollection beams,DBObjectCollection columns)
        {
            var builder = new ThBeamAreaBuilder(beams, columns);
            builder.Build();
            return builder.Results;
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
