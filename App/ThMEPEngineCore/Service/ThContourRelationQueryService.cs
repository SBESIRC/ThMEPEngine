using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThContourRelationQueryService
    {
        public List<Polyline> Boundaries { get; set; }
        public ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }

        private double SimilarityTolerance = 0.98;
        public ThContourRelationQueryService(List<Polyline> boundaries)
        {
            Boundaries = boundaries;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(Boundaries.ToCollection());
        }

        /// <summary>
        /// 包含关系
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> Contains(Polyline outline, bool isFindHole = true)
        {
            var dbObjs = SpatialIndex.SelectCrossingPolygon(outline);
            dbObjs.Remove(outline);
            var filterObjs = dbObjs.Cast<Polyline>().Where(o => IsContains(outline, o)).ToList();
            if(isFindHole)
            {
                // 尝试创建洞
                var objs = filterObjs.ToCollection();
                objs.Add(outline);
                var areas = objs.BuildArea();
                var mPolygons = areas.Cast<Entity>().Where(o => o is MPolygon).ToList();

                //收集以outline为外框的内部Hole 
                var holes = new List<Polyline>();
                mPolygons.ForEach(o=>
                {
                    var mPolygon = o as MPolygon;
                    var loops = mPolygon.Loops();
                    if(IsSimilarity(outline,loops[0], SimilarityTolerance))
                    {
                        for(int i=1;i<loops.Count;i++)
                        {
                            holes.Add(loops[i]);
                        }
                    }
                });

                // 寻找匹配的Polyline
                return filterObjs.Where(o => IsMatch(holes, o, SimilarityTolerance)).ToList();
            }
            else
            {
                return filterObjs.Cast<Polyline>().ToList();
            }
        }

        public List<Polyline> LoosenContains(Polyline outline, double tolerance = -5.0)
        {
            var dbObjs = SpatialIndex.SelectCrossingPolygon(outline);
            dbObjs.Remove(outline);
            return dbObjs.Cast<Polyline>().ToList().Where(o =>
            {
                return IsContains(outline, o.Buffer(tolerance)[0] as Polyline);
            }).ToList();
        }

        private bool IsMatch(List<Polyline> holes, Polyline origin, double tolerance)
        {
            foreach (var hole in holes)
            {
                if (IsSimilarity(origin, hole, tolerance))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSimilarity(Polyline first, Polyline second,double tolerance)
        {
            var measure = first.SimilarityMeasure(second);
            return measure>= tolerance;
        }

        /// <summary>
        /// 附近
        /// </summary>
        /// <param name="room"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public List<Polyline> Nears(Polyline outline, double distance)
        {
            var results = new List<Polyline>();
            var enlarge = outline.Buffer(distance)[0] as Polyline;
            var dbObjs = SpatialIndex.SelectCrossingPolygon(enlarge);
            dbObjs.Remove(outline);
            return dbObjs
                .Cast<Polyline>()
                .Where(o => IsNeighbor(enlarge, o, distance))
                .ToList();
        }

        private bool IsNeighbor(Polyline first, Polyline second,double distance)
        {
            if(IsContains(first, second) || IsContains(second,first))
            {
                return false;
            }
            var relate = new ThCADCoreNTSRelate(first, second);
            if(relate.IsIntersects==false)
            {
                var dis = first.Distance(second);
                return dis >= 0 && dis <= distance;
            }
            else
            {
                if (relate.IsOverlaps == false)
                {
                    return true;
                }
                else if(distance!=0.0)
                {
                    var narrow = first.Buffer(-distance)[0] as Polyline;
                    return IsNeighbor(narrow, second, 0.0);
                }
            }
            return false;
        }

        private bool IsContains(Polyline first, Polyline second)
        {
            var relate = new ThCADCoreNTSRelate(first, second);
            return relate.IsContains || relate.IsCovers;
        }
    }
}
