using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThNTSSpatialIndexService : ISpatialIndex
    {
        private DBObjectCollection Objs { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThNTSSpatialIndexService(DBObjectCollection objs)
        {
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public void Dispose()
        {            
        }

        public DBObjectCollection SelectAll()
        {
            return SpatialIndex.SelectAll();
        }

        public DBObjectCollection SelectCrossingPolygon(Polyline polyline)
        {
            return SpatialIndex.SelectCrossingPolygon(polyline);
        }

        public DBObjectCollection SelectCrossingPolygon(Point3dCollection polygon)
        {
            return SpatialIndex.SelectCrossingPolygon(polygon);
        }

        public DBObjectCollection SelectCrossingPolygon(MPolygon mPolygon)
        {
            return SpatialIndex.SelectCrossingPolygon(mPolygon);
        }

        public DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2)
        {
            return SpatialIndex.SelectCrossingWindow(pt1, pt2);
        }

        public DBObjectCollection SelectFence(Polyline polyline)
        {
            return SpatialIndex.SelectFence(polyline);
        }

        public DBObjectCollection SelectFence(Line line)
        {
            return SpatialIndex.SelectFence(line);
        }

        public DBObjectCollection SelectWindowPolygon(Polyline polyline)
        {
            return SpatialIndex.SelectWindowPolygon(polyline);
        }

        public DBObjectCollection SelectWindowPolygon(MPolygon mPolygon)
        {
            return SpatialIndex.SelectWindowPolygon(mPolygon);
        }
    }
}
