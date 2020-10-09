﻿using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementRecognitionEngine
    {
        /// <summary>
        /// 从图纸中提取出来的对象的集合
        /// </summary>
        public List<ThIfcBuildingElement> Elements { get; set; }
        /// <summary>
        /// 几何图元集合
        /// </summary>
        public DBObjectCollection Geometries
        {
            get
            {
                return Elements.Select(e => e.Outline).ToCollection();
            }
        }
        protected ThBuildingElementRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }
        public abstract void Recognize(Database database, Point3dCollection polygon);
        public IEnumerable<ThIfcBuildingElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }
        public ThIfcBuildingElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }
        public void UpdateSpatialIndex(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var objs = Geometries.Cast<DBObject>();
            var siObjs = spatialIndex.Geometries.Values;
            var adds = objs.Where(o => !siObjs.Contains(o)).ToCollection();
            var removals = siObjs.Where(o => !objs.Contains(o)).ToCollection();
            spatialIndex.Update(adds, removals);
        }
        public void UpdateWithSpatialIndex(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var objs = Geometries.Cast<DBObject>();
            var siObjs = spatialIndex.Geometries.Values;
            Elements = Elements.Where(o => siObjs.Contains(o.Outline)).ToList();
        }
    }
}
