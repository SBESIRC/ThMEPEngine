using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThFindDoorstoneNeighborService
    {
        /// <summary>
        /// 门垛与墙、柱存在的空隙
        /// </summary>
        public Polyline Neighbor { get; private set; }
        public NeighborType Kind { get; private set; }
        private double Interval { get; set; }
        public ThFindDoorstoneNeighborService()
        {
            Neighbor = new Polyline();
            Kind = NeighborType.None;
            Interval = ThMEPEngineCoreCommon.DoorStoneInterval;
        }
        public void Find(Polyline doorStone)
        {
            //获取门垛符合条件的邻居
            var nearArchwalls = FindArchitectureWalls(doorStone);
            var nearShearwalls = FindShearWalls(doorStone);
            var nearColumns = FindColumns(doorStone);

            //分析
            if(nearArchwalls.Count>0)
            {
                Neighbor = nearArchwalls[0];
                Kind = NeighborType.ArchitecureWall;
            }
            if (nearShearwalls.Count > 0)
            {
                Neighbor = nearShearwalls[0];
                Kind = NeighborType.ShearWall;
            }
            if (nearColumns.Count > 0)
            {
                Neighbor = nearColumns[0];
                Kind = NeighborType.StructureColumn;
            }
        }

        private List<Polyline> FindColumns(Polyline doorStone)
        {
            var results = ThObstacleSpatialIndexService.Instance.FindColumns(doorStone.Buffer(Interval)[0] as Polyline);
            var polylines = TransPolyines(results);
            Sort(polylines, doorStone);
            polylines = polylines.Where(o => ThDoorUtils.IsQualified(o, doorStone)).ToList();
            return polylines;
        }
        private List<Polyline> FindShearWalls(Polyline doorStone)
        {
            var results = ThObstacleSpatialIndexService.Instance.FindShearWalls(doorStone.Buffer(Interval)[0] as Polyline);
            var polylines = TransPolyines(results);
            Sort(polylines, doorStone);
            polylines = polylines.Where(o => ThDoorUtils.IsQualified(o, doorStone)).ToList();
            return polylines;
        }
        private List<Polyline> FindArchitectureWalls(Polyline doorStone)
        {
            var results = ThObstacleSpatialIndexService.Instance.FindArchitectureWalls(doorStone.Buffer(Interval)[0] as Polyline);
            var polylines=TransPolyines(results);
            Sort(polylines, doorStone);
            polylines = polylines.Where(o => ThDoorUtils.IsQualified(o, doorStone)).ToList();
            return polylines;
        }
        private void Sort(List<Polyline> nears,Polyline doorStone)
        {
            nears=nears.OrderBy(o=>o.Distance(doorStone)).ToList();
        }
        private List<Polyline> TransPolyines(List<Entity> entities)
        {
            var results = new List<Polyline>();
            entities.ForEach(o =>
            {
                if (o is Polyline poly)
                {
                    results.Add(poly);
                }
                else if (o is MPolygon mPolygon)
                {
                    results.AddRange(mPolygon.Loops());
                }
            });
            return results;
        }
    }
    public enum NeighborType
    {
        None,
        StructureColumn,
        ArchitecureWall,
        ShearWall,
    }
}
