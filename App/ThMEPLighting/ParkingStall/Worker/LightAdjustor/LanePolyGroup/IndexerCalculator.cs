using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Geometry;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class IndexerCalculator
    {
        private Polyline m_extendPolyline;
        private List<LightPlaceInfo> m_lightPlaceInfos;

        private Dictionary<Polyline, LightPlaceInfo> m_lightCacheMap = new Dictionary<Polyline, LightPlaceInfo>();

        private LaneGroup GroupInfo;

        public IndexerCalculator(Polyline polyline, List<LightPlaceInfo> lightPlaceInfos)
        {
            m_extendPolyline = polyline;
            m_lightPlaceInfos = lightPlaceInfos;
        }

        public static LaneGroup MakeLaneGroupInfo(Polyline polyline, List<LightPlaceInfo> lightPlaceInfos)
        {
            var indexerCalculator = new IndexerCalculator(polyline, lightPlaceInfos);
            indexerCalculator.Do();
            return indexerCalculator.GroupInfo;
        }

        public void Do()
        {
            var bigGroupPolys = CalculateCacheMap();

            // one Side
            var oneSideLightPlaceInfos = CalculateIntersectPolys(bigGroupPolys, ParkingStallCommon.LaneOffset);

            // another Side
            var anotherSideLightPlaceInfos = CalculateIntersectPolys(bigGroupPolys, -ParkingStallCommon.LaneOffset);

            GroupInfo = new LaneGroup(m_extendPolyline, oneSideLightPlaceInfos, anotherSideLightPlaceInfos);
        }

        private Polyline BufferLanePoly(Polyline polyline, double bufferWidth)
        {
            var dbCol = new DBObjectCollection();
            dbCol.Add(polyline);

            var polys = new List<Polyline>();
            foreach (Entity entity in dbCol.SingleSidedBuffer(bufferWidth))
            {
                if (entity is Polyline poly && poly.Closed)
                    polys.Add(poly);
            }

            return polys.OrderBy(p => p.Area).ToList().Last();
        }

        private List<LightPlaceInfo> CalculateIntersectPolys(List<Polyline> polylines, double bufferWidth)
        {
            var laneBufferPoly = BufferLanePoly(m_extendPolyline, bufferWidth);

            var polys = new List<Polyline>();
            var objs = new DBObjectCollection();
            polylines.ForEach(p => objs.Add(p));
            var spatialIndexer = new ThCADCoreNTSSpatialIndex(objs);
            foreach (Entity entity in spatialIndexer.SelectCrossingPolygon(laneBufferPoly))
            {
                if (entity is Polyline polyline)
                    polys.Add(polyline);
            }

            return CalculateLightPlaceInfos(polys);
        }

        private List<LightPlaceInfo> CalculateLightPlaceInfos(List<Polyline> intersectPolys)
        {
            var lightPlaceInfos = new List<LightPlaceInfo>();

            foreach (var intersectPoly in intersectPolys)
            {
                foreach (var pairInfo in m_lightCacheMap)
                {
                    if (intersectPoly.GetCentroidPoint().DistanceTo(pairInfo.Key.GetCentroidPoint()) <= 10.0)
                    {
                        lightPlaceInfos.Add(pairInfo.Value);
                        break;
                    }
                }
            }

            return lightPlaceInfos;
        }

        private List<Polyline> CalculateCacheMap()
        {
            var bigGroupPolys = new List<Polyline>();
            foreach (var lightPlaceInfo in m_lightPlaceInfos)
            {
                m_lightCacheMap.Add(lightPlaceInfo.BigGroupInfo.BigGroupPoly, lightPlaceInfo);
                bigGroupPolys.Add(lightPlaceInfo.BigGroupInfo.BigGroupPoly);
            }

            return bigGroupPolys;
        }
    }
}
