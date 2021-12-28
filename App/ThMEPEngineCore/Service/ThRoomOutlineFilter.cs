using System;
using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Match;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomOutlineFilter
    {
        private const double BufferDistance = 5.0;
        private const double SimiltaryRatio = 0.95;     
        private DBObjectCollection Gargbage { get; set; }

        private ThCADCoreNTSSpatialIndex BoundarySpatialIndex;

        public DBObjectCollection Results
        {
            get
            {
                return GetResults();
            }
        }
        public ThRoomOutlineFilter(DBObjectCollection boundaries)
        {
            Gargbage = new DBObjectCollection();
            BoundarySpatialIndex = new ThCADCoreNTSSpatialIndex(boundaries)
            {
                AllowDuplicate=true,
            };
        }
        public void Filter(DBObjectCollection components)
        {
            var buffer = new ThNTSBufferService();
            components.OfType<Entity>().ForEach(o=>
            {
               var ent = buffer.Buffer(o, BufferDistance);
               Query(ent)
                .OfType<Entity>()
                .Where(e=> !Gargbage.Contains(e))
                .Where(e=> IsSimilar(o,e)).ForEach(e=>Gargbage.Add(e));
            });
        }

        private DBObjectCollection GetResults()
        {
            return BoundarySpatialIndex
                .SelectAll()
                .OfType<Entity>()
                .Where(e => !Gargbage.Contains(e))
                .ToCollection();
        }

        private bool IsSimilar(Entity first,Entity second)
        {
            var measure = new HausdorffSimilarityMeasure();
            var value = measure.Measure(ToNTSPolygon(first),ToNTSPolygon(second));
            return value <= SimiltaryRatio;
        }

        private Polygon ToNTSPolygon(Entity entity)
        {
            if(entity is Polyline polyline)
            {
                return polyline.ToNTSPolygon();
            }
            else if(entity is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();  
            }
        }

        private DBObjectCollection Query(Entity outline)
        {
            return BoundarySpatialIndex.SelectWindowPolygon(outline);
        }
    }
}
