using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using Dreambuild;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace ThMEPEngineCore.Engine
{
    public class Roomdata
    {
        public static double BufferDistance = 10.0;
        private const double WallBufferDistance = 20.0;
        private const double SlabBufferDistance = 20.0;
        private DBObjectCollection _wall; //仅支持Polyline
        private DBObjectCollection _door; //仅支持Polyline
        private DBObjectCollection _window; //仅支持Polyline
        private DBObjectCollection _slab;  //仅支持Polyline
        private DBObjectCollection _cornice; //仅支持Polyline
        public Roomdata(Database database, Point3dCollection polygon)
        {
            var wallengine = new ThDB3ArchWallRecognitionEngine();
            wallengine.Recognize(database, polygon);
            _wall = wallengine.Elements.Select(o => o.Outline).ToList().ToCollection();
            var doorengine = new ThDB3DoorRecognitionEngine();
            doorengine.Recognize(database, polygon);
            _door = doorengine.Elements.Select(o => o.Outline).ToList().ToCollection();
            var windowengine = new ThDB3WindowRecognitionEngine();
            windowengine.Recognize(database, polygon);
            _window = windowengine.Elements.Select(o => o.Outline).ToList().ToCollection();
            var slabengine = new ThDB3SlabRecognitionEngine();
            slabengine.Recognize(database, polygon);
            _slab = slabengine.Elements.Select(o => o.Outline).ToList().ToCollection();
            var corniceengine = new ThDB3CorniceRecognitionEngine();
            corniceengine.Recognize(database, polygon);
            _cornice = corniceengine.Elements.Select(o => o.Outline).ToList().ToCollection();
        }
        /// <summary>
        /// 拿到数据后根据需求去毛皮
        /// </summary>
        public void Deburring()
        {
            _wall = _wall.FilterSmallArea(1.0);
            _door = _door.FilterSmallArea(1.0);
            _window = _window.FilterSmallArea(1.0);

            //墙和楼板去毛皮
            _wall = Buffer(_wall, -WallBufferDistance);
            _wall = Buffer(_wall, WallBufferDistance);
            _slab = BufferCollectionContainsLines(_slab, -SlabBufferDistance);
            _slab = BufferCollectionContainsLines(_slab, SlabBufferDistance);
            //墙和门再buffer以便形成房间洞
            _door = Buffer(_door, BufferDistance);
            _wall = Buffer(_wall, BufferDistance);
            //窗和线脚也可能出现没有完全搭接的情况
            _window = Buffer(_window, BufferDistance);
            _cornice = BufferCollectionContainsLines(_cornice, BufferDistance, true);
            _slab = BufferCollectionContainsLines(_slab, BufferDistance, true);
        }

        /// <summary>
        /// 为包含碎线的DBObjectCollection进行buffer，并保留碎线
        /// </summary>
        /// <param name="polys"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DBObjectCollection BufferCollectionContainsLines(DBObjectCollection polys,double length, bool lineflag=false)
        {
            DBObjectCollection res = new DBObjectCollection();
            polys.Cast<Entity>().ForEach(o => 
            {
                if (o is Polyline poly && ThAuxiliaryUtils.DoubleEquals(poly.Area, 0.0))
                {
                    if (length < 0)
                        res.Add(poly);//TODO: 碎线可能需要延伸一些长度
                    else if (lineflag)
                    {
                        var bufferRes = poly.ToNTSLineString().Buffer(
                            length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square }).ToDbCollection();
                        bufferRes.Cast<Entity>().ForEach(e => res.Add(e));
                    }
                    else
                        res.Add(poly);
                }
                else if(o is Polyline polygon && polygon.Area>1.0)
                    polygon.ToNTSPolygon().Buffer(length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square })
                    .ToDbCollection().Cast<Entity>()
                    .ForEach(e => res.Add(e));
            });
            return res;
        }


        /// <summary>
        /// 对每个Polyline进行buffer，而不进行多余的操作
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DBObjectCollection Buffer(DBObjectCollection polygons,double length)
        {
            var results = new DBObjectCollection();
            polygons = polygons.FilterSmallArea(1.0);
            polygons.Cast<Entity>().ForEach(e =>
            {
                e.ToNTSPolygon().Buffer(length, new BufferParameters() { JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square })
                .ToDbCollection().Cast<Entity>()
                .ForEach(o => results.Add(o));
            });
            results = results.FilterSmallArea(1.0);
            return results;
        }

        /// <summary>
        /// 将所有数据汇总打包
        /// </summary>
        /// <returns></returns>
        public DBObjectCollection MergeData()
        {
            var result = new DBObjectCollection();
            _wall.Cast<DBObject>().ForEach(o => result.Add(o));
            _door.Cast<DBObject>().ForEach(o => result.Add(o));
            _window.Cast<DBObject>().ForEach(o => result.Add(o));
            _slab.Cast<DBObject>().ForEach(o => result.Add(o));
            _cornice.Cast<DBObject>().ForEach(o => result.Add(o));
            return result;
        }
        public bool ContatinPoint3d(Point3d p)
        {
            bool res = false;
            foreach(DBObject obj in _wall)
            {
                if(obj is Polyline polyline && polyline.Area>1.0)
                {
                    if(polyline.ToNTSPolygon().Contains(p.ToNTSPoint()))
                    {
                        return true;
                    }
                }
            }//后续可能需要增加门和窗。楼板目前还不能加
            return res;
        }
    }
}

