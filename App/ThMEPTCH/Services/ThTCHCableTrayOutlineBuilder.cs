using System;
using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Engine;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.Services
{
    public class ThTCHCableTrayOutlineBuilder:IDisposable
    {
        private double _bufferLength = 1.0;
        private double _areaTolerance = 1.0;
        public ThTCHCableTrayOutlineBuilder()
        {
        }
        public void Dispose()
        {            
        }
        public DBObjectCollection Build(Database dabase,Point3dCollection pts)
        {
            // 提取
            var allSegments = ExtractTCHSegments(dabase);
            var allFittings = ExtractTCHFittings(dabase);

            // 移到近似原点
            var transformer = new ThMEPOriginTransformer(Point3d.Origin);
            if (pts.Count > 2)
            {
                var center = pts.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                if (allSegments.Count>0)
                {
                    transformer = new ThMEPOriginTransformer(allSegments);
                }
                else
                {
                    transformer = new ThMEPOriginTransformer(allFittings);
                }
            }
            transformer.Transform(allSegments);
            transformer.Transform(allFittings);
            var newPts = transformer.Transform(pts);
            var segments = Filter(allSegments, newPts);
            var fittings = Filter(allFittings, newPts);

            // 构建轮廓
            var outlines = Build(segments, fittings);

            // 还原到近似位置
            transformer.Reset(allSegments);
            transformer.Reset(allFittings);
            transformer.Reset(outlines);

            return outlines;
        }

        public DBObjectCollection Build(DBObjectCollection segments,DBObjectCollection fittings)
        {
            var bufferSegments = Buffer(segments, _bufferLength);
            var bufferFittings = Buffer(fittings, _bufferLength);
            var polygons = new DBObjectCollection();
            polygons = polygons.Union(bufferSegments);
            polygons = polygons.Union(bufferFittings);
            var unionPolygons = polygons.UnionPolygons(true);
            var results = Buffer(unionPolygons, -1.0 * _bufferLength);
            results = Clean(results);
            return results;
        }

        private DBObjectCollection Clean(DBObjectCollection objs)
        {
            return objs.FilterSmallArea(_areaTolerance);
        }

        private DBObjectCollection Buffer(DBObjectCollection polygons, double bufferLength)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var entity = bufferService.Buffer(e, bufferLength);
                if (entity != null)
                {
                    results.Add(entity);
                }
            });
            return results;
        }

        private DBObjectCollection Filter(DBObjectCollection objs,Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectCrossingPolygon(pts);
        }

        private DBObjectCollection ExtractTCHSegments(Database dabase)
        {
            var engine = new ThTCHCableCarrierSegmentExtractionEngine();
            engine.Extract(dabase);
            engine.ExtractFromMS(dabase);
            return engine.Results.Select(o => o.Geometry).ToCollection();
        }

        private DBObjectCollection ExtractTCHFittings(Database dabase)
        {
            var engine = new ThTCHCableCarrierFittingExtractionEngine();
            engine.Extract(dabase);
            engine.ExtractFromMS(dabase);
            return engine.Results.Select(o => o.Geometry).ToCollection();
        }
    }
}
