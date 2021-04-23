using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.BuildRoom.Interface;

namespace ThMEPEngineCore.BuildRoom.Service
{
    public class ThFilterService : IRoomFilter
    {
        public List<Entity> Results { get; private set; }

        public double InnerBufferLength { get; set; }

        public ThFilterService()
        {
            Results = new List<Entity>();
        }

        public void Filter(List<Entity> boundaries, List<Entity> generatedPolygons)
        {
            var iSpatialIndex = new ThNTSSpatialIndexService(boundaries.ToCollection());
            var iBuffer = new ThNTSBufferService();
            foreach (Entity ent in generatedPolygons)
            {
                if (ent is Polyline polyline)
                {
                    var objs = iSpatialIndex.SelectCrossingPolygon(polyline);
                    if(objs.Count>0)
                    {
                        //表示框内有东西
                        continue;
                    }
                    var enlargeBoundary = iBuffer.Buffer(ent, InnerBufferLength + 2.0);
                    if(enlargeBoundary == null)
                    {
                        continue;
                    }
                    objs = iSpatialIndex.SelectCrossingPolygon(enlargeBoundary as Polyline);
                    if (!HasContainer(objs, polyline))
                    {
                        Results.Add(ent);
                    }
                }
                else if (ent is MPolygon mPolygon)
                {
                    var enlargeBoundary = iBuffer.Buffer(ent, InnerBufferLength + 2.0);
                    if (enlargeBoundary == null)
                    {
                        continue;
                    }
                    var objs = iSpatialIndex.SelectCrossingPolygon(enlargeBoundary as MPolygon);
                    if (!HasContainer(objs, mPolygon))
                    {
                        Results.Add(ent);
                    }
                }
                else
                {
                    continue;
                }
            }
        }
        private bool HasContainer(DBObjectCollection objs,Entity entity)
        {
            return objs.Cast<Entity>().Where(o => o.IsContains(entity)).Count()>0;
        }
    }
}
