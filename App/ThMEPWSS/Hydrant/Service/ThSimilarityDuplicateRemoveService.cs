using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm.Match;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThSimilarityDuplicateRemoveService
    {
        private double Degree { get; set; }
        private List<Entity> Polygons { get; set; }
        public List<Entity> Results { get; private set; }
        public ThSimilarityDuplicateRemoveService(List<Entity> polygons)
        {
            Degree = 0.99;
            Polygons = polygons;
            Results = new List<Entity>();
        }
        public void DuplicateRemove()
        {
            var containers = new List<Entity>(); //收集重复的物体
            var sptialIndex = new ThCADCoreNTSSpatialIndex(Polygons.ToCollection());
            Polygons.ForEach(o =>
            {
                if(!containers.Contains(o))
                {
                    var objs = sptialIndex.SelectCrossingPolygon(o);
                    objs.Remove(o);
                    if (objs.Count == 0)
                    {
                        Results.Add(o);
                    }
                    else
                    {
                        var first = o.ToNTSPolygonalGeometry();
                        var similars = objs
                        .Cast<Entity>()
                        .Where(e => IsSimilar(first, e.ToNTSPolygonalGeometry()))
                        .ToList();
                        Results.Add(o);
                        containers.AddRange(similars);
                    }
                }
                
            });
        }
        public void SetDegree(double degree)
        {
            if(degree> 0 && degree<=1.0)
            {
                this.Degree = degree;
            }
        }
        private bool IsSimilar(Polygon first, Polygon second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first, second) >= Degree;
        }
    }
}
