using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThWallProcessService:IDisposable
    {
        private double _pcWallBufferDistance = 2.0;
        private double _smallAreaTolerance = 1.0;
        private ThCADCoreNTSSpatialIndex _spatialIndex;
        private HashSet<DBObject> _others;
        private HashSet<DBObject> _garbageCollecter;        

        public ThWallProcessService(DBObjectCollection others)
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

        public DBObjectCollection Difference(Entity wall)
        {
            var objs = _spatialIndex.SelectCrossingPolygon(wall);
            var enlargeObjs = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                enlargeObjs = enlargeObjs.Union(Buffer(e, _pcWallBufferDistance, true));
            });
            AppendToGC(enlargeObjs);
            enlargeObjs = FilterSmallArea(enlargeObjs, _smallAreaTolerance);

            var restObjs = wall.Difference(enlargeObjs, true);
            AppendToGC(restObjs);
            restObjs = FilterSmallArea(restObjs, _smallAreaTolerance);

            var results = new DBObjectCollection();
            restObjs.OfType<Entity>().ForEach(e =>
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
