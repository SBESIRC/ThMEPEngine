using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Engine
{
    public class Roomdata
    {
        private const double WallBufferDistance = 20.0;
        private const double SlabBufferDistance = 20.0;
        private DBObjectCollection _wall;
        private DBObjectCollection _door;
        private DBObjectCollection _window;
        private DBObjectCollection _slab;
        private DBObjectCollection _cornice;
        public Roomdata(Database database, Point3dCollection polygon)
        {
            _wall = new DBObjectCollection();
            _door = new DBObjectCollection();
            _window = new DBObjectCollection();
            _slab = new DBObjectCollection();
            _cornice = new DBObjectCollection();
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
            _wall = _wall.BufferPolygons(-WallBufferDistance).BufferPolygons(WallBufferDistance);
            _slab = _slab.BufferPolygons(-SlabBufferDistance).BufferPolygons(SlabBufferDistance);
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

