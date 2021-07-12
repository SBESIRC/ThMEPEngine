using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

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
            //墙和楼板去毛皮
            _wall = _wall.BufferPolygons(-WallBufferDistance).BufferPolygons(WallBufferDistance);
            _slab = _slab.BufferPolygons(-SlabBufferDistance).BufferPolygons(SlabBufferDistance);
            //对面积为0的Polyline进行处理
            _slab = _slab.BufferZeroPolyline();
            _cornice = _cornice.BufferZeroPolyline();
            //墙和门再buffer以便形成房间洞
            _door = _door.BufferPolygons(BufferDistance);
            _wall = _wall.BufferPolygons(BufferDistance);
            //窗和线脚也可能出现没有完全搭接的情况
            _window = _window.BufferPolygons(BufferDistance);
            _cornice = _cornice.BufferPolygons(BufferDistance);
            _slab = _slab.BufferPolygons(BufferDistance);
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
    }
}

