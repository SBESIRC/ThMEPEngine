using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADExtension;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineBuilderEngine
    {
        private const double AreaTolerance = 1e-6;
        private const double BufferDistance = 50.0; //用于处理墙、门、窗、柱等元素之间不相接的Case
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
            // 把传入的数据全部转成Polygon
            _data = ToAcPolygons(_data, BufferDistance);

            // 造面
            _data = _data.PolygonsEx();   
            _data = _data.FilterSmallArea(AreaTolerance);
            //_data.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 1);

            // 新造的区域,因为扩大导致面积变小，需要扩大
            _data = Buffer(_data, BufferDistance);
            _data = _data.FilterSmallArea(AreaTolerance);
        }

        private DBObjectCollection ToPolylines(DBObjectCollection datas)
        {
            var results = new DBObjectCollection();
            datas.Cast<Entity>().ForEach(e =>
            {
                if(e is Polyline)
                {
                    results.Add(e);
                }
                else if(e is MPolygon mPolygon)
                {
                    results.Add(mPolygon.Shell());
                    mPolygon.Holes().ForEach(h => results.Add(h));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }

        private DBObjectCollection ToAcPolygons(DBObjectCollection objs,double dis)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            objs.OfType<Curve>().ForEach(e =>
            {
                if (e is Polyline poly)
                {
                    if (IsClosed(poly))
                    {
                        poly.Closed = true;
                    }
                    var newPoly = bufferService.Buffer(poly, dis);
                    if (newPoly != null)
                    {
                        results.Add(newPoly);
                    }
                }
                else if (e is Line line)
                {
                    results.Add(line.Buffer(dis, ThBufferEndCapStyle.Square));
                }
                else
                {

                }
            });            
            return results;
        }

        private DBObjectCollection Buffer(DBObjectCollection polygons, double length)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                try
                {
                    var bufferEntity = bufferService.Buffer(e, length);
                    if (bufferEntity != null)
                    {
                        results.Add(bufferEntity);
                    }
                }
                catch
                {    
                    //
                }
            });
            return results;
        }

        private bool IsClosed(Polyline poly,double tolerance =5.0)
        {
            return poly.Closed || poly.StartPoint.DistanceTo(poly.EndPoint) <= tolerance;
        }


        public Entity Query(Point3d point)
        {
            var outlines =  ContainsPoint(_data, point);
            if(outlines.Count==0)
            {
                return null;
            }
            else
            {
                return outlines.Cast<Entity>().OrderByDescending(e => e.GetArea()).First();
            }
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
                    result.Add(polyline);
                }
                else if (obj is MPolygon polygon && polygon.Contains(point))
                {
                    result.Add(polygon);
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
