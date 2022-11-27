using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using System;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThPCWallProcessService:IDisposable
    {
        private double _pcWallBufferDistance = 1.0;
        private double _smallAreaTolerance = 1.0;
        private ThCADCoreNTSSpatialIndex _spatialIndex;
        private HashSet<DBObject> _others;
        private HashSet<DBObject> _garbageCollecter;        
        //private DBObjectCollection _others;
        public ThPCWallProcessService(DBObjectCollection others)
        {
            _garbageCollecter = new HashSet<DBObject>();
            _others= others.OfType<DBObject>().ToHashSet();
            _spatialIndex = new ThCADCoreNTSSpatialIndex(others);
        }
        public void Dispose()
        {
            var objs = _garbageCollecter.Except(_others).ToCollection();
            objs.MDispose();
        }
        public DBObjectCollection Difference(Entity pcWallPolygon)
        {
            
            // 用PcWall墙裁剪标准墙            
            var enlargeObjs = Buffer(pcWallPolygon, _pcWallBufferDistance, true);
            AppendToGC(enlargeObjs);
            enlargeObjs = FilterSmallArea(enlargeObjs, _smallAreaTolerance);

            var differenceObjs = new DBObjectCollection();
            enlargeObjs.OfType<Entity>()
                .ForEach(e =>
                {
                    var objs = _spatialIndex.SelectCrossingPolygon(e);
                    var restObjs = e.Difference(objs, true);
                    differenceObjs = differenceObjs.Union(restObjs);
                });
            AppendToGC(differenceObjs);

            // 缩小
            var innerBufferObjs = new DBObjectCollection();
            differenceObjs.OfType<Entity>()
                .ForEach(e =>
                {
                    innerBufferObjs = innerBufferObjs.Union(Buffer(e, -1.0 * _pcWallBufferDistance, true));
                });
            AppendToGC(innerBufferObjs);

            //
            var results = new DBObjectCollection();
            innerBufferObjs = FilterSmallArea(innerBufferObjs, _smallAreaTolerance);
            innerBufferObjs.OfType<Entity>().ForEach(e =>
            {
                if (e is Polyline polyline)
                {
                    results.Add(polyline);
                }
                else if (e is MPolygon mPolygon)
                {
                    var poly = Convert(mPolygon);
                    if (poly.Area > 0.0)
                    {
                        results.Add(poly);
                    }
                    else
                    {
                        results.Add(mPolygon);
                    }
                }
            });

            RemoveFromGC(results);
            return results;
        }

        private DBObjectCollection FilterSmallArea(DBObjectCollection polygons,double area)
        {
            return polygons.FilterSmallArea(area);
        }

        private Polyline Convert(MPolygon polygon)
        {
            // 把不包含Hole的MPolygon转成Polyline
            var holes = polygon.Holes();
            if(holes.Count==0)
            {
                return polygon.Shell();
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }

        private DBObjectCollection Buffer(Entity entity,double distance,bool keepHole)
        {
           if(entity is Polyline polyline)
            {
                return polyline.Buffer(distance, keepHole);
            }
           else if(entity is MPolygon polygon)
            {
                return polygon.Buffer(distance, keepHole);
            }
           else
            {
                return new DBObjectCollection();
            }
        }

        private void AppendToGC(DBObjectCollection objs)
        {
            objs.OfType<DBObject>().ForEach(o => AppendToGC(o));
        }
        private void AppendToGC(DBObject obj)
        {
            _garbageCollecter.Add(obj);
        }
        private void RemoveFromGC(DBObjectCollection objs)
        {
            _garbageCollecter = _garbageCollecter.Except(objs.OfType<Entity>()).ToHashSet();
        }
    }
}
