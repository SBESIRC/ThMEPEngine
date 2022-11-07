using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.BlockConvert.Model;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertSpatialIndexService
    {
        public static List<ThBlockReferenceData> SelectCrossingPolygon(List<ThRawIfcDistributionElementData> blocks, Polyline frame)
        {
            var objs = blocks.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            transformer.Transform(frame);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = spatialIndex.SelectCrossingPolygon(frame.Vertices());
            var filters = blocks.Where(o => result.Contains(o.Geometry)).ToList();
            transformer.Reset(objs);
            transformer.Reset(frame);
            return filters.Select(o => o.Data as ThBlockReferenceData).ToList();
        }

        public static List<ThTCHElementData> TCHSelectCrossingPolygon(List<ThRawIfcDistributionElementData> blocks, Polyline frame)
        {
            var objs = blocks.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            transformer.Transform(frame);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = spatialIndex.SelectCrossingPolygon(frame.Vertices());
            var filters = blocks.Where(o => result.Contains(o.Geometry)).ToList();
            transformer.Reset(objs);
            transformer.Reset(frame);
            return filters.Select(o => o.Data as ThTCHElementData).ToList();
        }
    }
}
