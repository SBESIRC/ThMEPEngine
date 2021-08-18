using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineBuilderEngine
    {
        private const double AreaTolerance = 20.0;
        public int Count { get { return _data.Count; }}
        private DBObjectCollection _data;
        private ObjectIdCollection CollectIds { get; set; }
        //创建数据
        public ThRoomOutlineBuilderEngine(DBObjectCollection dbobjs)
        {
            _data = dbobjs;
            CollectIds = new ObjectIdCollection();
        }

        //此时拿到的数据均作了初步处理，但可能存在缝隙
        public void CloseAndFilter()
        {
            _data = _data.FilterSmallArea(AreaTolerance);//这里不知道还需不需要
            //进行Polygonizer以便Build使用
            _data = _data.Polygons();
            _data = _data.FilterSmallArea(AreaTolerance);
        }


        public DBObjectCollection Build(Point3d point)
        {
            Polyline MinPolyline = new Polyline();
            double area = double.MaxValue;
            foreach (DBObject obj in _data)
            {
                if (obj is Polyline polyline && polyline.Contains(point) && polyline.Area < area)
                {
                    MinPolyline = polyline;
                    area = polyline.Area;
                }
            }
            DBObjectCollection result = new DBObjectCollection();
            if (MinPolyline.Area==0)
                return result;
            result.Add(MinPolyline);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(_data);
            var bufferService = new Service.ThNTSBufferService();
            foreach(DBObject dbObj in spatialIndex.SelectWindowPolygon(bufferService.Buffer(MinPolyline,-1.0))) //解决NTS共边导致的错误
            {
                result.Add(dbObj);
            }
            result = result.BuildArea();
            result = ContainsPoint(result, point);
            return result;
        }
        private DBObjectCollection ContainsPoint(DBObjectCollection polygons,Point3d point)
        {
            var result = new DBObjectCollection();
            foreach (DBObject obj in polygons)
            {
                if (obj is Polyline polyline && polyline.Contains(point))
                {
                    var Dbobjs = polyline.Buffer(Roomdata.BufferDistance);
                    foreach (DBObject o in Dbobjs)
                    {
                        result.Add(o);
                    }
                }
                else if (obj is MPolygon polygon && polygon.Contains(point))
                {
                    var Dbobjs = polygon.Buffer(Roomdata.BufferDistance);
                    foreach (DBObject o in Dbobjs)
                    {
                        result.Add(o);
                    }
                }
            }
            return result;
        }
    }
}
