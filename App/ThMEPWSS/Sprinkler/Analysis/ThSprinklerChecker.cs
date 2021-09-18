using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public abstract class ThSprinklerChecker
    {
        public ThMEPDataSet DataSet { get; set; }

        public abstract DBObjectCollection Check(ThExtractorBase extractor, Polyline pline);

        public abstract void Present(Database database, DBObjectCollection objs);

        public DBObjectCollection SelectCrossingPolygon(DBObjectCollection objs, Polyline frame)
        {
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            transformer.Transform(frame);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = spatialIndex.SelectCrossingPolygon(frame.Vertices());
            transformer.Reset(objs);
            transformer.Reset(frame);
            return result;
        }
    }
}
