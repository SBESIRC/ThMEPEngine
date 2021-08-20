using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADExtension;
using System.Collections.Generic;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineBuilderEngine
    {
        private const double SimplifyTolerance = 10.0;
        private const double AreaTolerance = 20.0;
        private const double RoomBufferDistance = 5.0;
        public List<DBObjectCollection> results { get; set; }
        public int Count { get { return _data.Count; }}
        private DBObjectCollection _data;
        private ObjectIdCollection CollectIds { get; set; }
        //创建数据
        public ThRoomOutlineBuilderEngine(DBObjectCollection dbobjs)
        {
            _data = dbobjs;
            CollectIds = new ObjectIdCollection();
            results = new List<DBObjectCollection>();
        }

        //此时拿到的数据均作了初步处理，但可能存在缝隙
        public void CloseAndFilter()
        {
            _data = _data.FilterSmallArea(AreaTolerance);//这里不知道还需不需要
            //进行Polygonizer以便Build使用
            _data = _data.Polygons();
            _data = _data.FilterSmallArea(AreaTolerance);
        }


        public void Build(Point3d point)
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
            if (MinPolyline.Area==0)
                return ;
            var result = new DBObjectCollection();
            result.Add(MinPolyline);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(_data);
            var bufferService = new Service.ThNTSBufferService();
            //认为索引对象均在数据层就进行了处理
            foreach(DBObject dbObj in spatialIndex.SelectWindowPolygon(bufferService.Buffer(MinPolyline,-1.0))) //解决NTS共边导致的错误
            {
                result.Add(dbObj);
            }
            result = result.BuildArea();
            result = ContainsPoint(result, point);
            results.Add(result);
            //makevalid似乎不支持DBObjectCollection作为参数
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
        public bool RoomContainPoint(Point3d point)
        {
            foreach(DBObjectCollection result in results)
            {
                foreach (DBObject obj in result)
                {
                    if (obj is Polyline polyline)
                    {
                        if (polyline.Contains(point))
                            return true;
                    }
                    else if (obj is MPolygon polygon)
                    {
                        if (polygon.Shell().Contains(point))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
